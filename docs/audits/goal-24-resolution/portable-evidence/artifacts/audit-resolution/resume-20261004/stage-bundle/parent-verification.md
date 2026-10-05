# Parent transport verification

The DAG output was treated as a claim. Parent read all four changed files and inspected the actual source/test diff: exactly the authorized .gitattributes, docs/harness.md, ReleaseProvenance.fs and ReleaseProvenanceTests.fs. No P/A, workflow, source-carrier or unrelated file changes.

Parent independently ran the full ReleaseProvenanceTests class:115passed/0failed/0skipped/exit0. Tests preserve legacy path/base64/rootartifact/index cases and reject missing/extra/duplicate/escaping/reparse/tampered/oversize/ordered-part mutations. The asynchronous CLI fixture awaits actual child exit with bounded cancellation, not timed sleeps.

Actual immutable P/A full-scale pack and stage each exited0:309logicalfiles/349binaryparts/1,474,888,931bytes/maxpart32MiB. Parent matched exactP51d55b6d,A6ef82eaf,report1ce284ac and independently rehashed all307rebuiltattachments with zero hash/length mismatches. No outer archive duplication and no P/A reserialization.

The first fresh-worktree formatter exited0butreportedunloadedanalyzerreferences. Parent restored all13solutionprojects inlocked mode with the established dependency proxy/exit0, then reranformatter. Only after its complete result will parent commit the verified source increment. No remote publication or whole69claimyet.

