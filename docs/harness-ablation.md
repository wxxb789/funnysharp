# F# harness ablation

## Current-root replay and parent matched observation

The parent independently executed the current candidate R9 entry and all 56
controls: both exited 0, with 522 objects, 547 locators, 230 rows/launches,
census 499/468/31 and runtime24 retained-record joins. No new measured children
were launched. All 80 original source bindings remain consumed; 77 producing
inputs match current source, while three traced receiver identities are retained
in exact catalog-verified source objects. Current raw policy, approved receipt
set, source inventory, coverage and semantic witness sources are still checked.

Including the added current-manifest/coverage obligations, the final receiver
uses 1,136 SHA calls over 116,913,831 bytes, 83 current source reads and 37 PE
parses, compared with 8,177 calls, 5,347,639,592 bytes, 224 reads and 6,152 PE
parses in the frozen original. Managed allocation was 312,155,192 versus
322,457,344 bytes. The added admission cost over the earlier successor is
+1 digest, +1,392,836 hash bytes and +10,282,312 allocated bytes; this is not
hidden setup or a second packet cache. These are one-run observations.

The repaired paired collector uses explicit immutable baseline and candidate
roots under PowerShell 7. Parent CLI elapsed times were 8,074.7411 versus
4,000.3066 ms first-observed, and 7,662.6998 versus 4,070.7794 ms warm. Both
complete positives preserve the same packet/result marker. Old C rejects in
both roots (exit 1) for genuine stale producer input. The 56-control run exits 0
and reports four disposable fixture writes, zero shared writes, cleaned fixtures
and zero network calls. `parent-offline-measurement.json` retains the actual
commands and output. Launcher peak memory is unavailable; launcher CPU excludes
the FSI child and is not reported as verifier CPU. An initial Windows PowerShell
5 invocation failed because ProcessStartInfo.ArgumentList is unavailable; the
PowerShell 7 run passed. No compatibility dependency was added.


This is the self-contained disposition ledger. Local implementation and the
behavior/cost gates below are verified; aggregate completion additionally requires
the separate frozen-tree independent gate report. The baseline is commit
`72d95c843aaafd9ff9c2e607f132b340b117ec75`, with 75 tracked F# files: 53
`.fs`, 20 `.fsx`, and two `.fsproj`. The complete inventory below classifies
each of the 75 paths. `Retain` preserves the independent behavior and its
decision. `Simplify` preserves that decision while removing redundant work.
`Remove` applies only to a specified active or repeated operation, never to
frozen bytes, historical commands, source receipts, or evidence pins.

Every SHA-256 or SHA-1 value is an identity or comparison input. It can reject
substituted bytes only where an independently held expected value is compared.
An emitted digest, a self-consistent digest, or a digest of source code does
not prove that the code is correct, that tests ran, or that a runtime result is
true. Tests, parser decisions, package execution, semantic joins, and independent
review remain separate owners.

### Baseline inventory: all 75 files

The first 27 paths are the root entry plus compiled harness sources/project.
The following 48 are offline/current stand-alone entries, historical scripts,
frozen snapshots, and supporting test sources. Paths use `/` separators and
are relative to the repository root.

