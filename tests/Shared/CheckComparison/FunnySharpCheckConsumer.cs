using FunnySharp;

namespace FunnySharp.TestSupport.Checks;

internal static class FunnySharpCheckConsumer
{
    internal static Result<CheckOrder, CheckError> FailFast(
        Result<CheckOrder, CheckError> input,
        Func<CheckOrder, UnitResult<CheckError>> stock,
        Func<CheckOrder, UnitResult<CheckError>> permission,
        Func<CheckOrder, UnitResult<CheckError>> fraud) =>
        input
            .Check(stock)
            .Check(permission)
            .Check(fraud);

    internal static async Task<Result<CheckOrder, CheckError>> FailFastAsync(
        Result<CheckOrder, CheckError> input,
        Func<CheckOrder, Task<UnitResult<CheckError>>> stock,
        Func<CheckOrder, Task<UnitResult<CheckError>>> permission,
        Func<CheckOrder, Task<UnitResult<CheckError>>> fraud)
    {
        var first = await input.CheckAsync(stock).ConfigureAwait(false);
        var second = await first.CheckAsync(permission).ConfigureAwait(false);

        return await second.CheckAsync(fraud).ConfigureAwait(false);
    }

    internal static Validation<CheckOrder, CheckError> Accumulate(
        Result<CheckOrder, CheckError> input,
        Func<CheckOrder, UnitResult<CheckError>>[] checks) =>
        input.CheckAll(checks);

    internal static Task<Validation<CheckOrder, CheckError>> AccumulateAsync(
        Result<CheckOrder, CheckError> input,
        Func<CheckOrder, Task<UnitResult<CheckError>>>[] checks) =>
        input.CheckAllAsync(checks);

    internal static ValueTask<Validation<CheckOrder, CheckError>> AccumulateValueAsync(
        Result<CheckOrder, CheckError> input,
        Func<CheckOrder, ValueTask<UnitResult<CheckError>>>[] checks) =>
        input.CheckAllValueAsync(checks);
}
