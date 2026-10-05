# Goal 24 portable evidence verification

`verify.fsx` uses the .NET 10 BCL and read-only Git blob lookup. It reads the frozen evidence and
the two **actual published raw ZIPs**; it does not produce evidence, make an
acceptance decision, run measurements, call a model, build the solution, or
change files. The retained `verify-final69.fsx` remains an unchanged historical
absolute-locator verifier. Use this new CLI for a different checkout location.

## Inputs and command

Use a checkout containing this directory, the unchanged current `findings.json`,
`original-criterion-matrix.csv`, and `final-evidence-index.md`. The SDK is pinned
by the repository's `global.json`. Both paths are explicit and can be on any
local drive; original `Q:/...` strings are identity keys, never fallback paths.
Git must be available on PATH, and the checkout must retain the immutable source
history named by the catalog. A current file that changed in a reviewed successor
can resolve through its exact catalogued Git blob; that blob's SHA256 and length
are checked before use. No fetch or other Git mutation is issued by this CLI.

```bash
dotnet fsi --warnaserror+ docs/audits/goal-24-resolution/portable-evidence/verify.fsx -- \
  --repository-root /absolute/path/to/funnysharp \
  --artifact-directory /absolute/path/to/downloaded-artifacts
```

The artifact directory must contain `provenance.zip` and `index.zip`, preserving
the raw download bytes. Repacking an extracted artifact cannot satisfy these
digests. `--help` compiles the script and prints usage without verifying proof.
Successful verification prints `PORTABLE_EVIDENCE_PASS` and exits 0; a missing,
unknown, malformed, or mismatched required input exits 1 without that marker.
Invalid arguments exit 2. A compile/help check is not a complete evidence pass.

## Existing publication and bounded retention

The existing publication is
[run 37210014612, attempt 1](https://github.com/wxxb789/funnysharp/actions/runs/37210014612/attempts/1).
Its artifact IDs are fixed, not selected by a latest-run query:

| Raw file | Artifact ID | Raw bytes | SHA256 |
| --- | --- | ---: | --- |
| `provenance.zip` | `11306431210` | 1321390111 | `1bde4542082cd5d49c2950de9a2517946a4658337fb421bfe7bfbb44b501cc0e` |
| `index.zip` | `11306386365` | 2827 | `9298258a7e24ada4419f8ac973851889455fdaa3f6aa1a71fa6dc85bc19df9a1` |

If these existing artifacts remain available and your GitHub account can read
them, the following **Bash** commands download the raw archives. They are a
separate retrieval step, not commands issued by the offline verifier:

```bash
mkdir -p downloaded-artifacts
gh api /repos/wxxb789/funnysharp/actions/artifacts/11306431210/zip > downloaded-artifacts/provenance.zip
gh api /repos/wxxb789/funnysharp/actions/artifacts/11306386365/zip > downloaded-artifacts/index.zip
```

GitHub retention is 90 days. The recorded expiry for both archives is
`2027-01-02T14:38:40Z`; recorded metadata was retrieved on
`2026-10-04T14:45:36.844Z`. Neither metadata nor this README guarantees continued
server availability. Previously retained exact raw ZIPs remain usable offline.
The immutable publication data ref
`92968c03bc89dd139095d3f39611170950223610` retains the original logical P/A and
attachments in the publication data layout. That is a separate source identity,
not a promise of permanent raw ZIP availability. This CLI does not reconstruct
or repack those files and cannot pass using data-ref metadata alone.

## Exact verification boundary

The fixed `locations.json` locator policy is itself SHA256-checked. All 62
retained proofs are checked for physical SHA256 and decoded length/SHA256.
Fifty-five are direct exact UTF-8 LF copies. Seven `*.source-bytes.base64` copies
reversibly preserve source bytes without a final newline or with CRLF; their
decoded original bytes, not a newline-normalized representation, are used for
proof and JSON parsing. No file is decoded to disk.

The verifier checks all 69 original identities and wording against the original
311-row CSV, preserving 40 FAIL and 29 UNVERIFIED judgments. It checks current
`finalAccepted` flags, resolved status, product binding
`d8744c934d86833f9817245ecc7c78b1177b8b76`, and the actual retained native
whole-Goal 24 `APPROVE` with 69 accepted and 0 open dispositions. The publication
A remains a Goals 01-13 attestation; it is not substituted for that later
whole-goal gate.

All 998 direct current-row evidence entries (87 distinct pins) and all 120
final-index pin triples must resolve to actual exact bytes. Sources are decoded
retained proofs, a catalogued Git-current file only when it matches the frozen
object hash, an exact catalogued immutable Git blob, catalogued actual P attachment
paths, P/A/review root entries, or
the explicitly catalogued nested canonical ZIP entries in `http/0007.body`.
The original U28 report hash and its current successor report remain distinct.
The catalog's two directory anchors are explicitly non-authoritative and cannot
satisfy any proof pin. Historical missing generated caches are not fabricated.

Both raw archives, all 310 provenance entries (P/A/review, 258 payloads and 49
HTTP bodies), and P/A/index candidate/hash bindings are verified. SHA256 is
streamed for archives, P, and attachments. Only structured JSON and the small
retained proofs are materialized; nested ZIP access uses a seekable read-only
stream that reopens/discards bytes rather than loading a whole archive or
writing scratch files. No extraction, old absolute-root lookup, network call,
mutable-history operation, new producer, or measurement occurs.

The pass is a reproduction of these frozen byte and identity joins. It does not
recount every numeric cell, establish universal runtime/performance evidence,
rerun historical experiments, or authorize a merge or package publication.