| # | Baseline path | Class | Disposition and retained equivalent owner |
| ---: | --- | --- | --- |
| 1 | `build.fsx` | Current entry | **Simplify.** Thin FSI launcher retains the command surface and routes to compiled `Program.fs`; locked SDK build failure must not dispatch an old binary. Original argument translation, streams, Fun.Build step behavior, and exact child exits remain. |
| 2 | `eng/harness/FunnySharp.Harness.fsproj` | Current support project | **Retain and reuse.** Existing development-only compilation owns harness freshness. Fun.Build 1.2.0 is relocated here; no shipping dependency is added. |
| 3 | `eng/harness/Output.fs` | Current module | **Retain.** Typed outcomes, ordering, first-error collection and stream reporting remain; callers include release, replay, inventory, compatibility and LOC paths. |
| 4 | `eng/harness/Proc.fs` | Current module | **Retain.** Argument-safe process launch, concurrent stdout/stderr drainage and exact child status remain for all callers. |
| 5 | `eng/harness/Repo.fs` | Current module | **Retain.** Nearest solution-marker root resolution remains for direct and stand-alone routes. |
| 6 | `eng/harness/ActionPins.fs` | Current module | **Retain.** Generic full-SHA, version-comment and configured pin-policy checks remain. No current invocation evidence justifies changing the generic configured setup-uv contract. |
| 7 | `eng/harness/DocsSnippets.fs` | Current module | **Retain.** Guide/fence/region membership, duplicates, missing/unused regions and content checks remain. Current case-insensitive line comparison is a pre-existing boundary, not byte-exact behavior. |
| 8 | `eng/harness/Inventory.fs` | Current module | **Retain.** Curated inputs, containment, deterministic output, 12 target logs and no-download behavior remain. The proposed tail-buffer consolidation lacks an executed ablation and is not part of this candidate. |
| 9 | `eng/harness/VerticalSlice.fs` | Current module | **Retain.** Package-only consumers, scenarios, markers, tests and measurements remain. Package digest attribution has a downstream receipt consumer; feed enumeration was not changed. |
| 10 | `eng/harness/ToolingVerify.fs` | Current module | **Simplify.** Remove the four-source/13-literal marker preflight. Retain real restore/build/test/example/format/docs output parsing, stop-at-first-failure and explicit solution-root check. |
| 11 | `eng/harness/Compatibility.fs` | Current module | **Retain.** Package/version/source binding, isolated restore, smoke/trim/AOT and failure decisions remain. Package/DLL hashes have downstream identity consumers and path SHA owns short isolated cache addressing. |
| 12 | `eng/harness/Loc.fs` | Current module | **Simplify.** Preserve the two distinct lexical metrics and output. Cache source lines within one report, reducing 49 reads to 10. |
| 13 | `eng/harness/ReproducibleBuilds.fs` | Current module | **Simplify.** Remove the unconsumed right-input SHA; compute the left-input SHA only for the published report. Comparable-root preconditions, parsed controlled-input equality, three byte-difference diagnostic layers and diagnostic receipt contract remain. |
| 14 | `eng/harness/Ruleset.fs` | Current module | **Retain.** Branch/App/ref, strict status, required contexts and bypass decisions remain. The generic malformed-input findings are reported, not changed under this ablation. |
| 15 | `eng/harness/Performance.fs` | Current module | **Simplify.** Keep current input closure, policy/budget, receipt/row/raw-report, launch/preflight/workload/MVID, census, coverage, source and witness checks. Same-epoch digests are reused. Approved historical receiver identity is handled by explicit bounded metadata, not by weakening current validation. |
| 16 | `eng/harness/PerformanceDocs.fs` | Current module | **Simplify.** Keep approved observation freshness, exact descriptors/rows/regions and verify-no-write behavior. Reuse the fingerprint implementation and approved recording identity; do not make this a second runtime evidence verifier. |
| 17 | `eng/harness/ReleaseProtocol.fs` | Current module | **Retain.** Ordered modes, tokens, command paths, attempts, benchmark inventory and clean-tree policy remain. Parser consolidation is only safe after scalar/case/reparse controls. |
| 18 | `eng/harness/XmlBuildBindings.fs` | Current module | **Retain.** Independently reviewed inputs, successful post-build seal, exact artifacts, capture timing and loaded-assembly MVID remain. Same-epoch duplicate reads may be reused, not across process boundaries. |
| 19 | `eng/harness/ReleaseRun.fs` | Current module | **Simplify.** Preserve clean-tree refusal, immutable attempts, commands/receipts, before/after source and distribution checks, and freeze-on-failure. Remove only unused private helpers and reuse same-epoch bytes. |
| 20 | `eng/harness/ReleaseVerifySource.fs` | Current module | **Simplify.** Preserve raw source/log/protocol/receipt/report and physical inventory checks. Hash log bytes first, compare, then seek and decode from the same open handle. |
| 21 | `eng/harness/ReleaseVerifyArtifacts.fs` | Current module | **Simplify.** Preserve actual API, XML, package/layout/dependency/environment and consumer evidence checks. Text-only API rendering no longer computes discarded DLL hashes; full inventory publication retains them. |
| 22 | `eng/harness/StableApiContracts.fs` | Current module | **Simplify.** Retain all semantic/source/runtime/compiler/XML obligations and each expected-value comparison. Reuse a verified path digest only within one identity epoch; hash the two distinct live XML files once while comparing all 468 rows. |
| 23 | `eng/harness/ApiBaseline.fs` | Current module | **Simplify.** Preserve reflected API text, write/verify distinction and actual diff decisions. Compare-only rendering avoids the discarded DLL digest; full published inventory still emits accurate hashes. Existing set-based order/multiplicity blind spot is outside scope. |
| 24 | `eng/harness/ReleaseVerify.fs` | Current module | **Retain.** Standalone and closing audit entries, ten independently recorded checks and their actual exits remain. Reflection reuse is a possible internal simplification, not removal of audit obligations. |
| 25 | `eng/harness/ReleaseProvenance.fs` | Current module | **Retain.** Independent network/provider, reviewer, P/A/I, bundle part/whole identity and uploaded-readback checks remain. Independent transport epochs and published hash fields were not changed. |
| 26 | `eng/harness/Evaluation.fs` | Current module | **Simplify live code only.** Keep producer/context/feedback bindings, immutable rounds, full history and distinct entry/exit snapshot checks. Frozen Evaluation sources and manifests remain unchanged. |
| 27 | `eng/harness/FrozenReplay.fs` | Current module | **Retain.** Independent original/staged identities, complete retained snapshots, contemporary-support label, before/after mutation checks, safe new output and child status remain. |
| 28 | `docs/audits/goal-24-resolution/portable-evidence/artifacts/audit-resolution/resume-20261004/verify-final69.fsx` | Historical runnable verifier | **Simplify successor only.** Retain all 69 row/status/evidence/catalog/index/gate joins; remove repeated reads only after expected hashes are compared per reference. Original verifier and records are immutable. |
| 29 | `docs/audits/goal-24-resolution/portable-evidence/artifacts/audit-resolution/resume-20261004/verify-frozen-p.fsx` | Historical runnable verifier | **Remove duplicate routine invocation only.** Its attachments are verified by the combined active traversal. Preserve this historical source and its recorded evidence. Its P hash is output-only without a trusted expected P pin. |
| 30 | `docs/audits/goal-24-resolution/portable-evidence/artifacts/audit-resolution/resume-20261004/verify-package-pe-continuity.fsx` | Historical runnable verifier | **Retain.** Exact package DLL identity and metadata continuity comparator remains; byte-array comparisons can replace diagnostic-only tuple hashes in a successor. |
| 31 | `docs/audits/goal-24-resolution/portable-evidence/artifacts/audit-resolution/resume-20261004/verify-publication-retrieval.fsx` | Historical runnable verifier | **Simplify successor only.** Preserve raw archive/provider identity, inventory, reviewer/chronology, report and replay joins. Remove duplicate report hash, not raw or decompressed archive checks. |
| 32 | `docs/audits/goal-24-resolution/portable-evidence/artifacts/audit-resolution/u15-authorized/xml-identity-probe.fsx` | Historical observational probe | **Remove from future gate.** Retain immutable diagnostic history. It only prints SHA/MVID/version and has no expected-value rejection. |
| 33 | `docs/audits/goal-24-resolution/portable-evidence/artifacts/audit-resolution/u15-authorized/xml-method-identity-probe.fsx` | Historical observational probe | **Remove from future gate.** Keep historical diagnostic; it prints method differences but does not fail on them. The rejecting PE continuity comparison remains owner. |
| 34 | `docs/audits/goal-24-resolution/portable-evidence/verify.fsx` | Current portable verifier | **Simplify active successor.** Preserve raw archives, frozen catalog, path/inventory/row/index/gate/locator decisions and 0/1/2 CLI. Remove duplicate hash work only when actual bytes are already verified within that invocation. |
| 35 | `docs/reviews/goal24-pr-20261004/current-performance-evidence/verify-current.fsx` | Current packet verifier, frozen evidence | **Simplify successor only.** Keep exact trust root and source/evidence joins. Old script correctly rejects the changed source; never relabel old receipt as current. |
| 36 | `docs/reviews/goal24-pr-20261004/traversal-r9-performance-evidence/verify-current.fsx` | Current packet verifier, frozen evidence | **Simplify successor only.** Keep R9 snapshot/MVID constraints and all expected comparisons. Historical script and packet remain immutable. |
| 37 | `docs/reviews/goal24-pr-20261004/traversal-r9-performance-evidence/run-controls.fsx` | Historical control runner | **Retain immutable, remove routine replay.** Its existing actual control records remain the evidence owner; fresh candidate controls use new fixtures/receipts. |
| 38 | `docs/reviews/goal24-pr-20261004/traversal-r9-performance-evidence/complete-controls.fsx` | Historical control/audit runner | **Retain immutable, simplify future scanner contract.** New active control must check findings, not just exit/count. |
| 39 | `docs/reviews/goal24-pr-20261004/traversal-r9-performance-evidence/scan-credentials.fsx` | Historical scanner | **Simplify successor.** Keep decoded payload dedup and all encoding/archive scans. Drop unconditional diagnostic hash only if no findings need it, and make findings an explicit rejection when clearance is claimed. |
| 40 | `eng/evaluation/replay-frozen.fsx` | Current independent replay entry | **Retain.** Its dedicated root/argument/exit route is independently callable and does not execute live build dispatch. |
| 41 | `eng/evaluation/studies/audit-resolution-v1/snapshot/environment/build.fsx` | Frozen source snapshot | **Retain immutable.** Incomplete freeze-start source record, not an independently complete 22-module entry. |
| 42 | `eng/evaluation/studies/audit-resolution-v1/snapshot/environment/eng/harness/Evaluation.fs` | Frozen source snapshot | **Retain immutable.** v1 interrupted freeze source; never update to match live edits. |
| 43 | `eng/evaluation/studies/audit-resolution-v2/snapshot/environment/build.fsx` | Frozen source snapshot | **Retain immutable.** Historical exact environment identity. |
| 44 | `eng/evaluation/studies/audit-resolution-v2/snapshot/environment/eng/harness/Evaluation.fs` | Frozen source snapshot | **Retain immutable.** v2 package source behavior differs; keep its exact versioned code. |
| 45 | `eng/evaluation/studies/audit-resolution-v3/snapshot/environment/build.fsx` | Frozen source snapshot | **Retain immutable.** Historical exact environment identity. |
| 46 | `eng/evaluation/studies/audit-resolution-v3/snapshot/environment/eng/harness/Evaluation.fs` | Frozen source snapshot | **Retain immutable.** Distinct v3 accepted-study predicate and evidence identity. |
| 47 | `eng/evaluation/studies/audit-resolution-v4/snapshot/environment/build.fsx` | Frozen source snapshot | **Retain immutable.** Historical exact environment identity. |
| 48 | `eng/evaluation/studies/audit-resolution-v4/snapshot/environment/eng/harness/Evaluation.fs` | Frozen source snapshot | **Retain immutable.** Distinct v4 accepted-study predicate and evidence identity. |
| 49 | `eng/evaluation/studies/audit-resolution-v5/snapshot/environment/build.fsx` | Frozen source snapshot | **Retain immutable.** Historical exact environment identity. |
| 50 | `eng/evaluation/studies/audit-resolution-v5/snapshot/environment/eng/harness/Evaluation.fs` | Frozen source snapshot | **Retain immutable.** Distinct v5 environment; producer shell records do not execute this FSI entry. |
| 51 | `eng/evaluation/studies/audit-resolution-v6/snapshot/environment/build.fsx` | Frozen source snapshot | **Retain immutable.** Replay stages it for environment comparison, but does not execute its pipeline. |
| 52 | `eng/evaluation/studies/audit-resolution-v6/snapshot/environment/eng/harness/Evaluation.fs` | Frozen source snapshot and replay input | **Retain immutable.** FrozenReplay actually loads this implementation; current live edits cannot rewrite v6. |
| 53 | `tests/FunnySharp.Harness.Tests/ApiBaselineTests.fs` | Supporting test | **Retain.** API surface drift/write/no-write/CLI exits remain. New binary-only control verifies omitted compare digest while full inventory digest remains. |
| 54 | `tests/FunnySharp.Harness.Tests/CheckActionPinsTests.fs` | Supporting test | **Retain.** Generic pin and delegated release-action ownership controls remain. |
| 55 | `tests/FunnySharp.Harness.Tests/CompatibilityTests.fs` | Supporting test | **Retain.** Package/source/scenario, RID, restore, publish, execution and failure controls remain. |
| 56 | `tests/FunnySharp.Harness.Tests/DocsSnippetsTests.fs` | Supporting test | **Retain.** Explicit 11 fixture obligations replace the count-only reflection assertion; each actual fixture continues to execute. |
| 57 | `tests/FunnySharp.Harness.Tests/EvaluationTests.fs` | Supporting test | **Retain.** All 43 declaration-presence IDs remain in five data-driven cases; they are declaration checks, not execution of C# semantics. Feedback, context, snapshot and history failures remain. |
| 58 | `tests/FunnySharp.Harness.Tests/FrozenReplayTests.fs` | Supporting test | **Retain.** Pre/staged/post evidence boundaries, contemporaneous support, output safety and mutation rejection remain. |
| 59 | `tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj` | Supporting project | **Retain.** Explicit compilation of 22 test modules and harness project reference remain. |
| 60 | `tests/FunnySharp.Harness.Tests/InventoryTests.fs` | Supporting test | **Retain.** Input, 13 child calls, 35 output inventory, path/symlink containment and target failure cases remain. A platform-conditional skip branch is pre-existing and was not run here. |
| 61 | `tests/FunnySharp.Harness.Tests/LocTests.fs` | Supporting test | **Retain.** Lexical metric fixtures, full 30-line/505 total report and cached-vs-uncached/mutation controls remain. |
| 62 | `tests/FunnySharp.Harness.Tests/PerformanceDocsTests.fs` | Supporting test | **Retain.** Independent fingerprint oracle, generated-region/idempotence/no-write and real-manifest controls remain. |
| 63 | `tests/FunnySharp.Harness.Tests/PerformanceProtocolTests.fs` | Supporting test | **Retain.** Independent hash oracle and unique U10/U13 binding/coverage/witness/budget/mutation cases remain. No test is deleted or weakened to accommodate B. |
| 64 | `tests/FunnySharp.Harness.Tests/PerformanceTests.fs` | Supporting test | **Retain.** Actual budgets, receipt rows, environment, CLI/proposal/exclusion and real-manifest controls remain. |
| 65 | `tests/FunnySharp.Harness.Tests/ReleaseProtocolTests.fs` | Supporting test | **Simplify limited duplicate assertions only.** Remove eight mirrored/private-oracle Facts with the specific ReleaseVerify/Ruleset/ReproducibleBuilds production tests as equivalent owners; keep full protocol command order, execution and unique failures. |
| 66 | `tests/FunnySharp.Harness.Tests/ReleaseProvenanceTests.fs` | Supporting test | **Retain.** P/A/I, artifact, source, denial, uploaded readback, attachment/part/whole hashes and independent reviewer joins remain. |
| 67 | `tests/FunnySharp.Harness.Tests/ReleaseRunTests.fs` | Supporting test | **Retain.** Immutable attempt, clean tree, step, verifier/feed and receipts remain. |
| 68 | `tests/FunnySharp.Harness.Tests/ReleaseVerifyTests.fs` | Supporting test | **Retain.** Actual output markers, aggregate audit checks, fingerprints and log decode/digest tests remain. New same-handle log behavior has deterministic encoding/mismatch controls. |
| 69 | `tests/FunnySharp.Harness.Tests/ReproducibleBuildsTests.fs` | Supporting test | **Retain.** Same commit/input/root checks and informational differing-build output contract remain. |
| 70 | `tests/FunnySharp.Harness.Tests/RulesetTests.fs` | Supporting test | **Retain.** This suite remains sole owner of strict-policy positive/missing/disabled, bypass and integration decisions. |
| 71 | `tests/FunnySharp.Harness.Tests/StableApiContractsTests.fs` | Supporting test | **Retain.** Identity, source/assertion spans, runtime/compiler case IDs/statuses and XML/archive mismatches remain. |
| 72 | `tests/FunnySharp.Harness.Tests/Support.fs` | Supporting fixture source | **Retain.** Temporary roots, workflow builders and fixture helpers have real test callers; not an unused standalone entry. |
| 73 | `tests/FunnySharp.Harness.Tests/ToolingVerifyTests.fs` | Supporting test | **Simplify.** Remove only source-marker tests; retain actual output semantics, root/usage/report/skip controls and POSIX process boundaries. |
| 74 | `tests/FunnySharp.Harness.Tests/VerticalSliceTests.fs` | Supporting test | **Retain.** Consumer packages, versions, markers, summaries and API/scenario failures remain. |
| 75 | `tests/FunnySharp.Harness.Tests/XmlBuildBindingsTests.fs` | Supporting test | **Retain.** Historical reviewed XML pins and post-build source/policy/XML/DLL/PDB/seal/reference-pack failures remain. |

