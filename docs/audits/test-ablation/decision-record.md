# Current test ablation decision record

## Outcome and evidence boundaries

The integration baseline is `refactor/code-simplify` at `0c5416e`.
The unit patches are integrated on `ulw/test-ablation-g001`. Shipping `src/`,
`eng/harness/` implementations and performance manifests/budgets are unchanged.
The sole permanent verifier change is the native-exit decision in the portable
control wrapper; its real false-green and controlled legitimate tuple are
recorded separately from archive/publication acceptance.

`lanes/` contains the original complete assessments, not claims that every
proposed mutation was executed. `final/` contains the final decisions: **1454
unique identities**, comprising 1440 source xUnit methods and 14 named executable
checks. There are **71 affected/simplified identities and 1383 retained**. No whole
method or input partition is deleted or merged. A fingerprint detector is
renamed from `GetSourceFingerprint_IsDeterministicAndCaseInsensitiveOverAGitTree`
to `GetSourceFingerprint_TracksFileCountAndChangedBytes`; baseline identity is
preserved in the crosswalk column. Two POSIX-only Facts remain source obligations,
not Windows skips or claimed Windows executions.

The **755 baseline helper/usage rows** include shared helper ownership and
cross-file uses; they are not 755 independent source files. The final tables
preserve all 755 baseline crosswalks and add one current `ConcurrencyTests.DrainAsync`
cleanup owner, for **756 helper/usage rows: 10 simplify and 746 retain**.
The final Compile receipt binds the source declarations to candidate `03435bb`.
The independent disposition reconciliation corrects 197 stale test/check line
values, 28 helper line locators and 27 additional helper locator references.
Native discovery, strict inventory validation and aggregate gates are parent-owned
receipts under the session evidence directory, not inferred from these CSVs.
File-level helper census is explicitly a lower bound; mixed-file helper reasoning
comes from actual declarations and helper ownership, not file coverage alone.

`VerifyGitHubRuleset_NonActiveRuleset_Rejects` is the additional affected identity:
its CLI diagnostic assertion changed from an English sentence to the structured
ruleset identifier while retaining exit 1. `assertSucceeds` is also simplified,
not unchanged: it retains exit 0 and empty stderr after removal of `successLine`.
`rejectionOf` replaces `messageOf`; `CreateCanceledSourceAsync` now takes a release
Task; and `AssertDiscardTargets` returns compiled assignment operations. Current
helper symbols describe these declarations, with historical names/signatures only
in the baseline crosswalk. A malformed baseline `ResultBoundaryTests.)` usage row
is explicitly an alias of current `CaptureCancellation`, not a new declaration.

## Detector-set reasoning

Each test detects a subset of possible contract violations. Equal names, covered
lines or green execution do not establish containment of those subsets. Different
presence/case/error/cancellation/ordering/consumption/path-trust partitions stay
separate unless a remaining executable owner contains the needed detection.
Unproved redundancy therefore results in retention, not a deletion quota.

The removed six direct empty BCL calls are platform contrasts, not FunnySharp
bridge obligations. Eighteen independently perturbed bridge routes still fail
the retained None/exception/empty-state checks in both old and revised tests.
An unequal-hash assertion rejects a valid Option collision; equal-value hash
requirements and actual equality/presence/conversion remain. Source fingerprint
identity comparisons confirm controlled file alteration only; they do not prove
semantic correctness or provenance, and no expected SHA is recomputed for them.

Analyzer wording and code-fix spacing are replaced by diagnostic ID/count/location
and compiled operation/RHS/await observations. HTTP ordinary wording is removed
while type, eager ParamName guards and wire values remain. The parent preserved a
rejected false-green HTTP candidate and then added prepared mapper exception
identity checks across synchronous, Task and ValueTask mappings.

Async fixture clocks are replaced by nonclock suspension. Concurrency fixtures
subscribe before invocation, hold registered entries, release actual tasks and
observe bounded completion. Valid reverse starts and post-cancel admission are
accepted; source-order output, concurrency caps and cancellation remain required.
Pending cancellation sources and overlapping state evaluations are explicitly
held, rather than relying on Task.Yield or ThreadPool timing.

Harness rejection uses typed exit/error observations. BOM/block fixtures now
distinguish actual stripping faults. Fingerprint schema/algorithm/count/digest
equivalence partitions stay independently tested. Unconsumed performance success
sentences and their shallow nonblank replacements are deleted; actual verdicts,
generated values, copies, cultures and schemas remain.

