# Reusable Check Consumer Evidence

This record supports the maintainer's judgment for
[Goal 0032](goals/archive/0032-goal.md): whether a reusable fallible check can serve a
value-preserving fail-fast workflow and an accumulating independent-rule workflow without manual
unit-payload discarding, value reconstruction, or unused tuples, and whether the result is an
independent semantic operation rather than an alias.

Source identity is the Goal 0032 working tree on top of commit `4c8b1e6`; the landing commit
becomes the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure, 4 logical cores.
- .NET SDK 10.0.401, runtime 10.0.12, x64; Release build.
- Date: 2026-11-07 (UTC).

Allocation is the blocking comparison; timing is directional on this shared host.

## Compared Consumers

Both consumers run the same three business checks over the same order fixtures. The check
definitions, fixtures, and comparison harness are shared and are not charged to either consumer.

| Version | File | Raw lines | Shape |
| --- | --- | ---: | --- |
| Ordinary idiom | [OrdinaryCheckConsumer.cs](../tests/Shared/CheckComparison/OrdinaryCheckConsumer.cs) | 120 | `Result.Bind` plus `UnitResult.ToResult(() => order)` for fail-fast; awaited checks plus `Validation.Zip` with `Pass` adapters and unused-pair projections for accumulation. |
| FunnySharp checks | [FunnySharpCheckConsumer.cs](../tests/Shared/CheckComparison/FunnySharpCheckConsumer.cs) | 43 | `Result.Check` / `CheckAsync` / `CheckValueAsync` for fail-fast; `Result.CheckAll` / `CheckAllAsync` / `CheckAllValueAsync` for accumulation; no value factory, adapter, or unused tuple. |

Shared definitions are
[CheckRules.cs](../tests/Shared/CheckComparison/CheckRules.cs) (67 lines),
[CheckFixtures.cs](../tests/Shared/CheckComparison/CheckFixtures.cs) (48 lines), and the comparison
harness [CheckComparison.cs](../tests/Shared/CheckComparison/CheckComparison.cs) (275 lines).
[CheckModels.cs](../tests/Shared/CheckComparison/CheckModels.cs) adds 21 lines for the order/error
records and callback log. The consumer-specific reduction is 77 raw lines (about 64%) even before
charging the one-time `CheckExtensions` implementation. Raw lines include braces and comments; this
is not a LOC target.

The ordinary consumer uses the same `CheckRuleSet` delegates in both workflows, so the comparison
isolates the carrier composition, not differently written business logic.

## Equivalence

`CheckComparison.Verify` and `CheckComparison.VerifyAsync` run six fixtures through both consumers:

`valid`, `out-of-stock`, `permission-denied`, `fraud-suspected`, `multiple-failures`, and
`already-failed`.

The comparison asserts:

- structural equality of the complete `Result<CheckOrder, CheckError>` or
  `Validation<CheckOrder, CheckError>` outcome;
- reference identity of the retained `CheckOrder` on every successful outcome;
- exact error order for accumulation; and
- the complete callback log for every fixture, for both fail-fast and accumulation, sync and async.

`CheckComparison` is called by
[CheckConsumerComparisonTests.cs](../tests/FunnySharp.Tests/CheckConsumerComparisonTests.cs) and by
the measurement harness; a disagreement fails the test run and the measurement run.

The comparison covers the important distinctions:

| Fixture | Fail-fast | Accumulation |
| --- | --- | --- |
| `valid` | success, same order, callbacks `stock, permission, fraud` | valid, same order, callbacks `stock, permission, fraud` |
| `out-of-stock` | `out-of-stock` with `SKU-BOOK|9`, callback `stock` | invalid with only that error, callbacks `stock, permission, fraud` |
| `permission-denied` | `permission-denied` with `BLOCKED`, callbacks `stock, permission` | invalid with only that error, all three callbacks |
| `fraud-suspected` | `fraud-suspected` with `tok-blocked`, all three callbacks | invalid with only that error, all three callbacks |
| `multiple-failures` | first error `out-of-stock`, callback `stock` | three errors in order: `out-of-stock`, `permission-denied`, `fraud-suspected`; all three callbacks |
| `already-failed` | existing error, no callbacks | existing error only, no callbacks |
| empty rules | — | valid, same order, no callbacks |

