# Goal 24 evidence portability inventory

Reviewed on 2026-10-04 by read-only child `st_01a107da`. This is an exact-byte retrieval inventory, not another behavioral gate. The parent owns source/accounting integration, final validation and PR publication.

## Result

The authoritative ledger has all 69 original IDs ACCEPTED_VERIFIED (40 historical FAIL, 29 historical UNVERIFIED; 0 OPEN). Its current row evidence has 998 occurrences of 87 distinct files. Of those files, 35 match tracked Git blobs at reviewed refs, 23 are retrievable from the actual P/publication (including four freshly verified nested ZIP entries), and **29 remain local-only**. Every one of the 69 rows depends on the four common local-only proposal, final gate, current source join and PE continuity receipts. Existing P/A/I plus the shipping PR alone cannot retrieve these later proof bytes.

The final evidence index pins 120 distinct existing files: 41 tracked, 28 published/attached, and **51 local-only files totaling 2,964,303 bytes**. These 51 files are the smallest verified additive copy set for all current row and report/index references. Three additional post-accounting closure files total 9,074 bytes; retaining them yields 54 files / 2,973,377 bytes and covers the delivered final handoff. No current direct file is missing and no current direct/index SHA256 pin mismatched. The one literal historical U28 path/hash difference is intentional and separately mapped to retained original bytes.

The JSON companion contains all 69 rows, 998 direct evidence entries, 2,517 current row reference occurrences (including source/binding/retained-boundary selectors), 191 de-duplicated objects, all 120 index pins, report/global references, 309 logical data objects with 349 exact Git part locators, and the complete original-path-to-copy mapping. Two of the 191 objects are directory anchors, not missing proof files. Relative accounting.scope paths resolve against the accounting worktree; they do not mean the files should already exist in the frozen transport worktree.

## Frozen identities and integration boundary

| Role | Ref / relationship |
| --- | --- |
| Reviewed base | a8863473fd53eddc7cb47201508426db2e454f84 |
| Product | d8744c934d86833f9817245ecc7c78b1177b8b76; ordinary release 37135069130/1 |
| Frozen transport | e7aea9995bac6c87a69db14a8b04c6925cb45d4c; direct child of product |
| Final accounting | 19b14119ee3eb33dd42378cafe765376dd6d9f13; separate direct child of product |
| Actual publication data | 92968c03bc89dd139095d3f39611170950223610; direct child of transport |
| Superseded producer input | fd771fa473c5c0378b68d36bc6bd97c4a2944b8c; historical input before P, not actual publication data |

Compared actual Git blob IDs for 60 src/build-policy/API-baseline paths: transport and accounting have zero shipping-input differences from product d8744. The transport delta from product is only `.gitattributes`, `docs/harness.md`, `eng/harness/ReleaseProvenance.fs`, and `tests/FunnySharp.Harness.Tests/ReleaseProvenanceTests.fs`. Accounting adds exactly `findings.json`, `report.md`, and `final-evidence-index.md`. Both increments are needed in an ordinary reviewed PR; accounting is not already part of frozen e7. The data ref adds `input.json` plus 349 binary parts (350 files), not ordinary source.

The four deliberately failing denial branches and the control are isolated proof-only branches rooted at ae54879. Each changes the release workflow, adds `DisposableReleaseDenialTests.fs`, and updates its project file. They must be cited as evidence, not integrated as ordinary shipping changes:

| Context / scope | Exact proof ref |
| --- | --- |
| win-x64 | 854742e824a42925d478054f1b42a3a3ca71089e |
| osx-arm64 | a1c95d3ae6dc40214138832d6538fd7ea154ed53 |
| osx-x64-consumer | bc00df2ed49bb76f0a10d3b095f68a428908eb0b |
| linux-x64 | 0072c3ff6c1b747992ee661fcd34309fa407fd9d |
| control | 0cdab9a9fd73b88b7e42146460f04e0385ec00b9 |

fd771fa remains truthful historical producer-input identity inside P; 92968c03 is the actual attested publication input. No merge, rewrite or relabeling of fd771fa or any negative/control branch is needed.

