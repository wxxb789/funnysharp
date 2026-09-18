# UnitResult

`FunnySharp` provides `UnitResult<TError>` for work that either succeeds without producing a value or
fails with a typed error. It keeps the fail-fast vocabulary of `Result<TValue, TError>` for commands,
deletes, and notifications without forcing a dummy success payload. Compiling examples are in
[examples/FunnySharp.Examples/Program.cs](../examples/FunnySharp.Examples/Program.cs).

## API Shape

Create values with `UnitResult.Success<TError>()` and `UnitResult.Failure(error)`, or with the struct
factories `UnitResult<TError>.Success()` and `UnitResult<TError>.Failure(error)`.
`UnitResult<TError>` is a `readonly struct` with no public constructor, so the default value is
uninitialized rather than a silent domain outcome. There is no public `Unit` type: a successful unit
result carries no payload at all.

Use `IsSuccess`, `IsFailure`, `TryGetError`, or either `Match` overload to inspect a unit result. There
is intentionally no throwing error accessor.

The synchronous composition surface is:

- `Map` turns a success into a `Result<TValue, TError>` by running a value-producing selector, so the
  successful branch can continue on the value carrier. There is no eager-result alternative.
- `Bind` sequences unit-result-producing functions and stops at the first failure.
- `Ensure` keeps a success only when a predicate accepts it, otherwise returns the supplied failure.
- `RecoverWith` replaces a failure with another unit result. There is no plain `Recover` because
  there is no value to produce eagerly.
- `MapError` transforms a failure value while preserving success.
- `Zip` combines two already-created unit results and returns the first failure in left-to-right order.
- `ZipWith` accepts a factory so the second operation is not invoked after the first failure.

A delete-or-notify command shows the shape: a precondition, a delete, and an idempotent recovery when
the delete fails.

<!-- documentation-sample: DocumentationSamples.UnitResult.DeleteOrNotify -->
```csharp
return UnitResult<OrderError>.Success()
    .Ensure(() => store.Exists(id), new OrderError("not-found", "The order was not found."))
    .Bind(() => store.Delete(id))
    .RecoverWith(error => NotifyAndAcknowledge(id, error));
```

There is no LINQ `Select` or `SelectMany` query syntax on this carrier: a successful unit result has
no value to project, and explicit `Bind` and `Ensure` calls already express sequencing and guarded
success. Member-centric method calls are the documented vocabulary.

## Choosing A Carrier

The four outcome carriers have distinct jobs:

- `Option<T>` represents presence or absence. Its default value remains a valid `None`, and `Some`
  rejects a runtime-null payload.
- `Result<TValue, TError>` represents fail-fast work that produces a meaningful value.
- `UnitResult<TError>` represents fail-fast work that produces no value. Use it instead of
  `Result<bool, TError>` or another dummy success payload.
- `Validation<TValue, TError>` represents independent checks that accumulate every error.

A workflow can cross those carriers honestly. Independent form checks stay on `Validation` so all
errors survive, and the dependent command that runs only after a valid draft is a `UnitResult`.

<!-- documentation-sample: DocumentationSamples.UnitResult.ValidationUnaffected -->
```csharp
Validation<OrderDraft, OrderError> validation = draft.Validate();
if (!validation.TryGetValue(out var order))
{
    _ = validation.TryGetErrors(out var errors);
    return UnitResult<OrderError>.Failure(
        new OrderError("validation", $"{errors!.Count} validation errors were found."));
}

return service.Submit(order!);
```

## Uninitialized And Default Values

Every member that reads the case or the payload throws `InvalidOperationException` with the message
`"The unit result has not been initialized."` for a default value. That includes `IsSuccess`,
`IsFailure`, `TryGetError`, both `Match` overloads, `Map`, `Bind`, `Ensure`, `RecoverWith`, `MapError`,
`Zip`, `ZipWith`, `Equals`, `GetHashCode`, and the `==` and `!=` operators. `ToString()` is the single
exception: it does not throw and returns the diagnostic text `"Uninitialized"`.

