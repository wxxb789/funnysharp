# Final local prepublication gate: b584

**Recommendation: REJECT. Confidence: high for the rejection; moderate for broad scope coverage.**

One current shipped exception contract is demonstrably inaccurate. This is a prepublication readiness decision for `b584e748699a4f19d8a901695634be5a85ce8b07`, not a new whole-Goal 24 decision. The frozen original approval at `d8744c934d86833f9817245ecc7c78b1177b8b76` is preserved. Publication, exact-head remote CI, babysitting and owned cleanup remain separate follow-on work.

Reviewer: one native `omo-native-gate-reviewer`, task `st_01a109c0`, session `01a109c0-38db-7e1e-b296-c908ffdf8472`, on 2026-10-05. No delegation, implementation, test edits, commit, push, remote communication, publication or cleanup was performed.

## Blocker

### FG-R9-001: Traverse XML promises a source-state guard that does not exist

**Violated criterion:** R9, intentional exception contracts and adequate shipped XML for every stable public member; original dimensions `G23-23.06` and `G23-23.09`. This is a current content failure, not a request for extra hardening or a design alternative.

`src/FunnySharp/SequenceExtensions.cs:95` documents `Traverse<TSource,TResult,TError>` as throwing `InvalidOperationException` when:

> A reached source carrier or selector result is uninitialized; reported while enumerating the source.

The implementation at `src/FunnySharp/SequenceExtensions.cs:98-119` passes each generic source item to the selector and inspects only the returned `Result`. It does not inspect the source item's carrier state. The sentence was added in the base-to-b584 diff. It appears unchanged in the actual r4 package, not just in source comments.

A foreground read-only runtime probe loaded the current built `FunnySharp.dll` and executed these F# statements through `dotnet fsi --readline- --quiet` on stdin:

```fsharp
open System
open System.Collections.Generic
open FunnySharp

let source = [Unchecked.defaultof<Result<int,string>>]
let actual =
    SequenceExtensions.Traverse<Result<int,string>,int,string>(
        source,
        Func<Result<int,string>,Result<int,string>>(
            fun _ -> Result<int,string>.Success(7)))
let mutable values = Unchecked.defaultof<IReadOnlyList<int>>
let success = actual.TryGetValue(&values)
if not success || values.Count <> 1 || values.[0] <> 7 then
    failwith "Unexpected traversal outcome"
printfn "TRAVERSE_DEFAULT_SOURCE success=%b count=%d value=%d exception=none"
    success values.Count values.[0]
```

Observed output, exit **0**:

```text
TRAVERSE_DEFAULT_SOURCE success=true count=1 value=7 exception=none
```

A separate foreground inspection opened the existing r4 nupkg with `ZipFile.OpenRead`, read the XML entry without extracting it, and found the same exception clause. The member is:

```text
M:FunnySharp.SequenceExtensions.Traverse``3(System.Collections.Generic.IEnumerable{``0},System.Func{``0,FunnySharp.Result{``1,``2}})
```

Evidence pointers:

- Source: `src/FunnySharp/SequenceExtensions.cs:95`, implementation `:98-119`.
- Built XML: `src/FunnySharp/bin/Release/net10.0/FunnySharp.xml:4516-4536`.
- Actual package: `artifacts/release-candidate/b584e748699a4f19d8a901695634be5a85ce8b07/r4/packages/FunnySharp.0.2.0.nupkg::lib/net10.0/FunnySharp.xml`, at the member above.
- Current crosswalk: `docs/audits/goal-24-resolution/stable-api-contracts.json:4000`, row `C:358`.
- Package SHA256 recorded by the current compatibility receipt: `6af81334e54911a4eeb3ed18189a8a88f302b70af4228af963cdffc7273492fb`.
- DLL SHA256 recorded by the current build seal: `304aa5e354b74c1ee4dab175386d8c292973339c6d38858bf34973b3f4b1836c`.

**Exact fix:** distinguish the actual guards in current XML. `Sequence` inspects source carriers; `Traverse` inspects returned selector carriers and propagates exceptions explicitly thrown by the selector or enumerator. Review the repeated synchronous, asynchronous and parallel clauses for this distinction. Do not add a runtime source-state guard to match the false prose. Refresh the current reviewed XML successor and affected source-bound proof after correction, preserving the original d8744 policy, ledgers and receipts. A removal-only or prose-pinning test is not appropriate.

The same sentence occurs on additional stable Traverse overloads at `SequenceExtensions.cs:149`, `:208`, `:313`, `:360`, `:405`, `:557`, `:593`, `:639`, `:692` and `:729`. These are one shared documentation-pattern finding, not eleven independent blockers.

