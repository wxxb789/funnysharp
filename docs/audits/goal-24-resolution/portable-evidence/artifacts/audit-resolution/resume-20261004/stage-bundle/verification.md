# Independent stage-bundle verification

Date: 2026-10-04. Worktree: `Q:/repos/funnysharp/.worktrees/goal24-stage-bundle`.

## Verdict

**PASS for the bounded stage-only transport contract.** This is not a publication,
remote upload, or Goal 24 acceptance claim. The requested `publication-run-plan.md`
was not present in the evidence directory; this report uses the commands recorded
in `implementation.md` and `docs/harness.md`.

## Scope and source inspection

`git diff --name-only` exited 0 and returned exactly these four files:

```text
.gitattributes
docs/harness.md
eng/harness/ReleaseProvenance.fs
tests/FunnySharp.Harness.Tests/ReleaseProvenanceTests.fs
```

`git diff --stat` reported 520 insertions and 2 deletions; `git diff --check`
exited 0. Direct inspection confirmed pack-stage-bundle streams sources into
ordered `.stage-part` files and writes `input.json` last; stage-attestation
reconstructs into scratch while checking part and whole-file length/hash, exact
P-derived inventory, and final P/A/report validation. `.gitattributes` marks
`*.stage-part binary`.

## Build and exact full-class run

Build command (exit **0**):

```text
dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj --no-restore
```

Result: Build succeeded, 0 warnings, 0 errors.

The required class was run exactly once against that build, with all requested
xUnit flags:

```text
dotnet run --project tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj --no-build -- -class FunnySharp.Harness.Tests.ReleaseProvenanceTests+ReleaseProvenanceTests -noColor -result-ctrf Q:/repos/funnysharp-goal24-resolution/artifacts/audit-resolution/resume-20261004/stage-bundle/independent-class.ctrf.json -result-trx Q:/repos/funnysharp-goal24-resolution/artifacts/audit-resolution/resume-20261004/stage-bundle/independent-class.trx
```

Exit **0**. xUnit summary: **Total 115, Errors 0, Failed 0, Skipped 0, Not Run
0**. CTRF summary: tests 115, passed 115, failed 0, pending 0, skipped 0.
Both CTRF and TRX were created.

The class covers missing/extra/duplicate logical files and parts, traversal,
reparse points, changed/tampered hashes, ordering and bounds, plus legacy
stage/index and independent replay. Representative cases include `missing-file`,
`extra-file`, `duplicate-file`, `duplicate-part`, `file-traversal`,
`part-traversal`, `part-reparse`, `input-reparse`, `extra-part`, and
`missing-part`.

## Real CLI pack/stage and byte round trip

Fresh output directories were used with the retained deterministic fixture:

```text
dotnet fsi build.fsx -- -p release-provenance -Mode pack-stage-bundle -InputPath Q:/repos/funnysharp-goal24-resolution/artifacts/audit-resolution/resume-20261004/stage-bundle/cli-focused-green/local.json -OutputDirectory Q:/repos/funnysharp-goal24-resolution/artifacts/audit-resolution/resume-20261004/stage-bundle/cli-independent-packed
```

Exit **0**.

```text
dotnet fsi build.fsx -- -p release-provenance -Mode stage-attestation -InputPath Q:/repos/funnysharp-goal24-resolution/artifacts/audit-resolution/resume-20261004/stage-bundle/cli-independent-packed/input.json -OutputDirectory Q:/repos/funnysharp-goal24-resolution/artifacts/audit-resolution/resume-20261004/stage-bundle/cli-independent-staged
```

Exit **0**.

An independent filesystem SHA256/length driver compared every reconstructed
staged logical file against the original fixture without JSON reserialization:
**19 logical files, 33,560,729 total bytes, 20 parts, 0 mismatches,
all_exact=True**. This includes the empty HTTP body and the 33,554,433-byte body
crossing the 32 MiB boundary. Originals remained unchanged.

## Boundaries

No remote publication, upload/index operation, real 67,642,132-byte P, 307
attachment run, or all-69 acceptance run was performed or claimed. No source,
test, P/A, or tracked worktree file was modified; only this report and local
verification output directories were created.