Counts reconcile: 27 root/compiled harness files plus 48 outside files equals
75. The 48 comprise four current operational entries, nine frozen historical
runnable verifiers/probes, 12 frozen snapshots and 23 supporting test/project
files. No file is classified genuinely unused. A frozen source can still be
an active replay input, specifically the v6 Evaluation source. Historical
probes without failure predicates are not future acceptance gates.

### Candidate entrypoints and supporting drivers

The newly compiled `eng/harness/Program.fs` owns the 24-pipeline dispatch
relocated from `build.fsx`; it is a current operational entry, not one of the
75 baseline files. Its dispatch retains every baseline pipeline: build, test,
format, check-action-pins, verify-docs-snippets, verify-api-baseline,
verify-stable-api-contracts, verify-tooling, generate-inventory, vertical-slice,
verify-performance, generate-performance-docs, compare-reproducible-builds,
verify-ruleset, compatibility, release, release-verify, release-provenance,
eval-prep-feed, eval-verify, eval-aggregate, rawloc, loc-extract and default.
The default help omission of `release-provenance` is an existing observed
quirk, not silently fixed. Debug harness outputs are separate from canonical
Release clean/build; the bounded Windows self-lock control passed. No staging
cache or stale-binary fallback is added.

New operational verifier files are `eng/verification/CurrentPacket.fs`,
`verify-current-performance.fsx`, `verify-traversal-r9-performance.fsx`, and
`verify-portable-history.fsx`. The first is shared code, the latter three are
user-facing current entries. Both current packet entries retain their fixed
trust roots; R9 additionally retains snapshot and workload MVID/name checks.
The portable entry retains archive, attachment, row/index/gate and locator
checks and the 0/1/2 CLI. Supporting, non-production drivers are
`offline-packet-controls.fsx`, `measure-current-packet.fsx`,
`measure-offline.fsx`, and `portable-available-controls.fsx`. The separate
evidence directory contains `compiled-entry-self-lock-smoke.fsx` and the paired
marker control scripts; they are reproducible QA drivers, not new product
gates. `recording-identity-projection.json` is a migration derivation, not a
runtime trust registry or producer receipt.