The green XML inventory cannot close this defect: `ReleaseVerifyArtifacts.fs:519-779` verifies exact identities, bindings, aliases and document presence, and explicitly emits `completeSemanticAcceptance=false`. Hash equality does not establish that the exception description is true.

## Original intent and desired outcome

The original request is to include all worthwhile owned Goal 24 implementation and evidence in PR 33 without omissions, credential leakage or lost history, then verify the published range and current CI and safely clean owned temporary resources. The present brief narrows this reviewer to one final local readiness audit before publication. The desired user-visible result is an honest readiness decision on the complete candidate, rather than a claim that later publication/CI/cleanup has already happened.

The implementation, retained evidence and extensive current QA are represented. A consumer-facing exception promise remains false in the package they would receive. Consequently the current artifact is not ready under R9, despite recorded build, test, formatter and release success.

## Scope and completeness

The dedicated worktree was independently observed clean on branch `audit/goal24-complete-pr` at `b584e748699a4f19d8a901695634be5a85ce8b07`. Base is `a8863473fd53eddc7cb47201508426db2e454f84`.

Foreground Git and inventory probes established:

- Full range: **7,131 files**, **2,035,174 insertions**, **1,779 deletions**.
- Final performance increment: **402 files**, exactly equal to all named `:(literal)` pathspecs, with no missing or extra names.
- Original inventory: all **6,661** recorded path/status entries equal the immutable base-to-e7 Git diff.
- `eng/evaluation/results`, `eng/evaluation/studies` and `docs/goals/archive` have no d8744-to-b584 changes.
- All **13** entries in committed `current-validation/receipt-index.json` independently match their declared SHA256 and byte length.
- The v6 manifest declares **188** files; **123** existing files hash-match; **65** missing files all have an `obj` segment; no existing-file hash mismatch was found.

Full Git diff SHA256 from the foreground probe: `40a360b2a7627faa97def197840758580864e772a01498636a16cc10e91e1608`.

Inventory artifact SHA256: `5d2b64fcbfc1dd69aae22347d009df1a0bc75c894955449e11867cde62011164`.

Changed shipping executable deltas were separated from XML-only changes and inspected. The non-concurrency/non-traversal executable deltas are version prefixes, the empty-key guard and cancellation before `Current`. The comparer and FirstSuccess changes, analyzers/code fix, critical current proof producers/verifiers, selected tests, full inventories and representative archive evidence were examined. **This is not an assertion that every archived line, repeated cohort or opaque payload was semantically reviewed.** No 52 MB base64 dump was inlined.

## Goal breakdown and constraints

| Area | Assessment |
| --- | --- |
| Complete owned inclusion | No concrete omission found in full inventories or the exact 402-file increment. |
| Original/adverse preservation | Historical scopes unchanged; missing v6 bytes remain unavailable, not synthesized. |
| R3/R4 traversal and concurrency | No additional verified blocker in comparer preservation, empty keys, bounded admission, cancellation-before-publication or drain/fault behavior examined. |
| R5 analyzers | Empty initializers, true-discard candidate binding and bounded completed/single-consumption proof inspected. |
| R6 typed HTTP and restore | Typed 500 metadata and same-host Kestrel test inspected; faithful attempt-local NuGet configuration retained. No HTTP rerun. |
| R7 performance | One fresh 190+40 pair, unchanged-policy claims, exact DLL witness scope and raw 24-case CTRF inspected; not three new repetitions or a package-layout claim. |
| R8 replay | Replay-only route and fail-closed guards are implemented; actual historical replay remains unavailable for 65 obj files. |
| R9 stable exception/XML accuracy | **FAIL: FG-R9-001.** |
| R10/current local release | Actual r4 artifacts support recorded local success with explicit verified-TLS mirrors; not current remote CI or default public NuGet endpoint proof. |
| Publication/CI/babysit/cleanup | Separate follow-on obligations; not claimed complete and not blockers solely for being future steps. |

Only the two report artifacts were written. No repository/evidence history was modified, no source or test was weakened, and no benchmark, model, HTTP or denial experiment was rerun. No extra panel was created. No SourceLink, AOT, allocation ceiling or TLS bypass was observed in the inspected current release route.

## QA audit

