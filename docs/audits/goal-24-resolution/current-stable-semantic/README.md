# Current stable semantic contract gate

This append-only successor preserves the original 468 stable identities and 3,744 dimensions. Historical runtime receipts and compiler observations remain historical. All current source excerpts, assertion bodies and helper bodies match the retained proofs exactly; the successor records only the necessary file hashes and coordinate migrations.

Run the enforced gate after a successful canonical release on the same clean Git candidate:

```text
dotnet fsi build.fsx -- -p verify-stable-api-contracts --proof-index docs/audits/goal-24-resolution/current-stable-semantic/current-proof-index.json --release-evidence artifacts/release-candidate/<candidate>/<attempt>/release-evidence/release-evidence.json
```

The required Windows release job runs this command before publishing its canonical candidate artifact. It verifies the immutable original index, every declared portable input, the actual checkout's source and assertions, the exact case-ID and compiler-source joins, and the current release DLL/PDB/XML seal. Evidence for another Git candidate or a dirty worktree cannot satisfy it.

`inputs.zip` and `input-catalog.json` retain exact historical bytes and both current XML snapshots. Historical logical paths, including original absolute compiler-source strings, are aliases in the byte catalog, never filesystem fallbacks. Current `src/` and `tests/` inputs must come from the checkout, not an archive. The original index, policies, receipts and adverse outcomes are unchanged.

The credential scan is bounded to the selected packet, package members and index/catalog bytes. It is not clearance of unselected private history, whole artifact roots, encrypted values or arbitrary credential formats.
