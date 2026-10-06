# Remaining harness prose ablation

## Scope and boundary

This experiment starts at `0fd02f4b81c1240cb178a3fca84e52880dd4809a` in
`ulw/test-ablation-harness-prose`. The initial worktree was clean. Permanent
edits are limited to the assigned harness tests and this report. RulesetTests,
EvaluationTests and ReleaseVerifyTests are parent-owned and untouched. No
implementation edit, framework change, dependency, skip, warning suppression,
commit or remote write is permitted. All temporary implementation mutants are
applied and restored serially with apply_patch.

## Contracts inspected and retained candidates

The scoped test bodies, implementation output paths, root/eng/docs rules,
product-contract.md, grammar.md, harness.md and the original harness lane report
and CSV candidate records were read. The following candidates remain:

- DocsSnippets success wording is not ordinary unconsumed prose:
  ToolingVerify.DocsVerdictFragment consumes its exact marker grammar. Preserve
  the current snippet count and every fixture partition, error prefix, marker
  and source-copy obligation.
- ToolingVerify explicitly declares first/last human lines and tail headers
  byte-exact contracts in its implementation header. The PASS/SKIP step names,
  bounded tail and parsed JSON ownership also remain unchanged.
- Negative diagnostic helpers distinguish preconditions/branches. Without
  isolated accept-invalid mutants for every input partition, replacing all
  fragments with nonempty stderr would admit unrelated failures. Retain them.
- Inventory titles flow into generation argv and exported markdown; include
  filters own exact type scope. Frozen reproduction and missing independent
  scope witnesses preclude relaxing these tables.
- LOC report includes 29 target identities and method presence. Its independent
  cache/uncached counting tests do not alone prove all real-target inventory
  detectors; retain the report snapshot and every encoding partition.
- Compatibility source ordering, package mappings, isolated path budgets and
  evidence field ordering are retained. Source formatting, UTC spelling and
  cache-key shape have no executed legal-control proof in this bounded unit.
- Release workflow/context regexes and workflow-script extraction carry actual
  transport trust and argv contracts. No YAML parser or abstraction is added.
- ReleaseProvenance stage positives are per-partition setup controls; every
  ordered part, attachment, reparse check, raw hash and 32 MiB boundary remains.
- Independently expected raw receipt/report/source/assembly/MVID/XML hashes are
  genuine joins, not self-hash correctness claims. Preserve them and the
  observation JSON ordered key set. The documentation-fixture policy rebind
  branch is retained until a separate raw-policy/culture witness proves it.
- VerticalSlice timeout coverage is already parent-integrated in the base;
  its actual Windows child, exit 124 and failed receipt remain untouched.

## Invocation and initial compile

Every command explicitly starts with
`cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-prose &&`.

The compile command is:

```bash
dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -p:RestoreLockedMode=true -p:RestoreSources=https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json --nologo -v:minimal
```

Initial compile: exit 0, 0 warnings, 0 errors, 17.31 seconds. Build output is
recorded by monitor `mon_4JAW4YYEDYT783DC`. Native xUnit filters use -class or
-method with -failSkips -failWarns -noLogo -noColor and a fresh -xml receipt
under `artifacts/harness-prose/`; compile failure and a launched run never count
as a successful fault witness. Proof results are appended below only after
completion.

Initial original control, `mon_6HM0DKS5A5TNQX1V`: exit 0, 205 total, 205 passed,
0 errors, 0 failures, 0 skips, 0 not run, 9.117 seconds. This selected the
PerformanceVerifierTests and PerformanceProtocolTests classes, all three
PerformanceDocs classes, ReleaseRunTests, both ReproducibleBuilds classes and
ToolingVerify PipelineVerdictTests. An extra `ToolingVerifyTests+ReportTests`
filter matched no class; the actual class is ReportingTests. This control is
not a full-suite result. The initial native `-xml` alias emitted a deprecation
notice; all subsequent receipts use supported `-result-xml`.

The original ReleaseRun header references an exact release-protocol console
contract. ReproducibleBuilds declares byte-copy parity with the original verdict
and precondition messages. These two additional candidates remain untouched
instead of treating explicit copy contracts as arbitrary prose.

The bounded edits are unconsumed performance verification/generation success
sentences. The documentation generator's final fixture policy rehash/rebind is
retained: no raw-policy/fixture-construction containment witness was executed
for its removal. Existing generated markdown rows and JSON ordering remain
contractual bytes. No SHA check is removed without its requested separate proof.

## Original compiled witnesses