### Hash computation and mismatch-owner map

This map covers every calculation family from the four inventory reports and
the later bounded recording identity and release-log evidence. A helper may
be called many times; each row names the actual expected-value consumer.
`Retain` below means retain the comparison and its mismatch behavior even when
the actual bytes/digest are reused in the same phase.

| Source family | Hash input and present consumer | Retained mismatch decision; allowed simplification |
| --- | --- | --- |
| `VerticalSlice.fs:247-250` | Package nupkg bytes, receipt `sha256` | No local equality predicate. The receipt attributes consumed feed bytes for downstream review. Keep the emitted attribution unless a new receipt contract explicitly removes it; it does not make `vertical-slice` PASS correct. |
| `Compatibility.fs:261-278` | Whole nupkg, published Smoke/trim/AOT DLLs, canonical package DLL entries | Whole package hashes bind canonical package versions/bytes in ReleaseProvenance and ReleaseVerifyArtifacts. Smoke DLL digest compares published bytes to packaged DLL and downstream host evidence. Trim/AOT execution outcomes remain semantic owners; non-Smoke published DLL hashes are diagnostic-only candidates. |
| `Compatibility.fs:285` | UTF-8 output path | 16-hex prefix chooses isolated short NuGet cache directory; no evidence mismatch predicate. Keep address generation for path length/isolation, do not call it tamper detection. |
| `ReproducibleBuilds.fs:111-113` | SDK/global policy, lockfiles, left input JSON attribution, assemblies, PDB/XML, package archives | SDK/build policy/lock lists decide comparable input; the unused right-input SHA is removed, while the left-input SHA remains published as `controlledInputs.inputEvidenceSha256`. Parsed input equality, not either raw input hash, rejects changed controlled inputs. Artifact hashes set `byteIdentical` and first differing layer, but a completed difference remains exit 0. |
| `ReproducibleBuilds.fs:229-237` | Decompressed archive entry content | Feeds package difference diagnostics and entry rows. Candidate lazy diagnostics may avoid entry parsing for identical outer archive bytes, but any byte difference must still classify packages. |
| `XmlBuildBindings.fs:43-180` | Reviewed policy/source files; six post-build DLL/XML/PDB files; build receipt; seal and pointer | Rejects unreviewed inputs, changed successful build outputs, altered receipt/pointer and stale expected policy/XML. Capture happens only after successful build; MVID remains independent of on-disk SHA. Reuse bytes only within one validate/capture phase. |
| `ReleaseRun.fs:140-148,690,705,820,1087,1121,1173,1279,1288` | Source fingerprint epochs, package-index text, protocol, preflight/final version JSON, execution/release records | Before/after source fingerprints veto drift; final distribution observations are a separate epoch. Protocol/preflight/final bytes must match verifier/pointer digests. HTTP 404 digest is emitted over synthetic empty-version JSON, not the lost raw body, so it does not prove response content. Reuse same-epoch preflight bytes; do not cross network/source epochs. |
| `ReleaseVerifySource.fs:179-184,286-336` | Current source file set and aggregate; log files | Source fingerprint expected/actual joins reject drift. Logs require digest mismatch rejection before decoding; exact correction below. |
| `ReleaseVerifySource.fs:919,1018,1020,1296,1393,1419` | Benchmark reports/CSV, receipts, command receipts, proposal and manifest | Report raw bytes compare against receipt; repeated CSV hash is redundant and may reuse earlier verified digest. Other published hashes are references consumed by XML/build/provenance joins; preserve fields and compute from already parsed byte snapshots. |
| `ReleaseVerifySource.fs:1099-1428` | Manifest, physical receipt/log inventory, command rows and canonical commands | Retain manifest versus separate receipt comparisons, fixed physical inventory, path/reparse checks, per-log expected SHA, command order and semantic markers. Remove only the derived in-memory expected-log-path set/count assertion because it duplicates the exact ordinal path formula. |
| `ReleaseVerifyArtifacts.fs:384,499,514,586-614,745-753,870,879,941-943` | Reflected DLL text inventory, reference-pack XML, sealed assemblies/XML, nupkg entries/outer bytes and candidate package files | Text-only API rendering discards DLL SHA and now avoids that calculation; full `public-api.json` still publishes accurate assembly digests. Reference XML, candidate DLL/XML/README and required package bytes have real comparisons. Three selected entry hashes own package layout; outer nupkg hash owns package identity. Other entry hashes are inventory output, not local equality checks. |
| `ApiBaseline.fs` compare path | No primitive hash; previously induced discarded DLL hash through inventory | API text/nullability/constants/member comparison and explicit write mode remain. Binary-only append does not change API text verdict; published inventory must still change its DLL digest. |
| `StableApiContracts.fs:21-24,30-188` | Source/assertion spans, package XML, final case receipts, compiler/runtime data, source excerpts | Each supplied digest is compared to actual bytes; all eight dimensions, case IDs/statuses, diagnostics/emissions and XML targets remain separate. Reuse actual file digest within a resolver epoch but compare every declared expected digest. Code excerpt SHA remains a manifest consistency check. |
| `StableApiContracts.fs:194-248,276-306` | Historical portable catalog/archive and current proof index/XML | Rejects changed catalog/archive/index and every row's wrong expected XML SHA. Hash two actual current XML files once per phase while preserving all 468 row comparisons. Current candidate/release acceptance remains separate. |
| `ReleaseProvenance.fs:29,134-175,222-279` | Raw HTTP bodies/artifacts, blobs, Git SHA-1 object representation and approved normalized text variants | Raw provider status/URL/body, archive, blob and candidate Git identity joins remain. Git SHA-1 is Git object identity only. For allowed CRLF/LF bytes, keep exact allowed-representation comparisons; equal arrays need one actual digest. |
| `ReleaseProvenance.fs:309-521` | Payload manifests, whole nupkg/DLL, host artifact membership, execution/verification/preflight, denial, P/A/replay/report | Preserve exact attachment, package, scenario, host lineage, reviewer/time and P/A/I acyclic joins. Reuse a digest for the same accepted bytes, never remove uploaded readback or independent reviewer/actual command claims. |
| `ReleaseProvenance.fs:558-668` | Streamed part and whole logical file bytes | Both incremental SHA contexts observe one bounded copy pass; part digests diagnose part substitution and whole-file digest binds ordered reconstruction. Keep both format decisions. |
| `Performance.fs:137-184` | Raw policy and sorted path/NUL/file-hash/LF input and protocol fingerprints | Reject raw policy, omitted/changed inputs, protocol-only producer/calibration changes. Preserve two distinct input/protocol aggregate identities. Same-path byte hashes can be shared in one phase. |
| `Performance.fs:191-227,306-378,446-489` | Environment key, snapshot, preflight/launch files, binaries/workloads | Reject mixed environment, wrong candidate snapshot, bad preflight/launch bindings, child/workload/census/MVID/name mismatches, reused launch or bad chronology. A supplied hash does not prove the measured semantic hook ran. |
| `Performance.fs:501-719` | Baselines, source spans, census package DLLs, APM and resource witnesses, policy files | Reject stale source/census/package identity, wrong ranges, row/member omissions, missing/failed witnesses, wrong binary joins and policy drift. Reuse verified bytes within the same phase; compare all rows and witnesses. |
| `Performance.fs:842-1187` | Three raw reports, receipt bytes, all policy rows and optional proposal hashes | Raw reports compare to receipt hashes; exact row descriptors/coverage/budgets/environment remain. Receipt self-hash is only needed when a proposal publishes it. Historical admission uses an exact approved receipt set and actual parsed byte acquisition. |
| `PerformanceDocs.fs:110-162,438-582` | Raw policy/input/protocol fingerprint and generated documentation regions | Retain current input/policy checks, row descriptor/count and exact region/no-write decisions. Share identity derivation only; do not open runtime evidence or treat output digest as semantic proof. |
| `Evaluation.fs:341-353,473-557,581-621,654-856` | Frozen manifests, plan/study/context/invocation/feedback, solution, historical round chain, retained assets | Reject changes, omissions, wrong parent/session/producer and broken correction chain. Reuse predecessor feedback once in one bind and verified snapshot/prompt values within the same pre-child epoch only. Preserve independent before/after child checks and full chain. |
| `FrozenReplay.fs:21,63-138` | Trusted v6 manifest, complete original snapshot, plan, source/staged solution/support and post-run inputs | Reject missing/extra/changed original files before reservation and original/staged mutation after child. Six snapshot-related passes including frozen Evaluation's own two checks have separate boundaries. v6's 65 missing `obj/` files remain a rejection. |
| `verify-final69.fsx`, portable verifier | CSV/proposal/evidence/attachment/catalog/index/row/gate bytes | Compare every row's expected hash. Deduplicate verified object reads by actual object identity; still reject another alias with a different expected digest. Output-only P/index hashes do not create a mismatch owner. |
| Current C/R9 packet verifiers | Catalog/physical and decoded objects, source bindings, receipts, launches, workload/assembly binaries and witnesses | Retain all expected-vs-actual comparisons, source fingerprint and R9-only snapshot/MVID rules. Per-launch repeated SHA and PE parsing may reuse actual verified object identity; never trust a caller-supplied expected digest as cache key. |
| Historical XML identity/method probes | XML/PE identity and method signature/IL tuple digests | No failure predicate for changes. Diagnostic history only, removed as future pass/fail gate; O03 rejecting comparator owns actual continuity verdict. |
| Supporting tests | Fixture-source hashes, independent oracle hashes, package/archive/XML/provenance bytes and git objects | Keep independent construction and mutation negatives; test digests establish fixture binding, not source semantic correctness. Eight identical ReleaseProtocol assertions are removed only because the named production tests keep the same expected decision. |

