using FunnySharp;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

var mapped = Option.Some("release").Map(static value => value.Length);
Require(mapped.TryGetValue(out var length) && length == 7, "Option mapping failed.");

var result = Result<int, string>.Success(20).Map(static value => value + 1);
Require(result.TryGetValue(out var resultValue) && resultValue == 21, "Result mapping failed.");

var validation = Validation<int, string>.InvalidMany(["first", "second"]);
Require(
    validation.TryGetErrors(out var errors) && errors.SequenceEqual(["first", "second"]),
    "Validation error accumulation failed.");

var effect = Effect
    .FromSync(() => Option.Some(6))
    .Map(static option => option.Map(static value => value * 7));
var effectValue = await effect.RunAsync();
Require(effectValue.TryGetValue(out var answer) && answer == 42, "Effect execution failed.");

await VerifyResultCancellationAsync();
await VerifyFirstSuccessCancellationPrecedenceAsync();

Console.WriteLine("FunnySharp core compatibility smoke passed.");

static async Task VerifyResultCancellationAsync()
{
    using var cancellationSource = new CancellationTokenSource();
    cancellationSource.Cancel();
    var cancellation = CaptureCancellation(cancellationSource.Token);
    var operation = Result.TryAsync<int>(() =>
        Rethrow<Task<int>>(ExceptionDispatchInfo.Capture(cancellation)));

    OperationCanceledException actual;
    try
    {
        _ = await operation;
        throw new InvalidOperationException("Result cancellation probe unexpectedly completed.");
    }
    catch (OperationCanceledException exception)
    {
        actual = exception;
    }

    Require(operation.IsCanceled, "Result cancellation probe was not canceled.");
    Require(ReferenceEquals(cancellation, actual), "Result cancellation identity changed.");
    Require(actual.CancellationToken == cancellationSource.Token, "Result cancellation token changed.");
    Require(actual.StackTrace?.Contains(nameof(ThrowOriginalCancellation), StringComparison.Ordinal) is true,
        "Result cancellation stack changed.");
}

static async Task VerifyFirstSuccessCancellationPrecedenceAsync()
{
    using var callerCancellation = new CancellationTokenSource();
    var loserCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var loserRelease = new TaskCompletionSource<Result<int, string>>(
        TaskCreationOptions.RunContinuationsAsynchronously);
    var effects = new[]
    {
        Effect.FromTask<Result<int, string>>(token =>
        {
            token.Register(() => loserCanceled.TrySetResult());
            return loserRelease.Task;
        }),
    };
    var operation = effects.FirstSuccessAsync(TimeSpan.Zero, callerCancellation.Token).AsTask();

    await loserCanceled.Task;
    Require(!operation.IsCompleted, "FirstSuccess cleanup was not held open.");
    callerCancellation.Cancel();
    loserRelease.SetResult(Result<int, string>.Failure("late"));

    OperationCanceledException actual;
    try
    {
        _ = await operation;
        throw new InvalidOperationException("FirstSuccess cancellation probe unexpectedly completed.");
    }
    catch (OperationCanceledException exception)
    {
        actual = exception;
    }

    Require(actual.CancellationToken == callerCancellation.Token,
        "FirstSuccess did not preserve caller cancellation precedence.");
}

static OperationCanceledException CaptureCancellation(CancellationToken cancellationToken)
{
    try
    {
        ThrowOriginalCancellation(cancellationToken);
    }
    catch (OperationCanceledException exception)
    {
        return exception;
    }

    throw new UnreachableException();
}

static TResult Rethrow<TResult>(ExceptionDispatchInfo dispatchInfo)
{
    dispatchInfo.Throw();
    throw new UnreachableException();
}

static void ThrowOriginalCancellation(CancellationToken cancellationToken) =>
    throw new OperationCanceledException("compatibility cancellation", innerException: null, cancellationToken);

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
