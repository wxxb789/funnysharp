using FunnySharp;

namespace FunnySharp.TestSupport.Checks;

internal static class OrdinaryCheckConsumer
{
    internal static Result<CheckOrder, CheckError> FailFast(
        Result<CheckOrder, CheckError> input,
        Func<CheckOrder, UnitResult<CheckError>> stock,
        Func<CheckOrder, UnitResult<CheckError>> permission,
        Func<CheckOrder, UnitResult<CheckError>> fraud) =>
        input
            .Bind(order => stock(order).ToResult(() => order))
            .Bind(order => permission(order).ToResult(() => order))
            .Bind(order => fraud(order).ToResult(() => order));

    internal static async Task<Result<CheckOrder, CheckError>> FailFastAsync(
        Result<CheckOrder, CheckError> input,
        Func<CheckOrder, Task<UnitResult<CheckError>>> stock,
        Func<CheckOrder, Task<UnitResult<CheckError>>> permission,
        Func<CheckOrder, Task<UnitResult<CheckError>>> fraud)
    {
        var first = await input
            .BindAsync(async order =>
                (await stock(order).ConfigureAwait(false)).ToResult(() => order))
            .ConfigureAwait(false);
        var second = await first
            .BindAsync(async order =>
                (await permission(order).ConfigureAwait(false)).ToResult(() => order))
            .ConfigureAwait(false);

        return await second
            .BindAsync(async order =>
                (await fraud(order).ConfigureAwait(false)).ToResult(() => order))
            .ConfigureAwait(false);
    }

    internal static Validation<CheckOrder, CheckError> Accumulate(
        Result<CheckOrder, CheckError> input,
        Func<CheckOrder, UnitResult<CheckError>>[] checks)
    {
        if (!input.TryGetValue(out var order))
        {
            return ExistingFailure(input);
        }

        var validation = Validation<CheckOrder, CheckError>.Valid(order);

        foreach (var check in checks)
        {
            validation = validation.Zip(
                Pass(order, check(order)),
                static (value, _) => value);
        }

        return validation;
    }

    internal static async Task<Validation<CheckOrder, CheckError>> AccumulateAsync(
        Result<CheckOrder, CheckError> input,
        Func<CheckOrder, Task<UnitResult<CheckError>>>[] checks)
    {
        if (!input.TryGetValue(out var order))
        {
            return ExistingFailure(input);
        }

        var validation = Validation<CheckOrder, CheckError>.Valid(order);

        foreach (var check in checks)
        {
            var result = await check(order).ConfigureAwait(false);
            validation = validation.Zip(
                Pass(order, result),
                static (value, _) => value);
        }

        return validation;
    }

    internal static async ValueTask<Validation<CheckOrder, CheckError>> AccumulateValueAsync(
        Result<CheckOrder, CheckError> input,
        Func<CheckOrder, ValueTask<UnitResult<CheckError>>>[] checks)
    {
        if (!input.TryGetValue(out var order))
        {
            return ExistingFailure(input);
        }

        var validation = Validation<CheckOrder, CheckError>.Valid(order);

        foreach (var check in checks)
        {
            var result = await check(order).ConfigureAwait(false);
            validation = validation.Zip(
                Pass(order, result),
                static (value, _) => value);
        }

        return validation;
    }

    private static Validation<CheckOrder, CheckError> Pass(
        CheckOrder order,
        UnitResult<CheckError> check) =>
        check.Match(
            () => Validation<CheckOrder, CheckError>.Valid(order),
            error => Validation<CheckOrder, CheckError>.Invalid(error));

    private static Validation<CheckOrder, CheckError> ExistingFailure(
        Result<CheckOrder, CheckError> input)
    {
        if (input.TryGetError(out var error))
        {
            return Validation<CheckOrder, CheckError>.Invalid(error!);
        }

        throw new InvalidOperationException("The result has not been initialized.");
    }
}