## Exact retrieval classes

- **Tracked Git blob:** read the JSON object gitMatches.ref/path/blob fields. Use the immutable Git blob, not a later working-tree file. Accounting has its own ref; current final metadata is not the old canonical 65/4 ledger.
- **Actual P attachment / externally published ZIP:** retrieve run 37210014612/1 artifact 11306431210 and select the exact stored payload or body path. P has 258 payloads and 49 HTTP bodies, with 307 attachments / 1,407,188,687 attachment bytes and 13 contracts. The actual provenance ZIP has 310 entries: P.json, A.json, review.md and those 307 attachments.
- **Proof data ref 92968c03:** its root input.json is freshly checked at 153,008 bytes / SHA256 `5745a01e5a148c88fda2c46756760d3522ae5bdf258f6cc41563036d9d8a416d`. It maps 309 logical files to 349 tracked <=32 MiB parts, totaling 1,474,888,931 logical bytes. Concatenate parts in descriptor order as binary bytes, then check each logical length/hash. This preserves original P/A/attachments without requiring a giant Git blob. It does not contain the later whole-goal gate or accounting.
- **Local-only small immutable proof:** exact existing bytes were freshly hashed; no same-byte reviewed Git blob, direct P/data locator, or specifically verified nested locator was found. A file predating P is not automatically in P. A no-locator finding does not claim every uninspected nested evidence envelope was recursively searched. Byte-exact additive copies are narrower and safer than rewriting old ledgers or rerunning proof.
- **Local-only heavy/scratch/missing:** retain the historical declared absences and directory anchors as such. There are no absent current direct proof files in this inventory. No scratch/output directory needs to be merged as ordinary source.

Four files that initially look local-only are actually available inside the P-retained canonical ZIP:

| Catalog / object | Exact nested locator | Bytes |
| --- | --- | ---: |
| OBJ054 | provenance.zip::http/0007.body::xml-build-bindings.json | 1857 |
| OBJ143 | provenance.zip::http/0007.body::release-evidence/public-api.json | 111501 |
| OBJ144 | provenance.zip::http/0007.body::release-evidence/public-api.txt | 82836 |
| OBJ145 | provenance.zip::http/0007.body::release-evidence/xml-documentation.json | 293657 |

The four inner entries were independently extracted in memory and SHA256-matched to current direct pins. The outer http/0007.body digest `c511925be6b9ac9e8fc22670d1a12a0c23630b8528796b71a25acfb67d43ff9a` is reused from P and its already-verified bundle descriptor; the large body was not rehashed. `independent-attestation/review.md` is also published as root review.md and as A.json#/report/base64: decoding yields the same 31,951 bytes / `1ce284acac64c06768a7c6b5dea0bf698581b0dd6404d88d5520e598460326b5`. A exists after P and approves Goals 01-13 only; it is not the later whole-Goal24 approval.

## Proofs that cannot come from P

P was frozen at 2026-10-04T09:31:08.0869669Z (67,642,132 bytes / `51d55b6d1d328c3a0caa69f2ec42ba03b7fdd37e3c6475f8dcefaa2435897d72`). The source reconciliation declares createdUtc 14:50:53.512Z; the proposal declares createdAtUtc 15:30:50.359Z and updatedAtUtc 15:35:30.819Z. The final whole-goal gate evaluates that proposal and the actual later I/retrieval/QA. The PE receipt and final QA/resource census are later parent products and are not among P attachment hashes. Those common current-row pins cannot be retrieved by claiming P contains the final decision.

33 of the 54 currently unportable local objects have filesystem modification times later than P. This is evidence of observed file timing, not an invented creation timestamp; embedded times and successor dependencies are kept separately in JSON. Producer completion (09:31:08.510Z), the later independent-A parent verification (A at 09:49:49.596Z), publication dispatch, I at 14:41:31.891Z, completed metadata retrieval at 14:45:36.844Z, the final source join/proposal/gate, final QA and final closure receipts are distinct later objects. The 21 pre-P local objects still lack a verified exact-byte portable locator; P publication does not automatically cover them.