The approved recording split is bounded to the exact traced receiver files:
main receiver files are `eng/harness/Performance.fs`,
`eng/harness/PerformanceDocs.fs`, and
`tests/FunnySharp.Harness.Tests/PerformanceProtocolTests.fs`; competitor
receiver files are the first two only. All other protocol members remain
current-sensitive. Current input closure, raw policy/revision, receipts,
producer/preflight/launch/workload/MVID, rows/budgets/reports, coverage, census
and witness validators remain required. Historical receiver identities are
reconstructed from separately approved recording metadata and catalog
provenance. Original observation values, receipt bytes, pins and frozen files
are not rewritten. This is bounded **option B**, selected by the parent as best
judgment after the owner question timed out. It is not an explicit owner choice.
The lost incidental quarantine from a receiver hash change is acknowledged;
test/source review owns verifier correctness, not forced remeasurement.

The migration projection independently reconstructed the recorded protocol
from exact catalog-bound source objects. For the main manifest, the raw policy
SHA-256 is
`0f700b3b231a2ae914f526a565d75874680c4036a1c2c50ae90e54ef83ad04b6`, input
fingerprint is
`b4c74b46b265d46ed83e28136e5c4f19e1ec7fd144fa97114dc0cfae018bdbe7`, and
recorded protocol fingerprint is
`765702cfa309615ebad721de84cd992a4ef3c883560703541d3a2f9c6eb8b207`.
Its independently reconstructed snapshot suffix is
`41f49879678dfc2e7b2484900930e061f4e4badaf3e9699b78a0a145f7c90749`. For
the competitor manifest, the corresponding values are
`07e26d0baedb09c5562f824004d2f63df935639d4986e61358c622549d71b35a`,
`0077f4bfafb239c77ead1b660e9f04f5c038750d7acc633769349f990b4e9ba2`,
`b3f5f0ed419a004320407f6e8f6c58d795128bb3ae1e536f6b624e2ffa55eddd`, and
`c2d709af56bf711289d0221769eadd0cf9fb3e6ad2e9504b61a8ba1744b7bd58`.
The unchanged approved observations contain these values. Their agreement is
an identity reconstruction from retained bytes and metadata, not a correctness
claim. The projection binds the exact current benchmark inputs and protocol
sources; the R9 catalog physical hash is
`6508608e4b1d0ae8ce41d176cb45afbbb240c0004c730a69ab1867cd3a5d1b80`.
The candidate inserted 1,361 bytes into the main manifest and 1,113 into the
competitor manifest, of which the metadata itself is 1,019 and 822 bytes.
The original policy, observations, receipt pins, catalog and frozen packet
bytes are unchanged.

