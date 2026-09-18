using System.Globalization;
using System.Runtime.CompilerServices;

using FunnySharp;

// The Goal 15 unit-result examples. Kept separate from Program.cs so the runnable
// example file stays navigable as more carriers are added.
internal static class UnitResultExamples
{
    internal static void Verify()
    {
        var success = UnitResult<CheckoutError>.Success();
        ExampleAssertions.True(success.IsSuccess, "Success must report success.");
        ExampleAssertions.True(!success.IsFailure, "Success must not report failure.");
        ExampleAssertions.True(!success.TryGetError(out var absentError), "TryGetError must be false for a success.");
        ExampleAssertions.True(absentError is null, "TryGetError must not fabricate an error for a success.");

        var error = new CheckoutError("payment-declined");
        var failure = UnitResult<CheckoutError>.Failure(error);
        ExampleAssertions.True(failure.IsFailure, "Failure must report failure.");
        ExampleAssertions.True(failure.TryGetError(out var capturedError), "TryGetError must be true for a failure.");
        ExampleAssertions.True(ReferenceEquals(error, capturedError), "TryGetError must preserve the failure object.");
        ExampleAssertions.Equal("payment-declined", failure.Match(() => "success", value => value.Code));
        ExampleAssertions.Equal("success", success.Match(() => "success", value => value.Code));

        var matchTrace = new List<string>();
        success.Match(() => matchTrace.Add("success"), value => matchTrace.Add(value.Code));
        failure.Match(() => matchTrace.Add("success"), value => matchTrace.Add(value.Code));
        ExampleAssertions.SequenceEqual(["success", "payment-declined"], matchTrace);

        ExampleAssertions.True(success == UnitResult<CheckoutError>.Success(), "Equal successes must compare equal.");
        ExampleAssertions.True(
            failure == UnitResult<CheckoutError>.Failure(new CheckoutError("payment-declined")),
            "Equal failures must compare equal.");
        ExampleAssertions.True(failure != success, "A failure must not equal a success.");
        ExampleAssertions.True(!failure.Equals(null), "An initialized unit result must not equal null.");
        ExampleAssertions.True(!failure.Equals("payment-declined"), "An initialized unit result must not equal another type.");
        ExampleAssertions.Equal(UnitResult<CheckoutError>.Success().GetHashCode(), success.GetHashCode());
        ExampleAssertions.Equal(
            UnitResult<CheckoutError>.Failure(new CheckoutError("payment-declined")).GetHashCode(),
            failure.GetHashCode());
        ExampleAssertions.Equal("Success", success.ToString());
        ExampleAssertions.Equal("Failure(declined)", UnitResult<string>.Failure("declined").ToString());

        var nullPayloadFailure = UnitResult<string?>.Failure(null);
        ExampleAssertions.True(nullPayloadFailure.IsFailure, "A null error payload must still be a failure.");
        ExampleAssertions.True(nullPayloadFailure.TryGetError(out var nullPayload), "A null error payload must be retrievable.");
        ExampleAssertions.True(nullPayload is null, "A null error payload must be preserved exactly.");
        ExampleAssertions.Equal("Failure()", nullPayloadFailure.ToString());

        var uninitialized = default(UnitResult<string>);
        ExampleAssertions.Equal("Uninitialized", uninitialized.ToString());
        ExampleAssertions.UninitializedThrows(() => uninitialized.IsSuccess);
        ExampleAssertions.UninitializedThrows(() => uninitialized.IsFailure);
        ExampleAssertions.UninitializedThrows(() => uninitialized.TryGetError(out _));
        ExampleAssertions.UninitializedThrows(() => uninitialized.GetHashCode());

        var mapped = success.Map(() => 42);
        ExampleAssertions.Equal(Result<int, CheckoutError>.Success(42), mapped);
        ExampleAssertions.Equal(Result<int, CheckoutError>.Failure(error), failure.Map(() => 42));

        var bound = success.Bind(() => UnitResult<CheckoutError>.Failure(new CheckoutError("missing-address")));
        ExampleAssertions.Equal(UnitResult<CheckoutError>.Failure(new CheckoutError("missing-address")), bound);
        ExampleAssertions.Equal(
            failure,
            failure.Bind(() => throw new InvalidOperationException("Bind must skip a failure.")));

        ExampleAssertions.True(
            success.Ensure(() => true, new CheckoutError("unused")).IsSuccess,
            "Ensure must keep a success when the predicate is true.");
        ExampleAssertions.Equal(
            UnitResult<CheckoutError>.Failure(new CheckoutError("terms-not-accepted")),
            success.Ensure(() => false, new CheckoutError("terms-not-accepted")));
        ExampleAssertions.Equal(
            UnitResult<CheckoutError>.Failure(new CheckoutError("lazy-terms")),
            success.Ensure(() => false, () => new CheckoutError("lazy-terms")));
        ExampleAssertions.Equal(
            failure,
            failure.Ensure(
                () => throw new InvalidOperationException("Ensure must skip a failure."),
                new CheckoutError("unused")));

        var recovered = failure.RecoverWith(value =>
            value.Code == "payment-declined"
                ? UnitResult<CheckoutError>.Success()
                : UnitResult<CheckoutError>.Failure(value));
        ExampleAssertions.True(recovered.IsSuccess, "RecoverWith must invoke the recovery for a failure.");
        ExampleAssertions.True(
            success.RecoverWith(_ => throw new InvalidOperationException("RecoverWith must skip a success.")).IsSuccess,
            "RecoverWith must skip a success.");

        ExampleAssertions.Equal(UnitResult<int>.Failure("payment-declined".Length), failure.MapError(value => value.Code.Length));
        ExampleAssertions.True(
            success.MapError(value => value.Code.Length).IsSuccess,
            "MapError must preserve a success without invoking the selector.");

        ExampleAssertions.True(success.Zip(UnitResult<CheckoutError>.Success()).IsSuccess, "Zipping two successes must succeed.");
        ExampleAssertions.Equal(
            UnitResult<CheckoutError>.Failure(new CheckoutError("first")),
            UnitResult<CheckoutError>.Failure(new CheckoutError("first"))
                .Zip(UnitResult<CheckoutError>.Failure(new CheckoutError("second"))));
        ExampleAssertions.Equal(
            UnitResult<CheckoutError>.Failure(new CheckoutError("second")),
            success.Zip(UnitResult<CheckoutError>.Failure(new CheckoutError("second"))));

        var zipFactoryInvoked = false;
        var lazyZip = success.ZipWith(() =>
        {
            zipFactoryInvoked = true;
            return UnitResult<CheckoutError>.Success();
        });
        ExampleAssertions.True(lazyZip.IsSuccess && zipFactoryInvoked, "ZipWith must invoke the factory for a success.");
        ExampleAssertions.Equal(
            UnitResult<CheckoutError>.Failure(new CheckoutError("first")),
            UnitResult<CheckoutError>.Failure(new CheckoutError("first"))
                .ZipWith(() => throw new InvalidOperationException("ZipWith must skip the factory for a failure.")));

        var observedException = new InvalidOperationException("ledger unavailable");
        var caught = UnitResult.Try(() => throw observedException);
        ExampleAssertions.True(caught.TryGetError(out var caughtError), "Try must convert a thrown exception to a failure.");
        ExampleAssertions.True(ReferenceEquals(observedException, caughtError), "Try must preserve the exact exception object.");

        var ran = false;
        var executed = UnitResult.Try(() => ran = true);
        ExampleAssertions.True(executed.IsSuccess && ran, "Try must invoke the operation and succeed when it returns normally.");

        var mappedBoundary = UnitResult.Try<CheckoutError>(
            () => throw observedException,
            exception => new CheckoutError("ledger-unavailable", exception));
        var mappedBoundaryError = mappedBoundary.Match(
            () => throw new InvalidOperationException("Expected the boundary to fail."),
            value => value);
        ExampleAssertions.Equal("ledger-unavailable", mappedBoundaryError.Code);
        ExampleAssertions.True(
            ReferenceEquals(observedException, mappedBoundaryError.Cause),
            "The typed boundary must receive the original exception.");

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellationException = new OperationCanceledException(cancellationSource.Token);
        try
        {
            _ = UnitResult.Try(() => throw cancellationException);
            throw new InvalidOperationException("Try must not convert cancellation into a failure.");
        }
        catch (OperationCanceledException actual) when (ReferenceEquals(actual, cancellationException))
        {
            ExampleAssertions.True(actual.CancellationToken == cancellationSource.Token, "Try must preserve the cancellation token.");
        }

        var mapperInvoked = false;
        try
        {
            _ = UnitResult.Try<CheckoutError>(
                () => throw cancellationException,
                exception =>
                {
                    mapperInvoked = true;
                    return new CheckoutError("canceled", exception);
                });
            throw new InvalidOperationException("Try must not map a cancellation.");
        }
        catch (OperationCanceledException actual) when (ReferenceEquals(actual, cancellationException))
        {
        }

        ExampleAssertions.True(!mapperInvoked, "The error mapper must never receive OperationCanceledException.");

        ExampleAssertions.True(
            Result<int, CheckoutError>.Success(5).ToUnitResult().IsSuccess,
            "A successful result must bridge to a successful unit result.");
        var failedResultBridge = Result<int, CheckoutError>.Failure(error).ToUnitResult();
        ExampleAssertions.True(failedResultBridge.TryGetError(out var bridgedError), "A failed result must bridge to a failed unit result.");
        ExampleAssertions.True(ReferenceEquals(error, bridgedError), "The bridge must preserve the failure object.");

        ExampleAssertions.True(Option.Some(3).ToUnitResult(new CheckoutError("missing")).IsSuccess, "Some must bridge to success.");
        var absentBridge = Option.None<int>().ToUnitResult(new CheckoutError("missing"));
        ExampleAssertions.True(absentBridge.TryGetError(out var absentBridgeError), "None must bridge to the supplied failure.");
        ExampleAssertions.Equal("missing", absentBridgeError!.Code);

        var errorFactoryCalls = 0;
        var lazySomeBridge = Option.Some(3).ToUnitResult(() =>
        {
            errorFactoryCalls++;
            return new CheckoutError("lazy-missing");
        });
        ExampleAssertions.True(lazySomeBridge.IsSuccess, "Some must bridge to success without invoking the error factory.");
        ExampleAssertions.Equal(0, errorFactoryCalls);
        var lazyNoneBridge = Option.None<int>().ToUnitResult(() =>
        {
            errorFactoryCalls++;
            return new CheckoutError("lazy-missing");
        });
        ExampleAssertions.True(lazyNoneBridge.TryGetError(out var lazyBridgeError), "None must bridge to the lazy factory result.");
        ExampleAssertions.Equal(1, errorFactoryCalls);
        ExampleAssertions.Equal("lazy-missing", lazyBridgeError!.Code);

        ExampleAssertions.True(Option.FromBoolean(true, 42).TryGetValue(out var present), "FromBoolean(true, value) must be Some.");
        ExampleAssertions.Equal(42, present);
        ExampleAssertions.True(Option.FromBoolean(false, 42).IsNone, "FromBoolean(false, value) must be None.");

        var eagerFactoryCalls = 0;
        var configured = Option.FromBoolean(true, () =>
        {
            eagerFactoryCalls++;
            return "configured";
        });
        ExampleAssertions.True(configured.TryGetValue(out var configuredValue), "FromBoolean(true, factory) must invoke the factory.");
        ExampleAssertions.Equal("configured", configuredValue!);
        ExampleAssertions.Equal(1, eagerFactoryCalls);

        var unused = Option.FromBoolean(false, () =>
        {
            eagerFactoryCalls++;
            return "unused";
        });
        ExampleAssertions.True(unused.IsNone, "FromBoolean(false, factory) must be None.");
        ExampleAssertions.Equal(1, eagerFactoryCalls);

        ExampleAssertions.True(Option.Some(42).ToNullable() is 42, "Some must bridge to a populated nullable value.");
        ExampleAssertions.True(Option.None<int>().ToNullable() is null, "None must bridge to a null nullable value.");

        ExampleAssertions.Equal(Option.Some(6), Option.Some(2).Zip(Option.Some(3), (quantity, unitPrice) => quantity * unitPrice));
        ExampleAssertions.True(
            Option.Some(2).Zip(Option.None<int>(), (quantity, unitPrice) => quantity * unitPrice).IsNone,
            "An absent option operand must short-circuit the combiner.");

        var optionCombinerRan = false;
        var optionMissingThird = Option.Some(2).Zip(
            Option.Some(3),
            Option.None<decimal>(),
            (quantity, count, price) =>
            {
                optionCombinerRan = true;
                return quantity * count;
            });
        ExampleAssertions.True(optionMissingThird.IsNone && !optionCombinerRan, "An absent third operand must short-circuit the combiner.");
        ExampleAssertions.Equal(
            Option.Some("book|2|12.50"),
            Option.Some("book").Zip(
                Option.Some(2),
                Option.Some(12.50m),
                (sku, quantity, unitPrice) => $"{sku}|{quantity}|{unitPrice.ToString(CultureInfo.InvariantCulture)}"));

        ExampleAssertions.Equal(
            Result<int, CheckoutError>.Success(6),
            Result<int, CheckoutError>.Success(2)
                .Zip(Result<int, CheckoutError>.Success(3), (quantity, unitPrice) => quantity * unitPrice));
        ExampleAssertions.Equal(
            Result<int, CheckoutError>.Failure(new CheckoutError("first")),
            Result<int, CheckoutError>.Failure(new CheckoutError("first"))
                .Zip(
                    Result<int, CheckoutError>.Failure(new CheckoutError("second")),
                    (left, right) => left + right));
        ExampleAssertions.Equal(
            Result<int, CheckoutError>.Success(6),
            Result<int, CheckoutError>.Success(1)
                .Zip(
                    Result<int, CheckoutError>.Success(2),
                    Result<int, CheckoutError>.Success(3),
                    (first, second, third) => first + second + third));
        ExampleAssertions.Equal(
            Result<int, CheckoutError>.Failure(new CheckoutError("third")),
            Result<int, CheckoutError>.Success(1)
                .Zip(
                    Result<int, CheckoutError>.Success(2),
                    Result<int, CheckoutError>.Failure(new CheckoutError("third")),
                    (first, second, third) => first + second + third));

        ExampleAssertions.Equal(
            Validation<int, AccountValidationError>.Valid(6),
            Validation<int, AccountValidationError>.Valid(2)
                .Zip(
                    Validation<int, AccountValidationError>.Valid(3),
                    (quantity, unitPrice) => quantity * unitPrice));
        var accumulatedCombination = Validation<int, AccountValidationError>
            .Invalid(new AccountValidationError("quantity", "required"))
            .Zip(
                Validation<int, AccountValidationError>.Invalid(new AccountValidationError("price", "required")),
                (quantity, unitPrice) => quantity * unitPrice);
        ExampleAssertions.True(accumulatedCombination.TryGetErrors(out var combinationErrors), "Both invalid operands must accumulate.");
        ExampleAssertions.SequenceEqual(
            [
                new AccountValidationError("quantity", "required"),
            new AccountValidationError("price", "required"),
        ],
            combinationErrors!);
        var threeWayCombination = Validation<int, AccountValidationError>
            .Invalid(new AccountValidationError("first", "invalid"))
            .Zip(
                Validation<int, AccountValidationError>.Valid(2),
                Validation<int, AccountValidationError>.Invalid(new AccountValidationError("third", "invalid")),
                (first, second, third) => first + second + third);
        ExampleAssertions.True(
            threeWayCombination.TryGetErrors(out var threeWayErrors),
            "Only the invalid third operand must contribute an error.");
        ExampleAssertions.SequenceEqual(
            [
                new AccountValidationError("first", "invalid"),
            new AccountValidationError("third", "invalid"),
        ],
            threeWayErrors!);

        ExampleAssertions.True(
            Array.Empty<UnitResult<CheckoutError>>().Sequence().IsSuccess,
            "An empty unit-result sequence must succeed.");

        var sequenceError = new CheckoutError("preserved");
        var preservedSequence = new[] { UnitResult<CheckoutError>.Failure(sequenceError) }.Sequence();
        ExampleAssertions.True(preservedSequence.TryGetError(out var preservedSequenceError), "A failed sequence must expose its error.");
        ExampleAssertions.True(ReferenceEquals(sequenceError, preservedSequenceError), "Sequence must preserve the first failure object.");

        var reached = 0;
        var failFast = new[]
        {
        UnitResult<CheckoutError>.Success(),
        UnitResult<CheckoutError>.Failure(new CheckoutError("first")),
        UnitResult<CheckoutError>.Failure(new CheckoutError("second")),
    }.Traverse(value =>
    {
        reached++;
        return value;
    });
        ExampleAssertions.Equal(UnitResult<CheckoutError>.Failure(new CheckoutError("first")), failFast);
        ExampleAssertions.Equal(2, reached);

        var selectorCalls = 0;
        var traversed = new[] { "first", "second", "third" }.Traverse(item =>
        {
            selectorCalls++;
            return item == "second"
                ? UnitResult<CheckoutError>.Failure(new CheckoutError("stopped-at-second"))
                : UnitResult<CheckoutError>.Success();
        });
        ExampleAssertions.Equal(UnitResult<CheckoutError>.Failure(new CheckoutError("stopped-at-second")), traversed);
        ExampleAssertions.Equal(2, selectorCalls);
    }