All witness commands use the compile command above followed by:

```bash
dotnet tests/FunnySharp.Harness.Tests/bin/Debug/net10.0/FunnySharp.Harness.Tests.dll -method "*GeneratePerformanceDocumentation_GeneratesThenVerifies_UnderInvariantCulture" -method "*GeneratePerformanceDocumentation_GenerateIsIdempotent" -method "*BaselineManifestVerifiesElevenRegions" -method "*CompetitorManifestReproducesProtocolStep13" -failSkips -failWarns -noLogo -noColor -result-xml artifacts/harness-prose/<run>.xml
```

`old-docs-numeric-fault`: change only `PerformanceDocs.formatMean`'s ns precision
from N3 to N2. Compile exit 0, 0 warnings/errors (8.45 seconds). Test exit 1,
5 total, 4 failed, 1 passed, 0 errors/skips/not run (0.338 seconds). Both focused
and independently fingerprinted invariant-culture fixtures fail at the numeric
`2.500 ns` assertion; both real manifests fail their exact exit-0 check because
their committed generated regions are stale under this format regression.
The idempotence/LF method remains green. Receipt: `old-docs-numeric-fault.xml`;
monitor: `mon_GRPT663ZWVN0QSSZ`. The precision source hunk was restored before
applying the next isolated fault.

`old-docs-lf-fault`: append one CR when generating a guide, leaving all table
values and -Verify logic intact. Compile exit 0, 0 warnings/errors (10.45
seconds). Test exit 1, 5 total, 1 failed, 4 passed, 0 errors/skips/not run (0.463
seconds). GenerateIsIdempotent fails at actual first/second guide byte equality:
the second run gains a second CR. Receipt: `old-docs-lf-fault.xml`; monitor:
`mon_0JJH7V33S9QD9A35`. The source write was restored before the reword-only
control. This is a real generated-copy regression, not an exception/setup fault.

`old-docs-legal-prose`: rewrite only the final console result from
`%s %d performance documentation regions.` to
`Performance documentation %s for %d regions.`. Compile exit 0, 0 warnings/errors
(9.51 seconds). Test exit 1, all 5 selected methods failed only on their English
success assertions, 0 errors/skips/not run (0.388 seconds). Generated markdown,
counts, cultures, fingerprints, exits and -Verify logic are unchanged. Receipt:
`old-docs-legal-prose.xml`; monitor: `mon_6XYJKBQSZY1JTCAD`. The console hunk was
restored before applying the isolated Performance exit-contract mutant.

Performance witness selection (used identically before and after the edit):

```bash
dotnet tests/FunnySharp.Harness.Tests/bin/Debug/net10.0/FunnySharp.Harness.Tests.dll -method "*VerifyPerformance_ValidPolicyAndReceipt_Succeeds" -method "*VerifyPerformance_UnavailableTiming_IsNonBlocking" -method "*VerifyPerformance_ObservationProposal_HasTheOrderedKeySet" -failSkips -failWarns -noLogo -noColor -result-xml artifacts/harness-prose/<run>.xml
```

`old-performance-exit-fault`: return 1 instead of 0 only from Performance's
successful mainWith branch. Both valid fixture constructors still run the real
verifier, including the unavailable-timing branch, and the real proposal is
written; the observable CLI verdict is wrong. Compile exit 0, 0 warnings/errors
(8.63 seconds). Test exit 1, all 5 methods failed at their exact success exit
checks, 0 errors/skips/not run (0.398 seconds). Receipt:
`old-performance-exit-fault.xml`; monitor: `mon_E7X4196Q3HEB2T4R`. The branch exit
was restored before applying the unconsumed success-sentence rewording control.

F# LSP diagnostics could not run because `fsautocomplete` is not installed. No
tool or dependency is installed for this experiment; native locked compilation
with warnings-as-errors is the executable diagnostic owner. The real performance
manifests explicitly list PerformanceProtocolTests as an approved historical
receiver, separate from benchmark/producer inputs. Their real documentation
verification remains in the full-suite gate after source restoration.

`old-performance-legal-prose`: change only the final console sentence to
`Performance verification accepted %d rows, %d exclusions, %d receipts.`.
Compile exit 0, 0 warnings/errors (9.04 seconds). Test exit 1, all 5 selected
methods failed only at the old success-wording locks, 0 errors/skips/not run
(0.379 seconds). The actual verifier/proposal behavior is unchanged. Receipt:
`old-performance-legal-prose.xml`; monitor: `mon_C1TV59KGMV9PS3DV`.