### Measured work, exits and limits

Completed current-root evidence is 744/744 harness cases, 1,958/1,958
shipping/analyzer cases, and 162/162 focused recording-identity tests.
Combined controls account for 1,958 shipping plus 744 harness, or 2,702 passes.
These are completed controls, not final acceptance. The earlier pre-ablation
baseline CLI snapshot in `baseline-cli.json` records 678 cases. Counts are not
interchangeable and no test is skipped or edited green.

Matched five-command CLI workload, including each actual FSI process, locked
restore/incremental build and first source recompile, took 147,258.3173 ms on
baseline and 41,858.9887 ms on candidate, an observed 71.57% reduction. It is
one same-machine observation per command. NuGet cache was warm, host contention
was possible, and no fixed-hardware or OS-cold result is claimed. Compiled-entry
preparation included 1.870687 s lock generation, 1.871363 s locked-restore
validation, 9.098007 s first Debug compile and 6.688529 s subsequent help, a
19.528586 s sum of separate commands. These setup costs are reported rather
than omitted. Candidate docs, pins, missing-input and invalid-provenance command
individual times were respectively 7.1543192 s, 7.0575271 s, 5.8417653 s and
6.1874385 s; exit results preserved the actual gate behavior.

The R9 packet same-workload runtime counters fell from 8,177 SHA calls and
5,347,639,592 SHA input bytes to 1,135 calls and 115,520,995 bytes; source opens
fell 224 to 82, PE parses 6,152 to 37. Verifier-only FSI-process CPU fell from
5,656.250 ms to 890.625 ms, managed allocation from 322,457,344 to 301,872,880
bytes, and whole-process peak working set from 381,411,328 to 344,240,128
bytes. These are actual one-run instrumented observations, not medians; FSI
compilation is excluded only from the verifier-only stopwatch but included in
the outer CLI times. Both original and successor R9 checks passed with exact
`objects=522 locators=547 rows=230 launches=230 census=499/468/31 runtime=24`
marker, zero writes and zero network calls. First-observed and warm input cache
results were separately reported; neither is truly OS-cold.

