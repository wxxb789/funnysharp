# Parent final Goal24 manual QA

Product candidate: d8744c934d86833f9817245ecc7c78b1177b8b76. This is main-session real-surface evidence, not final aggregate approval.

| Scenario | Actual result | Evidence |
| --- | --- | --- |
| Publication and external retrieval | Run37210014612/1 succeeded. Both raw ZIP digests and lengths match metadata. I, P, A, report, all310files and307attachments match exactly. | parent-publication-retrieval-proof.json |
| Real full-scale bundle | The309logicalfiles/349parts round trip preserved1,474,888,931bytes, with32MiB maximum parts. | stage-bundle/parent-full-scale-proof.json |
| Edges and adjacent regression | Parent ran115provenance tests:0failed/0skipped. Empty,32MiB,32MiB+1, malformed inventory, ordering, traversal, reparse and tampering cases reject; legacy file/base64/root-artifact/index cases remain. | stage-bundle/parent-class.ctrf.json and parent-class.trx |
| Enforcement | Rule24414858 is active and strict, with four exact contexts, App15368, no bypass actors and caller bypass never. | parent-u15-authorized-provenance-proof.json |
| Ordinary actor denial | Four exact run/job/check/head/attempt tuples each have one intended FAILURE and three SUCCESS checks. Bound signed-in actor wxxb789 had a disabled merge control and no bypass. This is not an HTTP merge-endpoint refusal. | parent-u15-authorized-provenance-proof.json |
| Current product and hosts | Four required checks succeeded at d8744; Windows/Linux/Arm source suites have2568/2570/2570passing cases and no failures/skips. Canonical package and consumer hashes join; Intel remains bounded smoke. | ../u15-authorized/d8744-four-host-binding-audit.json |
| Package PE continuity | All1,327method signatures/bodies/local signatures/exception regions/flags match. All3,604custom-attribute rows match except the exact assembly informational-version source commit stamp. | parent-current-package-pe-continuity.json |
| Accepted65row identity and source | Parent exact65original identity/wording join passed. Declared216dependencies,51reviewed inputs,158semantic files,80performance inputs,45model-visible inputs and22reviewed sources match. | same-product-65-row-reconciliation.json |
| Final feed | Exact NuGet origin responses prove both0.2.0versions absent, with URL/status/hash/time joins before P freeze. | frozen-producer-parent-verification.json |
| Independent A | Parent checked13contract joins, exact report and a genuine post-P packaged CoreSmoke execution. A covers Goals01-13, not whole Goal24. | independent-a-parent-verification.json |

All current transport commands completed and producer bridges were closed. No benchmark, model, old HTTP or denial proof was replayed. Existing cleanup corrections and historical witnesses remain intact. The dedicated clean d8744 review worktree is locked; it must be unlocked/removed after final reviewer termination, never while the reviewer is active.

Explicit limits:65ignored historical obj-cache files are missing; all45model-visible inputs and22reviewed sources still match. Do not claim all188files remain. Historical timing/model observations retain their original PE and environment identities. Exact69final accounting and the whole-Goal24 reviewer remain pending; this matrix does not waive either.
