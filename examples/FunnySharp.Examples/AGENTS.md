# FunnySharp.Examples/ — compiling core-API examples

**Earned:** score ~10 — distinct domain (executable assertion suite over the public API).

## OVERVIEW

Executable "documentation": every example compiles against the shipped analyzers,
asserts its own behavior, and prints one success marker.

## WHERE TO LOOK

| File | Role |
| --- | --- |
| `Program.cs` (866 LOC) | Top-level runner; prints `FunnySharp examples passed.` |
| `UnitResultExamples.cs` | `Verify()` / `VerifyAsync()` — UnitResult surface incl. `ExampleAssertions.UninitializedThrows` pinning `The unit result has not been initialized.` |
| `CollectionExamples.cs` | Collection/sequence examples (`Verify`/`VerifyAsync`) |
| `FunctionGrammarSamples.cs` | Function-composition grammar samples |
| `ExampleAssertions.cs` | Shared asserts: `Equal`, `True`, `SequenceEqual`, `FaultIsPreserved`, `CancellationIsPreserved`, `UninitializedThrows` |

## CONVENTIONS

- Pattern per example group: a static `Verify*` / `VerifyAsync` method that asserts, called
  from `Program`. Static local functions; no test framework — this is an app, not a test project.
- Analyzer dogfooding is enforced, not advisory: the csproj references the analyzer projects
  as analyzers, so the build FAILS on any FunnySharp diagnostic.
- Assertion helpers deliberately assert fault/cancellation preservation semantics
  (lazy pipelines must keep exceptions and cancellation where they belong).

## ANTI-PATTERNS

- NEVER weaken an example into a non-asserting printout — every `Verify*` must assert.
- NEVER suppress a FunnySharp diagnostic here; that would break the dogfooding invariant.

## COMMANDS

```bash
# From repo root (release-protocol step `examples` runs exactly this, --no-build)
dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release
# expect stdout: FunnySharp examples passed.
```

## NOTES

- `Program.cs` is the repo's known >500-LOC hotspot; it is inline-example-shaped by design
  — split only when an API area outgrows it, not for line count alone.