## Initial candidate method actions

Eleven ordinary success-prose assertions are replaced by nonblank-success-output
checks. No method, attribute, input partition, helper fixture, exit/type, schema,
ordering, diagnostic branch locator or detector is deleted or merged. The unused
PerformanceTests successLine string is removed because its two uses are replaced.

| File | Method/action | Retained obligations |
| --- | --- | --- |
| PerformanceTests.fs | assertSucceeds, affecting VerifyPerformance_ValidPolicyAndReceipt_Succeeds and VerifyPerformance_UnavailableTiming_IsNonBlocking | Exact exit 0 and empty stderr; both fixture states; all ten negative/identity/usage methods |
| PerformanceTests.fs | VerifyPerformance_ObservationProposal_HasTheOrderedKeySet | Real file creation, exact ordered ten-field JSON schema, independent raw receipt SHA256 and actual filename |
| PerformanceProtocolTests.fs | VerifyPerformance_ValidPolicyAndReceipt_Succeeds | Independent fixture fingerprints and actual exit 0 |
| PerformanceProtocolTests.fs | VerifyPerformance_UnavailableTiming_IsNonBlocking | Independent unavailable/null timing fixture and exit 0 |
| PerformanceProtocolTests.fs | GeneratePerformanceDocumentation_GeneratesThenVerifies_UnderInvariantCulture | fr-FR restoration, both real generation/verify exits, two rows and 2.500 ns |
| PerformanceDocsTests.fs | GeneratePerformanceDocumentation_GeneratesThenVerifies_UnderInvariantCulture | Both exact exits, empty error streams, culture restoration, numeric/ratio/allocation/N/A rows, exclusions and surrounding guide prose |
| PerformanceDocsTests.fs | GeneratePerformanceDocumentation_GenerateIsIdempotent | Both exact exits, actual first/second guide equality and LF-only output |
| PerformanceDocsTests.fs | BaselineManifestVerifiesElevenRegions | Real manifest/documentation verification, exact exit 0 and empty stderr |
| PerformanceDocsTests.fs | CompetitorManifestReproducesProtocolStep13 | Actual competitor manifest verification, exact exit 0 and empty stderr |

These are ten affected test methods (three focused verifier, three independent
protocol, four focused documentation methods), retaining every original case.
Remaining assigned files are reviewed and retained for the concrete reasons
above, not silently omitted or reported as changes.

## Matching candidate witnesses

`new-performance-legal-prose`: exactly the original performance reword-only
source hunk and exactly the same five method filters. Compile exit 0, 0
warnings/errors (11.58 seconds); test exit 0, 5 passed, 0 errors/failures/skips/
not run (0.334 seconds). Receipt: `new-performance-legal-prose.xml`; monitor:
`mon_9VZMXK1EE45DJQFK`. Only the test success-prose locks changed between old
and new. The console source hunk was restored before the matching exit fault.

`new-performance-exit-fault`: the same successful-mainWith exit 1 hunk, same
five fixtures/method filters. Compile exit 0, 0 warnings/errors (8.64 seconds);
test exit 1, the identical 5 methods fail at exact exit-0 assertions, 0 errors/
skips/not run (0.372 seconds). Receipt: `new-performance-exit-fault.xml`;
monitor: `mon_6Z7470T4MSPDD6T9`. The source exit was restored before the next
isolated documentation wording control. The permanent assertion diff was
reviewed with `git diff --check` (exit 0); only the eleven prose checks and their
unused shared string change, with all machine schema/order/hash checks intact.

`new-docs-legal-prose`: the same documentation sentence rewording, same five
methods and unchanged generator/verification logic. Compile exit 0, 0
warnings/errors (8.58 seconds); test exit 0, all 5 passed, 0 errors/failures/
skips/not run (0.329 seconds). Receipt: `new-docs-legal-prose.xml`; monitor:
`mon_RFEYX8RE46J7F9F6`. Its source hunk was restored before applying the matching
numeric-format fault. The actual XML receipts were read: performance exit-fault
old/new failed method identities match exactly, and both legal-control groups
discover the same original/candidate methods rather than passing an empty filter.

`new-docs-numeric-fault`: the same N3-to-N2 ns formatter regression and the
same five methods. Compile exit 0, 0 warnings/errors (8.10 seconds); test exit
1, exactly the same 4 methods fail at numeric output/real-document verification,
1 idempotence control passes, 0 errors/skips/not run (0.340 seconds). Receipt:
`new-docs-numeric-fault.xml`; monitor: `mon_RRE5EHK29RK5PMP0`. The precision hunk
was restored before the final matching LF/idempotency mutant.