`Zip` validates both operands before it short-circuits, so it throws when either the receiver or
`second` is uninitialized even when the receiver is already a failure. `ZipWith` throws for an
uninitialized receiver and skips its factory only after that check; the factory result is validated
when it is read. `Equals(UnitResult<TError>)` reads both operands and throws when either is
uninitialized, while
`Equals(object?)` returns `false` for a null or unrelated object and throws only when the receiver is
uninitialized.

Async composition extension methods inspect their `UnitResult` receiver synchronously when they are
called, before returning a task. A default unit result therefore throws at call time from `MapAsync`,
`MapValueAsync`, `BindAsync`, and `BindValueAsync`. Traversal reads a selected unit result when the
source reaches it, so a selector that returns a default value throws during synchronous enumeration
and faults the asynchronous operation when it is awaited. Null delegate arguments are still rejected
with `ArgumentNullException` before the carrier is inspected, matching the entry validation order of
the synchronous members.

## Result And Option Interop

`Result<TValue, TError>.ToUnitResult()` drops the successful value and preserves the failure object, so
no dummy value is materialized. `ToUnitResultAsync` accepts `Task<Result<...>>` and
`ValueTask<Result<...>>` completions, awaits them directly, and preserves faults and cancellation
instead of wrapping them.

`Option<T>.ToUnitResult(error)` and `ToUnitResult(errorFactory)` map presence to success and absence to
the supplied failure. The factory runs only for `None`, and a null factory result is a legitimate
failure. The conditional-presence sample uses both lazy factories.

<!-- documentation-sample: DocumentationSamples.UnitResult.ConditionalPresence -->
```csharp
return Option
    .FromBoolean(inventory.HasStock(sku, requested), () => new Reservation(sku, requested))
    .ToUnitResult(() => new ReservationError(sku, "The requested quantity is not available."));
```

## Asynchronous Composition

`MapAsync` and `BindAsync` use Task-returning callbacks; `MapValueAsync` and `BindValueAsync` use
ValueTask-returning callbacks. `Map*Async` produces `Result<TResult, TError>`; `Bind*Async` produces
another `UnitResult<TError>`. Each has a cancellation-aware overload that passes the exact supplied
`CancellationToken` to the callback without eagerly cancelling.

A failure returns an already-completed failed carrier carrying the same error, does not invoke a
callback, and does not inspect the token. A success invokes the callback once and observes the returned
awaitable once with `ConfigureAwait(false)`. Synchronous callback exceptions fault the returned
awaitable, and an `OperationCanceledException` from a callback becomes a canceled operation, matching
`ResultAsync` behavior. Faults and cancellation from the awaited work remain ordinary asynchronous
failures with the original exception object and token; they are never converted into a unit-result
failure. Cancellation is never mapped to failure. `ValueTask` follows its normal single-consumption
rule.

Because the composition methods live on the carrier, an asynchronous command chain awaits each step
before binding the next one.

<!-- documentation-sample: DocumentationSamples.UnitResult.AsyncCommandChain -->
```csharp
UnitResult<CheckoutError> reservation = await UnitResult
    .TryAsync(() => service.ReserveAsync(cart), MapCheckoutFailure);

UnitResult<CheckoutError> confirmed = await reservation
    .BindAsync(() => service.ConfirmAsync(cart));

return await confirmed.BindAsync(() => service.SendReceiptAsync(cart));
```

## Exception Boundaries

`UnitResult.Try`, `UnitResult.TryAsync`, and `UnitResult.TryValueAsync` are explicit adapters for APIs
that throw, mirroring `Result.Try*`. Their default overloads return `UnitResult<Exception>` and store
the exact exception object. Mapper overloads convert a non-cancellation exception to a domain error;
retaining the original exception in that error is an explicit caller choice.

The delegates are deliberately tokenless `Action`, `Func<Task>`, and `Func<ValueTask>` shapes: the
boundary does not own cancellation. An `OperationCanceledException` or its subclasses is never caught or
mapped. Synchronous cancellation is thrown to the caller. Asynchronous cancellation remains a canceled
Task or ValueTask with its original token, and a faulted awaitable containing an
`OperationCanceledException` remains faulted. A `TryAsync` operation that returns a null Task faults
the returned task with `InvalidOperationException` and is not sent through the error mapper. Mapper
exceptions are not swallowed: `Try` throws them to the caller, and the asynchronous forms surface
them through the returned awaitable, with an `OperationCanceledException` following the
canceled-operation rule. A `ValueTask` returned by the delegate is observed once.