## All 69 current rows

IDs and dispositions below are copied from final accounting, not new review judgments. Direct gaps select the exact byte objects listed in JSON and in the copy table.

| Tracking | Canonical criterion ID | Original | Current | Direct entries | Local-only direct objects |
| --- | --- | --- | --- | ---: | --- |
| F01 | G14-01 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F02 | G14-12 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F03 | G16-01 | FAIL | ACCEPTED_VERIFIED | 12 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F04 | G16-04 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F05 | G16-05 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F06 | G16-12 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F07 | G16-15 | FAIL | ACCEPTED_VERIFIED | 12 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F08 | G16-E1 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F09 | G17-11 | FAIL | ACCEPTED_VERIFIED | 9 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F10 | G17-19 | FAIL | ACCEPTED_VERIFIED | 9 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F11 | G18-06 | FAIL | ACCEPTED_VERIFIED | 10 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F12 | G18-08 | FAIL | ACCEPTED_VERIFIED | 10 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F13 | G18-13 | FAIL | ACCEPTED_VERIFIED | 10 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F14 | G19-01 | FAIL | ACCEPTED_VERIFIED | 10 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F15 | G20-01 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F16 | G20-02 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F17 | G20-03 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F18 | G20-04 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F19 | G20-06 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F20 | G20-07 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F21 | G20-09 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| F22 | G20-13 | FAIL | ACCEPTED_VERIFIED | 10 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F23 | G20-15 | FAIL | ACCEPTED_VERIFIED | 10 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F24 | G20-19 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| F25 | G20-22 | FAIL | ACCEPTED_VERIFIED | 12 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| F26 | G20-25 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| F27 | G20-27 | FAIL | ACCEPTED_VERIFIED | 12 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F28 | G20-28 | FAIL | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F29 | G21-03 | FAIL | ACCEPTED_VERIFIED | 13 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F30 | G21-04 | FAIL | ACCEPTED_VERIFIED | 13 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F31 | G21-05 | FAIL | ACCEPTED_VERIFIED | 13 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F32 | G21-08 | FAIL | ACCEPTED_VERIFIED | 13 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F33 | G21-12 | FAIL | ACCEPTED_VERIFIED | 13 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F34 | G21-19 | FAIL | ACCEPTED_VERIFIED | 13 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F35 | G22-22.14 | FAIL | ACCEPTED_VERIFIED | 10 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F36 | G22-22.15 | FAIL | ACCEPTED_VERIFIED | 10 | OBJ002, OBJ001, OBJ003, OBJ004 |
| F37 | G23-23.01 | FAIL | ACCEPTED_VERIFIED | 46 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ050, OBJ056, OBJ057, OBJ058, OBJ059, OBJ060, OBJ062, OBJ067, OBJ068, OBJ069, OBJ070, OBJ071, OBJ072, OBJ073, OBJ074, OBJ075, OBJ076, OBJ077, OBJ079, OBJ080, OBJ081, OBJ082, OBJ083 |
| F38 | G23-23.36 | FAIL | ACCEPTED_VERIFIED | 46 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ050, OBJ056, OBJ057, OBJ058, OBJ059, OBJ060, OBJ062, OBJ067, OBJ068, OBJ069, OBJ070, OBJ071, OBJ072, OBJ073, OBJ074, OBJ075, OBJ076, OBJ077, OBJ079, OBJ080, OBJ081, OBJ082, OBJ083 |
| F39 | G23-R.04 | FAIL | ACCEPTED_VERIFIED | 46 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ050, OBJ056, OBJ057, OBJ058, OBJ059, OBJ060, OBJ062, OBJ067, OBJ068, OBJ069, OBJ070, OBJ071, OBJ072, OBJ073, OBJ074, OBJ075, OBJ076, OBJ077, OBJ079, OBJ080, OBJ081, OBJ082, OBJ083 |
| F40 | G18-CANCELLATION-PUBLICATION | FAIL | ACCEPTED_VERIFIED | 10 | OBJ002, OBJ001, OBJ003, OBJ004 |
| U01 | G16-02 | UNVERIFIED | ACCEPTED_VERIFIED | 10 | OBJ002, OBJ001, OBJ003, OBJ004 |
| U02 | G16-E4 | UNVERIFIED | ACCEPTED_VERIFIED | 10 | OBJ002, OBJ001, OBJ003, OBJ004 |
| U03 | G17-32 | UNVERIFIED | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| U04 | G18-14 | UNVERIFIED | ACCEPTED_VERIFIED | 10 | OBJ002, OBJ001, OBJ003, OBJ004 |
| U05 | G18-19 | UNVERIFIED | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| U06 | G18-21 | UNVERIFIED | ACCEPTED_VERIFIED | 10 | OBJ002, OBJ001, OBJ003, OBJ004 |
| U07 | G18-23 | UNVERIFIED | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| U08 | G19-11 | UNVERIFIED | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| U09 | G19-21 | UNVERIFIED | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| U10 | G20-05 | UNVERIFIED | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| U11 | G20-10 | UNVERIFIED | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| U12 | G20-14 | UNVERIFIED | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004 |
| U13 | G20-16 | UNVERIFIED | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| U14 | G20-23 | UNVERIFIED | ACCEPTED_VERIFIED | 11 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| U15 | G21-01 | UNVERIFIED | ACCEPTED_VERIFIED | 19 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ135 |
| U16 | G21-24 | UNVERIFIED | ACCEPTED_VERIFIED | 19 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ135 |
| U17 | G21-25 | UNVERIFIED | ACCEPTED_VERIFIED | 19 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ135 |
| U18 | G21-27 | UNVERIFIED | ACCEPTED_VERIFIED | 19 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ135 |
| U19 | G21-29 | UNVERIFIED | ACCEPTED_VERIFIED | 19 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ135 |
| U20 | G21-30 | UNVERIFIED | ACCEPTED_VERIFIED | 19 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ135 |
| U21 | G21-31 | UNVERIFIED | ACCEPTED_VERIFIED | 19 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ135 |
| U22 | G23-23.03 | UNVERIFIED | ACCEPTED_VERIFIED | 18 | OBJ002, OBJ001, OBJ003, OBJ004 |
| U23 | G23-23.05 | UNVERIFIED | ACCEPTED_VERIFIED | 18 | OBJ002, OBJ001, OBJ003, OBJ004 |
| U24 | G23-23.06 | UNVERIFIED | ACCEPTED_VERIFIED | 18 | OBJ002, OBJ001, OBJ003, OBJ004 |
| U25 | G23-23.08 | UNVERIFIED | ACCEPTED_VERIFIED | 18 | OBJ002, OBJ001, OBJ003, OBJ004 |
| U26 | G23-23.09 | UNVERIFIED | ACCEPTED_VERIFIED | 18 | OBJ002, OBJ001, OBJ003, OBJ004 |
| U27 | G23-23.28 | UNVERIFIED | ACCEPTED_VERIFIED | 12 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ034 |
| U28 | G23-23.29 | UNVERIFIED | ACCEPTED_VERIFIED | 19 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ135 |
| U29 | G23-R.06 | UNVERIFIED | ACCEPTED_VERIFIED | 46 | OBJ002, OBJ001, OBJ003, OBJ004, OBJ050, OBJ056, OBJ057, OBJ058, OBJ059, OBJ060, OBJ062, OBJ067, OBJ068, OBJ069, OBJ070, OBJ071, OBJ072, OBJ073, OBJ074, OBJ075, OBJ076, OBJ077, OBJ079, OBJ080, OBJ081, OBJ082, OBJ083 |

