# One-Pass Sequence Summaries

`FunnySharp` lets a consumer define count, sum, extrema, and domain-specific summaries
independently, compose them into one typed result, and evaluate the composition over a BCL sequence
with one traversal. The same summary definition also evaluates an asynchronous source, so only
input acquisition changes. Compiling examples are in
[SummaryExamples.cs](../examples/FunnySharp.Examples/SummaryExamples.cs); the reference consumers,
consumption ledger, MapAccum assessment, and measured costs are in
[one-pass-summaries-evidence.md](one-pass-summaries-evidence.md).

## API Shape

A summary is a reusable description, not a carrier. A leaf summary holds a seed or seed factory, an
accumulate delegate, and a result delegate; `Zip`/`MapResult` build composed summaries from
independent component descriptions. Evaluation state is local to one evaluation.

| Member | Source | Result |
| --- | --- | --- |
| `Summary.Create<TSource, TState>(seed, accumulate)` | `IEnumerable<TSource>` / `IAsyncEnumerable<TSource>` | `Summary<TSource, TState, TState>` |
| `Summary.Create<TSource, TState>(seedFactory, accumulate)` | same | `Summary<TSource, TState, TState>` |
| `Summary.Create<TSource, TState, TResult>(seed, accumulate, result)` | same | `Summary<TSource, TState, TResult>` |
| `Summary.Create<TSource, TState, TResult>(seedFactory, accumulate, result)` | same | `Summary<TSource, TState, TResult>` |
| `Summary<TSource>.Count()` | same | `Summary<TSource, int, int>` |
| `Summary<TSource>.Sum(selector)` | same | `Summary<TSource, TNumber, TNumber>` |
| `Summary<TSource>.Min(selector[, comparer])` | same | `Summary<TSource, Option<TKey>, Option<TKey>>` |
| `Summary<TSource>.Max(selector[, comparer])` | same | `Summary<TSource, Option<TKey>, Option<TKey>>` |
| `Summary.Zip(first, second[, third[, fourth]])` | same | summary of a flat tuple state and result |
| `summary.MapResult(selector)` | same | summary with a projected result |
| `summary.Evaluate(source)` | `IEnumerable<TSource>` | `TResult` |
| `summary.EvaluateAsync(source, cancellationToken)` | `IAsyncEnumerable<TSource>` | `ValueTask<TResult>` |

`Summary` and `Summary<TSource>` are static factory classes. `Summary<TSource, TState, TResult>` is
the reusable description. The state type is explicit so composition can keep every component's
accumulator separate and so measurement can report the exact state size.

## Defining A Summary

`Summary.Create` defines a summary without a new type or interface:

<!-- documentation-sample: DocumentationSamples.OnePassSummaries.DefineLeafSummaries -->
```csharp
var firstSku = Summary.Create<OrderLine, Option<string>>(
    Option<string>.None,
    static (first, line) => first.IsSome ? first : Option.Some(line.Sku));

var categories = Summary.Create<OrderLine, Dictionary<string, int>, IReadOnlyList<CategoryCount>>(
    static () => new Dictionary<string, int>(StringComparer.Ordinal),
    static (counts, line) =>
    {
        counts.TryGetValue(line.Category, out var count);
        counts[line.Category] = count + 1;
        return counts;
    },
    static counts => counts
        .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
        .Select(static pair => new CategoryCount(pair.Key, pair.Value))
        .ToArray());
```

The seed factory is invoked once per component per evaluation. A value seed is assigned to the
evaluation-local state as-is; it is never cloned, snapshotted, or rolled back.

## Composition

`Summary.Zip` composes two to four summaries. It returns a summary whose state and result are flat
tuples in operand order, so the canonical four-summary case does not produce nested tuples.
`MapResult` projects the final tuple once after traversal.

<!-- documentation-sample: DocumentationSamples.OnePassSummaries.ComposeReport -->
```csharp
var totals = Summary.Zip(
        Summary<OrderLine>.Count(),
        Summary<OrderLine>.Sum(static line => line.Amount),
        Summary<OrderLine>.Min(static line => line.UnitPrice),
        Summary<OrderLine>.Max(static line => line.UnitPrice))
    .MapResult(static results => new OrderTotals(
        results.First,
        results.Second,
        results.Third,
        results.Fourth));

var summary = Summary.Zip(totals, categories, firstSku)
    .MapResult(static results => new OrderReport(
        results.First.Count,
        results.First.Total,
        results.First.Cheapest,
        results.First.MostExpensive,
        results.Second,
        results.Third));

return summary;
```

