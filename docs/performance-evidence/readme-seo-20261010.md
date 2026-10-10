# README SEO measurement evidence

This archive retains 401 original evidence files for the README source snapshot, without relabeling the older R9 recording.

- Main: 190 executed rows, 13 receipts, 11 unchanged explicit exclusions; strict allocation and run-binding verification passed.
- Competitor: 40 executed rows, 2 receipts, no exclusions; strict verification passed.
- Current metadata census: 49 types, 499 members, 468 stable and 31 experimental.
- Required resource and generated-delegate witnesses: 24 passed, 0 failed, 0 skipped.
- Raw child logs, reports, preflights, retained binaries, proposals, current census/witness inputs, previous manifests, and both rejected zero-launch attempts are retained under their original repository-relative paths.
- The archive was decoded and compared byte-for-byte with all source files after creation. No additional hash chain was introduced.

The embedded retained-files.json records the original repository root and both measured source snapshots. Strict live receipt verification uses the recorded absolute binary paths; this archive is byte retention, not a portable attestation or a new release approval. A different workspace must generate its own current census and measurements rather than relabeling these objects.

## Verification

Restore the archive entries into the original recorded workspace without overwriting different existing evidence, then run:

```shell
dotnet fsi build.fsx -- -p verify-performance -ReceiptDirectory artifacts/pr-validation/readme-seo-performance-20261010-01/main-attempt-03/results
dotnet fsi build.fsx -- -p verify-performance -ManifestPath eng/performance/competitor-baseline.json -ReceiptDirectory artifacts/pr-validation/readme-seo-performance-20261010-01/competitor-attempt-01/results
```

Timing remains directional. Allocation budgets and benchmark input closure are unchanged; RepositoryProjectToolchain.cs is explicitly included because it fixes solution-wide duplicate project discovery without changing builders, executors, SDK validation, JIT validation, or GC validation.
