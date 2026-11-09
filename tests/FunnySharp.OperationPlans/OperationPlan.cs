using System.Collections.ObjectModel;

using FunnySharp;

namespace FunnySharp.OperationPlans;

/// <summary>
/// An immutable sequence of named, typed operations over a caller-owned state and transaction.
/// </summary>
/// <typeparam name="TEnvironment">The caller-owned execution context.</typeparam>
/// <typeparam name="TState">The caller-owned typed state carried between operations.</typeparam>
/// <typeparam name="TError">The caller's domain failure type.</typeparam>
public sealed class OperationPlan<TEnvironment, TState, TError>
{
    private readonly OperationStep<TEnvironment, TState, TError>[] _steps;
    private readonly ReadOnlyCollection<OperationInfo> _description;

    private OperationPlan(OperationStep<TEnvironment, TState, TError>[] steps)
    {
        _steps = steps;
        _description = Array.AsReadOnly(Array.ConvertAll(steps, static step => step.Info));
    }

    /// <summary>The immutable operation descriptions in execution order.</summary>
    public IReadOnlyList<OperationInfo> Describe => _description;

    /// <summary>
    /// Validates and copies an operation sequence.
    /// </summary>
    /// <param name="steps">The operations in execution order.</param>
    /// <returns>An immutable plan.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="steps"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// The plan is empty; a step is null; a name is duplicated; a dependency is not an earlier
    /// operation; or the Conditional flag disagrees with the presence of a condition.
    /// </exception>
    public static OperationPlan<TEnvironment, TState, TError> Create(
        IEnumerable<OperationStep<TEnvironment, TState, TError>> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var array = steps.ToArray();

        if (array.Length == 0)
        {
            throw new ArgumentException("A plan must contain at least one operation.", nameof(steps));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var step in array)
        {
            if (step is null)
            {
                throw new ArgumentException("The plan contains a null operation.", nameof(steps));
            }

            var info = step.Info;

            foreach (var dependency in info.DependsOn)
            {
                if (!seen.Contains(dependency))
                {
                    throw new ArgumentException(
                        $"Operation '{info.Name}' depends on '{dependency}', which is not an earlier operation.",
                        nameof(steps));
                }
            }

            if (!seen.Add(info.Name))
            {
                throw new ArgumentException(
                    $"Operation name '{info.Name}' is duplicated.",
                    nameof(steps));
            }

            if (info.Conditional != (step.Condition is not null))
            {
                throw new ArgumentException(
                    $"Operation '{info.Name}' marks Conditional={info.Conditional} but "
                    + (step.Condition is null ? "has no condition." : "has a condition."),
                    nameof(steps));
            }
        }

        return new OperationPlan<TEnvironment, TState, TError>(array);
    }

    /// <summary>
    /// Runs the operations in declared order against a caller-owned transaction.
    /// </summary>
    /// <param name="environment">The caller-owned execution context.</param>
    /// <param name="initialState">The initial typed state.</param>
    /// <param name="transaction">The caller-owned transaction the plan commits or rolls back.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>
    /// A successful run with the final state, executed names, and skipped names, or a failure that
    /// identifies the responsible operation and preserves the last good state.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="transaction"/> is null.</exception>
    public async Task<Result<OperationSuccess<TState>, RunFailure<TState, TError>>> RunAsync(
        TEnvironment environment,
        TState initialState,
        IPlanTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var state = initialState;
        var executed = new List<string>(_steps.Length);
        var skipped = new List<string>(_steps.Length);

        foreach (var step in _steps)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return await FailAsync(
                    RunFailureKind.Canceled,
                    step.Info.Name,
                    default,
                    null,
                    executed,
                    state,
                    transaction).ConfigureAwait(false);
            }

            if (step.Condition is not null)
            {
                bool shouldRun;

                try
                {
                    shouldRun = step.Condition(state);
                }
                catch (OperationCanceledException exception)
                {
                    return await FailAsync(
                        RunFailureKind.Canceled,
                        step.Info.Name,
                        default,
                        exception,
                        executed,
                        state,
                        transaction).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    return await FailAsync(
                        RunFailureKind.OperationFaulted,
                        step.Info.Name,
                        default,
                        exception,
                        executed,
                        state,
                        transaction).ConfigureAwait(false);
                }

                if (!shouldRun)
                {
                    skipped.Add(step.Info.Name);
                    continue;
                }
            }

            Result<TState, TError> stepResult;

            try
            {
                var task = step.Run(environment, state, cancellationToken);

                if (task is null)
                {
                    throw new InvalidOperationException(
                        $"Operation '{step.Info.Name}' returned a null task.");
                }

                stepResult = await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                return await FailAsync(
                    RunFailureKind.Canceled,
                    step.Info.Name,
                    default,
                    exception,
                    executed,
                    state,
                    transaction).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                return await FailAsync(
                    RunFailureKind.OperationFaulted,
                    step.Info.Name,
                    default,
                    exception,
                    executed,
                    state,
                    transaction).ConfigureAwait(false);
            }

            if (!stepResult.TryGetValue(out var next))
            {
                stepResult.TryGetError(out var error);

                return await FailAsync(
                    RunFailureKind.OperationFailed,
                    step.Info.Name,
                    error,
                    null,
                    executed,
                    state,
                    transaction).ConfigureAwait(false);
            }

            state = next;
            executed.Add(step.Info.Name);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return await FailAsync(
                RunFailureKind.Canceled,
                null,
                default,
                null,
                executed,
                state,
                transaction).ConfigureAwait(false);
        }

        try
        {
            await transaction.CommitAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            return await FailAsync(
                RunFailureKind.Canceled,
                null,
                default,
                exception,
                executed,
                state,
                transaction).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return await FailAsync(
                RunFailureKind.CommitFailed,
                null,
                default,
                exception,
                executed,
                state,
                transaction).ConfigureAwait(false);
        }

        return Result<OperationSuccess<TState>, RunFailure<TState, TError>>.Success(
            new OperationSuccess<TState>(state, executed, skipped));
    }

    private static async Task<Result<OperationSuccess<TState>, RunFailure<TState, TError>>> FailAsync(
        RunFailureKind kind,
        string? operation,
        TError? error,
        Exception? exception,
        List<string> executed,
        TState state,
        IPlanTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
        }
        catch (Exception rollbackException)
        {
            return Result<OperationSuccess<TState>, RunFailure<TState, TError>>.Failure(
                new RunFailure<TState, TError>(
                    RunFailureKind.RollbackFailed,
                    kind,
                    operation,
                    error,
                    exception,
                    rollbackException,
                    executed,
                    state));
        }

        return Result<OperationSuccess<TState>, RunFailure<TState, TError>>.Failure(
            new RunFailure<TState, TError>(
                kind,
                null,
                operation,
                error,
                exception,
                null,
                executed,
                state));
    }
}
