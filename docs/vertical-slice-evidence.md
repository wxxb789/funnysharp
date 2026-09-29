# Vertical Slice: Evidence Record

Goal 22 (`docs/goals/archive/0022-goal.md`, line 2) asks for a realistic ASP.NET Core vertical slice that
consumes the produced packages through their public APIs, plus the evidence that it behaves as
claimed. This record maps every clause of that contract to an artifact and to the command that
verifies it. The comparison narrative lives in
[vertical-slice-call-sites.md](vertical-slice-call-sites.md).

## Reproduce

```shell
# whole evidence set: pack, isolated consumer restore/build/test, both apps, measurements
dotnet fsi build.fsx -- -p vertical-slice --output artifacts/vertical-slice/consumer-run

# just the consumer bundle against an existing feed
dotnet fsi build.fsx -- -p vertical-slice --no-pack --feed artifacts/vertical-slice/feed
```

The tool packs the solution, then restores, builds, and tests every consumer project against the
packed `.nupkg` files with an isolated `NUGET_PACKAGES` directory and the local feed listed before any
remote feed. It writes `vertical-slice-results.json` (package hashes, versions, per-step exit codes,
test summary) and `measurements.json` next to its output, and exits non-zero on any failed step or on
any behavioral difference between the two applications.

## Clause To Artifact

| Contract clause | Artifact | Verified by |
| --- | --- | --- |
| Package-only consumption | `FunnySharp.VerticalSlice.Api` references `FunnySharp` / `FunnySharp.AspNetCore` `PackageReference`s only | `restore-api` + `build-api` in the receipt; the Api project has no `ProjectReference` to `src/` (the Tests and Measurements projects reference their sibling consumer projects) |
| Compiled examples | `examples/FunnySharp.Examples`, `examples/FunnySharp.AspNetCore.Examples`, `examples/FunnySharp.DocumentationSamples` (all three are projects of `FunnySharp.slnx`) | `build` and `verify-docs-snippets` steps: the solution build compiles every example project with zero `FS####` diagnostics, and the snippet verifier byte-compares the ten primary guides against their `documentation-sample` regions |
| External-input validation | `Http/OrderRequestValidation.cs`; `Validation<Command, InputError>` per request | `ValidationTests` (field keys in input order, seven cases including a null line element) |
| Refined domain values | `Domain/OrderId.cs`, `CustomerId.cs`, `Sku.cs`, `Quantity.cs`, `Money.cs`, `TrackingCode.cs` | `OrderLifecycleTests`, `ValidationTests`; every factory returns a carrier |
| Value-producing outcomes | `Result<Order, OrderError>` from placement, payment, shipment, timeline | `OrderEndpointTests`, `ProblemMappingTests`, `PaymentGuardTests` |
| No-value outcomes | `UnitResult<OrderError>` from cancellation, mapped to `204` | `OrderEndpointTests.CancellationReturnsNoContentAndTheOrderReadsAsCancelled` |
| Asynchronous persistence / I/O | `IOrderStore`, `IInventoryService`, `IPaymentGateway`, `ISupplierGateway` ports with async adapters | every endpoint test; `TheStoreRejectsAStaleRevisionInsteadOfOverwritingIt` |
| Request cancellation | endpoints pass the request token; `Effect` overloads use exactly `context.RequestAborted` | `CancellationTests` (token identity across dependencies, honoring cancellation), `StreamingExportTests.AClientDisconnectStopsTheStoreEnumeration` on real Kestrel |
| Bounded parallel work | `SupplierQuoting.BestAsync`, `SourceAsync`, reconcile traversal | `BoundedParallelismTests` (cap observed, admission refills, deterministic tie-break) |
| Streaming where beneficial | `OrderExport.RowsAsync` (NDJSON + running aggregate), `SupplierQuoting.StreamAsync` | `StreamingExportTests`, `BoundedParallelismTests.TheStreamingQuoteEndpointDeliversEachAnswerAsItArrives` |
| Pure state decisions, separately interpreted commands | `Domain/OrderLifecycle.cs` (machine), `Application/OrderCommandExecutor.cs` (interpreter) | `OrderLifecycleTests` asserts emitted commands and replay; endpoints never execute inside a transition |
| Explicit HTTP error mapping | `Http/ProblemMappings.cs`, `Http/UnexpectedExceptionHandler.cs` | `ProblemMappingTests` (404/402/409/503/500 cases with extensions), `UnexpectedExceptionTests` |
| ProblemDetails / typed results / OpenAPI fidelity | `.Produces<T>` / `.ProducesProblem` metadata on every route | `OpenApiTests` (ten paths, per-status schemas, no handler or domain type in the contract) |
| No HTTP types in the domain | `Domain/` and `Application/` have no ASP.NET reference | `OpenApiTests.TheContractCarriesNoHandlerOrDomainTypes`; domain files import only `FunnySharp` |
| No swallowed unexpected exceptions | one catch-all that logs the exception object and returns a generic 500 | `UnexpectedExceptionTests` asserts the logged exception identity and the generic problem type |
| No global service location | dependencies arrive as handler parameters or an explicit environment value | endpoint signatures; the composition root is the only place that registers services |
| No FunnySharp runtime requirement | domain code uses `FunnySharp` types only, never a host or scheduler | the comparison app runs the same scenarios with no FunnySharp reference at all |
| Idiomatic-C# comparison | `FunnySharp.VerticalSlice.Baseline` | `measurements` step: identical status and payload for all ten scenarios |
| End-to-end measurements | `measurements.json` (allocation, latency, throughput per scenario, both apps) | `measurements` step; table reproduced in the comparison guide |

