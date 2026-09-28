# eng/evaluation/ — Goal 21 coding evaluation harness

**Earned:** score ~18 — model-agnostic evaluation harness; own entry point and layout.

## OVERVIEW

Repeatable, model-agnostic comparison of idiomatic C# (BCL only) vs. FunnySharp across
five task areas: business-outcomes, collections, async-streams, concurrency, aspnetcore.
The harness never talks to a model; it consumes solution files and measures them.

## WHAT AN EVALUATION TASK IS

Each `tasks/<area>/` contains exactly five pieces:

| Piece | Content |
| --- | --- |
| `prompt-funnysharp.md` | The business brief + style directive "use the FunnySharp package"; no task-specific API hints |
| `prompt-idiomatic.md` | The identical brief + "idiomatic C#, .NET 10 BCL only" |
| `template-funnysharp/` | Kit csproj ONLY (no `.cs`): references FunnySharp 0.1.0 from the local evaluation feed |
| `template-idiomatic/` | Kit csproj ONLY: BCL-only, no FunnySharp reference |
| `tests/` | The fixed oracle: `Contract.cs` + `<Area>Tests.cs`, compiled in on top of either template |

- The two variants of an area share one assembly name (e.g. `CollectionsKit`); the kit
  csproj is the sole project file and templates must contain no `.cs` (they would collide).
- **The style-neutral seam**: `Contract.cs` declares all DTOs, fakes, and the entry method
  each solution must provide (e.g. `OrderWorkflow.PlaceOrder`, `FeedCleaner.Clean`,
  `SensorStream.ProcessAsync`, `AvailabilityCoordinator.CheckAvailabilityAsync`). Its header
  states it explicitly — no FunnySharp types appear there on purpose; both styles end at
  the same seam. Solutions must declare types in the global namespace.
- Tests are xUnit `[Fact]`s, black-box against the contract only: exact error-code
  ordering, exact decimals/status codes, deterministic fakes, cancellation asserted via
  `TaskCompletionSource` barriers (no sleeps). The same suite must pass both variants.

## RUNS AND RECORDS

- Recorded runs: `results/<area>/<style>/run-<n>/solution/` + `record.json` (committed evidence).
- `record.json` measures: compilation (build success + error count), semantic correctness
  (the fixed suite), correction feedback (verify rounds until green, capped at three),
  consumer-side LOC, API misuse (FS#### analyzer diagnostics). Blinded A/B readability
  reviews land as `review-<n>.json` beside the runs; final verdict in
  `docs/next-stage/ai-usability-goal-21.md`.
- `results/review-assignments.json` tracks the A/B anonymization.
- Transient build trees go under `artifacts/evaluation/builds/` and are never committed.

## CONVENTIONS

- Kit csprojs deviate from test-project defaults: `OutputType=Exe` with `IsTestProject=true`,
  xunit.v3 4.0.0 via a global `<Using Include="Xunit" />`, no `Microsoft.NET.Test.Sdk`.
- Runner is a PEP 723 uv script, like `eng/tools`; run it from the repository root.

## ANTI-PATTERNS

- NEVER reference FunnySharp types in `tests/Contract.cs` — the seam is style-neutral by
  design; both variants must end at the same contract or the comparison is invalid.
- NEVER edit or reorder recorded runs under `results/` — they are append-only evidence
  (`run-<n>/` + `record.json` + `review-<n>.json`); corrections happen as new runs.
- NEVER copy files into a template or task dir by hand — the runner owns all artifact
  copying (template csproj + tests + solution into the transient build tree); templates
  must stay csproj-only (a stray `.cs` collides at verify time).

## COMMANDS

```bash
python3 eng/evaluation/runner.py prep-feed        # pack FunnySharp + FunnySharp.AspNetCore into artifacts/evaluation/feed
python3 eng/evaluation/runner.py verify <area> <idiomatic|funnysharp> <run-directory>
python3 eng/evaluation/runner.py aggregate <out.md>
```

## NOTES

- `prep-feed` first for any funnysharp run — restore silently uses stale cached globals
  if the local feed is missing the fresh pack.
- Area `<area>` is one of: `business-outcomes|collections|async-streams|concurrency|aspnetcore`.