The sync, `Task`, and `ValueTask` accumulation paths all produce the same error lists. The
`Task`/`ValueTask` fail-fast paths produce the same single failure. Dedicated tests pin the token,
cancellation, fault, pending-operation, and `ValueTask` single-consumption behavior.

## Callback Ledger

The following behavior is directly asserted in
[CheckTests.cs](../tests/FunnySharp.Tests/CheckTests.cs),
[CheckAsyncTests.cs](../tests/FunnySharp.Tests/CheckAsyncTests.cs), and
[CheckAllAsyncTests.cs](../tests/FunnySharp.Tests/CheckAllAsyncTests.cs):

| Scenario | Observed callback behavior |
| --- | --- |
| successful single `Check` | one call, exact input value |
| failed receiver | zero calls, same failure object |
| successful `CheckAll` | every rule once in order |
| accumulating typed failures | all rules still run; errors in rule order |
| empty `CheckAll` | zero calls, value retained |
| already-failed input | zero calls and zero rule-source enumeration |
| null rule element | rejected before any callback |
| rule-source enumeration fault | rejected before any callback |
| callback exception | stops later rules and propagates unchanged |
| uninitialized check result | throws or faults, as appropriate, and stops later rules |
| duplicate rule | invoked once per position; duplicate errors preserved |
| repeated `CheckAll` calls | each rule runs again; no caching |
| async accumulation | rule N+1 starts only after rule N completes |
| async fault or cancellation | later rules do not start; no partial validation is returned |
| async callback cancellation | returned operation is canceled with the original token |
| async faulted `OperationCanceledException` | returned operation remains faulted |
| already-canceled token | forwarded unchanged; not inspected eagerly |
| pending callback | returned operation stays pending; no blocking |

The out-of-stock fixture exercises the same order in both workflows. Fail-fast logs
`stock` and stops; accumulation logs `stock, permission, fraud` and reports independent errors. That
is the intended distinction: dependent rules use `Check`, `Bind`, or ordinary control flow, while
`CheckAll` is only for rules that are genuinely independent over one original value.

## Measurements

Command:

```bash
dotnet run --project tests/FunnySharp.CheckMeasurements/FunnySharp.CheckMeasurements.csproj -c Release
```

The harness first runs the complete sync and async comparison, then measures precreated check
delegates. Synchronous allocation uses `GC.GetAllocatedBytesForCurrentThread`; asynchronous
allocation uses `GC.GetTotalAllocatedBytes(precise: true)`. Timing is the minimum of three rounds.

| Operation | Allocated B/op | Time ns/op |
| --- | ---: | ---: |
| Fail-fast, current `Bind` + `UnitResult.ToResult`, all pass | 496.0 | 377.6 |
| Fail-fast, `Check`, all pass | 0.0 | 175.8 |
| Fail-fast, current `Bind` + `UnitResult.ToResult`, first fails | 400.0 | 334.7 |
| Fail-fast, `Check`, first fails | 104.0 | 158.8 |
| Accumulate, current `Validation.Zip`, all pass | 255.5 | 56.4 |
| Accumulate, `CheckAll`, all pass | 48.0 | 337.3 |
| Accumulate, current `Validation.Zip`, all fail | 712.0 | 241.8 |
| Accumulate, `CheckAll`, all fail | 304.0 | 266.7 |
| Accumulate, current `Validation.Zip`, empty rules | 0.0 | 16.5 |
| Accumulate, `CheckAll`, empty rules | 0.0 | 37.4 |
| Accumulate, current `Validation.Zip`, already failed | 56.0 | 46.2 |
| Accumulate, `CheckAll`, already failed | 56.0 | 37.7 |
| Rule array with three reused delegates | 48.0 | 14.9 |
| Three-rule delegate snapshot | 240.0 | 45.6 |