## Traversal

The shared collection operations include `UnitResult<TError>`:

- `Sequence` collects `IEnumerable<UnitResult<TError>>`.
- `Traverse` applies a unit-result-producing selector.
- `SequenceAsync`, `TraverseAsync`, and `TraverseValueAsync` are the asynchronous counterparts, with
  overloads that receive the caller's `CancellationToken`.

Traversal is fail-fast: it stops at the first failure and returns that failure's error object, empty
input is success, null arguments are rejected eagerly, the source is enumerated once, and a selector is
invoked once per reached item in source order. Unlike Option, Result, and Validation traversal, a
successful unit-result traversal has no value list to materialize. Traversal does not catch source or
selector exceptions, and an enumerator is still disposed when one has been acquired.

The traversal overloads differ only by the returned carrier, so a selector whose return type cannot be
inferred from its body (for example a lambda that only throws) or a null selector argument needs an
explicit delegate type such as `(Func<int, UnitResult<string>>)` to select the unit-result overload.
Selectors that return `UnitResult<TError>` resolve without a cast.

The asynchronous operations return `ValueTask<UnitResult<TError>>`, process one item at a time, forward
the supplied token unchanged to the enumerator and to a token-aware selector, await each source and
selector `ValueTask` once, and dispose the asynchronous enumerator after success, failure, cancellation,
or a selector fault. An `OperationCanceledException` escaping a source or selector completes the
returned operation as canceled with that exception's token; other faults propagate unchanged. The
common ordering, disposal, and cancellation rules are documented in
[Validation and traversal semantics](validation.md).

## Evaluation And Failure Semantics

All callback-taking members validate their delegates at entry, even when the active case will
short-circuit, and a selected callback is invoked at most once. Entry validation happens before the
uninitialized check, so a null delegate is reported before a default carrier is read.

`Map`, `Bind`, and `Ensure` do not invoke success callbacks after failure. `MapError` and `RecoverWith`
do not invoke failure callbacks after success. `Zip` chooses the left failure before the right failure
and does not undo computation already performed to create its argument; use `ZipWith` or `Bind` when
the later operation itself must be skipped. Callback exceptions are not caught, wrapped, or replaced by
ordinary composition methods.

Failure payloads preserve runtime nulls: `UnitResult<string?>.Failure(null)` is a legitimate failure
and keeps its state. Equality, `==`, and `!=` compare the active case and, for failures, the error with
`EqualityComparer<TError>.Default`. Equal unit results have equal hashes, the hash includes the case,
and success never equals failure solely because their payloads compare equally. `ToString()` returns
diagnostic `Success`, `Failure(error)`, or `Uninitialized` text and is not a serialization contract.

Unit results do not flatten. `UnitResult<UnitResult<string>>` is a failure whose error payload is
another unit result; comparing two such values compares the inner carriers with their own structural
equality. `UnitResult<UnitResult<string>>.Success()` and
`UnitResult<UnitResult<string>>.Failure(UnitResult<string>.Success())` are therefore different values.

There are no implicit conversions, so the created branch is always visible in code.

## Deliberate Boundaries

`UnitResult<TError>` does not add an effect runtime, an async wrapper type, exception conversion, retry
behavior, serialization support, analyzers, or source generators. Error accumulation stays on
`Validation<TValue, TError>`; dependent sequencing stays on `Bind` and `Ensure`. The core package
remains BCL-first, and the optional HTTP integration is documented in
[ASP.NET Core integration](aspnet-core.md).

## Performance Evidence

No numeric benchmark claim is made for `UnitResult<TError>` in this release. The carrier is a
`readonly struct` whose factories and case inspection are allocation-free value operations over a
private state byte and an error field; this structural statement is not a timing or throughput claim.
Any future performance claim requires a benchmark scenario with an approved baseline and a comparison
contract, matching the evidence process used for the existing carriers in
[Result semantics](result.md) and [Validation semantics](validation.md).
