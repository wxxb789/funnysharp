# Coding Evaluation

The evaluation compares idiomatic C# (BCL only) and FunnySharp across
business-outcomes, collections, async-streams, concurrency and aspnetcore.
The harness consumes solution files; it does not call a model.

## One Semantic Runner

Each task has two prompts/templates and one shared style-neutral `Contract.cs`
and xUnit oracle. The runner copies the template, trusted tests and solution into
an isolated build tree, builds, and executes the tests. Solutions cannot replace
oracle or template files.

Measurements record compilation errors, semantic correctness, analyzer diagnostics,
consumer LOC and correction rounds. A test result is green only with positive,
consistent, all-passing discovery and zero skips. Failed and interrupted rounds
remain visible; corrections append up to three total rounds and cannot replace
history or continue after green.

## Commands

```bash
dotnet fsi build.fsx -- -p eval-prep-feed
dotnet fsi build.fsx -- -p eval-verify --task <area> --style <idiomatic|funnysharp> --run-dir <run-directory>
dotnet fsi build.fsx -- -p eval-aggregate --output <out.md>
```

Builds live under `artifacts/evaluation/builds`. Checked-in `results/` and `studies/`
are historical records and stay read-only. Git identifies committed source/history;
there are no custom source, package, context, invocation or round checksums.

## Registered Studies

Prepare a new named study with `eval-prep-feed --study <name>`. It snapshots the
selected tasks, guides, packages and consumed build configuration. Registered
runs use `results/<study>/<area>/<style>/<run-name>` and a preregistered cohort.
`eval-verify --study <name>` checks task/style/cohort routing and expected test count.
Do not repurpose a historical study name for a new experiment.

Each AI round supplies `producer/<four-digit-round>.json` with:

- `study`, `task`, `style`;
- `sessionId`, `invocationId`, `route`, `producerKind` (`ai`), `requestedModel`;
- `providerModel`: a `value`, or null with an `unknownReason`;
- `startedUtc`, `finishedUtc`, `status`.

Corrections keep the session and use a new invocation. Initial runs cannot reuse
another run's session. Metadata describes routing and producer identity; it does
not attest file contents or prove which provider request was sent.

Manual study controls use `--replay` and a new run directory outside `results/`.
They execute the same oracles without claiming AI generation or consuming a
cohort slot. The dedicated frozen hash replay entrypoint has been removed.

## Boundaries

Semantic oracles test output, ordering, cancellation and diagnostics. Blinded
readability review is separate. Historical results cannot establish current
product correctness or unassisted API discovery. No evidence checksum substitutes
for executing the intended tests.