The portable raw `provenance.zip` and `index.zip` were not present at either
provided root. Original and successor portable CLIs therefore both rejected
with exit 1 after verifying the 62 retained objects; no complete raw archive,
307 attachment or nested archive verification PASS is claimed. The original
frozen v6 evaluation manifest declares 188 files and 65 generated `obj/`
inputs are absent, so full replay must reject and no fresh restore can recreate
historical bytes. No archive or snapshot was regenerated.

The release log implementation is specifically **one open handle and two
byte passes**: call `File.OpenRead` once; hash the raw bytes to EOF; compare
against the expected digest and reject immediately on mismatch; only after a
match, seek that same stream to offset zero and decode with `StreamReader` on
the same handle. Thus a bad digest fails before allocating the decoded text
string. This preserves BOM-aware decoding and replacement fallback after raw
integrity succeeds. The old implementation opened the path once for hashing
and again for `File.ReadAllText`. For `k` release commands and `2k` stdout/stderr
logs, opens fall from `4k` to `2k`: 56 to 28 in 14-command benchmarkSkipped
mode, and 64 to 32 in 16-command full mode. Hashed/decoded raw bytes remain
the same logical byte length, now read twice through one stream. Hash decision,
path checks, physical log inventory, manifest/receipt joins and semantic marker
parsing remain. No log timing or allocation speedup was measured.

