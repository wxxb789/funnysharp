# Reusable Checks

`FunnySharp` lets one fallible business check serve two workflows without an adapter: a fail-fast
workflow that keeps the exact input value, and an accumulating workflow that keeps the same input
value or every independent rule error. Compiling examples are in
[CheckExamples.cs](../examples/FunnySharp.Examples/CheckExamples.cs); the comparison, callback
ledger, and measured costs are in [check-evidence.md](check-evidence.md).

## API Shape

A check is a caller-supplied no-value operation:

```text
Func<TValue, UnitResult<TError>>
```

The synchronous surface is:

- `Result<TValue, TError>.Check(check)` runs one check after a success and returns the same
  `Result<TValue, TError>`. A successful check retains the exact successful input value; a failed
  check replaces the value with that check's typed error.
- `Result<TValue, TError>.CheckAll(checks)` runs an ordered set of independent checks against the
  successful value and returns `Validation<TValue, TError>`.
- `Validation<TValue, TError>.CheckAll(checks)` does the same when the validation is already valid.
  An invalid validation returns its existing error snapshot without running a check.

The asynchronous surface follows the package's naming rules:

- `CheckAsync` / `CheckValueAsync` run one `Task`- or `ValueTask`-returning check and preserve the
  input value.
- `CheckAllAsync` / `CheckAllValueAsync` run an ordered `Task`- or `ValueTask`-returning rule set
  and accumulate its errors.
- Each async member has a tokenless overload and a cancellation-aware overload that receives an
  explicit `CancellationToken` and forwards it unchanged.

There are no `Task<Result<...>>` receiver overloads and no raw-`T` extension. The operation belongs
to the existing outcome carriers; a caller that already has a plain value starts the fail-fast form
with `Result.Success(value)` or the accumulating form with `Validation.Valid(value)`.

## Choose The Workflow

| Need | Operation | Result |
| --- | --- | --- |
| Keep the input only if one business check succeeds | `Check` | Same `Result<TValue, TError>` |
| Keep the input or report every independent check error | `CheckAll` | `Validation<TValue, TError>` |
| Apply a Boolean predicate with a prebuilt error | `Ensure` | Existing carrier |
| Change the successful value while sequencing | `Bind` | Carrier with the binder's value type |
| A later check depends on an earlier check succeeding | `Check` / `Bind` / ordinary control flow | Fail-fast carrier |

`Check` is deliberately not an alias for `Ensure` or `Bind`. `Ensure` takes a Boolean predicate and
an already-known error, so it cannot represent a fallible business operation that constructs its own
error or performs asynchronous work. `Bind` takes a binder that can change the successful value
type; retaining the same value requires an adapter such as
`Bind(value => check(value).ToResult(() => value))` and a reconstruction factory. `Check` accepts the
no-value outcome directly, never changes the input type, and returns the original successful result
without invoking a factory.

## Check Semantics

`Check` validates its delegate at entry, then inspects the receiver. An already-failed result returns
the same failure without invoking the check. A success invokes the check once and interprets its
unit result after the callback returns. A successful check returns the exact successful value,
including a runtime-null reference; a failed check returns a new failure containing that check's
error, including a null error.

Callback exceptions propagate unchanged. A check that returns an uninitialized `UnitResult` is a
programming error and throws `InvalidOperationException`. A default receiver throws
`InvalidOperationException`; a null delegate throws `ArgumentNullException` before the receiver is
inspected.

## CheckAll Semantics

`CheckAll` is for independent rules over one input. Its behavior is explicit:

- The rule sequence is snapshotted exactly once after the input is found active. Later mutation of
  the source list does not change the rule set used by that call.
- A null rule element is rejected with `ArgumentException` before any check is invoked. Source
  enumeration exceptions also prevent every check callback.
- An already-failed result or an invalid validation does not enumerate, validate, or invoke the
  rules. The existing failure or error snapshot is preserved.
- Empty rules return the input value: a valid validation for an active input, or the existing
  invalid/failed state for an inactive input. No callback runs.
