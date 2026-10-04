# Stage bundle implementation evidence

Status: focused implementation proof passed. Ready for the independent verification
node; no publication or Goal 24 acceptance claim.

Scope: `Q:/repos/funnysharp/.worktrees/goal24-stage-bundle`, branch
`audit/goal24-stage-bundle`, clean baseline
`d8744c934d86833f9817245ecc7c78b1177b8b76`.

F# language-server symbol lookup reported `fsautocomplete` is not installed.
Use the authorized F# compiler substitution; do not install an LSP or change feeds.

The producer, independent reviewer worktree, workflow, index validator and product
candidate are read-only. Only the four assigned tracked files may change.

## Implementation

- Added exclusive file-input `{repository,bundle:{schemaVersion:1,files:[...]}}`
  staging. Logical inventory is P/A plus every P payload and raw HTTP body.
- Reconstruction checks each bounded part and logical file with SHA256 and exact
  lengths using a 64 KiB copy buffer. Duplicate paths, case aliases, traversal,
  absolute paths, device paths, reparse ancestors, malformed hashes, empty-part
  ambiguity, missing/extra entries and P-unbound bytes fail closed.
- Final P/A and report are emitted only after existing independent A and post-P
  replay validation. Scratch is removed after success; no outer producer archive
  is introduced. Legacy stage/index logic is unchanged.
- `pack-stage-bundle` accepts legacy local path/SHA256 P/A references, validates
  sources without network requests, creates new 32 MiB parts, and writes the new
  descriptor last. `.stage-part` files carry the binary attribute.
- Chose per-file parts rather than an outer split ZIP, avoiding duplicated ZIP
  evidence and whole-archive buffering. P/A JSON bytes are copied, not serialized.

## Verification receipts

- `restore.log`: locked, no-cache restore succeeded without feed overrides.
- `red-compiled.log`: test fixture compiled with zero warnings/errors; its first
  runner invocation used an unsupported flag and did not execute tests.
- `red.log`, `red.ctrf.json`: actual baseline run, 38 cases, 32 failed, 6 passed,
  zero skipped. Failures are the missing bundle form and missing pack mode, not
  compile errors. Each adversarial stage case first requires a valid round trip.
- `implementation-build-fixed.log`: implementation and test project built with
  zero warnings/errors after the compiler required a path type annotation.
- `focused-green.log`, `focused-green.ctrf.json`: **53 passed, 0 failed, 0 skipped**,
  exit 0. Includes 41 StageBundle cases and 12 existing stage/index/A cases. The
  same command first rebuilt with zero warnings and zero errors. No full class
  was run after this focused green; the verifier owns that run.
- Binary attributes were observed as `binary: set`, `text/diff/merge: unset` for a
  sample part, while `docs/harness.md` remained text.
- Final `git diff --check` exited 0. Worktree status lists only the four authorized
  tracked files on `audit/goal24-stage-bundle`: 520 insertions and 2 deletions.

The earlier fixture-only build attempts exposed F# reserved-word/nullability and
task-expression rethrow issues; those were corrected before the compiled red run.
No test was weakened or skipped to accommodate those compiler errors.

The passing CLI fixture is retained in `cli-focused-green/`: `local.json`, original
P/A/attachments, `cli.fsx`, `packed/input.json`, 20 binary parts and `staged/`.
It launches real `dotnet fsi --exec cli.fsx` child processes using the compiled
harness `main` entry point, without network collaborators. Both command logs are
retained there. Its 19 logical files total 33,560,729 bytes; largest part is exactly
33,554,432 bytes. It includes an empty HTTP body and a 33,554,433-byte HTTP body,
verifies every staged byte against the original, and proves originals unchanged.
This is synthetic transport evidence, not a producer/auditor execution claim.

Tests cover valid transport, schema/exclusivity/file-only enforcement, exact
inventory, missing/extra/duplicate files and parts, part ordering, declared/actual
lengths, malformed/wrong hashes, P-unbound reconstructed bytes, traversal/absolute/
backslash paths, actual descriptor/source/ancestor symbolic links, empty files,
32 MiB boundaries, new-output-only packing, and unchanged Goal 04/09 replay checks.
One legacy root-artifact plus inline-input test supplements the existing path,
base64, independent A and uploaded-index tests.

## Exact commands

Run from `Q:/repos/funnysharp/.worktrees/goal24-stage-bundle` (Git Bash syntax):

```bash
dotnet restore tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj --locked-mode --no-cache
dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj --no-restore
```

The implementation's focused green command was:

```bash
FUNNYSHARP_STAGE_BUNDLE_FIXTURE=Q:/repos/funnysharp-goal24-resolution/artifacts/audit-resolution/resume-20261004/stage-bundle/cli-focused-green \
dotnet run --project tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj --no-build -- \
  -method '*StageBundle*' \
  -method '*StagesExactBytesAndIndexesOnlyActualUploadedArtifact' \
  -method '*RejectsInvalidIndependentAttestation' -noColor \
  -result-ctrf Q:/repos/funnysharp-goal24-resolution/artifacts/audit-resolution/resume-20261004/stage-bundle/focused-green.ctrf.json
```

Do not reuse that fixture output directory. The independent verifier should run
the full relevant class **once**, with fixture retention unset (or a fresh path):

```bash
unset FUNNYSHARP_STAGE_BUNDLE_FIXTURE
dotnet run --project tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj --no-build -- \
  -class 'FunnySharp.Harness.Tests.ReleaseProvenanceTests+ReleaseProvenanceTests' -noColor \
  -result-ctrf Q:/repos/funnysharp-goal24-resolution/artifacts/audit-resolution/resume-20261004/stage-bundle/independent-class.ctrf.json
```

To independently exercise the actual root pipeline wrapper, use the retained local
fixture and new output directories (these two wrapper commands were not run by the
implementation node; the focused test exercised the compiled `main` CLI):

```bash
evidence=Q:/repos/funnysharp-goal24-resolution/artifacts/audit-resolution/resume-20261004/stage-bundle
dotnet fsi build.fsx -- -p release-provenance -Mode pack-stage-bundle \
  -InputPath "$evidence/cli-focused-green/local.json" \
  -OutputDirectory "$evidence/cli-independent-packed"
dotnet fsi build.fsx -- -p release-provenance -Mode stage-attestation \
  -InputPath "$evidence/cli-independent-packed/input.json" \
  -OutputDirectory "$evidence/cli-independent-staged"
```

Compare original P/A and all manifest payload/body SHA256 values against the staged
files; compare `review.md` with the decoded original A `report.base64`. All output
directories are new-only; failed output is evidence and cannot be reused.

## Boundaries and assumptions

The descriptor's logical inventory is exact; unrelated checkout files are not
transported. Parts are resolved beneath the selected input directory, with no
remote/source fallback. `.stage-part` attributes must accompany the data checkout.
Disk sources/checkout remain immutable during the operation, as specified; reparse
paths and changed bytes are rejected. P/A parsing retains the existing in-memory
JSON validation, while attachment copy/hash is bounded streaming.

The actual 67,642,132-byte P, its 307 attachments, genuine independent A, whole
publication artifact capacity, upload/download and external I have not been run
or changed here. Product candidate stays `d8744c934d86833f9817245ecc7c78b1177b8b76`;
the publication/data revision is a different identity. No commits, pushes, PRs,
dispatches, package/feed/TLS/permission changes or product/model/benchmark reruns.
Parent owns formatting/integration and the atomic commit after independent proof.