Components initialize, accumulate, and finalize in operand order. Each component has its own typed
state; the composition never asks the caller to write one monolithic accumulator. Components are
independent: one component's result does not gate another component's accumulation.

`Zip` is bounded at four operands, matching the carrier `Zip` surface. More components remain
expressible by composing a `Zip` result with additional summaries, as the example does, or by
defining a domain-specific summary.

### Reusable Definitions

Inside a method, `var` keeps the composed state type local. A reusable field or method signature
cannot use `var`, so name the state with a file-scope alias when the definition lives beside other
code:

<!-- documentation-sample: DocumentationSamples.OnePassSummaries.ReportStateAlias -->
```csharp
using ReportState = (
    (int First, decimal Second, FunnySharp.Option<decimal> Third, FunnySharp.Option<decimal> Fourth) First,
    System.Collections.Generic.Dictionary<string, int> Second,
    FunnySharp.Option<string> Third);
```

The example stores the composed definition in a field declared as
`Summary<OrderLine, ReportState, OrderReport> ReusableReport = ComposeReport();`. The
`ComposeReport` helper composes the four totals with the category and first-SKU definitions and
returns the composed summary. A method that returns `Func<IEnumerable<OrderLine>,
OrderReport>` can instead return `summary.Evaluate`; the state tuple then never appears in the
caller's declaration.

## Evaluation

`Evaluate` is eager and enumerates the source exactly once in source order. It creates each
component's local state, invokes each reached component's accumulate logic once per item, and
returns the projected result. Nothing is materialized, buffered, cached, or deferred.

`EvaluateAsync` runs the same synchronous calculation over an `IAsyncEnumerable<TSource>`. Only
input acquisition is asynchronous. The exact caller token is forwarded to `GetAsyncEnumerator`;
the accumulate delegates are synchronous and do not observe the token. Each item is processed before
the next `MoveNextAsync`; there is no prefetching, parallelism, blocking, or async accumulate
overload.

The same summary instance can be evaluated synchronously and asynchronously:

<!-- documentation-sample: DocumentationSamples.OnePassSummaries.ReuseSyncAndAsync -->
```csharp
var synchronous = summary.Evaluate(lines);
var asynchronous = await summary.EvaluateAsync(ToAsync(lines), cancellationToken);
```

## Edge Semantics

### Empty input and empty extrema

Empty input returns each component's result computed from its seed without invoking an accumulate
delegate. `Count` is `0`, `Sum` is `TNumber.Zero`, and `Min`/`Max` are `None`. A custom summary
returns whatever its result selector produces from the seed. `None` therefore means exactly "no
item" or "no non-null key" for an extrema; it is not an error.

### Numeric overflow and order

`Count` and `Sum` use `checked` arithmetic, matching BCL `Count`/`Sum` overflow behavior. An
integral or `decimal` overflow throws `OverflowException` during evaluation; it is never converted
to a result value. `Sum` adds strictly in source order and never reassociates, vectorizes, or
evaluates in parallel. Floating-point and `decimal` results therefore follow their actual
sequential machine semantics, not mathematical associativity.

### Ordering, ties, and null keys

The source order is preserved. `Min`/`Max` skip a null selected key before invoking the comparer,
so the comparer never observes a null key. Empty or all-null-key input is `None`. When two keys
compare equal, the first encountered key is retained.

### Seeds, mutable state, and isolation

A value seed is assigned to the evaluation-local state exactly once. If it is a mutable reference,
the state aliases that object: mutations are visible to later items in the same evaluation, to later
evaluations of the same definition, and to the caller. The library does not clone or roll it back.

A seed factory is the isolation form. It runs once per component per evaluation, so two evaluations
and two components in the same `Zip` receive separate state objects when the factory creates new
ones. A factory that returns the same object each time deliberately shares it.

<!-- documentation-sample: DocumentationSamples.OnePassSummaries.FactoryIsolation -->
```csharp
var visits = Summary.Create<OrderLine, List<string>>(
    static () => [],
    static (seen, line) =>
    {
        seen.Add(line.Sku);
        return seen;
    });

var (firstVisit, secondVisit) = Summary.Zip(visits, visits).Evaluate(lines);
// firstVisit and secondVisit are separate lists.
```

A result selector may return the mutable state itself. The summary does not copy it; ownership of
the returned object stays with the caller.

### Exceptions and disposal

