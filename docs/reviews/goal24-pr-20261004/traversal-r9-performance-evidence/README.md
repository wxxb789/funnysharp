# Traverse R9 source-snapshot performance evidence

This distinct packet retains the completed fresh serial main/competitor pair: 13 successful main receipts with 190 rows, and 2 competitor receipts with 40 rows. The measurements used the working source based on parent commit `b584e748699a4f19d8a901695634be5a85ce8b07` plus 29 XML-only comment corrections in three files, before a new Git commit. `measuredSourceCommit` is null. The parent is not asserted to be the exact measured Git source. Both exact source snapshots, input/protocol fingerprints and all 80 working-source bindings are in the catalog. The unchanged preflight `baseCommit`/`baseTree` values are protocol anchors, not substitutes for the measured-source identity.

## Read-only portable verification

From a complete checkout with .NET 10:

```powershell
dotnet fsi --warnaserror+ docs/reviews/goal24-pr-20261004/traversal-r9-performance-evidence/verify-current.fsx -- .
```

Catalog SHA256: `6508608e4b1d0ae8ce41d176cb45afbbb240c0004c730a69ab1867cd3a5d1b80`. The catalog contains 547 logical locators and 522 SHA256-named reversible base64 objects: 48,750,487 distinct decoded bytes, 67,637,251 decoded bytes counting aliases, and 65,001,970 physical base64 bytes. It is the full logical file/hash/byte map; `packet-files.json` is the landed physical file inventory. Every catalog path is an absolute Q-drive locator label. Old absolute paths resolve only through the catalog; there is no original-filesystem fallback.

The verifier reuses the sibling BCL FSI v1 guards for policy raw-text hashes, source/protocol fingerprints, exact verifier proposals, allocations, row identities, actual launch joins, retained PE/MVIDs, census totals, and CTRF/TRX witnesses. It additionally derives the source-snapshot identity using the unchanged production formula and checks workload PE MVIDs. It reads only checkout source files and packet byte objects, writes nothing, makes no network request and runs no benchmarks or runtime tests. `run-controls.fsx` captures exact stdout, stderr and exits for one landed positive and three skeletal early-failure fixtures; `scan-credentials.fsx` performs the bounded credential-pattern scan. Actual results are in `verification.json` and `credential-scan.json`.

## Retained closure and boundaries

The landed positive ran once (exit 0), and the three real negative controls ran once (exit 1 each, no PASS). The final control audit/scanner runner completed with exit 0 on `mon_WY6CGM2A7FD6B0BP`. Four scanner-only RED attempts retain their exact results and failed sources in `scanner-red-01.json` through `scanner-red-04.json`: two F# offside errors, a missing explicit BCL reference, and source-marker overmatching. `complete-controls.fsx` audits the unchanged real control records and runs only the corrected scanner; the positive and negatives were not repeated. `control-fixture-sources.json` retains all seven exact fixture source bodies/hashes. Fixtures are intentionally skeletal because the requested defects fail before later objects or source files are needed.

The final bounded scan returned exit 0 and zero findings over all 522 decoded objects (48,750,487 bytes), plus 230 distinct decoded workload JSON payloads (3,263,821 bytes), with 11 credential pattern families in Latin-1 and both UTF-16LE alignments. There were no retained ZIP/TAR/GZIP archive candidates or entries. This result is bounded pattern evidence, not universal credential clearance.

All 230 actual launch logs, 45 raw CSV/Markdown/HTML reports, both BenchmarkRun logs, receipts, source preflights and their byte-distinct receipt copies, actual loaded/workload PEs, census 499/468/31, binding-check 230 launches, and the 24-case CTRF/TRX closure are retained. Producers, the witness project/lock/NuGet.Config, all 69 linked test source files plus the shared source, shipping/benchmark projects and locks, current installed manifests, current coverage and parent operation records are retained. The original preflight files have no terminal CRLF; their receipt copies have one. Both variants retain their real original hashes. No source hash has been rebound.

Operational RED records remain visible: strict verification initially used the wrong ReceiptDirectory, and documentation generation initially used the wrong Root. Original truncated output bodies remain byte-identical, not expanded into invented full logs. The actual corrected strict monitors are `mon_H2GSB3ABGBPVAAMV` and `mon_4M6KPXGFZ1456729`, both exit 0, as recorded in the retained installation record. Earlier preparation records with PENDING fields remain historical step records, not final verdicts.

The credential scan covers decoded objects, recognized ZIP/TAR/GZIP entries and decoded workload markers in Latin-1 and both UTF-16LE alignments. Findings contain only bounded hashes/offsets, never matched secrets. This is not universal credential clearance, an entropy proof or encrypted-archive coverage. No unbound caches or generated bin/obj trees are retained. No missing bound bytes were found. The original d8744 three-repetition evidence, b584 release proof, old packets and reviews are separate and unchanged. Timing remains directional; this packet proves neither a fresh release, public feed/package-layout acceptance nor remote CI. Parent owns the 12 guide regions, combined validation, publication and cleanup.