Other measured savings are source-derived rather than stopwatch claims: LOC
report reads 49 to 10, saving 39 opens and 438,228 byte visits;
ToolingVerify removes four reads/173,623
bytes and 13 source searches; API text-only comparison removes two discarded
DLL hashes while full digest publication remains; stable API XML hashes fall
from 468 to two while all 468 expected values are checked; packet verification
reuses decoded object hashes and PE metadata, with no change to expected-value
comparisons. Every counter has the mismatch owner identified above.

For the performance coverage pass, the source-derived member and witness scan
was 547 source reads totaling 12,985,841 byte visits, reduced to 36 unique
source reads totaling 780,770 bytes. This saves 511 reads and 12,205,071 byte
visits for that specific scan; six separately owned APM source-binding hashes
remain. The temporary path-local decoded-line cache is estimated at about
2.0 MiB of strings and line arrays by CLR object-layout arithmetic, not measured
heap allocation. Another A03 group removes 17 duplicate hash passes: two
baselines, two package DLLs, six APM witnesses and seven resource witnesses.
A01 saves three manifest reads/parses in a canonical Performance invocation,
visiting 555,960 fewer bytes, and 305,893 bytes across two independent Docs
invocations. These are deterministic operation/byte counts, not elapsed-time
or physical disk measurements; they do not include or replace any actual
producer/measurement work.

Actual command exit contracts are per gate, not a universal 0/1/2 rewrite.
Compiled command routing returns the exact child or gate exit. The observed
docs missing-root and invalid provenance examples both return 1. The portable
verifier returns 0/1/2 for pass/verification/usage-or-environment. Stable API
proof verification retains its 0/1/2; ReleaseVerify itself uses 0/1 including
usage failure; compatibility and other modules retain their documented local
mapping. Reproducible byte differences still exit 0 with `byteIdentical:false`.
Do not infer generic exits from `docs/harness.md` prose.

### Current status and explicit final acceptance blanks

The first final independent review rejected tree `78f466e` only for its retained
unconsumed right reproducibility-input SHA (R1). The correction removes the
`PreparedRoot.InputSha256` field and computes `sha256File left.InputPath` only
when publishing the report. An accepted comparison therefore hashes input
evidence once instead of twice; a controlled-input rejection hashes it zero
times instead of twice. Both input files are still parsed and compared, and the
published raw digest still belongs to the left file even when the right JSON
has equivalent whitespace. No artifact comparison or output field is removed.
R1 proof and the corrected-tree re-review are separate execution-ledger artifacts;
the rejected review remains immutable and is not relabeled as approval.

Completed: candidate Debug compile and bare help, Windows Release clean/build
self-lock smoke, candidate five-command matched CLI workload, ToolingVerify
17-case paired marker controls, focused and full current test evidence, 24
offline packet guard controls, two real PerformanceDocs commands, 162 focused
recording-identity tests and both real current packet R9 positive runs. Existing
shipping/analyzer and harness pass counts are as stated above. The candidate
contains 13 scoped implementation/documentation commits through `3588b47`.
The final five-command workload took 30,335.4045 ms versus the baseline
147,258.3173 ms, preserving exits 0/0/0/1/1. Tooling verification passed restore,
Release build/full tests, both executable examples, C# format and docs. Independent
pack created both nupkg/snupkg families, and current PerformanceDocs verified
11 main and 1 competitor regions.

Completed parent-level gates and the distinct final review boundary:

| Acceptance item | Result | Evidence reference |
| --- | --- | --- |
| Candidate portable replay with exact original raw archives | PASS: 62 objects, 69 identities, 998/87 row pins, 120 index pins, actual bundle, no writes/network | Real main-session CLI; original archive directory retained in the execution ledger |
| Matched final-tree CLI | PASS: 0/0/0/1/1, 30,335.4045 ms | `final-cli-receipts.json` |
| Full release protocol | ENVIRONMENT FAILURE, not release acceptance: default NuGet TLS fails before distribution preflight on both unchanged baseline and candidate | `final-release-environment-and-pack.txt` |
| Release build/full tests/examples/C# format/docs and independent pack | PASS, plus current performance docs 11/1 regions | `final-tooling-transcript.txt`, `final-release-environment-and-pack.txt` |
| Historical and producing input comparison | PASS: no changed frozen/producer/coverage paths, all baseline 75 files mapped and 7 new F# files classified | `final-inventory-data-diff.json` |
| Frozen-tree independent gate | Approval/recommendation is recorded separately; no approval is inferred from the local checks in this document | Final independent report bound to the exact HEAD/tree in the execution ledger |

The required `release -AttemptId harness-ablation-3588b47 -SkipBenchmarks`
attempt uses the protocol-computed canonical output directory. Both it and the
untouched baseline reject `https://api.nuget.org/v3/index.json` with
`The SSL connection could not be established`. No TLS disablement, replacement
feed, fake distribution response or historical evidence regeneration was used.
Full release commands after that preflight were not reached. This environment
limit remains separate from successful local tooling, pack, tests and retained
runtime validation. Earlier missing-AttemptId and noncanonical-output calls
correctly rejected with exits 2 and 1 before the actual attempt.

Unrelated findings are left unfixed: docs snippet comparison is
case-insensitive despite byte-exact prose; standalone ruleset malformed bypass
handling is narrower than provenance validation and the wrapper's
`-RepositoryRoot` adapter is mismatched; VerticalSlice summary validation is
narrower than a full total equation; API text set comparison ignores order and
duplicate multiplicity; historical v6 replay inputs are absent; scanner finding
exit behavior is not an explicit rejection. These findings are not smuggled
into the ablation and hashes do not cure them.
