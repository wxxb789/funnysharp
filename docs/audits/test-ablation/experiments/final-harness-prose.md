# Final harness CLI prose delta (B1)

## Removed obligation and remaining owners

The independent final review rejected candidate `52cf7ec`, tree `43d78f7`, for
two remaining complete human-sentence assertions. Both existing Facts and every
original input partition remain. No shipping or harness implementation is changed.

`VerifyGitHubRuleset_RecordsStrictPolicyEvidence` retains exit 0, empty stderr,
the transported whole ruleset ID token `42`, the actual outputPath locator, a
readable generated JSON report, both policy booleans, required-context/check counts
and JSON rulesetId. It no longer owns ordinary success-sentence identity. A word
boundary prevents accepting ID 142, or a random temp-path substring, as ID 42.

`Main_AllChecksFail_WritesFailedReportAndNamesTheEvidenceReport` retains exit 1,
the actual joined evidence-report locator, generated report readable at that
location, FAILED status, failure section and JSON evidence existence. It no longer
owns the English sentence or PowerShell's doubled separator spelling. The inline
replacement of the doubled separator with the native directory separator admits
the historical locator and a normal native Path.Combine locator; it does not
parse or depend on sentence grammar. A wrong report name or absent locator fails.

## Actual old/new experiment

The parent used one isolated worktree off 52cf7ec and installed the same bounded
fault switches in the real Ruleset.mainWith success branch and ReleaseVerify.mainWith
failure/report branch. Each invocation set FUNNYSHARP_ABLATION_B1 for exactly one
case. There were no mocks replacing the CLI emit/report integration. Old tests
were restored byte-for-byte before their matrix, and both module originals were
restored after all cases. No fault switch is part of the final candidate.

Both modes had a successful actual build with zero warnings/errors before native
test execution. Each selected fault runs exactly one existing Fact. The unchanged
healthy input runs both Facts and passes 2/2. Native result XML requires exact total,
failed, skipped and errors counts; exit alone is insufficient.

| Same installed scenario | Old | Revised | Remaining observation |
| --- | --- | --- | --- |
| rules-wrong-exit | 1/1 failed | 1/1 failed | Native exit code |
| rules-missing-evidence | 1/1 failed | 1/1 failed | Actual JSON report read |
| rules-wrong-policy | 1/1 failed | 1/1 failed | strictRequiredStatusChecksPolicy boolean |
| rules-wrong-id | 1/1 failed | 1/1 failed | JSON rulesetId |
| rules-id-loss | 1/1 failed | 1/1 failed | Whole transported ID token |
| rules-locator-loss | 1/1 failed | 1/1 failed | Actual outputPath in stdout |
| release-wrong-exit | 1/1 failed | 1/1 failed | Failure exit code |
| release-missing-report | 1/1 failed | 1/1 failed | Actual evidence-report read |
| release-wrong-status | 1/1 failed | 1/1 failed | FAILED report status |
| release-missing-json | 1/1 failed | 1/1 failed | JSON evidence existence |
| release-wrong-locator | 1/1 failed | 1/1 failed | Joined report locator |
| release-locator-loss | 1/1 failed | 1/1 failed | Joined report locator |
| rules-legal-prose | 1/1 failed | 1/1 passed | Same ID/report/locator, new ordinary sentence |
| release-legal-prose | 1/1 failed | 1/1 passed | Same failure/report, normal native locator |
| healthy | 2/2 passed | 2/2 passed | All original healthy inputs |

This is 30 scenario invocations and 32 native case executions: 24 actual behavior
fault rejections, four healthy cases and four legal-control cases. Two legal-control
old failures are wording false-reds, not additional behavior faults. There are two
successful mode builds, not thirty independent compilations.

Receipts are under the session evidence root
`.omo/evidence/ulw/01a111dc-d4c4-7a26-bedd-b50ee7f966db/G001-outcome-on-funnysharp-branch-refacto/a1/b1-proof/`.
`new-corrected/receipt.json` and `old/receipt.json` retain every PID, exit, exact
XML count, expected result and completed process; adjacent build/native stdout,
stderr and XML are retained. The first `new/` installer build failed F# nullable
diagnostics and is preserved but excluded from every rejection/pass count.

## Restoration and whole harness

The final patch changes only the two existing test files. `git diff --exit-code
HEAD -- src eng/harness eng/performance` returned 0 after module restoration.
The revised tests were then restored, the actual harness rebuilt, and the complete
native suite passed **745/745, errors 0, failed 0, skipped 0, not-run 0**, 24.851 s.
The complete restored result is `b1-proof/restored-harness.xml`; the selected healthy
preflight also passed 2/2 before any fault installation. The normal whole candidate
gate and fresh independent delta review remain separate final acceptance steps.

The reduction is two complete-sentence assertions and one explanatory comment
whose only purpose was to freeze a doubled separator. ID, locator, output creation,
policy, status and payload detectors all remain independently executable. This
does not establish portable archive or performance-recording acceptance.