| QA row | Inspected evidence and conclusion |
| --- | --- |
| Current full suite | Actual `r4/receipts/04-test.json` and complete raw `r4/logs/04-test.stdout.log`: exit 0, 2,630 succeeded, 0 failed, 0 skipped. This is artifact inspection, not a reviewer suite rerun. |
| Build and formatter | Actual canonical `03-build.json` and `08-format.json`: both exit 0. Formatter is C# only. |
| Current release | `release-outcome.json` names b584/r4 and succeeds; actual release evidence has 10 passed checks and no failures. Raw compatibility command/log and final feed state inspected. |
| Four trim/AOT cases | `compatibility-results.json` records CoreTrimmed, CoreNativeAot, AspNetCoreTrimmed and AspNetCoreNativeAot Passed. Raw log contains their real smoke markers and both `PublishAot=true` routes. |
| Current DLL witnesses | Raw current-coverage CTRF contains all 24 passed cases with 0 failed/pending/skipped/other. Source cases use real carrier/effect behavior and causal signals. |
| Fresh performance/docs | Original strict-verifier checkpoints record main 190 rows/11 exclusions/13 receipts, competitor 40/0/2, and 11+1 generated doc regions. Catalog and producer/verifier code inspected. Extra reviewer portable execution has no observed completion and is not counted as PASS. |
| Current negatives | Three retained missing/tampered-object/catalog outputs each show exit 1 without PASS; runner exit 0. Rejection code paths inspected; controls not rerun. |
| Original portable proof | Full verifier source, catalog contract, original approval boundaries, retained parent receipts and all four negative outputs inspected. Extra reviewer positive run has no observed completion; no new original acceptance claimed. |
| Replay retention | Independently verified 123 present hashes and 65 missing obj paths. Parent's zero-additions result remains attributed; the reviewer's corrected separator check has no captured result. |
| Consumer exception description | Foreground runtime and actual nupkg inspection establish **R9 failure**. |

The r4 binding pointers read from actual artifacts are:

- Execution evidence: `0d21f5b49877845f97c034e3cb2f5ee90932e23238e10bb8a16e7fa83ada2aef`.
- Release evidence: `1dac4a061785f067f9d794e21005fbd66dedcba3e167660e1b7dcd745ff0deed`.
- Build receipt in XML seal: `b4692a2ecbbc89099985df7e7f2481eb846ac8428bd813a5985d7f2c137dbfac`.
- Current reviewed XML inputs: `d5319f334d5220b38861da411f5025cf28e4c187ef5b5258677751690ccc6140`.
- Preserved original XML policy: `696fe5154ee970778ce7b415ea9bc964cedbe6158d34e3f6e545be3843d08601`.

These are read artifact pins, not falsely labeled results of the background full-hash sweep.

## Representative and edge-case traces

1. **Keyed traversal:** `SequenceExtensions.cs:474-773,1265-1282` -> `TraversalContextTests` -> recorded current core suite. Supported comparer discovery, explicit opaque comparer and empty-result policy are retained. `Location.Key` accepts empty strings while retaining null rejection.
2. **Concurrent work:** `ConcurrentEffectExtensions.cs:165-404` and `ParallelAsyncEnumerableExtensions.cs:305` -> `FirstSuccessTests.cs:617-997`, `ParallelAsyncEnumerableTests.cs:318-416` -> actual current CTRF. Examined edges include held 1,024-item admission/refill, 4,096 single-consumption failures, caller cancellation during a noncooperating ordered pull, and a winner whose loser disposal faults.
3. **Package/XML:** `ReleaseRun.fs:1230` -> `XmlBuildBindings.capture/validate` -> `ReleaseVerify.fs:372` -> XML inventory -> actual r4 package. Current/historical XML, changed source, wrong candidate/attempt, build receipt and capture ordering are distinct checks. FG-R9-001 demonstrates the remaining semantic gap.
4. **Relocatable evidence:** current object/locator/source/PE/CTRF joins and original raw ZIP/P/A/I/Git-blob joins -> committed catalogs and retained negatives. Missing object, changed catalog and changed bytes fail before PASS. Historical Q-drive labels are not fallback input locations.
5. **Replay and diagnostics:** exact snapshot validation precedes output reservation; existing output/cohort paths are rejected. Synthetic replay tests do not prove real archived compilation. Code-fix candidates must bind to `IDiscardOperation`; aliases, escapes, wrong receivers and repeated ValueTask consumption prevent exemptions.

These traces cover more than the required three representative scenarios and five edges. Cases not actually rerun are labeled accordingly.

## Direct programming and overfit/slop review

Loaded both available skill files before final judgment:

