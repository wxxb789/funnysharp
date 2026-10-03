using System.Runtime.ExceptionServices;
using System.Threading.Channels;

namespace FunnySharp;

/// <summary>
/// Provides concurrent coordination operations for deferred effects.
/// </summary>
/// <remarks>
/// Inputs must be finite and are eagerly snapshotted once, requiring O(N) storage. Existing overloads admit
/// at most 32 candidates at a time. A candidate occupies its slot until its completion has been accounted for.
/// Each ready batch selects its lowest-input-index success. Selection stops admission, cancels remaining work,
/// and awaits every admitted candidate, completion observer, and cancellation callback before publication.
/// Independent candidate faults propagate in input order, followed by cancellation-callback faults; they are
/// not hidden by a successful alternative. Caller cancellation remains primary, otherwise a selected timeout
/// remains primary. When there is no caller cancellation or timeout, the first independent source cancellation
/// is retained. Only canceled-task artifacts carrying the canceled operation token are suppressed.
/// </remarks>
public static class ConcurrentEffectExtensions
{
    /// <summary>
    /// Runs up to 32 source effects concurrently and returns the first observed successful result.
    /// </summary>
    /// <typeparam name="TValue">The successful result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="effects">The effects to start concurrently.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>
    /// An asynchronous operation that returns the first observed successful value as a valid validation. When every
    /// effect returns a typed failure, it returns an invalid validation containing those failures in input order.
    /// Started effects receive an internal operation token, and remaining work is canceled and drained after a success.
    /// Independent faults and source cancellation propagate even after a success instead of becoming typed errors.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="effects"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="effects"/> is empty.</exception>
    /// <exception cref="OperationCanceledException">Caller or independent source cancellation propagates when it is the sole failure.</exception>
    /// <exception cref="AggregateException">Multiple independent candidate, source-cancellation, or cancellation-callback failures are retained; caller cancellation is first when present.</exception>
    public static ValueTask<Validation<TValue, TError>> FirstSuccessAsync<TValue, TError>(
        this IEnumerable<Effect<Result<TValue, TError>>> effects,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effects);
        return FirstSuccessAsyncCore(Snapshot(effects), 32, cancellationToken, null);
    }

    /// <summary>
    /// Runs up to 32 source effects concurrently and returns the first observed success before the timeout expires.
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
    /// Independent faults and source cancellation propagate even after a success instead of becoming typed errors.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="effects"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="effects"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is invalid.</exception>
    /// <exception cref="TimeoutException">The timeout expires before a successful result is observed.</exception>
    /// <exception cref="OperationCanceledException">Caller or independent source cancellation propagates when it is the sole failure.</exception>
    /// <exception cref="AggregateException">Multiple independent failures are retained, with caller cancellation or the selected timeout first when present.</exception>
    public static ValueTask<Validation<TValue, TError>> FirstSuccessAsync<TValue, TError>(
        this IEnumerable<Effect<Result<TValue, TError>>> effects,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) =>
        FirstSuccessAsync(effects, timeout, TimeProvider.System, cancellationToken);

    /// <summary>
    /// Runs up to 32 source effects concurrently and returns the first observed success before the timeout expires.
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
    /// Independent faults and source cancellation propagate even after a success instead of becoming typed errors.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="effects"/> or <paramref name="timeProvider"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="effects"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is invalid.</exception>
    /// <exception cref="TimeoutException">The timeout expires before a successful result is observed.</exception>
    /// <exception cref="OperationCanceledException">Caller or independent source cancellation propagates when it is the sole failure.</exception>
    /// <exception cref="AggregateException">Multiple independent failures are retained, with caller cancellation or the selected timeout first when present.</exception>
    public static ValueTask<Validation<TValue, TError>> FirstSuccessAsync<TValue, TError>(
        this IEnumerable<Effect<Result<TValue, TError>>> effects,
        TimeSpan timeout,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default) =>
        FirstSuccessAsync(effects, timeout, timeProvider, cancellationToken, 32);

    /// <summary>
    /// Runs source effects within the supplied concurrency bound and returns the first observed success.
    /// </summary>
    /// <typeparam name="TValue">The successful result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="effects">The finite effects to snapshot and admit in input order.</param>
    /// <param name="timeout">
    /// The maximum duration to wait for a success, or <see cref="Timeout.InfiniteTimeSpan"/> for no timeout.
    /// </param>
    /// <param name="timeProvider">The time provider used to measure <paramref name="timeout"/>.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <param name="maxConcurrency">The positive maximum number of admitted, unaccounted-for candidates.</param>
    /// <returns>
    /// The lowest-input-index success in the selected ready batch as a valid validation, or all typed failures
    /// in input order as an invalid validation. Independent exceptions and source cancellation propagate rather
    /// than becoming typed errors. Publication waits for cancellation callbacks and all admitted work to drain.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="effects"/> or <paramref name="timeProvider"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="effects"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="timeout"/> is invalid or <paramref name="maxConcurrency"/> is not positive.
    /// </exception>
    /// <exception cref="TimeoutException">The timeout is selected before a successful result.</exception>
    /// <exception cref="OperationCanceledException">Caller or independent source cancellation propagates.</exception>
    /// <exception cref="AggregateException">Multiple independent failures are retained, with caller cancellation or the selected timeout first when present.</exception>
    public static ValueTask<Validation<TValue, TError>> FirstSuccessAsync<TValue, TError>(
        this IEnumerable<Effect<Result<TValue, TError>>> effects,
        TimeSpan timeout,
        TimeProvider timeProvider,
        CancellationToken cancellationToken,
        int maxConcurrency)
    {
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);

        var timeoutCancellationSource = new CancellationTokenSource(timeout, timeProvider);
        try
        {
            return FirstSuccessAsyncCore(
                Snapshot(effects), maxConcurrency, cancellationToken, timeoutCancellationSource);
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
        int maxConcurrency,
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
            var completions = Channel.CreateUnbounded<FirstSuccessCandidate<Result<TValue, TError>>?>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    AllowSynchronousContinuations = false,
                });
            using var cancellationRegistration = signalCancellationSource.Token.Register(
                () => completions.Writer.TryWrite(null));
            var operationToken = operationCancellationSource.Token;
            var typedFailures = new TError[effects.Length];
            var faults = new IReadOnlyList<Exception>?[effects.Length];
            var cancellations = new OperationCanceledException?[effects.Length];
            var nextIndex = 0;
            var pending = 0;
            var winnerIndex = int.MaxValue;
            TValue? winner = default;

            while (true)
            {
                // Fill the initial window before selecting any synchronous success. Subsequent
                // windows only refill slots whose candidate and observer have been accounted for.
                while (nextIndex < effects.Length &&
                    pending < maxConcurrency &&
                    !signalCancellationSource.IsCancellationRequested)
                {
                    var index = nextIndex++;
                    pending++;
                    _ = new FirstSuccessCandidate<Result<TValue, TError>>(
                        index, effects[index].RunAsync(operationToken).AsTask(), completions.Writer);
                }

                if (signalCancellationSource.IsCancellationRequested || pending == 0)
                {
                    break;
                }

                var completed = await completions.Reader.ReadAsync().ConfigureAwait(false);
                do
                {
                    if (completed is not null)
                    {
                        await ObserveAsync(completed, selectWinner: true).ConfigureAwait(false);
                    }
                }
                while (completions.Reader.TryRead(out completed));

                // No new admissions occur while observing a batch, so it contains at most K
                // candidate notifications, plus the single cancellation wake-up.
                if (winnerIndex != int.MaxValue || signalCancellationSource.IsCancellationRequested)
                {
                    break;
                }
            }

            var timedOut = timeoutCancellationSource is { IsCancellationRequested: true };
            StopTimeout(timeoutCancellationSource);
            Task? cancellation = null;
            if (winnerIndex != int.MaxValue || cancellationToken.IsCancellationRequested || timedOut)
            {
                // One cancellation owner means callback faults cannot be lost between signal
                // handling and winner cleanup. CancelAsync also gives cleanup an awaitable lifetime.
                cancellation = operationCancellationSource.CancelAsync();
            }

            while (pending > 0)
            {
                var completed = await completions.Reader.ReadAsync().ConfigureAwait(false);
                if (completed is not null)
                {
                    await ObserveAsync(completed, selectWinner: false).ConfigureAwait(false);
                }
            }

            if (cancellation is not null)
            {
                // Read the task's complete exception collection below, not just await's first exception.
                await cancellation.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }

            List<Exception>? failures = null;
            var errors = winnerIndex == int.MaxValue ? new List<TError>(nextIndex) : null;
            var hasSourceCancellation = false;
            for (var index = 0; index < nextIndex; index++)
            {
                if (faults[index] is { } candidateFaults)
                {
                    (failures ??= []).AddRange(candidateFaults);
                }
                else if (cancellations[index] is { } sourceCancellation)
                {
                    // Preserve the existing first-source-cancellation selection rule.
                    if (!hasSourceCancellation)
                    {
                        hasSourceCancellation = true;
                        (failures ??= []).Add(sourceCancellation);
                    }
                }
                else
                {
                    errors?.Add(typedFailures[index]);
                }
            }

            if (cancellation?.Exception is { } callbackFaults)
            {
                (failures ??= []).AddRange(callbackFaults.InnerExceptions);
            }

            Exception? primaryFailure = cancellationToken.IsCancellationRequested
                ? new OperationCanceledException(cancellationToken)
                : timedOut ? new TimeoutException() : null;
            if (primaryFailure is not null)
            {
                (failures ??= []).Insert(0, primaryFailure);
            }

            ThrowFailures(failures);
            return winnerIndex != int.MaxValue
                ? Validation<TValue, TError>.Valid(winner!)
                : Validation<TValue, TError>.InvalidFromOwnedErrors(errors!);

            async ValueTask ObserveAsync(
                FirstSuccessCandidate<Result<TValue, TError>> candidate,
                bool selectWinner)
            {
                await candidate.Observer.ConfigureAwait(false);
                pending--;
                var index = candidate.Index;
                var task = candidate.Completion;
                if (task.IsFaulted)
                {
                    // A faulted OCE is an independent fault, regardless of its token. Reading
                    // InnerExceptions also retains every exception represented by a source task.
                    faults[index] = task.Exception!.InnerExceptions;
                    return;
                }

                try
                {
                    var result = await task.ConfigureAwait(false);
                    if (result.TryGetValue(out var value))
                    {
                        if (selectWinner && index < winnerIndex)
                        {
                            winnerIndex = index;
                            winner = value;
                        }
                    }
                    else
                    {
                        result.TryGetError(out var error);
                        typedFailures[index] = error!;
                    }
                }
                catch (OperationCanceledException exception) when (task.IsCanceled)
                {
                    if (!operationToken.IsCancellationRequested || exception.CancellationToken != operationToken)
                    {
                        cancellations[index] = exception;
                    }
                }
                catch (Exception exception)
                {
                    faults[index] = [exception];
                }
            }
        }
        finally
        {
            timeoutCancellationSource?.Dispose();
        }
    }

    private sealed class FirstSuccessCandidate<T>
    {
        public FirstSuccessCandidate(
            int index,
            Task<T> completion,
            ChannelWriter<FirstSuccessCandidate<T>?> writer)
        {
            Index = index;
            Completion = completion;
            Observer = NotifyAsync(this, writer);
        }

        public int Index { get; }

        public Task<T> Completion { get; }

        public Task Observer { get; }

        private static async Task NotifyAsync(
            FirstSuccessCandidate<T> candidate,
            ChannelWriter<FirstSuccessCandidate<T>?> writer)
        {
            // The coordinator accounts for the source task's full terminal outcome.
            await ((Task)candidate.Completion).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            writer.TryWrite(candidate);
        }
    }

    private static void StopTimeout(CancellationTokenSource? timeoutCancellationSource)
    {
        if (timeoutCancellationSource is { IsCancellationRequested: false })
        {
            timeoutCancellationSource.CancelAfter(Timeout.InfiniteTimeSpan);
        }
    }

    private static void ThrowFailures(List<Exception>? failures)
    {
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
}