## Minimal additive portability plan

Keep final findings.json, report.md and final-evidence-index.md byte-for-byte from accounting 19b14119. Add a mapping from each original absolute locator to either its exact Git blob, published ZIP entry/nested entry, 929 binary-part reconstruction, or byte-preserving copy. This inventory JSON already carries that mapping; a small parent-owned retrieval/readme wrapper can point to it without modifying historical proof. The copy destination proposed here is `docs/audits/goal-24-resolution/portable-evidence/<original path relative to funnysharp-goal24-resolution>`. SHA256 and bytes are explicit for every destination in JSON.

Copy the following 51 files unchanged to complete frozen report/index and current direct references. Marking Direct means that exact file appears in the current row evidence array; Index-only files remain required by the authoritative final index. The shared OBJ001-004 gaps affect all 69 rows. Preserve line endings, original bytes, statuses, unfavorable results and predecessor hashes; do not reserialize JSON or normalize Markdown.

| Object | Original relative path | Bytes | Required scope |
| --- | --- | ---: | --- |
| OBJ001 | artifacts/audit-resolution/resume-20261004/final69-review-proposal.json | 1158823 | Direct + index |
| OBJ002 | artifacts/audit-resolution/resume-20261004/final-goal24-gate-review.json | 46767 | Direct + index |
| OBJ003 | artifacts/audit-resolution/resume-20261004/same-product-65-row-reconciliation.json | 577752 | Direct + index |
| OBJ004 | artifacts/audit-resolution/resume-20261004/parent-current-package-pe-continuity.json | 1941 | Direct + index |
| OBJ034 | artifacts/audit-resolution/u13-formatted-current-index/attempt-01/verified-file-bindings.json | 440430 | Direct + index |
| OBJ050 | artifacts/audit-resolution/u15-authorized/d8744-portable-dependency-inventory.json | 113573 | Direct + index |
| OBJ056 | artifacts/audit-resolution/resume-20261004/parent-u15-authorized-provenance-proof.json | 23114 | Direct + index |
| OBJ057 | artifacts/audit-resolution/resume-20261004/history-authority.md | 18019 | Direct + index |
| OBJ058 | artifacts/audit-resolution/resume-20261004/stage-bundle/actual-publication-dispatch.json | 811 | Direct + index |
| OBJ059 | artifacts/audit-resolution/resume-20261004/publication-data-commit.json | 554 | Direct + index |
| OBJ060 | artifacts/audit-resolution/u15-authorized/ui-denials/ruleset-readback.json | 1390 | Direct + index |
| OBJ062 | artifacts/audit-resolution/u15-authorized/ui-denials/index.json | 5088 | Direct + index |
| OBJ067 | artifacts/audit-resolution/resume-20261004/frozen-producer-parent-verification.json | 4003 | Direct + index |
| OBJ068 | artifacts/audit-resolution/resume-20261004/producer-local-owner-browser-02.feed-provider/completion.json | 1346 | Direct + index |
| OBJ069 | artifacts/audit-resolution/resume-20261004/actual-producer-p.md | 1675 | Direct + index |
| OBJ070 | artifacts/audit-resolution/resume-20261004/provider-input-binding.json | 3903 | Direct + index |
| OBJ071 | artifacts/audit-resolution/resume-20261004/producer-local-owner-browser-02.feed-provider/attempt.json | 3234 | Direct + index |
| OBJ072 | artifacts/audit-resolution/resume-20261004/independent-a-parent-verification.json | 3027 | Direct + index |
| OBJ073 | artifacts/audit-resolution/resume-20261004/actual-independent-a.md | 1522 | Direct + index |
| OBJ074 | artifacts/audit-resolution/resume-20261004/parent-publication-retrieval-proof.json | 1908 | Direct + index |
| OBJ075 | artifacts/audit-resolution/resume-20261004/actual-publication-metadata.json | 15603 | Direct + index |
| OBJ076 | artifacts/audit-resolution/resume-20261004/publication-retrieved-01/raw-retrieval.json | 821 | Direct + index |
| OBJ077 | artifacts/audit-resolution/resume-20261004/verify-publication-retrieval.fsx | 24367 | Direct + index |
| OBJ079 | artifacts/audit-resolution/resume-20261004/provider-integration-qa.md | 1884 | Direct + index |
| OBJ080 | artifacts/audit-resolution/resume-20261004/stage-bundle/parent-full-scale-proof.json | 1634 | Direct + index |
| OBJ081 | artifacts/audit-resolution/resume-20261004/parent-final-manual-qa.json | 5121 | Direct + index |
| OBJ082 | artifacts/audit-resolution/resume-20261004/parent-final-manual-qa.md | 3275 | Direct + index |
| OBJ083 | artifacts/audit-resolution/resume-20261004/parent-owned-resource-census.json | 1877 | Direct + index |
| OBJ135 | artifacts/audit-resolution/u15-authorized/u28-cohort-report-successor-parent-verification.json | 7503 | Direct + index |
| OBJ147 | artifacts/audit-resolution/u15-authorized/u28-cohort-report-original-884a9707.md | 7142 | Index-only |
| OBJ149 | artifacts/audit-resolution/resume-20261004/final-goal24-gate-review.md | 37706 | Index-only |
| OBJ150 | artifacts/audit-resolution/resume-20261004/final69-review-packet.md | 76377 | Index-only |
| OBJ158 | artifacts/audit-resolution/resume-20261004/verify-package-pe-continuity.fsx | 6589 | Index-only |
| OBJ159 | artifacts/audit-resolution/resume-20261004/same-product-65-row-reconciliation.md | 48925 | Index-only |
| OBJ160 | docs/audits/goal-24-resolution/findings.json | 239802 | Index-only |
| OBJ161 | artifacts/audit-resolution/resume-20261004/remaining-closure-map.md | 20777 | Index-only |
| OBJ167 | artifacts/audit-resolution/u15-authorized/file-input-review-parent-cleanup-followup.json | 1422 | Index-only |
| OBJ168 | artifacts/audit-resolution/resume-20261004/stage-bundle/source-commit.json | 845 | Index-only |
| OBJ169 | artifacts/audit-resolution/resume-20261004/stage-bundle/parent-verification.md | 1315 | Index-only |
| OBJ170 | artifacts/audit-resolution/resume-20261004/provider-surface-qa-02.feed-provider/attempt.json | 3216 | Index-only |
| OBJ171 | artifacts/audit-resolution/resume-20261004/stage-bundle/verification.md | 3932 | Index-only |
| OBJ173 | artifacts/audit-resolution/resume-20261004/active-freeze.md | 2230 | Index-only |
| OBJ174 | artifacts/audit-resolution/u15-authorized/provenance-full-sha-file-input.json | 3059 | Index-only |
| OBJ175 | artifacts/audit-resolution/u15-authorized/review-fix-complete-logs.json | 17484 | Index-only |
| OBJ176 | artifacts/audit-resolution/u15-authorized/d8744-local05-parent-verification.json | 2538 | Index-only |
| OBJ177 | artifacts/audit-resolution/resume-20261004/publication-root-descriptor-verification.json | 1202 | Index-only |
| OBJ178 | artifacts/audit-resolution/resume-20261004/stage-bundle/parent-scope-and-tests.json | 707 | Index-only |
| OBJ179 | artifacts/audit-resolution/u15-authorized/xml-fix-current-source-boundary.json | 14002 | Index-only |
| OBJ180 | artifacts/audit-resolution/u15-authorized/xml-platform-parent-proof.json | 2017 | Index-only |
| OBJ181 | artifacts/audit-resolution/u15-authorized/xml-method-identity-probe.fsx | 1506 | Index-only |
| OBJ182 | artifacts/audit-resolution/u15-authorized/xml-identity-probe.fsx | 545 | Index-only |