| Async operation | Allocated B/op | Time ns/op |
| --- | ---: | ---: |
| Fail-fast, current `BindAsync` + awaited `UnitResult.ToResult`, all pass | 1261.7 | 336.5 |
| Fail-fast, `CheckAsync`, all pass | 592.0 | 432.4 |
| Accumulate, current await + `Validation.Zip`, all pass | 400.0 | 482.1 |
| Accumulate, `CheckAllAsync`, all pass | 376.0 | 814.3 |
| Accumulate, value-task current await + `Validation.Zip`, all pass | 72.0 | 683.6 |
| Accumulate, `CheckAllValueAsync`, all pass | 48.0 | 631.7 |
| Accumulate, current await + `Validation.Zip`, empty rules | 88.0 | 169.5 |
| Accumulate, `CheckAllAsync`, empty rules | 88.0 | 289.8 |
| Accumulate, value-task current await + `Validation.Zip`, empty rules | 0.0 | 172.4 |
| Accumulate, `CheckAllValueAsync`, empty rules | 0.0 | 242.3 |
| Accumulate, current await + `Validation.Zip`, already failed | 144.0 | 178.2 |
| Accumulate, `CheckAllAsync`, already failed | 144.0 | 139.3 |
| Accumulate, current await + `Validation.Zip`, one pending rule | 664.0 | 944.3 |
| Accumulate, `CheckAllAsync`, one pending rule | 744.0 | 648.3 |
| Accumulate, value-task current await + `Validation.Zip`, one pending rule | 544.0 | 1234.9 |
| Accumulate, `CheckAllValueAsync`, one pending rule | 584.0 | 831.4 |

The failure rows include the `CheckError` records and interpolated detail strings constructed by
the failing business rules; the all-pass rows do not. Repeated runs varied by up to roughly 25% on
the current-idiom rows that allocate adapter closures and factories; the row shown is one
representative run. `Check` on a successful path is allocation-free. `CheckAll` snapshots a
three-rule sequence into one array (measured separately at 48 B for an array of three reused
delegates; creating the three method-group delegates as well costs 240 B). It creates error storage
only after a rule fails. The async accumulation rows allocate 24 B less than the current composition
on the all-pass paths (376 B against 400 B for `Task`, 48 B against 72 B for `ValueTask`) and the
same on both empty-rule paths (88 B for `Task`, 0 B for `ValueTask`). No universal allocation win is
claimed: the observed benefit is the smaller consumer and the absence of value reconstruction for
fail-fast checks.

Every rule in the default fixtures completes synchronously, so the rows above never enter the
pending continuation state. The four `one pending rule` rows exist to disclose that path: the first
rule returns an incomplete awaitable that the fixture completes after the call. There the
accumulation overloads allocate a state object, a `TaskCompletionSource`, its task, and one
continuation task per pending rule, measured at 744 B against 664 B for the awaited composition
(`Task`) and 584 B against 544 B for the awaited composition (`ValueTask`). That is the cost of
resuming the ordered rule loop without blocking or boxing the remaining rules; a rule set that
completes synchronously does not pay it.

The value-task paths consume each supplied `ValueTask` once. The dedicated tests use
[CountingValueTaskSource.cs](../tests/FunnySharp.Tests/CountingValueTaskSource.cs) to assert the
single `GetResult` consumption for both completed and pending check results.

Timing is directional. The repository's performance budgets and benchmark receipts remain the
blocking performance gate; this goal did not start the heavy benchmark suite and makes no new
timing claim. The same table does show a real per-call CPU cost on the accumulating success path:
`CheckAll` measured 337.3 ns/op against 56.4 ns/op for the `Validation.Zip` composition in the
Release run recorded above; repeated runs on this host varied in both directions but stayed at
roughly 5 to 6 times, well above the run-to-run variance. The all-fail path is within noise (266.7
against 241.8). That gap comes from the rule snapshot plus the carrier plumbing, not from extra
business work. `CheckAll` is chosen for the value-preserving API shape -- the same input value or
every ordered error without adapters or unused pairs -- not for speed; a caller whose accumulating
validation is throughput-bound should keep `Validation.Zip`.