## Baseline Receipt

From `artifacts/vertical-slice/consumer-run/vertical-slice-results.json`, produced by
`eng/harness/VerticalSlice.fs` (`dotnet fsi build.fsx -- -p vertical-slice`) at commit `7e61a59`
plus the Goal 22 closure edits (documentation, this record, and the harness's failing-test field):

- The pipeline printed `vertical-slice consumer verification: PASS` and exited 0
- Steps, all exit code 0: `pack`, `restore-api`, `restore-tests`, `restore-baseline`,
  `restore-measurements`, `build-api`, `build-tests`, `build-baseline`, `build-measurements`,
  `api-verify`, `baseline-verify`, `measurements-verify`, `measurements`, `consumer-tests`
- Packages produced by that run: `FunnySharp.0.1.0.nupkg`,
  `FunnySharp.AspNetCore.0.1.0.nupkg` (the receipt records both sha256 digests)
- Consumer test summary: 49 total, 49 passed, 0 failed, 0 skipped
- The receipt records the identity of any failing consumer test in `testFailures` — the harness
  parses the runner's failure lines in either localization — and names it in the `FAIL:` line, so a
  failing run is attributable from the receipt alone
- `api-verify`, `baseline-verify`, and `measurements-verify` printed their exact success markers
- The measurement harness reported all ten scenarios equivalent and wrote `measurements.json`

Toolchain note: the receipt above is the F# harness's own run. `eng/harness/VerticalSlice.fs` replaced
the Python implementation of this tool, and the port was verified against that implementation's
receipts field by field (only build-timing text and the sha256 of independently packed nupkgs
differ); the earlier receipts in this record's history came from the Python implementation.

## Test Inventory

`tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Tests` — 49 xUnit v3 tests, no skips:

| Suite | What it proves |
| --- | --- |
| `OrderLifecycleTests` | Pure transitions (`Applied`/`Rejected`/`Failed`), emitted command order, revision arithmetic, replay determinism and divergence, store conflict |
| `OrderEndpointTests` | The realistic flow: place, read, pay, ship, cancel, timeline, reconcile |
| `ValidationTests` | Complete, ordered validation errors; route-identifier failures are 404, not 400 |
| `ProblemMappingTests` | Every expected failure's status, problem type, and extensions; history divergence as a typed 500 |
| `OpenApiTests` | Ten paths with their real outcomes; no `IResult`, `HttpContext`, or domain type in the document |
| `CancellationTests` | One request token reaches every dependency; a canceled client cancels that token instead of producing a response |
| `BoundedParallelismTests` | The concurrency cap is enforced and refills; completion-order streaming; deterministic best quote; a failing supplier is reported without failing the quote |
| `StreamingExportTests` | Per-row NDJSON delivery with running totals; first row before the next exists; disconnect stops the enumeration |
| `UnexpectedExceptionTests` | A throwing dependency produces a logged, generic 500 and never a domain outcome |
| `PlacementAtomicityTests` | A rejected placement leaves no draft in the store and releases the lines it had reserved |
| `PaymentGuardTests` | Paying a paid or cancelled order is a typed 409 and never reaches the payment gateway |
| `MeasurementsEquivalenceTests` | The comparison verdict treats a status-only divergence as a behavioral difference |

## Maintainer Review

The call-site comparison in [vertical-slice-call-sites.md](vertical-slice-call-sites.md) is the
artifact Goal 22 requires the maintainer to review. Review status is recorded here when the
maintainer accepts or amends it:

- Status: **accepted** (2026-09-28). The maintainer reviewed the comparison, including the measured
  trade-off it states — the carrier slice costs a little more on happy-path placement and supplier
  fan-out, and costs substantially less whenever the response payload is a typed failure — and
  accepted it as the Goal 22 comparison, with no amendments requested.

## Deliberate Boundaries

- This evidence is local developer-machine evidence produced by `eng/harness/VerticalSlice.fs`; it is
  not a `release.yml` job and it does not modify `eng/release-protocol.json`. The release gate keeps
  running the compatibility suite exactly as before.
- The measurement numbers do not change `eng/performance/baseline.json`: the `excluded|aspnet-mapping`
  row's rationale still holds for *release claims*, and the harness output is scoped to application
  end-to-end behavior on one machine.
- The consumer bundle is not part of `FunnySharp.slnx`, because its projects can only restore after
  the packages exist; this mirrors the compatibility suite's placement outside the solution.
- The consumer suite is deterministic by construction: no test sleeps or polls, every wait is gated by
  a `TaskCompletionSource` and bounded by `TestContext.Current.CancellationToken`, and the slice has no
  mutable static state. It passed more than 40 standalone suite runs and every pipeline run recorded in
  this closure's session. One run on
  2026-09-29 reported 1 failure of 49 — on a shared 2-vCPU host at load 3.4-6.4 with 5 GB of 7.8 GB in
  use and no swap, where the failing run took 12.7 s against a 4.4 s typical — and was not reproducible
  in any of those runs. That receipt could not name the failing test because it kept only an
  8000-character stdout tail; the harness now records `testFailures`, so a recurrence names itself.