Add these three further files if the PR is to carry the delivered final accounting/closure state as well:

| Object | Original relative path | Bytes |
| --- | --- | ---: |
| OBJ189 | artifacts/audit-resolution/resume-20261004/final-handoff.md | 4625 |
| OBJ190 | artifacts/audit-resolution/resume-20261004/parent-final69-closure-proof.json | 2714 |
| OBJ191 | artifacts/audit-resolution/resume-20261004/parent-final-goal24-gate-acceptance.json | 1735 |

Do not copy 1.3 GiB of raw publication ZIP into the ordinary source PR. The immutable data branch provides the logical original bytes; external artifacts provide the original raw ZIP containers and actual I. Data reconstruction does not recreate the same raw ZIP digest. If long-term original-container retention is required, the parent should retain the existing raw ZIPs in an authorized durable external location, with their already-established hashes and real run/artifact identity. This is separate from the 51 small-file source-PR copy set.

| External artifact | Original raw ZIP bytes | SHA256 | Expiry recorded by metadata |
| --- | ---: | --- | --- |
| 11306431210 | 1321390111 | 1bde4542082cd5d49c2950de9a2517946a4658337fb421bfe7bfbb44b501cc0e | 2027-01-02T14:38:40Z |
| 11306386365 | 2827 | 9298258a7e24ada4419f8ac973851889455fdaa3f6aa1a71fa6dc85bc19df9a1 | 2027-01-02T14:38:40Z |