## Semantic Independence

The existing operations have different shapes and guarantees:

- `Ensure` accepts a Boolean predicate and an already-built error. It cannot run a fallible
  operation, construct the error lazily from business state, or perform asynchronous work.
- `Bind` accepts a function that returns a carrier with a possibly different successful value
  type. Retaining the input requires an adapter such as
  `Bind(value => check(value).ToResult(() => value))`, which discards the unit payload and
  reconstructs the original value through a caller factory.
- The external `CSharpFunctionalExtensions.Check` family is a Bind-like operation over
  `Result`/`UnitResult`; it can change or return a different value. FunnySharp's `Check` accepts a
  `Func<TValue, UnitResult<TError>>` and guarantees the original `TValue` on success. The names are
  not the acceptance condition; the signature and retained-value contract are.
- `CheckAll` accepts only rules that receive the same original input. It cannot chain one rule's
  output into another, and it does not add a monadic `Bind` or `SelectMany` to `Validation`. The
  current composition needs a per-rule `UnitResult`-to-`Validation` adapter and unused pair
  projections; `CheckAll` removes those adapters without pretending dependent rules are
  independent.

`Check` can be expressed as `Bind` plus `UnitResult.ToResult`, and `Apply` is similarly expressible
through `Zip` and `Map`; the question is whether the public operation has an independent contract.
Here it does: no existing member accepts a no-value check and promises the exact input value on
success, and `CheckAll` adds the ordered independent-rule policy without changing `Validation`'s
deliberate no-`Bind` boundary. The comparison and callback ledger above disclose the derivation and
the remaining cost rather than claiming a new runtime primitive.

## Guarantees Kept Separate

| Fact | Where it is established | What it does not establish |
| --- | --- | --- |
| A check succeeded for the supplied value | `Check` / `CheckAll` | That the business state is still true later, or that an external command ran once. |
| The successful input value is retained | `Check` result identity and `CheckAll` valid value identity | That a mutable reference input was not changed by a check. |
| Every independent rule ran | `CheckAll` callback ledger | That dependent rules are safe to run independently. |
| Errors are in rule order | `CheckAll` validation order | That errors can be deduplicated or merged by business identity. |
| Asynchronous faults and cancellation keep their status | Async check tests | That later operations or external systems were rolled back. |

## Deliberate Non-Guarantees

The evidence does not claim:

- parallel rule execution, bounded or unbounded; rule sets are sequential;
- memoization, caching, or exactly-once execution across processes or retries;
- transaction, compensation, rollback, or idempotency behavior;
- cloning, isolation, or immutability for reference-type inputs;
- conversion of exceptions into typed carrier failures;
- retry, scheduling, or timeout policy inside a check;
- schema introspection, domain construction, serialization, or wire-format changes;
- a new outcome carrier, async wrapper, rule hierarchy, or `Validation.Bind`;
- a general benchmark or timing claim from the measurement harness.

## Verification

```bash
dotnet build examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release
dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release --no-build
dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -c Release
cd tests/FunnySharp.Tests/bin/Release/net10.0
DOTNET_ROOT=/home/azureuser/.dotnet ./FunnySharp.Tests -class "FunnySharp.Tests.Check*" -noLogo -reporter quiet
```

The focused run reports 49 passing tests, 0 failed, 0 skipped across `CheckTests`,
`CheckAsyncTests`, `CheckAllAsyncTests`, and `CheckConsumerComparisonTests`. The complete
`FunnySharp.Tests` run reports 2,006 passing tests, 0 failed, 0 skipped. `dotnet fsi build.fsx --
-p verify-docs-snippets` verifies the 60 existing primary-guide snippets, and `verify-api-baseline`
reports 93 types and 854 members across the two shipping assemblies. The example program prints
`FunnySharp examples passed.` The measurement harness first runs both comparison passes and then
prints the tables above. The table is one representative run. Allocation rows for the ordinary adapter consumers varied
across runs as disclosed above; timing remains directional.