Source, seed-factory, accumulate, result, selector, comparator, and `MapResult` exceptions propagate
unchanged. A seed-factory exception occurs before source enumeration; in `EvaluateAsync` it faults
the returned operation. No exception becomes a result value. The acquired enumerator is disposed exactly once
after success or failure. Normal C#/async precedence applies: a disposal exception can replace a
pending outcome. A component exception stops the later components for that item and the final
result projection; earlier caller-owned mutations are not rolled back.

### Cancellation

`EvaluateAsync` forwards the exact token to the asynchronous enumerator. It does not inspect the
token eagerly, so a source that ignores an already-canceled token can still complete. A canceled
source or `OperationCanceledException` leaves the returned operation canceled; a non-cancellation
fault leaves it faulted. Neither becomes a summary result. The token is not passed to synchronous
accumulate delegates because they receive no token by design.

### Early-determined components

Summaries are total folds. Every component observes every source item. A component whose result is
already determined (for example, a "first SKU" summary) still receives the remaining items, so
composing it cannot stop a source that another required component still needs. There is no
completion predicate, partial result, or skip protocol. Use `FirstOrNone`, `AggregateResult`, or
ordinary control flow when stopping the source is the goal.

### Reuse and repeated evaluation

A summary definition is immutable and reusable. Each `Evaluate`/`EvaluateAsync` call creates fresh
evaluation state (or factory results) and enumerates the source again. The library keeps no cache
or memoization. Concurrent evaluations are isolated only when the seed factory returns fresh state;
the library adds no locks, cloning, or snapshotting.

## Choosing The Operation

| Need | Operation |
| --- | --- |
| Combine independent summaries in one traversal | `Summary` + `Zip` + `Evaluate`/`EvaluateAsync` |
| One fallible fold that stops at the first typed failure | `AggregateResult` / async-source forms |
| Running accumulator after each item | `Scan` / `ScanValueAsync` |
| One final non-fallible fold | BCL `Aggregate`, `NonEmpty<T>.Aggregate` |
| Collect every successful value or independent error | `Sequence`/`Traverse` or `Validation` traversal |
| Cardinality or one extrema only | `Count`, `Sum`, `MinOrNone`, `MaxOrNone` |
| Exactly one keyed match per input row, with structured failures | [`JoinExactlyOne`](cardinality-joins.md) |

A summary is a good fit when several independent calculations share an expensive, single-pass, or
asynchronous source. A single summary over a cheap in-memory array is usually clearer as the
corresponding BCL/`*OrNone` call. MapAccum-style state-plus-output transformation is not a summary
operation; the assessment against `Scan` plus `Select` is recorded in the
[evidence](one-pass-summaries-evidence.md#mapaccum-assessment).

## Costs

- A leaf summary evaluates with a local state and no library-owned allocation beyond the source
  enumerator and the result object. Built-in `Count`, `Sum`, `Min`, and `Max` plans avoid an
  accumulate delegate call; a custom `Create` summary pays one delegate call per item.
- A composed `Zip` creates one small evaluation object per component and per `Zip` node for each
  evaluation, so its allocation is constant in the source length. It does not allocate per item;
  `MapResult` wraps the inner result without adding a per-item allocation. State size is the
  declared tuple size, but the evaluator threads each component's state separately rather than
  copying the whole tuple per item.
- Delegate, virtual-call, selector, and comparer costs remain visible. A hand-written single loop
  is the lower bound for a cheap in-memory source; one-pass composition pays dispatch cost in
  exchange for a single traversal and independent definitions. Separate summary evaluation avoids
  dispatch composition but enumerates the source once per component.
- Value-type state and source items are copied through delegate boundaries. A large mutable or
  reference state avoids that copy but owns its storage and lifetime.

Measured state sizes, allocation, delegate overhead, large-value copying, and combined versus
separate versus hand-written rows are in
[one-pass-summaries-evidence.md](one-pass-summaries-evidence.md#measurements).

## Deliberate Boundaries

A summary adds no carrier, absence channel, or failure channel; the result is the caller's
`TResult` and exceptions remain exceptions. It adds no public typeclass, interface, or
higher-kinded hierarchy, no automatic parallel evaluation, no replacement LINQ implementation, and
no compile-time fusion or expression-tree analysis. It does not materialize, buffer, cache, retry,
memoize, or replay a source, and it provides no early-exit or partial-result protocol. It does not
clone, isolate, snapshot, or roll back caller-owned mutable state, and it makes no thread-safety
claim for a shared seed object. It does not convert cancellation or faults into results, and it
provides no async accumulate callback or `Task`-step overload; `EvaluateAsync` changes only input
acquisition. General fallible composition remains `AggregateResult`, and the core package gains no
runtime dependency.
