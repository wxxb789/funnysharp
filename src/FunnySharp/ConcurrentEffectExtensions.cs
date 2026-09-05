using System.Runtime.ExceptionServices;

namespace FunnySharp;

/// <summary>
/// Provides concurrent coordination operations for deferred effects.
/// </summary>
public static class ConcurrentEffectExtensions
{
    /// <summary>
    /// Starts every source effect and returns the first observed successful result.
    /// </summary>
    /// <typeparam name="TValue">The successful result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="effects">The effects to start concurrently.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>
    /// An asynchronous operation that returns the first observed successful value as a valid validation. When every
    /// effect returns a typed failure, it returns an invalid validation containing those failures in input order.
    /// Started effects receive an internal operation token, and remaining work is canceled and drained after a success.
    /// Without a success, ordinary faults and source cancellation propagate instead of becoming typed errors.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="effects"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="effects"/> is empty.</exception>
    /// <exception cref="OperationCanceledException">The caller cancellation token is canceled.</exception>
    public static ValueTask<Validation<TValue, TError>> FirstSuccessAsync<TValue, TError>(
        this IEnumerable<Effect<Result<TValue, TError>>> effects,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effects);
        return FirstSuccessAsyncCore(Snapshot(effects), cancellationToken, null);
    }

    /// <summary>
    /// Starts every source effect and returns the first observed successful result before the timeout expires.
    /// </summary>
    /// <typeparam name="TValue">The successful result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="effects">The effects to start concurrently.</param>
    /// <param name="timeout">The maximum duration to wait for a successful result.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>
    /// An asynchronous operation that returns the first observed successful value as a valid validation. When every
    /// effect returns a typed failure, it returns an invalid validation containing those failures in input order.
    /// Started effects receive an internal operation token, and remaining work is canceled and drained after a success.
    /// Without a success, ordinary faults and source cancellation propagate instead of becoming typed errors.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="effects"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="effects"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is invalid.</exception>
    /// <exception cref="TimeoutException">The timeout expires before a successful result is observed.</exception>
    /// <exception cref="OperationCanceledException">The caller cancellation token is canceled.</exception>
    public static ValueTask<Validation<TValue, TError>> FirstSuccessAsync<TValue, TError>(
        this IEnumerable<Effect<Result<TValue, TError>>> effects,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) =>
        FirstSuccessAsync(effects, timeout, TimeProvider.System, cancellationToken);

    /// <summary>
    /// Starts every source effect and returns the first observed successful result before the timeout expires.
    /// </summary>
    /// <typeparam name="TValue">The successful result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="effects">The effects to start concurrently.</param>
    /// <param name="timeout">The maximum duration to wait for a successful result.</param>
    /// <param name="timeProvider">The time provider used to measure <paramref name="timeout"/>.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>
    /// An asynchronous operation that returns the first observed successful value as a valid validation. When every
    /// effect returns a typed failure, it returns an invalid validation containing those failures in input order.
    /// Started effects receive an internal operation token, and remaining work is canceled and drained after a success.
    /// Without a success, ordinary faults and source cancellation propagate instead of becoming typed errors.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="effects"/> or <paramref name="timeProvider"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="effects"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is invalid.</exception>
    /// <exception cref="TimeoutException">The timeout expires before a successful result is observed.</exception>
    /// <exception cref="OperationCanceledException">The caller cancellation token is canceled.</exception>
    public static ValueTask<Validation<TValue, TError>> FirstSuccessAsync<TValue, TError>(
        this IEnumerable<Effect<Result<TValue, TError>>> effects,
        TimeSpan timeout,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var timeoutCancellationSource = new CancellationTokenSource(timeout, timeProvider);
        try
        {
            return FirstSuccessAsyncCore(Snapshot(effects), cancellationToken, timeoutCancellationSource);
        }
        catch
        {
            timeoutCancellationSource.Dispose();
            throw;
        }
    }

    private static Effect<Result<TValue, TError>>[] Snapshot<TValue, TError>(
        IEnumerable<Effect<Result<TValue, TError>>> effects)
    {
        var snapshot = effects.ToArray();

        if (snapshot.Length == 0)
        {
            throw new ArgumentException("At least one effect is required.", nameof(effects));
        }

        return snapshot;
    }

    private static async ValueTask<Validation<TValue, TError>> FirstSuccessAsyncCore<TValue, TError>(
        Effect<Result<TValue, TError>>[] effects,
        CancellationToken cancellationToken,
        CancellationTokenSource? timeoutCancellationSource)
    {
        try
        {
            using var operationCancellationSource = new CancellationTokenSource();
            using var signalCancellationSource = timeoutCancellationSource is null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutCancellationSource.Token);
            Exception? signalCancellationFailure = null;
            var cancellationSignal = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellationRegistration = signalCancellationSource.Token.Register(() =>
            {
                try
                {
                    operationCancellationSource.Cancel();
                }
                catch (Exception exception)
                {
                    Interlocked.CompareExchange(ref signalCancellationFailure, exception, null);
                }
                finally
                {
                    cancellationSignal.TrySetResult();
                }
            });

            var operationToken = operationCancellationSource.Token;
            var tasks = new Task<Result<TValue, TError>>[effects.Length];
            for (var index = 0; index < effects.Length; index++)
            {
                tasks[index] = effects[index].RunAsync(operationToken).AsTask();
            }

            var pending = new List<Task<Result<TValue, TError>>>(tasks);
            var typedFailures = new TError[effects.Length];
            var hasTypedFailure = new bool[effects.Length];
            var faults = new Exception?[effects.Length];
            var cancellations = new OperationCanceledException?[effects.Length];

            while (pending.Count > 0)
            {
                var observedCompletion = false;
                var hasWinner = false;
                TValue? winner = default;

                for (var index = 0; index < tasks.Length; index++)
                {
                    var task = tasks[index];
                    if (!task.IsCompleted || !pending.Remove(task))
                    {
                        continue;
                    }

                    observedCompletion = true;
                    try
                    {
                        var result = await task.ConfigureAwait(false);
                        if (result.TryGetValue(out var value))
                        {
                            if (!hasWinner)
                            {
                                hasWinner = true;
                                winner = value;
                            }

                            continue;
                        }

                        result.TryGetError(out var error);
                        typedFailures[index] = error!;
                        hasTypedFailure[index] = true;
                    }
                    catch (OperationCanceledException exception) when (task.IsCanceled)
                    {
                        cancellations[index] = exception;
                    }
                    catch (Exception exception)
                    {
                        faults[index] = exception;
                    }
                }

                if (hasWinner)
                {
                    Exception? primaryFailure = null;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        primaryFailure = new OperationCanceledException(cancellationToken);
                    }
                    else if (timeoutCancellationSource is { IsCancellationRequested: true })
                    {
                        primaryFailure = new TimeoutException();
                    }
                    else
                    {
                        StopTimeout(timeoutCancellationSource);
                    }

                    var winnerCleanupFailure = await CancelAndDrainAsync(pending, operationCancellationSource).ConfigureAwait(false);
                    winnerCleanupFailure ??= Volatile.Read(ref signalCancellationFailure);
                    ThrowPostDrainFailure(cancellationToken, primaryFailure, winnerCleanupFailure);

                    return Validation<TValue, TError>.Valid(winner!);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    var cleanupFailure = await CancelAndDrainAsync(pending, operationCancellationSource).ConfigureAwait(false);
                    cleanupFailure ??= Volatile.Read(ref signalCancellationFailure);
                    ThrowPostDrainFailure(
                        cancellationToken,
                        new OperationCanceledException(cancellationToken),
                        cleanupFailure);
                    continue;
                }

                if (timeoutCancellationSource is { IsCancellationRequested: true })
                {
                    var cleanupFailure = await CancelAndDrainAsync(pending, operationCancellationSource).ConfigureAwait(false);
                    cleanupFailure ??= Volatile.Read(ref signalCancellationFailure);
                    ThrowPostDrainFailure(cancellationToken, new TimeoutException(), cleanupFailure);
                    continue;
                }

                if (observedCompletion)
                {
                    continue;
                }

                var taskCompletion = Task.WhenAny(pending);
                var signal = await Task.WhenAny(taskCompletion, cancellationSignal.Task).ConfigureAwait(false);
                if (signal == cancellationSignal.Task)
                {
                    Exception? primaryFailure = cancellationToken.IsCancellationRequested
                        ? new OperationCanceledException(cancellationToken)
                        : timeoutCancellationSource is { IsCancellationRequested: true }
                            ? new TimeoutException()
                            : null;
                    var cleanupFailure = await CancelAndDrainAsync(pending, operationCancellationSource).ConfigureAwait(false);
                    cleanupFailure ??= Volatile.Read(ref signalCancellationFailure);
                    ThrowPostDrainFailure(cancellationToken, primaryFailure, cleanupFailure);
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (timeoutCancellationSource is { IsCancellationRequested: true })
            {
                throw new TimeoutException();
            }

            ThrowFailures(faults);
            ThrowCancellations(cancellations);

            var errors = new List<TError>(effects.Length);
            for (var index = 0; index < typedFailures.Length; index++)
            {
                if (hasTypedFailure[index])
                {
                    errors.Add(typedFailures[index]);
                }
            }

            return Validation<TValue, TError>.InvalidFromOwnedErrors(errors);
        }
        finally
        {
            timeoutCancellationSource?.Dispose();
        }
    }

    private static async Task<Exception?> CancelAndDrainAsync<TValue, TError>(
        IReadOnlyCollection<Task<Result<TValue, TError>>> tasks,
        CancellationTokenSource operationCancellationSource)
    {
        Exception? cancellationFailure = null;
        if (tasks.Count == 0)
        {
            return cancellationFailure;
        }

        try
        {
            operationCancellationSource.Cancel();
        }
        catch (Exception exception)
        {
            cancellationFailure = exception;
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception)
        {
        }

        return cancellationFailure;
    }

    private static Exception CreatePrimaryFailure(Exception primaryFailure, Exception? cleanupFailure) =>
        cleanupFailure is null
            ? primaryFailure
            : new AggregateException(primaryFailure, cleanupFailure);

    private static void ThrowPostDrainFailure(
        CancellationToken cancellationToken,
        Exception? primaryFailure,
        Exception? cleanupFailure)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            primaryFailure = new OperationCanceledException(cancellationToken);
        }

        if (primaryFailure is null)
        {
            ThrowCleanupFailure(cleanupFailure);
            return;
        }

        ExceptionDispatchInfo.Capture(CreatePrimaryFailure(primaryFailure, cleanupFailure)).Throw();
    }

    private static void ThrowCleanupFailure(Exception? cleanupFailure)
    {
        if (cleanupFailure is null)
        {
            return;
        }

        ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
    }

    private static void StopTimeout(CancellationTokenSource? timeoutCancellationSource)
    {
        if (timeoutCancellationSource is { IsCancellationRequested: false })
        {
            timeoutCancellationSource.CancelAfter(Timeout.InfiniteTimeSpan);
        }
    }

    private static void ThrowFailures(IReadOnlyList<Exception?> faults)
    {
        List<Exception>? failures = null;
        for (var index = 0; index < faults.Count; index++)
        {
            if (faults[index] is { } failure)
            {
                (failures ??= []).Add(failure);
            }
        }

        if (failures is null)
        {
            return;
        }

        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
            return;
        }

        throw new AggregateException(failures);
    }

    private static void ThrowCancellations(IReadOnlyList<OperationCanceledException?> cancellations)
    {
        for (var index = 0; index < cancellations.Count; index++)
        {
            if (cancellations[index] is { } cancellation)
            {
                ExceptionDispatchInfo.Capture(cancellation).Throw();
            }
        }
    }
}