    internal static async Task VerifyAsync()
    {
        var expectedFailure = new InvalidOperationException("ledger unavailable");

        var faulted = await UnitResult.TryAsync(() => Task.FromException(expectedFailure));
        var faultedError = faulted.Match(
            () => throw new InvalidOperationException("Expected the unit boundary to fail."),
            error => error);
        ExampleAssertions.True(ReferenceEquals(expectedFailure, faultedError), "TryAsync must preserve exception identity.");

        var typedFaulted = await UnitResult.TryAsync<CheckoutError>(
            () => Task.FromException(expectedFailure),
            exception => new CheckoutError("ledger-unavailable", exception));
        var typedFaultedError = typedFaulted.Match(
            () => throw new InvalidOperationException("Expected the unit boundary to fail."),
            error => error);
        ExampleAssertions.Equal("ledger-unavailable", typedFaultedError.Code);
        ExampleAssertions.True(
            ReferenceEquals(expectedFailure, typedFaultedError.Cause),
            "The typed boundary must receive the original exception.");

        var taskRan = false;
        var taskSuccess = await UnitResult.TryAsync(() =>
        {
            taskRan = true;
            return Task.CompletedTask;
        });
        ExampleAssertions.True(taskSuccess.IsSuccess && taskRan, "TryAsync must invoke the operation and succeed when it completes.");

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellationMapperInvoked = false;
        await ExampleAssertions.CancellationIsPreserved(
            UnitResult.TryAsync<CheckoutError>(
                () => Task.FromCanceled(cancellationSource.Token),
                exception =>
                {
                    cancellationMapperInvoked = true;
                    return new CheckoutError("canceled", exception);
                }));
        ExampleAssertions.True(!cancellationMapperInvoked, "TryAsync must never map a canceled operation.");

        try
        {
            await UnitResult.TryAsync(() => Task.FromCanceled(cancellationSource.Token));
            throw new InvalidOperationException("TryAsync must preserve a canceled operation.");
        }
        catch (OperationCanceledException actual)
        {
            ExampleAssertions.True(
                actual.CancellationToken == cancellationSource.Token,
                "TryAsync must preserve the original cancellation token.");
        }

        var faultedValue = await UnitResult.TryValueAsync(() => ValueTask.FromException(expectedFailure));
        var faultedValueError = faultedValue.Match(
            () => throw new InvalidOperationException("Expected the value boundary to fail."),
            error => error);
        ExampleAssertions.True(ReferenceEquals(expectedFailure, faultedValueError), "TryValueAsync must preserve exception identity.");

        var valueRan = false;
        var valueSuccess = await UnitResult.TryValueAsync(() =>
        {
            valueRan = true;
            return ValueTask.CompletedTask;
        });
        ExampleAssertions.True(
            valueSuccess.IsSuccess && valueRan,
            "TryValueAsync must invoke the operation and succeed when it completes.");

        var valueMapperInvoked = false;
        await ExampleAssertions.CancellationIsPreserved(
            UnitResult.TryValueAsync<CheckoutError>(
                () => ValueTask.FromCanceled(cancellationSource.Token),
                exception =>
                {
                    valueMapperInvoked = true;
                    return new CheckoutError("value-canceled", exception);
                })
            .AsTask());
        ExampleAssertions.True(!valueMapperInvoked, "TryValueAsync must never map a canceled operation.");

        try
        {
            await UnitResult.TryValueAsync(() => ValueTask.FromCanceled(cancellationSource.Token));
            throw new InvalidOperationException("TryValueAsync must preserve a canceled operation.");
        }
        catch (OperationCanceledException actual)
        {
            ExampleAssertions.True(
                actual.CancellationToken == cancellationSource.Token,
                "TryValueAsync must preserve the original cancellation token.");
        }

        var mapSelectorRan = false;
        var mappedSuccess = await UnitResult<CheckoutError>.Success().MapAsync(() =>
        {
            mapSelectorRan = true;
            return Task.FromResult(21);
        });
        ExampleAssertions.Equal(Result<int, CheckoutError>.Success(21), mappedSuccess);
        ExampleAssertions.True(mapSelectorRan, "MapAsync must invoke the selector for a success.");

        var mapShortCircuited = false;
        var mappedFailure = await UnitResult<CheckoutError>.Failure(new CheckoutError("denied")).MapAsync(() =>
        {
            mapShortCircuited = true;
            return Task.FromResult(21);
        });
        ExampleAssertions.Equal(Result<int, CheckoutError>.Failure(new CheckoutError("denied")), mappedFailure);
        ExampleAssertions.True(!mapShortCircuited, "MapAsync must short-circuit a failure without invoking the selector.");

        using var workSource = new CancellationTokenSource();
        var mapTokens = new List<CancellationToken>();
        var tokenMapped = await UnitResult<CheckoutError>.Success().MapAsync(
            token =>
            {
                mapTokens.Add(token);
                return Task.FromResult(42);
            },
            workSource.Token);
        ExampleAssertions.Equal(Result<int, CheckoutError>.Success(42), tokenMapped);
        ExampleAssertions.True(
            mapTokens.Count == 1 && mapTokens[0] == workSource.Token,
            "MapAsync must forward the supplied token to the selector.");

        var bindRan = false;
        var boundSuccess = await UnitResult<CheckoutError>.Success().BindAsync(() =>
        {
            bindRan = true;
            return Task.FromResult(UnitResult<CheckoutError>.Success());
        });
        ExampleAssertions.True(boundSuccess.IsSuccess && bindRan, "BindAsync must invoke the binder for a success.");

        var bindShortCircuited = false;
        var boundFailure = await UnitResult<CheckoutError>.Failure(new CheckoutError("denied")).BindAsync(() =>
        {
            bindShortCircuited = true;
            return Task.FromResult(UnitResult<CheckoutError>.Success());
        });
        ExampleAssertions.Equal(UnitResult<CheckoutError>.Failure(new CheckoutError("denied")), boundFailure);
        ExampleAssertions.True(!bindShortCircuited, "BindAsync must short-circuit a failure without invoking the binder.");

        var mapValueRan = false;
        var mappedValue = await UnitResult<CheckoutError>.Success().MapValueAsync(() =>
        {
            mapValueRan = true;
            return ValueTask.FromResult(21);
        });
        ExampleAssertions.Equal(Result<int, CheckoutError>.Success(21), mappedValue);
        ExampleAssertions.True(mapValueRan, "MapValueAsync must invoke the selector for a success.");

        var mapValueShortCircuited = false;
        var mappedValueFailure = await UnitResult<CheckoutError>.Failure(new CheckoutError("denied")).MapValueAsync(() =>
        {
            mapValueShortCircuited = true;
            return ValueTask.FromResult(21);
        });
        ExampleAssertions.Equal(Result<int, CheckoutError>.Failure(new CheckoutError("denied")), mappedValueFailure);
        ExampleAssertions.True(!mapValueShortCircuited, "MapValueAsync must short-circuit a failure.");

        var bindValueRan = false;
        var boundValue = await UnitResult<CheckoutError>.Success().BindValueAsync(() =>
        {
            bindValueRan = true;
            return ValueTask.FromResult(UnitResult<CheckoutError>.Success());
        });
        ExampleAssertions.True(boundValue.IsSuccess && bindValueRan, "BindValueAsync must invoke the binder for a success.");

        var bindValueShortCircuited = false;
        var boundValueFailure = await UnitResult<CheckoutError>.Failure(new CheckoutError("denied")).BindValueAsync(() =>
        {
            bindValueShortCircuited = true;
            return ValueTask.FromResult(UnitResult<CheckoutError>.Success());
        });
        ExampleAssertions.Equal(UnitResult<CheckoutError>.Failure(new CheckoutError("denied")), boundValueFailure);
        ExampleAssertions.True(!bindValueShortCircuited, "BindValueAsync must short-circuit a failure.");

        var taskBridge = await Task.FromResult(Result<int, CheckoutError>.Success(5)).ToUnitResultAsync();
        ExampleAssertions.True(taskBridge.IsSuccess, "A successful result task must bridge to a successful unit result.");

        var taskFailure = new CheckoutError("task-failure");
        var taskFailureBridge = await Task
            .FromResult(Result<int, CheckoutError>.Failure(taskFailure))
            .ToUnitResultAsync();
        ExampleAssertions.True(taskFailureBridge.TryGetError(out var taskBridgeError), "A failed result task must bridge to a failure.");
        ExampleAssertions.True(ReferenceEquals(taskFailure, taskBridgeError), "The task bridge must preserve the failure object.");

        var valueTaskBridge = await ValueTask
            .FromResult(Result<int, CheckoutError>.Success(5))
            .ToUnitResultAsync();
        ExampleAssertions.True(valueTaskBridge.IsSuccess, "A successful result value task must bridge to a successful unit result.");

        var valueTaskFailure = new CheckoutError("value-task-failure");
        var valueTaskFailureBridge = await ValueTask
            .FromResult(Result<int, CheckoutError>.Failure(valueTaskFailure))
            .ToUnitResultAsync();
        ExampleAssertions.True(
            valueTaskFailureBridge.TryGetError(out var valueTaskBridgeError),
            "A failed result value task must bridge to a failure.");
        ExampleAssertions.True(
            ReferenceEquals(valueTaskFailure, valueTaskBridgeError),
            "The value-task bridge must preserve the failure object.");

        await ExampleAssertions.FaultIsPreserved(
            Task.FromException<Result<int, CheckoutError>>(expectedFailure).ToUnitResultAsync(),
            expectedFailure);

        using var traversalSource = new CancellationTokenSource();
        var traversalTokens = new List<CancellationToken>();
        var traversal = await AsyncUnitResultCommands().TraverseValueAsync(
            (command, token) =>
            {
                traversalTokens.Add(token);
                return ValueTask.FromResult(UnitResult<CheckoutError>.Success());
            },
            traversalSource.Token);
        ExampleAssertions.True(traversal.IsSuccess, "Every traversed command must succeed.");
        ExampleAssertions.Equal(2, traversalTokens.Count);
        ExampleAssertions.True(
            traversalTokens.All(token => token == traversalSource.Token),
            "TraverseValueAsync must forward the supplied token to the selector.");

        var reached = 0;
        var failFast = await AsyncUnitResultCommands().TraverseValueAsync(
            (command, token) =>
            {
                reached++;
                return ValueTask.FromResult(
                    command == "validate"
                        ? UnitResult<CheckoutError>.Failure(new CheckoutError("first"))
                        : UnitResult<CheckoutError>.Success());
            },
            CancellationToken.None);
        ExampleAssertions.Equal(UnitResult<CheckoutError>.Failure(new CheckoutError("first")), failFast);
        ExampleAssertions.Equal(1, reached);

        var sequence = await AsyncUnitResultSteps().SequenceAsync(traversalSource.Token);
        ExampleAssertions.True(sequence.IsSuccess, "SequenceAsync must succeed when every unit result succeeds.");

        var failFastSequence = await AsyncUnitResultAttempts().SequenceAsync();
        ExampleAssertions.Equal(UnitResult<CheckoutError>.Failure(new CheckoutError("first")), failFastSequence);

        using var canceledTraversalSource = new CancellationTokenSource();
        canceledTraversalSource.Cancel();
        await ExampleAssertions.CancellationIsPreserved(
            AsyncUnitResultCommands()
                .TraverseValueAsync(
                    static (command, token) =>
                    {
                        token.ThrowIfCancellationRequested();
                        return ValueTask.FromResult(UnitResult<CheckoutError>.Success());
                    },
                    canceledTraversalSource.Token)
                .AsTask());
        await ExampleAssertions.CancellationIsPreserved(
            AsyncUnitResultSteps().SequenceAsync(canceledTraversalSource.Token).AsTask());

        var valid = Validation<int, AccountValidationError>.Valid(21);
        var mappedValid = await valid.MapAsync(value => Task.FromResult(value * 2));
        ExampleAssertions.Equal(Validation<int, AccountValidationError>.Valid(42), mappedValid);

        var validationErrors = new[]
        {
        new AccountValidationError("age", "must-be-adult"),
    };
        var invalid = Validation<int, AccountValidationError>.Invalid(validationErrors[0]);
        var invalidSelectorRan = false;
        var mappedInvalid = await invalid.MapAsync(value =>
        {
            invalidSelectorRan = true;
            return Task.FromResult(value * 2);
        });
        ExampleAssertions.True(mappedInvalid.TryGetErrors(out var mappedInvalidErrors), "An invalid validation must keep its errors.");
        ExampleAssertions.SequenceEqual(validationErrors, mappedInvalidErrors!);
        ExampleAssertions.True(!invalidSelectorRan, "MapAsync must short-circuit an invalid validation.");

        var mappedValidValue = await valid.MapValueAsync(value => ValueTask.FromResult(value * 2));
        ExampleAssertions.Equal(Validation<int, AccountValidationError>.Valid(42), mappedValidValue);

        var invalidValueSelectorRan = false;
        var mappedInvalidValue = await invalid.MapValueAsync(value =>
        {
            invalidValueSelectorRan = true;
            return ValueTask.FromResult(value * 2);
        });
        ExampleAssertions.True(
            mappedInvalidValue.TryGetErrors(out var mappedInvalidValueErrors),
            "An invalid validation must keep its errors.");
        ExampleAssertions.SequenceEqual(validationErrors, mappedInvalidValueErrors!);
        ExampleAssertions.True(!invalidValueSelectorRan, "MapValueAsync must short-circuit an invalid validation.");
    }

    static async IAsyncEnumerable<string> AsyncUnitResultCommands(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return "validate";
        await Task.Yield();
        yield return "reserve";
    }

    static async IAsyncEnumerable<UnitResult<CheckoutError>> AsyncUnitResultSteps(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return UnitResult<CheckoutError>.Success();
        await Task.Yield();
        yield return UnitResult<CheckoutError>.Success();
    }

    static async IAsyncEnumerable<UnitResult<CheckoutError>> AsyncUnitResultAttempts(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return UnitResult<CheckoutError>.Success();
        await Task.Yield();
        yield return UnitResult<CheckoutError>.Failure(new CheckoutError("first"));
        throw new InvalidOperationException("SequenceAsync must stop at the first failure.");
    }
}