Actual URLs:

- https://github.com/wxxb789/funnysharp/actions/runs/37210014612/artifacts/11306431210
- https://github.com/wxxb789/funnysharp/actions/runs/37210014612/artifacts/11306386365

The API artifact download URLs and exact part/blob locators are in JSON. Existing verified metadata was retrieved at 2026-10-04T14:45:36.844Z; this child did not perform a fresh network retrieval or assert current access for another reader. Raw P/ZIP digests are explicitly reused, not claimed freshly recomputed.

## Historical bytes, missing coverage and sensitive paths

The old canonical 65/4 findings.json (`93cd7f82ce070247efbcef8b9cd3e0bdabdbb1cefb6cf3e03675f6fda9ea1823`) is a different object from final accounting (`5064a7f5c6e9daa11e90691a71cdb9e50167071257339fd0e9348506f77d00c0`). It is among the retained index-only local copies; it must not overwrite final metadata. The historical U28 expected `884a9707...` resolves to OBJ147, `u28-cohort-report-original-884a9707.md`; current ai-cohort-report-v6.md is `1bdae18f...`, with accepted successor E42/E43. OBJ146 records the literal historical path/hash mismatch as historicalExpected, not a current evidence failure.

The historical v6 snapshot still records 123 present matched / 65 missing ignored obj paths out of 188; those exact missing-path records remain in the proposal and JSON. Historical 15 missing benchmark receipts remain missing with accepted bounded successors. No cache/benchmark evidence was recreated. This direct-reference inventory is not a new recursive replay of all transitive model, performance, HTTP or session sources.