- Every snapshot rule runs exactly once in order, even after an earlier typed failure. A failed
  rule contributes its error; a successful rule contributes none.
- Errors are ordered by rule position. Duplicate rules run once per position and their duplicate
  errors are preserved. Null errors are preserved.
- Callback exceptions propagate unchanged and stop later rules; no partial validation is returned.
  An uninitialized `UnitResult` from a rule is likewise an `InvalidOperationException`.
- Repeated calls run the rule set again; results are not memoized. The returned validation can be
  inspected repeatedly without rerunning checks.
- Reference-type inputs are passed as the same object to every rule. A rule that mutates the input
  can affect later rules; the library does not clone or isolate user state.
- `CheckAll` never feeds one rule's success into the next rule and never adds a `Bind` or
  `SelectMany` to `Validation`. A dependent rule must use `Check`, `Bind`, or ordinary control
  flow; evaluating it as an independent accumulating rule would report failures that an earlier
  failure should have prevented.

`CheckAll` also accepts a valid `Validation<TValue, TError>` and returns the same carrier. This lets
existing decoding or field validation feed an ordered business-rule layer without adding a
monadic bind to `Validation`.

## Asynchronous Checks

The async forms observe the normal `Task` and `ValueTask` contracts:

- A delegate is guarded synchronously even when the receiver has already failed.
- An inactive receiver returns an already-completed failure or invalid validation without invoking
  a callback or inspecting the token.
- An active receiver invokes each reached callback once and observes each returned awaitable once.
  `ValueTask` keeps its single-consumption rule.
- A cancellation-aware overload forwards the exact supplied token to every reached callback. It
  does not cancel eagerly when the token is already canceled and does not inspect the token
  independently.
- `CheckAllAsync` and `CheckAllValueAsync` run rules sequentially in rule order. The next rule
  starts only after the previous check's awaitable completes. A typed failure is accumulated and
  does not stop the next rule.
- A callback fault or cancellation stops the sequence immediately; later rules do not run and no
  partial validation is returned. A canceled awaitable produces cancellation with its token. A
  faulted awaitable remains faulted, including a faulted `OperationCanceledException`; faults and
  cancellation are never converted to typed carrier failures.
- A synchronous callback exception is represented by the returned awaitable rather than thrown to
  the caller. A null returned `Task` faults the returned operation with `NullReferenceException`.
  An uninitialized check result faults it with `InvalidOperationException`.
- Rule source materialization is eager: null rules, null source, and enumeration faults are
  synchronous argument errors before the returned awaitable exists.
- The operations never block on asynchronous work. A pending check leaves the returned awaitable
  pending.

Asynchronous rule sets are not parallel. Independent here means logically independent over the same
input, not concurrent execution. The package's explicit parallel operators remain the separate
choice for bounded concurrency.

## Costs

`Check` is a value-preserving guard over existing structs and does not allocate for a synchronous
success. `CheckAll` snapshots the rule sequence into an array so callbacks cannot observe later
mutation, and creates one list for errors only after a rule fails. The returned validation owns a
private read-only error snapshot. Async forms add the normal task and state-machine costs; a rule set
that completes synchronously does not allocate a task for the result of `CheckAllValueAsync`.

The [evidence record](check-evidence.md) compares complete fail-fast and accumulating consumers
against `Bind` plus `UnitResult.ToResult` and existing `Validation.Zip` composition, including
allocation and delegate costs.

## Deliberate Boundaries

Checks keep the existing carriers and caller-defined errors; no result wrapper, check interface,
rule hierarchy, or second absence/failure carrier is added. Rule storage remains BCL delegates and
`IEnumerable<T>`. `Check` is not a predicate alias, `CheckAll` does not add a monadic `Bind` to
`Validation`, and neither operation performs dependent sequencing. There is no parallel rule
execution, retry, compensation, transaction, caching, memoization, exception conversion, schema
introspection, or domain construction. The operations do not change the thin effect boundary or add
a runtime dependency.
