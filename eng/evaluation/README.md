# Goal 21 Coding Evaluation

A repeatable, model-agnostic evaluation comparing idiomatic C# (BCL only) with FunnySharp
across five task areas: business outcomes, collections, async streams, concurrency, and
ASP.NET Core. The goal measures whether an ordinary coding agent can discover the canonical
FunnySharp expression of each workflow from the public package documentation and compiler
feedback alone, and what the library's types and diagnostics change about the outcome.

## Model-agnostic contract

The harness never talks to a model: it consumes solution files and emits measurements.

1. A producer (any coding model or human) reads one prompt from `tasks/<area>/prompt-<style>.md`
   and writes C# files into `results/<area>/<style>/run-<n>/solution/`.
2. The runner copies the style's template plus the area's fixed tests and contract into a
   transient build tree, restores from the evaluation feed, builds, and runs the tests.
3. The runner writes `record.json` next to the solution with the measurements.
4. A correction-feedback loop returns the compiler errors, analyzer diagnostics, and test
   failures to the producer, which revises; the rounds until green are recorded.
5. A blinded reviewer (any model or human) receives both final solutions of an area
   anonymized as A/B in randomized order and records a readability judgment.

Both styles receive the identical business brief, the identical fixed tests, and the same
seam. The only difference is the style section: idiomatic C# (.NET 10 BCL only) versus
FunnySharp (the package plus its published documentation). The FunnySharp prompt contains no
task-specific API hints, no copy of any solution, and no prompt-level instructions beyond the
style directive; discovery from public signatures and documentation is the measurement.

## Measurements (per run)

| Measurement | How it is recorded |
| --- | --- |
| Compilation | build success and compiler error count from `dotnet build` |
| Semantic correctness | the area's fixed xUnit suite: `dotnet test` total/failed |
| Correction feedback | verify rounds until green (record.json history kept by the orchestrator) |
| Consumer-side LOC | non-blank, non-comment lines of the solution files |
| API misuse | the FS#### analyzer diagnostics emitted while building |
| Maintainer readability | blinded A/B review JSON next to the runs |

## Commands

```shell
# Prepare the evaluation feed (packs FunnySharp + FunnySharp.AspNetCore with embedded analyzers).
python3 eng/evaluation/runner.py prep-feed

# Verify one solution run (write record.json into the run directory).
python3 eng/evaluation/runner.py verify <area> <idiomatic|funnysharp> <run-directory>

# Aggregate every recorded run into a markdown table.
python3 eng/evaluation/runner.py aggregate <out.md>
```

The transient build trees live under `artifacts/evaluation/builds/` and are never committed;
the recorded runs under `results/` are the evidence. The recorded verdict lives in
`docs/next-stage/ai-usability-goal-21.md`.

## Recording a run

Each production run is produced by one independent agent run that received exactly one prompt
file. The correction loop is capped at three verify rounds; the rounds used are recorded in the
evaluation record. Blinded reviews are stored as `review-<n>.json` beside the runs with
the A/B assignment and the reviewer's rubric verdicts.