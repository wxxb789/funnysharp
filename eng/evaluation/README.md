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
3. The runner reserves a new `rounds/0001/` directory, snapshots the solution, and writes
   full process logs, diagnostics/assets, inputs, exact feedback, record and SHA256 receipt.
4. A correction-feedback loop returns the compiler errors, analyzer diagnostics, and test
   failures to the producer, which revises; the rounds until green are recorded.
5. A blinded reviewer (any model or human) receives both final solutions of an area
   anonymized as A/B in randomized order and records a readability judgment.

Both styles receive the same business brief, fixed tests and neutral seam. Historical
prompts are retained: some prescribed carrier families and the ASP.NET prompt named mapping
methods, so those records do not establish unassisted discovery. A fresh study uses a
curated public-guide snapshot and neutral style directives instead of unrestricted docs/.

## Measurements (per run)

| Measurement | How it is recorded |
| --- | --- |
| Compilation | build success and compiler error count from `dotnet build` |
| Semantic correctness | the area's fixed xUnit suite: `dotnet test` total/failed |
| Correction feedback | immutable rounds, exact feedback payload and predecessor SHA256 chain; at most initial plus two corrections |
| Consumer-side LOC | non-blank, non-comment lines of the solution files |
| API misuse | the FS#### analyzer diagnostics emitted while building |
| Maintainer readability | blinded A/B review JSON next to the runs |

## Commands

```shell
# Prepare the evaluation feed (packs FunnySharp + FunnySharp.AspNetCore with embedded analyzers).
dotnet fsi build.fsx -- -p eval-prep-feed

# Verify a new local replay outside historical results (not evidence of AI generation).
dotnet fsi build.fsx -- -p eval-verify --task <area> --style <idiomatic|funnysharp> --run-dir <run-directory>

# Aggregate every recorded run into a markdown table.
dotnet fsi build.fsx -- -p eval-aggregate --output <out.md>
```

The transient build trees live under `artifacts/evaluation/builds/` and are never committed;
the recorded runs under `results/` are the evidence. The recorded verdict lives in
`docs/next-stage/ai-usability-goal-21.md`.

## Recording a run

Historical tasks/results/audit evidence are read-only. Fresh runs live under
`results/audit-resolution-v4/<area>/<style>/run-1|run-2/`. The interrupted
`audit-resolution-v1` freeze is retained as failed preparation evidence; it has no
accepted manifest or producer cohort. The v2 transport and the first v3 transport
are retained as rejected evidence: the serialized provider request contained
unregistered runtime system instructions. The v3 attempt additionally exposed a
system-prompt getter that changed after startup. The registered v4 successor
preserves the same 20-session topology and byte-identical public workloads, with
fresh logical session IDs. It captures the registered system once at startup,
records the original provider payload, and replaces only the outgoing input with
that immutable system and the exact request bytes. Every actual outgoing payload
must be checked; startup metadata and a no-model check alone prove no cohort member.
Before ANY generation,
finish candidate documents/analyzers/packages, review the curated allowlist and neutral
oracles, then run:

```shell
dotnet fsi build.fsx -- -p eval-prep-feed --study audit-resolution-v4
dotnet fsi build.fsx -- -p eval-verify --study audit-resolution-v4 --task concurrency --style funnysharp --run-dir eng/evaluation/results/audit-resolution-v4/concurrency/funnysharp/run-1 --round 1
```

Manual controls and subsequent package replays add `--replay` and use a new run directory
outside all results/ subtrees. They run the SAME frozen suite and bind packages/inputs, but
record evidenceKind=replay, require no claimed AI identity, and never count toward the 20
sessions. A GREEN generation record cannot be overwritten or relabeled by a replay.

### Independently replaying the retained v6 suite

The live `build.fsx` entrypoint intentionally requires the current runner/build environment
to equal the study's frozen environment. Unrelated release-pipeline additions can therefore
prevent that entrypoint from replaying v6. Use the dedicated replay-only route instead:

The retained checkout currently has 123 of the original 188 manifest files. The 65
unavailable files are historical `obj/` build-cache entries; every available file
still matches its frozen SHA256. The route below rejects that incomplete snapshot
before reserving output or building a consumer. It requires those original bytes
to be restored; a fresh restore cannot establish their historical identity. The
parent's actual replay attempt returned exit 1 for this known retention limit, not
GREEN. No original manifest entry is omitted or regenerated to bypass the check.

```shell
dotnet fsi eng/evaluation/replay-frozen.fsx -- collections funnysharp eng/evaluation/results/audit-resolution-v6/collections/funnysharp/run-1/solution Q:/tmp/funnysharp-v6-collections-replay-1 83d7d3522ccad4fa04cc56bbf8ed540521147d178d1940cc656d9667e49ac76d
```