## Executed experiment receipts

| Unit | Evidence report |
| --- | --- |
| Windows timeout detector | `experiments/platform-timeout.md` |
| Analyzer semantic oracles | `experiments/analyzer-oracles.md` |
| HTTP exception oracles and rejected candidate | `experiments/http-prose.md` |
| Async stream suspension | `experiments/async-fixture-timers.md` |
| Controlled concurrency and legal controls | `experiments/concurrency-fixture-gates.md` |
| Core oracle containment | `experiments/core-oracles.md` |
| Cancellation/composition/state overlap | `experiments/core-lifecycle.md` |
| Harness lexical/rejection/fingerprint detection | `experiments/harness-oracles.md` |
| Remaining ordinary success prose | `experiments/harness-prose.md` |
| Portable native exits | `experiments/portable-control.md` |
| Final ruleset/release output preservation (B1) | `experiments/final-harness-prose.md` |

Every counted fault first compiles successfully; runner failures are distinguished
from compiler/setup failures, malformed filters, intentional controls and outer
timeouts. Rejected and incomplete attempts remain described rather than relabeled.
All temporary shipping/harness mutants are restored. The final aggregate gate,
current discovery/join, manual CLI QA and independent review receipts live under
`.omo/evidence/ulw/01a111dc-d4c4-7a26-bedd-b50ee7f966db/G001-outcome-on-funnysharp-branch-refacto/a1/`.

The experiment references in `final/` identify executed unit/group proofs.
They do not claim an individual mutation for every affected method: shared-fixture
changes also affect healthy-control methods outside the selected fault filters.
Every remaining test/check row is retained, not an unexecuted simplification
proposal. Baseline lane proposals remain historical assessments only.

The first final review of `52cf7ec` rejected two remaining complete-sentence pins
in Ruleset and ReleaseVerify CLI tests. The B1 successor keeps both Facts and all
input/exit/ID/report/locator/policy/status obligations, with twelve identical
old/new faults rejected and two legal reword controls accepted only by the revised
oracles. Its successful mode builds cover 32 native cases; restored full harness
passes 745/745. The original REJECT and failed nullable installer build are retained,
not rewritten as approvals. Independent successor review remains required.

## Strict join and crosswalk schema

Final test/check tables use the exact validator fields
`test_id,source,line,disposition,obligation,fault_class,independent_owner,rationale,experiment`
followed by `baseline_test_id,baseline_disposition`. Final helper tables use
`helper_id,source,symbol,disposition,obligation,independent_owner,rationale` followed
by `baseline_helper_id`. The existing extractor rejects extended headers, so the
parent must validate an exact-column projection under session evidence with
`--include-checks --require-helpers`, preserving every current row and field value.
The original extended tables remain the crosswalk authority. Projection must not
drop identities, helper aliases, new controls or required fields, and must not
change the extractor's strict header or completeness rules. Independently require
one preserved mapping for each of the 1454 baseline test/check identities and 755
baseline helper/usage rows; the sole new helper has no baseline row. Exact projection
instructions and the declaration/ownership checks are recorded in
`a1/final-disposition-join.md` outside the tracked candidate.

## Burden and retained limits

The checkable structural reduction includes seven scheduling timer sites, six
nonproduct BCL contrasts, invalid collision/length oracles, ordinary diagnostic
and success text pins, a redundant completion continuation, and an unnecessary
scratch Git fixture commit/configuration. No test-count reduction is claimed.
Controlled concurrency increases fixture LOC to remove timing luck; this tradeoff
is explicit, and total LOC is not presented as a universal maintenance metric.
Baseline full executions and source-size census are in `baseline.md`; final
same-command receipts and size/discovery comparisons are in the session evidence.
Single standalone timings in unit reports are observations, not an overall speedup.

Existing source-only 43-name census is retained without pretending it establishes
compiled oracle identity. The original core BOM leading-comment control has a
documented false-green limitation; the strengthened wrapper detector rejects the
retained-BOM fault. FSharp LSP is unavailable and some clone CSharp freshness
requests timed out; actual locked compiler diagnostics are the authority.
Genuine portable archive acceptance is not claimed from an empty archive or the
controlled positive tuple. NuGet's default TLS failure and supported-feed recovery
are retained as separate baseline attempts, not hidden by stale no-build passes.
