# Actual publication retrieval verifier

`verify-publication-retrieval.fsx` verifies this specific immutable P/A publication
using F# and the .NET BCL. It is offline and read-only. It neither downloads nor
extracts ZIPs, writes P/A/I, changes a ledger, nor creates a finding verdict.
The parent session owns execution against the actual downloaded artifacts and
interpretation of that output.

## Commands

Run from `Q:/repos/funnysharp-goal24-resolution` with the repository-pinned SDK:

```text
dotnet fsi --warnaserror+ artifacts/audit-resolution/resume-20261004/verify-publication-retrieval.fsx -- --help

dotnet fsi --warnaserror+ artifacts/audit-resolution/resume-20261004/verify-publication-retrieval.fsx -- artifacts/audit-resolution/resume-20261004/actual-publication-metadata.json artifacts/audit-resolution/resume-20261004/publication-retrieved-01
```

The two positional arguments are the metadata JSON and retrieval directory.
The latter must contain `provenance.zip`, `index.zip`, and `raw-retrieval.json`.
The metadata's parent directory must contain
`stage-bundle/actual-publication-dispatch.json`. All files and their ancestors
must be ordinary paths without reparse points. No credentials or network access
are needed.

`--help` compiles the entire script with warnings as errors, prints usage, and
returns 0 without opening evidence. Invalid arguments return 2. Verification
returns 0 only after all checks finish; the first failure prints its exception
type and exact condition to stderr and returns 1. Earlier `RAW` output is not a
complete verification result: require process exit 0 and the final `VERIFIED`
line together.

## Checks

- Require publication run `37210014612`, attempt `1`, `release.yml`,
  `workflow_dispatch`, repository `wxxb789/funnysharp`, publication branch
  `audit/goal24-stage-bundle`, and head
  `e7aea9995bac6c87a69db14a8b04c6925cb45d4c`. Current retrieved API metadata must
  say `completed` and `success`; the dispatch record must identify the same run.
- Require exactly the real provenance artifact `11306431210` and index artifact
  `11306386365`, their exact names, repository/run/head bindings, and unexpired
  retention. Stream each raw ZIP's SHA256 and byte count against its API digest
  and size, then independently join the saved raw retrieval records.
- Require exactly one case-sensitive root `index.json`, schema version 1 and
  kind `index`, candidate `d8744c934d86833f9817245ecc7c78b1177b8b76`, exact P/A
  hashes, and the actual provenance artifact URL. Compare embedded artifact
  metadata and stable embedded publication run fields with the current API.
  The embedded run is historical: its `status`, `conclusion`, and `updated_at`
  are not required to equal later completed-success readback. It may have been
  `in_progress` when I was generated. Require I creation after A and after the
  provenance upload, but before the index upload and metadata retrieval. The
  index upload boundary respects GitHub's whole-second timestamp precision.
- Require the exact provenance inventory of `P.json`, `A.json`, `review.md`, 258
  P evidence files, and 49 P HTTP bodies. Reject extras, missing entries,
  duplicates, case aliases, directories, symlinks, unsafe path segments,
  traversal, and duplicate JSON properties.
- Stream all 307 attachment SHA256 values and P-declared length fields. Require
  exact P/A/report hashes, P/A lengths, total attachment byte count, candidate
  and repository bindings, all 13 A-to-P contract joins, A evidence-ID joins,
  the named independent read-only reviewer, the embedded report, and the two
  existing replay receipt hashes and P/reviewer/time bindings. Existing A
  approval fields are read and checked, not reissued as new findings.

Successful stdout includes raw archive sizes/hashes, artifact IDs, the inner I
hash, publication identity and candidate, P/A/report hashes and lengths, exact
inventory counts, attachment byte total, contract count, and reviewer identity.

## Resource bounds and scope

ZIP and attachment hashing uses a 64 KiB buffer, never a whole archive or
attachment byte array. Raw provenance ZIP and cumulative attachment payloads
are each capped at 2 GiB. ZIP entry counts are capped at 310 provenance files
and 1 index file. Structured JSON parsing uses `JsonDocument` with maximum
depth 64 and explicit byte limits: P 80 MiB, metadata and I 2 MiB, and A,
dispatch, raw retrieval records, report, and replay receipts 1 MiB each. P is
the only large structured document retained in memory (actual size 67,642,132
bytes); this does not stream P's JSON tokens. Report and replay base64 are
decoded only within the bounded A document. This is a publication-byte binding
check, not a replay of producer acquisition, source suites, models, denial
experiments, benchmarks, or whole-Goal24 acceptance.

No genuine publication verification is performed by the implementation child.
The parent must read the code and run the real verification command above.