Arguments are task, style, existing C# solution directory, new output directory, and the
independently retained SHA256 of the original v6 `manifest.json`. Supply an unused output
path, outside every `results/` subtree. Existing paths, including empty or interrupted
attempts, are rejected. The source solution and all historical evidence remain read-only.

This entrypoint copies the exact manifest-bound snapshot, plan and environment into
`<output>/frozen-root/`, including the frozen `build.fsx` bytes needed by `loadStudy`.
It loads the frozen `Evaluation.fs` directly through FSI, rather than executing either
version of the full build pipeline. The unchanged runner uses the original frozen packages,
oracles, templates, locked restore and expected test count. Its receipts and
`evidenceKind=replay` record live in `<output>/run/rounds/0001/`; no producer receipt,
model invocation or generation cohort slot is used. Current generation keeps its live
environment equality guard.

The historical manifest did **not** freeze `Output.fs`, `Proc.fs`, `Repo.fs` or `Loc.fs`.
These required support modules are copied from the current checkout and bound separately
in `replay-bindings.json`, with `supportBinding=contemporary-not-historical`. That binding
also covers retained copies of the replay driver/entrypoint, the generated FSI entrypoint,
the exact frozen inputs and the copied C# solution. `runner.process.json` and full runner
logs preserve execution status. This route establishes a frozen-suite replay with explicitly
bound contemporary support, not a recovered historical support dependency closure. The
installed FSI/SDK follows frozen `global.json`; SHA256 bindings are alteration checks, not
signatures or historical provenance for files absent from the original manifest.

Prep freezes a new snapshot once; never rerun it over a snapshot. An interrupted freeze or
round is retained and disqualifies that attempt until a separately identified revision is
authorized. Study/source changes invalidate dependent evidence. `plan.json` preregisters
all 20 runs; its manifest binds prompts, guides, oracles, templates, SDK configuration and
candidate packages. The runner remains file-based and never dispatches models.

The preregistered `upstreamPackageFeed` selects the explicit upstream used during kit
preparation and every round. Without it, historical routes retain their original
nuget.org configuration. An explicit study config clears inherited disabled-source
entries within its own two-source configuration; it does not edit user settings or
disable TLS, package auditing or warnings-as-errors.

Before each verify, the orchestrator writes `producer/0001.json` (then 0002/0003) and
`producer/0001-context/context.json` plus `payload/` containing the EXACT supplied files.
The context JSON is `{ "files": [{ "path": "...", "sha256": "..." }] }`, sorted by path.
Producer fields: studySha256, task, style, sessionId, invocationId, route, producerKind=ai,
requestedModel, providerModel (`value`, or null plus unknownReason), contextSha256,
solutionFiles (the complete output C# file/hash manifest), invocationSha256 (hash of
the actual route/request/response receipt in the context directory's invocation.json),
startedUtc, finishedUtc, status; corrections also bind feedbackSha256 to the predecessor's
exact feedback.json. Session IDs must be fresh across initial runs and unchanged within
corrections. Actual route/request records, outputs and feedback delivery must be captured
by the orchestrator; self-reported JSON alone is not independent proof of invocation.

GREEN requires one positive, complete, consistent expected test count, zero failures and
zero skips, passed summary and exit zero. Solutions cannot replace contract/oracle files.
Duplicate/fourth/missing-predecessor rounds, changed archived bytes or bindings are rejected.
Failed, interrupted and capped runs remain negative, not omitted. Legacy aggregate reads
legacy records only; do not use it to claim fresh-cohort completeness.

The new concurrency workload explicitly requires winner-triggered cancel-and-drain and
propagation of independent cleanup faults in BOTH styles. This is a new common workload,
not a retrospective reinterpretation. Tokens owned by the coordinator must propagate
caller cancellation; they need not equal the caller token. Sequential stream validation
retains exact token forwarding. No timers establish test ordering: release/admission,
cancellation and exit gates do.

Output DLL hashes are build assets, not a claim of runtime assembly loading. Main execution
must verify the frozen same-package FS1001 negative control, real runner failure/correction witnesses,
runtime package/assembly binding, loopback Kestrel HTTP, complete cohort outcomes and
attributable exact-input maintainer review. No producer/reviewer identity is inferred from
a task title or parent session. Hashes detect alteration relative to an independently
retained manifest/terminal receipt; they are not signatures or access-control immutability.

Each production run is produced by one independent agent run that received exactly one prompt
file. The correction loop is capped at three verify rounds; the rounds used are recorded in the
evaluation record. Blinded reviews are stored as `review-<n>.json` beside the runs with
the A/B assignment and the reviewer's rubric verdicts.