`new-docs-lf-fault`: the same appended-CR generation hunk and same five
methods. Compile exit 0, 0 warnings/errors (9.91 seconds); test exit 1, exactly
the same GenerateIsIdempotent byte-equality assertion fails, four other controls
pass, 0 errors/skips/not run (0.430 seconds). Receipt: `new-docs-lf-fault.xml`;
monitor: `mon_DRSGYDJ3DSBJV4CT`. The final write hunk was restored. The actual
candidate XML numeric/LF receipts were read and match the original failed/pass
method sets exactly.

Summary of matching proof (every build exited 0 with 0 warnings/errors):

| Witness | Old test exit / passed / failed | New test exit / passed / failed | Total in each | Detection |
| --- | --- | --- | ---: | --- |
| Performance success exit 1 | 1 / 0 / 5 | 1 / 0 / 5 | 5 | Identical exit checks |
| Documentation N2 ns precision | 1 / 1 / 4 | 1 / 1 / 4 | 5 | Identical numeric/real-copy checks |
| Documentation appended CR | 1 / 4 / 1 | 1 / 4 / 1 | 5 | Identical actual guide equality |
| Performance legal prose | 1 / 0 / 5 | 0 / 5 / 0 | 5 | Noncontractual sentence only |
| Documentation legal prose | 1 / 0 / 5 | 0 / 5 / 0 | 5 | Noncontractual sentence only |

All ten witness runs have 0 errors, 0 skips and 0 not run. No setup exception
or compile failure is credited as a meaningful detector. This is assertion
simplification, not test deletion or a whole-library correctness proof; no
runtime/speedup claim is made.

## Restoration and full Windows gate

Before the full gate, `git diff --exit-code -- eng/harness` passed. For both
temporarily touched modules, the actual `git hash-object --no-filters` blob
equals `git rev-parse HEAD:<path>`; the raw-byte restoration command exited 0
and printed EXACT_RESTORATION for Performance.fs and PerformanceDocs.fs.
No implementation change remains. `git diff --check` also exited 0.

The full restored gate is the same locked compile followed by the native
runner with no class/method filter:

```bash
dotnet tests/FunnySharp.Harness.Tests/bin/Debug/net10.0/FunnySharp.Harness.Tests.dll -failSkips -failWarns -noLogo -noColor -result-xml artifacts/harness-prose/restored-full-windows.xml
```

Monitor: `mon_ASTSA0H52ZAC8B25`. Restored compile exited 0, 0 warnings and 0
errors, 9.07 seconds. The full native Windows run exited 0: **745 total, 745
passed, 0 errors, 0 failures, 0 skips, 0 not run**, 18.912 seconds. The XML
records x64 .NET 10.0.12 / xUnit v3 4.0.0, started at
2026-10-06T19:06:46.2595556+00:00 and finished at
2026-10-06T19:07:05.1716608+00:00. Its actual assembly summary and all 745 test
elements were inspected via full read: every result is Pass. The tool.read
line reader could not return this 461.7 KiB single-line XML, so the full eval
read path was used; the grep line count was not accepted as a case count.
The two POSIX-only Facts are absent from Windows compilation, not reported as
Windows skips or passes.

The initial 205-case control's exact class selection was:

```bash
dotnet tests/FunnySharp.Harness.Tests/bin/Debug/net10.0/FunnySharp.Harness.Tests.dll -class "FunnySharp.Harness.Tests.PerformanceTests+PerformanceVerifierTests" -class "FunnySharp.Harness.Tests.PerformanceProtocolTests+PerformanceProtocolTests" -class "FunnySharp.Harness.Tests.PerformanceDocsTests+DocumentationFixtureTests" -class "FunnySharp.Harness.Tests.PerformanceDocsTests+DocumentationCommandLineTests" -class "FunnySharp.Harness.Tests.PerformanceDocsTests+RealManifestTests" -class "FunnySharp.Harness.Tests.ReleaseRunTests" -class "FunnySharp.Harness.Tests.ReproducibleBuildsTests+ComparisonTests" -class "FunnySharp.Harness.Tests.ReproducibleBuildsTests+RejectionTests" -class "FunnySharp.Harness.Tests.ToolingVerifyTests+PipelineVerdictTests" -class "FunnySharp.Harness.Tests.ToolingVerifyTests+ReportTests" -failSkips -failWarns -noLogo -noColor -xml artifacts/harness-prose/old-control.xml
```

