# Goal 21: AI-Usability Coding Evaluation Record

This record documents the repeatable, model-agnostic coding evaluation delivered by Goal 21. The
harness, prompts, tests, and every recorded run live under `eng/evaluation/` and
are committed with this record; nothing here depends on a specific model, prompt trick, or
single run.

## What was measured

Twenty production runs - five task areas (business outcomes, collections, async streams,
concurrency, ASP.NET Core) x two styles (idiomatic C# with the .NET BCL only, versus FunnySharp
with its published documentation) x two independent runs each - each produced by a separate
independent agent run that received exactly one prompt file and nothing else. Every run verified
through the same harness (`eng/evaluation/runner.py`): fixed style-neutral contract,
fixed xUnit suite, correction feedback capped at three verify rounds, then blinded readability
reviews of each area's two first-run solutions by two independent reviewers each.

The FunnySharp prompts contained no task-specific API hints: the business brief, the fixed tests,
and the style directive were identical to the idiomatic prompts, and the FunnySharp producers
could read only the public package documentation (README.md and docs/). Discovery from public
signatures and documentation was the measurement. The idiomatic producers could not read the
FunnySharp documentation at all. Blinding is imperfect in one documented way: a reviewer sees
each solution's `using` directives, so the review is blinded to assignment order and
style directives, not to the visible imports.

## Results

All 20 runs ended GREEN: both styles compiled and passed their complete fixed suites.

| area | style | run | verify rounds | tests | LOC | FS diagnostics |
| --- | --- | --- | --- | --- | --- | --- |
| business-outcomes | idiomatic | run-1 | 1 | 8/8 | 98 | none |
| business-outcomes | idiomatic | run-2 | 1 | 8/8 | 78 | none |
| business-outcomes | funnysharp | run-1 | 2 | 8/8 | 110 | none |
| business-outcomes | funnysharp | run-2 | 1 | 8/8 | 120 | none |
| collections | idiomatic | run-1 | 1 | 9/9 | 79 | none |
| collections | idiomatic | run-2 | 1 | 9/9 | 61 | none |
| collections | funnysharp | run-1 | 2 | 9/9 | 80 | none |
| collections | funnysharp | run-2 | 1 | 9/9 | 87 | none |
| async-streams | idiomatic | run-1 | 1 | 8/8 | 63 | none |
| async-streams | idiomatic | run-2 | 1 | 8/8 | 38 | none |
| async-streams | funnysharp | run-1 | 1 | 8/8 | 52 | none |
| async-streams | funnysharp | run-2 | 1 | 8/8 | 40 | none |
| concurrency | idiomatic | run-1 | 1 | 9/9 | 67 | none |
| concurrency | idiomatic | run-2 | 1 | 9/9 | 90 | none |
| concurrency | funnysharp | run-1 | 1 | 9/9 | 62 | none |
| concurrency | funnysharp | run-2 | 1 | 9/9 | 58 | none |
| aspnetcore | idiomatic | run-1 | 1 | 9/9 | 108 | none |
| aspnetcore | idiomatic | run-2 | 1 | 9/9 | 100 | none |
| aspnetcore | funnysharp | run-1 | 2 | 9/9 | 175 | none |
| aspnetcore | funnysharp | run-2 | 2 | 9/9 | 151 | none |

Raw records: `eng/evaluation/results/<area>/<style>/run-<n>/record.json` plus the
solution that produced them; `eng/evaluation/runner.py aggregate` regenerates the
table.

### Compilation and semantic correctness

Parity: every run in both styles compiled clean under warnings-as-errors and passed its complete
fixed suite on the final round. A coding agent can discover a passing FunnySharp expression of
each representative workflow from the public documentation and package signatures alone.

### Correction feedback

Ten of ten idiomatic runs were green on the first verify round. Six of ten FunnySharp runs were
green on the first round; four needed a second (business-outcomes run-1, collections run-1,
aspnetcore run-1 and run-2). The recorded friction causes:

- `Result`/`UnitResult`/`Validation`/`TransitionResult` `TryGet*` out
  parameters were annotated `[MaybeNull]` rather than `[MaybeNullWhen(false)]`, so the
  true branch produced CS8604 for non-nullable payloads and consumers added `!` operators
  (collections run-1). Fixed during this goal: all five members now carry
  `[MaybeNullWhen(false)]`, verified by the repository's 729-test suite.