Separate security review should inspect metadata/path categories before public copying: `history-authority.md`; `.omo/audit-resolution/research/*`; model/cohort reports and study/native-protocol inputs; source-bound owner/maintainer verdicts; `ui-denials/*`; `*.feed-provider/attempt.json` and completion metadata; provider bridge/owner scripts and acquisition records; actual publication/raw-retrieval metadata; and P http/0000-0048.body with their nested archives. The JSON lists each inventoried candidate and all 49 raw body paths. Historical session JSONL/loop ledger locations are path-only candidates; their raw contents were not scanned. No tokens, authorization headers or effective signed download URLs were printed. This inventory does not certify those paths as free of sensitive data.

## Evidence and limits of this track

Small referenced objects were freshly SHA256/Git-blob hashed, all 120 index pins and all current direct pins matched, the 929 descriptor was freshly hashed and matched its tracked blob, four targeted canonical ZIP entries and the A embedded report were freshly verified, and the large published ZIP central directory was inspected for the exact 310-entry count. P and original raw ZIP whole-file digests/large attachment hashes were reused from actual previously verified descriptors/metadata. No new tests, build, benchmark, model execution, HTTP, denial, P/A/I publication, source/evidence edit, Git write or remote mutation occurred.

Completion blockers for the parent are concrete: integrate the three accounting files with transport source, provide the 51 exact-byte local supplements and mapping (plus the three final closure supplements if desired), preserve separately retrievable 929 data and actual I/raw ZIPs, then perform the independent security lens and parent-owned final PR validation. There is no additional original-row acceptance blocker asserted by this portability inventory.