An incidental native `-help` probe exited 3 (unknown option); it is not test or
fault evidence. No unsupported build `--locked-mode` was used. Every reported
build uses the supported MSBuild RestoreLockedMode property and supported feed.

## Final boundary and cleanup

The post-suite boundary command exited 0: `git diff --exit-code -- eng/harness`
and the same comparison for RulesetTests.fs, EvaluationTests.fs and
ReleaseVerifyTests.fs report no changes. `git diff --check` passed. The final
tracked test diff is exactly three assigned files, 11 insertions and 17
deletions; this English report is the only untracked permanent file. The other
twelve assigned test files have no changes. No generated/frozen evidence,
implementation, project, lock file, helper framework or product API is edited.

Owned process cleanup was checked after the full run with:

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-prose && powershell.exe -NoProfile -Command '$owned = @(Get-CimInstance Win32_Process | Where-Object { $_.ProcessId -ne $PID -and $_.Name -match "^(dotnet|FunnySharp.Harness.Tests|git|powershell|pwsh)(\.exe)?$" -and $_.CommandLine -like "*test-ablation-harness-prose*" }); $owned | Select-Object ProcessId,ParentProcessId,Name,CommandLine | ConvertTo-Json -Compress; if ($owned.Count -gt 0) { exit 1 }; "OWNED_PROCESS_COUNT=0"'
```

It exited 0 and printed `OWNED_PROCESS_COUNT=0`; no owned harness/git/PowerShell
child remained to terminate. An initial incorrectly double-quoted shell probe
exited 1 due to expansion of `$_`; the corrected single-quoted command above
supplies the actual cleanup evidence. No process belonging to another session
was killed. Every command monitor completed; none is left running or waiting.
The temporary source mutants were restored one at a time, and the last build
contains restored implementation, not a mutant binary. Fixture directories use
their original scoped disposal; no manually staged mutant directory or script
exists. Fresh XML receipts under `artifacts/harness-prose/` are retained without
overwriting an earlier result, and are ignored generated evidence, not shipped
implementation or source edits.

All requested permanent changes and their executed proof are ready for parent
integration: no method deletion, no weakened machine contract, no skip or
warning suppression, no test timing wait/retry, no dependency, no commit and no
remote write. The pre-existing DocsSnippets case-insensitive comparison versus
byte-exact product wording is retained and reported by the source lane; it was
not changed or credited as byte-exact proof here. Only native F# LSP inspection
was unavailable; locked compiler diagnostics and the full requested Windows
suite are clean. Remaining candidate assertions are deliberately retained for
their named contracts or missing containment witness, not claimed as ablated.

## Parent final simplification and actual replay

The parent removed the eleven replacement nonblank-output checks as well.
These stdout strings have no machine consumer in the changed tests; exit/error,
actual generated-file equality, numeric values, culture, schema/order and real
manifest verification are their independent detector owners. Replacing a prose
lock with a shallow nonblank check would add a new unneeded obligation. Unused
output bindings now use `_`; all original methods and partitions remain.

The final contents were replayed against the same original production faults:

| Final parent replay | Build diagnostics | Actual test outcome |
| --- | --- | --- |
| N3 -> N2 ns precision | 0 warnings/errors, exit 0 | 5 total, 4 failed, exit 1 |
| Append CR to generated guide | 0 warnings/errors, exit 0 | 5 total, 1 failed, exit 1 |
| Return 1 from successful performance verifier | 0 warnings/errors, exit 0 | 5 total, 5 failed, exit 1 |
| Both original legal prose rewrites together | 0 warnings/errors, exit 0 | 10 total, 10 passed, exit 0 |

Every replay has Errors/Skipped/Not Run 0; filters are unchanged from the
original matrix. XML receipts are under `artifacts/harness-prose/` with names
`parent-{numeric,lf,performance-exit,legal-output}-final.xml`. Each source hunk
is restored by apply_patch before the next experiment; no source mutation,
diagnostic rewrite, compiler failure or skipped test is counted as proof.

After restoration, `git diff --exit-code -- eng/harness` passes. The explicit
locked compiler rebuild exits 0 with zero warnings/errors. The actual full
Windows run passes **745/745**, zero errors/failures/skips/not-run, exit 0,
18.573 s. Receipt: `artifacts/harness-prose/parent-restored-full-final.xml`.
The complete repository formatter exits 0, 26.316 s. This final candidate
deletes unconsumed assertion burden; no overall speedup claim is made.