- `Option.ToResult` inferred `TError` from the error factory's natural return type, so
  nested error records broke the chain until callers added explicit type arguments (aspnetcore
  run-2) or base-typed factories (run-1).
- Closed record hierarchies do not produce exhaustive switches (CS8509), so endpoint mappers
  needed a documented unreachable arm (aspnetcore run-1).

### Consumer-side LOC

Idiomatic mean 78.2 LOC per solution versus FunnySharp 93.5 (+19.6%). The gap concentrates in
the ASP.NET Core area (175/151 versus 108/100) where outcome-to-problem mapping through the
integration package costs an extra mapping layer; the async-streams and concurrency areas are at
or below the idiomatic size (40-62 versus 38-90).

### API misuse

Zero FS1001-FS1005 diagnostics across all twenty runs, including all builds of every corrected
round: the producers used the carriers, cardinality, parsing, traversal, and concurrency APIs
without once creating an uninitialized carrier, discarding an outcome silently, ignoring a
TryGet* presence result, blocking a ValueTask, or disposing an async-disposable resource
synchronously. The analyzer suite stayed quiet on correct discovered usage; the deliberate
uninitialized-default demonstration in the examples is the only suppressed diagnostic in the
repository.

### Maintainer readability judgment

Ten blinded reviews (two per area, assignment order swapped between reviewers, recorded in
results/review-assignments.json): ten of ten reviewers preferred the idiomatic solution. Mean
ratings - failure visibility 4.9 vs 3.4, absence handling 4.8 vs 3.5, control flow 4.9 vs 3.2,
overall 4.9 vs 3.3. The recorded friction taxonomy, consistent across reviewers:

- failure and absence causes sit one hop from the branch site, inside `Match`/`TryGet*` unwrapping
  or `ToHttpResult` problem factories, instead of at the branch;
- correctness-critical semantics (source order, error-accumulation order, seed/empty emission,
  drain-all) ride on combinator contracts that are not visible in the file;
- generic-type noise (`Validation<IReadOnlyList<double>, string>`), discard-heavy lambdas
  (`(_, _, _, _) => request`), and adapter types distance the reader from the rules;
- unwrapping ceremony - paired `TryGetValue`/`TryGetErrors` calls, `!` operators, and `_ =` discards -
  annotates the library rather than the business rules.

The `!`-operator part of that tax is the `[MaybeNull]` annotation friction fixed above; the
remainder is the honest, currently-open readability cost of the combinator style, recorded here
as input for a future goal rather than hidden.

## Verdict against the Goal 21 questions

- Discoverability: supported. Independent producers expressed all five representative workflows
  correctly from public signatures and documentation alone, with zero analyzer misuse findings,
  and reached green in at most two rounds.
- Compiler feedback: delivered. The five shipped analyzers (FS1001-FS1005) stayed silent on
  every correct line the producers wrote, the examples build with zero unsuppressed diagnostics,
  and the evaluation itself drove one concrete annotation fix (`[MaybeNullWhen(false)]`).
- Human readability: honestly negative. Blinded maintainers preferred the idiomatic solutions
  ten of ten times; the recorded taxonomy above is the actionable evidence trail. FunnySharp's
  claim is functional parity and compiler-guarded correctness, not a readability win over plain
  C#; that claim is what the README and guides may state.

## Reproduction

```shell
# Prepare the evaluation feed (packages FunnySharp with the embedded analyzers).
dotnet fsi build.fsx -- -p eval-prep-feed

# Re-verify any recorded run (writes record.json into the run directory).
dotnet fsi build.fsx -- -p eval-verify --task <area> --style <idiomatic|funnysharp> --run-dir eng/evaluation/results/<area>/<style>/run-<n>

# Aggregate every recorded run.
dotnet fsi build.fsx -- -p eval-aggregate --output <out.md>
```

New runs: follow one prompt file in `eng/evaluation/tasks/<area>/prompt-<style>.md`, write
solution files into a fresh run directory, and verify. The harness consumes files only, so any
model or human can produce runs; the protocol and blinding rules are documented in
eng/evaluation/README.md.