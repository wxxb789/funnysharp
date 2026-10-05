# Historical b584 local release retention

This packet preserves the already completed local release for commit `b584e748699a4f19d8a901695634be5a85ce8b07`, attempt `r4`. Its recorded outcome is **LOCAL PASS**, followed by the independent **REJECT FG-R9-001** prepublication gate. It does not describe the forthcoming Traverse XML successor, remote CI, a public NuGet endpoint proof, or publication. Original Goal 24 approval at d8744 remains separate.

## Portable objects and inventory

`catalog.json` SHA256: `cc074f460185bc3d6d826bcf4e6117c3d806665118a7f5305228d4e3aa2b00ee`. The packet has 288 SHA256-addressed byte objects, 408 reversible base64 parts, 380 original locators and 113,282,860 distinct decoded bytes. Every original file is recoverable by strict-decoding each object's ordered parts and concatenating their decoded bytes. Original filenames and absolute paths are labels in the catalog, never fallback read locations. Parts carry independent encoded and decoded lengths and SHA256 values. No ZIP was repacked.

`inventory.json` covers all 219 required r4 files, including outcome, execution evidence, 10 release checks, all 14 receipts and 28 command streams, verifier streams, XML seal, version preflight/final, environment/API/XML/package inventories, four package archives, generated NuGet configuration, compatibility results, and the complete four publish directories (155 files). It also lists exact parent release summaries, both final gate reports, 01/02 operational RED records and package-entry objects. The six sealed DLL/PDB/XML aliases resolve to exact r4 nupkg/snupkg entries; current moving bin/obj was not read for these aliases.

`input-graph.json` cites all 7,568 b584 source fingerprint paths using immutable commit/blob/length/SHA256 references, plus the preceding e7 reviewed-input anchor and its changed Option source. A read-only Git batch pass checked 2,840 distinct b584 blobs covering 449,433,193 source bytes, with zero mismatches. The source fingerprints before and after r4 are equal. The packet also retains the exact external System.Runtime.xml (hash-bound), historical policy XML and original hash-bound historical package entries. Both historical policy DLL hashes were recovered from explicitly bound original packages, not substituted with mismatching moving bin DLLs.

## Read-only verification

Python 3 standard library only; no install, builds, tests, workloads, network, release execution or extraction:

```powershell
python docs/reviews/goal24-pr-20261004/current-release-b584-evidence/verify-b584.py
# Optional recheck of immutable Git citations, independent of the current working tree:
python docs/reviews/goal24-pr-20261004/current-release-b584-evidence/verify-b584.py --git .
```

The first command validates catalog and graph pins, physical/base64/decoded object bytes, locators, package entries, r4 outcome/execution/seal/receipt/log joins, source fingerprint equality and four compatibility outputs. It scans every decoded object and every package entry with 13 strong credential-pattern classes across Latin-1 (ASCII-preserving), UTF-16LE and one-byte-shifted UTF-16LE. Match reports are bounded and contain hashes/offsets only. This is not a claim that arbitrary secrets cannot exist outside those patterns.

## Operational RED and exact limits

Retained historical failures are usage exit 2 (missing AttemptId), attempt 01 NU1100/local feed mapping failure, attempt 02 LNK1104/263-character SourceLink path, and custom-output attempt 03 precondition rejection. Actual attempt 01/02 outcome, execution, receipts, logs, compatibility results and configs are retained; none was rerun. The r4 success used a short valid AttemptId and the canonical output path, not a source, AOT, SourceLink, warning or TLS bypass. Mirror feeds remain explicit.

Two exact observed external runner/config files are retained as `operational-input-observed-unpinned`; the r4 manifest did not pin their execution-time bytes. The label does not claim otherwise.

Missing raw-byte limits: the 404 version-response body is represented only by its recorded SHA256 `e64db0fde59206a0cf85c783e644f5e2ffd6a519dbf7d720cc9e5b0ace3d46a4`, with unknown length; no raw HTTP body was retained or synthesized. Usage/custom-output-precondition failures have exact parent JSON records, but no named standalone raw stdout/stderr files. Restore cache and compatibility bin/obj intermediates are explicitly excluded and must not stand in for immutable release evidence. The unrelated original v6 replay's 65 missing obj bytes remain unavailable; this packet does not close that historical experiment gap.

This child performed data-only reads, hashes, ZIP-entry reads, base64 transport and bounded credential scans. No build/test/benchmark, workload/protocol verifier, Git mutation, commit, push, remote message, merge, NuGet action, source/lock/policy edit, cleanup, polling or waiting was performed. Parent owns integration and final verification.