- `C:/Users/lhan/.omo/binary-runtime/5.1.15/plugin/skills/remove-ai-slops/SKILL.md`
- `C:/Users/lhan/.omo/binary-runtime/5.1.15/plugin/skills/programming/SKILL.md`

Applied their review perspective directly to current code, diffs and tests. There is no C#/F# language-specific reference set. The user requested a read-only audit, so skill cleanup/delegation instructions do not authorize edits or expand this gate with a file-size policy.

The direct pass found the overgeneralized production XML in FG-R9-001. In the inspected tests, no removal-only or prose-pinning test was added for the Option comment correction. API absence tests guard real public-surface contracts. Per-overload semantic matrices are repetitive but exercise independent dispatch branches, payload/null behavior and callback exceptions; they are not discarded as useless merely because they are numerous. The sampled hash self-comparisons do not independently prove a particular numeric hash, but adjacent null/exception checks are real behavioral evidence.

Concurrency assertions use test-controlled pending sources, positive subscription/start signals and bounded failure timeouts. Literal searches found cancellation waits using `Timeout.InfiniteTimeSpan` and an analyzer input using `Task.Delay(0)`, not a new timed-absence ordering test. FrozenReplay mocks the external process outcome while running the real existing evaluator; the test and README explicitly limit that claim. StageBundleFixture tests byte transport rather than full publication acceptance. Parsing and hashing in the proof code enforce the stated evidence boundary; no needless production extraction or normalization blocker was established.

`source-repairs-review.md` explicitly records the same skill-perspective and removal/prose-pin, tautology, implementation-mirror, abstraction and parsing/normalization coverage, but only for its 13-file repair scope. The older CE report covers ten roles without an equivalent explicit skill checklist for the entire b584 successor. Neither report replaced this gate's own check.

## Nonblocking notes

- The four CE structural findings remain **P1 under that persona**, deferred after the unanswered scope question. No repository maximum-line policy exists. This audit does not reclassify them or require broad extraction.
- The brief's **15 immutable EOF warnings** are the portable-evidence subset. The full base-to-b584 raw `git diff --check` produced 38 EOF warnings and 221,830 trailing-whitespace diagnostics; allowing CR-at-EOL reduced the latter to 132 while retaining 38 EOF warnings. `.gitattributes` deliberately preserves frozen CRLF. The final **402-file increment independently exits 0**. No proof bytes or repository whitespace configuration were changed.
- `XmlBuildBindingsTests.fs:144-254` combines numerous meaningful scenarios in one integration Fact, making failure localization harder. No current criterion requires splitting it.
- Unchanged experimental located UnitResult prose at `SequenceExtensions.cs:1138-1170` says a dictionary is materialized even though the method returns a valueless UnitResult. It is outside the stable-member blocker and was not edited.

## Exact evidence gaps and runtime limits

Actual v6 replay remains unavailable for 65 manifest-bound historical obj files. No new cache generation can establish their original identity. POSIX relocation, current-head remote CI/publication and every archived line/payload were not independently exercised or semantically reviewed here. No universal secret-free certification is made.

Five additional read-only background checks returned handles but this child route exposes neither `bash_output` nor `monitor`, and no completion event was delivered. Their outputs are **unknown, not PASS**, and are not the basis of this rejection:

| Handle | Pending result |
| --- | --- |
| `bash_5` | Current portable performance verifier |
| `bash_6` | Complete r4 source/receipt/log/XML hash sweep |
| `bash_11` | Original portable P/A/I and 69 byte-join verifier |
| `bash_12` | Original binary and portable-retention secret scan |
| `bash_18` | Current object hash/secret scan and corrected Windows separator inventory |

The package XML inspection exited 0 with two `FS0524` warnings from top-level FSI `use` bindings; these are probe warnings, not current shipping build warnings. The runtime counterexample itself exited 0 without warnings. F# LSP was unavailable (`Command not found: fsautocomplete`); no installation was attempted.

No eval or todo tool is exposed. SDK status consulted through Bun returned `ULW_LOOP_PLAN_MISSING` for this child; no plan was created or changed. The explicit brief's two paths supersede the generic report fallback. `apply_patch` is unavailable; the developer-required `write` tool authored only these two reports.

The JSON companion contains the full checked-artifact ledger, exact probe source, blocker fields, scope counts and constraints. Relative source and evidence references in this report resolve under `Q:/repos/funnysharp/.worktrees/goal24-complete-pr`; review input filenames resolve under `Q:/repos/funnysharp/docs/reviews/goal24-pr-20261004`.

**Current gate result: REJECT for FG-R9-001. Whole-goal completion is not claimed.**
