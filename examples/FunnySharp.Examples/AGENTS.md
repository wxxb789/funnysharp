# FunnySharp.Examples/ — compiling core-API examples

**Earned:** score ~10 — distinct domain (executable assertion suite over the public API).

## OVERVIEW

Executable "documentation": every example compiles against the shipped analyzers,
asserts its own behavior, and prints one success marker.

## WHERE TO LOOK

| File | Role |
| --- | --- |
| `Program.cs` (863 LOC) | Top-level runner; prints `FunnySharp examples passed.` |
| `UnitResultExamples.cs` | `Verify()` / `VerifyAsync()` — UnitResult surface incl. `ExampleAssertions.UninitializedThrows` pinning `The unit result has not been initialized.` |
| `CollectionExamples.cs` | Collection/sequence examples (`Verify`/`VerifyAsync`) |
| `FunctionGrammarSamples.cs` | Function-composition grammar samples |
| `DecoderExamples.cs` | Decoder scalars (`Guid`, `Int32`, `NonEmptyString`), `Object`/`Field`/`NonEmptyList`, `Ensure`, automatic error paths, and accumulated errors |
| `ContractExamples.cs` | One nested contract reused for validation, decoding, explanation, generation, shrinking, and capability/limitation extension paths |
| `PatchExamples.cs` | Allowed-field decode, explicit clearing, candidate validation, conflict reporting, empty-patch identity, and rejected fields |
| `BatchExamples.cs` | Explicit batch sources, independent `Zip`, dependent `Bind`, key deduplication, request association, and missing/per-key outcomes |
| `DomainExamples.cs` | Tagged identifiers, validated construction/decoding, explicit draft/placed/shipped states, and typed module-error composition |
| `CheckExamples.cs` | Value-preserving `Check`, ordered independent `CheckAll`, empty/already-failed behavior, and Task/ValueTask forms |
| `FallibleReductionExamples.cs` | Seeded `Result`-returning fold, empty seed, first failure, mutable-accumulator no-rollback, and sync/async source forms |
| `SummaryExamples.cs` | Independent count/sum/extrema/category summaries, one-pass typed report, empty extrema, mutable-state isolation, overflow, and sync/async reuse |
| `JoinExamples.cs` | Complete-input duplicate and missing failures with positions, explicit comparer, prebuilt unique index, cardinality-checked reads, and streaming partial output with later failures |
| `NestedDataExamples.cs` | Decode-and-expand import, empty lines, rejection by origin, aligned mismatch versus BCL truncation, Cartesian-versus-aligned counts, and async expansion parity |
| `DomainProgramExamples.cs` | Lifted inventory/payment children with a parent cancellation decision, ordered commands and correlation, late/duplicate/stale replies, explicit time/randomness, command-failure distinction, replay purity, child reuse, and the ordinary async execution loop |
| `KeyedConcurrencyExamples.cs` | Per-account serialization with cross-account overlap, typed failure continuation, per-key source order, and explicit admission/buffering bounds |
| `ExampleAssertions.cs` | Shared asserts: `Equal`, `True`, `SequenceEqual`, `Throws`, `FaultIsPreserved`, `CancellationIsPreserved`, `UninitializedThrows` |

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
