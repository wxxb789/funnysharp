# FunnySharp.VerticalSlice — package-consumer vertical slice (Goal 22)

**Why this file:** a distinct domain (a whole application, not a test suite) with its own build
path: it consumes packed nupkgs, lives outside `FunnySharp.slnx`, and is verified by
`dotnet fsi build.fsx -- -p vertical-slice` (`eng/harness/VerticalSlice.fs`) rather than by the
solution build.

## OVERVIEW

An ASP.NET Core order-fulfilment slice written in C# against the `FunnySharp` and
`FunnySharp.AspNetCore` **packages**, plus the idiomatic-C# comparison it is measured against and the
harness that proves both behave identically. The one place in the repository where the public API is
exercised the way an external consumer would exercise it.

## STRUCTURE

```
FunnySharp.VerticalSlice/
├── FunnySharp.VerticalSlice.Api/          # the slice: Domain/, Application/, Infrastructure/, Http/, Endpoints/
├── FunnySharp.VerticalSlice.Baseline/     # the same ten scenarios with no FunnySharp reference
├── FunnySharp.VerticalSlice.Tests/        # 49 integration tests (TestServer + one Kestrel disconnect case)
└── FunnySharp.VerticalSlice.Measurements/ # hosts both apps, asserts equivalence, measures them
```

## WHERE TO LOOK

| Task | Location |
| --- | --- |
| Domain values and the lifecycle machine | `FunnySharp.VerticalSlice.Api/Domain/` |
| Ports, quoting fan-out, command interpreter | `FunnySharp.VerticalSlice.Api/Application/` |
| Simulated store, stock, payments, suppliers | `FunnySharp.VerticalSlice.Api/Infrastructure/` |
| Problem mapping and the exception boundary | `FunnySharp.VerticalSlice.Api/Http/` |
| Endpoint mapping and typed results | `FunnySharp.VerticalSlice.Api/Endpoints/` |
| Comparison call sites | `docs/vertical-slice-call-sites.md` |
| Evidence record | `docs/vertical-slice-evidence.md` |
| Orchestrator | `eng/harness/VerticalSlice.fs` (`dotnet fsi build.fsx -- -p vertical-slice`) |

## CONVENTIONS

- The `Api`, `Tests`, and `Measurements` projects take `$(FunnySharpPackageVersion)` /
  `$(FunnySharpAspNetCorePackageVersion)` from the orchestrator; they never hardcode a version and
  commit no lock file. The `Baseline` project references no FunnySharp package at all.
- `VerticalSliceApp.Build(args, configureServices, configureHost)` is the composition root: tests
  replace a port by registering it later, never by reaching into a built provider.
- Tests never sleep and never poll: a dependency that must wait is held on a
  `TaskCompletionSource` the test releases, and every wait is bounded by
  `TestContext.Current.CancellationToken`.
- Success is the exact stdout marker (`VerticalSliceApp.VerifyMarker`,
  `BaselineApp.VerifyMarker`, `Program.VerifyMarker`), not exit code 0 alone.
- Simulated latency is a knob (`VerticalSliceOptions`): tests and measurements set it to zero, and a
  zero latency also disables the per-supplier stagger.

## ANTI-PATTERNS

- NEVER add these projects to `FunnySharp.slnx`: they can only restore after `dotnet pack` has run.
- NEVER replace a `PackageReference` with a `ProjectReference` "to make debugging easier" — the
  package boundary is the entire point of this subtree.
- NEVER let the slice reference an ASP.NET type from `Domain/` or `Application/`, and never let
  domain code resolve a service from a global provider.
- NEVER convert an unexpected exception into an `OrderError`: the only catch-all is
  `UnexpectedExceptionHandler`.
- NEVER make the measurement harness a release gate or copy its numbers into
  `eng/performance/baseline.json` (the `excluded|aspnet-mapping` policy stands).

## COMMANDS

```bash
# pack, isolated consumer restore/build/test, both apps, measurements
dotnet fsi build.fsx -- -p vertical-slice --output artifacts/vertical-slice/consumer-run
# inside the bundle
dotnet run --project tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Api -c Release -- --verify
dotnet run --project tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Measurements -c Release -- --verify
```
