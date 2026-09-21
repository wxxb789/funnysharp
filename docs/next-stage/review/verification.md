# Goal 14 Evidence-Set Verification (Independent Audit)

- **Audit date:** 2026-09-17
- **Audited state:** `HEAD` = `4dbebd94b7b58648632112b7ca47c39cc517f153` (`main`); worktree has
  `M docs/product-contract.md` and untracked `docs/next-stage/` + `eng/next-stage-inventory/`;
  no other modifications. All artifacts below are the working-tree versions, not committed ones.
- **Auditor:** independent audit workstream; read-only except this file.
- **Method:** every checklist item was executed against the files themselves: Python set
  comparison of the generated JSON inventories against `decision-matrix.csv`; regex extraction
  and reconciliation of the decision tables in the five external-baseline memos and the
  FunnySharp-surface memo against `decision-record.md`; `sha256sum` of the pinned `.nupkg`
  files and the rebuilt assemblies; file-existence checks for every dump referenced by
  `inventory/generated/README.md`; targeted citation checks (`grep`/`sed`) across
  `docs/next-stage/*.md` and `api-decisions.md`; `dotnet build -c Release` (and
  `--no-incremental` rebuilds) in the repository and all four call-site projects; repository-wide
  project-file scans for competitor references.
- **Rule applied:** fail-closed. A missing, contradictory, or unsupported element is a finding;
  weak coverage counts as not verified. Only checked-in/working-tree evidence counts; prose
  claims are not accepted without inspection.
- **Round 2:** the fixes for V-01…V-20 were re-verified on 2026-09-17 against the same worktree.
  See **§7 Re-verification (round 2)** for the updated status, the fail-closed re-runs, and the
  round-2 verdict. §1–§6 are the round-1 record; the §6 verdict is superseded by §7.6.
- **Round 3:** the fixes for the two remaining round-2 items were re-verified on 2026-09-17 against
  the same worktree. See **§8 Re-verification (round 3)**. §7 is the round-2 record; the §7.6
  verdict is superseded by §8.4, and V-02/R-1 are resolved in §8.1.
- **Round 4:** the post-acceptance delta (maintainer acceptance recorded; A-2 accepted as option B;
  CSV, `api-decisions.md`, `docs/product-contract.md`, and `decision-record.md` synchronized) was
  re-verified on 2026-09-17. See **§9 Re-verification (round 4: post-acceptance delta)**. §8 is the
  round-3 record; the §8.4 verdict is superseded by §9.5.

## 1. Checklist Results

| # | Item | Result | Exact evidence |
| --- | --- | --- | --- |
| 1 | Member-level completeness | **partial** | JSON vs CSV set comparison: 34/34 types, 269/269 members, zero missing/extra/duplicates; CSV member decisions keep 220 / redesign 49 / experimental 0 / remove 0; type decisions keep 30 / redesign 4, matching the `api-decisions.md` summary (lines 28–33). 8 operators, 3 ctors, 3 delegate members, 4 enum values all present (enum values written as `Name = value`). Type-level statements in `api-decisions.md` AD-1…AD-10 agree with the CSV for all 34 types. Finding V-01: 20 of the 49 redesign-marked members are not covered by the AD-2/AD-3 prose redesign lists. |
| 2 | External-capability coverage | **partial** | Memo decision rows extracted: CFE 36 rows (33 decisions), FSharp.Core 28, Funcky 50 (48 decisions), language-ext 49 (46 decisions) = 155 decision-bearing rows. 109 are cited in `decision-record.md` (exact IDs, expanded ranges such as `LE 2.1–2.3`, `FS #13, #15`, `FS #20–#21`, and section-level `CFE F7`); 46 decision-bearing rows are not cited. Of those, 29 are semantically represented by an equivalent record row (minor; not itemized individually) and 17 have no equivalent row or carry a conflicting decision (V-04, material). Six further rows are cited with the opposite decision (V-05). One record citation points at evidence that does not exist as labelled: E142 cites `OWN G18` while `funny-sharp-surface.md` has only G1–G17 (V-13). |
| 3a | `.nupkg` SHA256 fidelity | **pass** | `sha256sum /tmp/opencode/baselines/*.nupkg`: `csharpfunctionalextensions.3.7.0` = `3a7c3d59…9723c8`, `funcky.3.6.0` = `1a7ab6c6…940cc32f`, `fsharp.core.10.1.401` = `cf1f4e69…c68963e`, `languageext.core.4.4.9` = `633636d9…18347437` — all four recorded pins match `baselines.md:21–27`. The fifth local nupkg (`fsharp.core.10.0.100`, cross-check only) has no recorded hash by design. Rebuilt DLL hashes also match `baselines.md:10–13` exactly (`FunnySharp.dll` `793c8532…9bf9d4`, `FunnySharp.AspNetCore.dll` `3810e1de…2f77a42`). |
| 3b | Referenced dumps exist | **partial** | All 22 dump names referenced by `inventory/generated/README.md` exist as `.md`+`.json`; `types-languageext-4.4.9.txt` exists. Counts in the README table verified against the JSONs: all canonical rows match (core 33/254/8/98, aspnetcore 1/15/0/15, CFE 42/1872/31/1664, Funcky 85/1156/21/760, FSharp.Core 102/1625/81/0, language-ext 159/6745/115/2939, all four BCL rows, both v5 rows). Three supplementary rows are stale (V-06). `inv-fsharp-core-focus.*` exists and is documented in `inventory/fsharp-core.md:61–69` but is absent from the generated README (V-12). |
| 3c | Citation spot-checks (≥20) | **partial** | 32 targeted checks run; 29 confirmed the cited content in the cited range, 3 drift/mislabel. Confirmed examples: `docs/option.md:31` (`Some` rejects runtime null), `docs/validation.md:35` (`InvalidMany` rejects null/empty), `docs/function-composition.md:39–41` (binary arity boundary), `docs/data-pipelines.md:91–94`, `docs/state-machines.md:110–115` (457.67x, 187 296 B), `docs/effects.md:61–92`, `src/FunnySharp/Result.cs:40`, `src/FunnySharp/AsyncEnumerablePipelineExtensions.cs:79`, `src/FunnySharp/Option.cs:115`, `tests/FunnySharp.Tests/StateMachineTests.cs:128`, `docs/goals/archive/0015-goal.md:2`. Drift/mislabel: the 59 line-pinned references to `docs/product-contract.md` were written against `HEAD` (143 lines) and no longer resolve against the revised 258-line contract (V-03, material); `funcky.md` cites `inv-bcl-sequences-linq.md:1454,1651` for `Chunk`, committed dump has it at 1471/1668 (V-15, minor); the CFE memo's `inventory:NNN` ranges only resolve against `generated/inv-csharpfunctionalextensions.md`, not the named `inventory/csharpfunctionalextensions.md` (427 lines; V-16, minor). |
| 3d | Call-site builds | **pass** | `dotnet build -c Release --no-incremental` in all four projects: `idiomatic` exit 0, 0 warnings/0 errors; `funnysharp` exit 0, 0/0; `funnysharp-aspnet` exit 0, 0/0; `competitors` exit 0, 0/0. (`--no-incremental` was used because the first re-run was an up-to-date no-op.) |
| 3e | Repository build | **pass** | `dotnet build FunnySharp.slnx -c Release` (SDK 10.0.400) exit 0: “0 个警告 / 0 个错误” (0 warnings, 0 errors), 8 projects built. |
| 4 | Independence | **partial** | Repo-wide scan of `*.csproj`/`*.props`/`*.targets`/`*.slnx`: exactly one project references competitor packages — `docs/next-stage/call-sites-code/competitors/Competitors.csproj` (CFE 3.7.0, Funcky 3.6.0, LanguageExt.Core 4.4.9). None under `src/`, `tests/`, `examples/`, `benchmarks/`; `FunnySharp.slnx` lists only the 8 shipping/test/sample projects; no script/props/slnx outside `docs/` references `call-sites-code`. FunnySharp call-site sources contain no competitor usings/types. `docs/product-contract.md:172–175` states the isolation rule. Finding V-17 (minor): `Competitors.csproj` does not set `IsPackable=false` while the decision record calls it “non-packable”. |
| 5 | Constitution consistency | **partial** | `docs/product-contract.md` (revised, 258 lines) vs `api-decisions.md` and `decision-record.md`: carrier set, vocabulary rules, stability boundary, deferrals, first-success shape, retry boundary, HTTP/typed-results deferral, analyzer reopening, independence text all agree. Findings: `Unit` is listed among “Deferred capabilities” in the contract (line 195) while E98/G17 decide **reject** (V-08); analyzer packaging is described as an “optional, non-runtime dependency of the core package” (line 138–139) while E127 decides “in the main package with build assets” and OWN G9 recommends a separate package (V-09); against `docs/goals/0015…0023-goal.md`, Goal 20's required competitor comparisons are enabled only indirectly (isolated-tooling clause) with no decision row resolving standing-constraint C5 (V-10); Goal 17's deferred non-empty/exact-vs-truncating items and Goal 23's release notes are absent from the contract (V-19, minor). |
| 6 | Unverified discipline | **partial** | 72 `UNVERIFIED` markers exist across the memos/inventories/call-sites plus 5 items each in `decision-record.md` §“Unverified judgments” and `api-decisions.md` §“Unverified judgments”. Full lists retained below (§3). Unflagged/unpropagated judgments found: E140 states the language-ext maintenance risk as fact while LE 11.7 flags it UNVERIFIED (V-11, moderate); AD-6 asserts the parallel operators “are bounded, ordered, cancellation-correct” while `baseline-bcl.md:507–513` records ordering and drain behavior as UNVERIFIED (V-11). |
| 7 | Call-sites coverage | **pass** | W1–W11 all present (§4–§14); 11 per-workflow LOC tables plus the summary table; every section has an explicit compile-evidence sentence and `§17.2` records the four project builds; `tools/rawloc.py` re-run from the committed copy reproduces the document's raw column exactly (including total 489 raw lines); the four committed project directories are byte-identical to `/tmp/opencode/callsites/*` (`diff -rq` exit 0); competitor variants pinned in `Competitors.csproj` to CFE 3.7.0 / Funcky 3.6.0 / LanguageExt.Core 4.4.9 with SHA256 pins in `README.md` and `call-sites.md:20–24`. |

## 2. Findings

### Material

**V-01 — `decision-matrix.csv` redesign scope contradicts the AD-2/AD-3 prose.**
- Location: `docs/next-stage/decision-matrix.csv` rows for `FunnySharp.Result\`2` and
  `FunnySharp.Validation\`2` vs `docs/next-stage/api-decisions.md:73–81, 94–97`.
- Evidence: all 25 `Result<TValue,TError>` rows and all 18 `Validation<TValue,TError>` rows carry
  `member_decision=redesign`, but the prose redesigns only the inspection/equality/text members
  (`IsSuccess`, `IsFailure`, `TryGetValue`, `TryGetError`, `Match`, `Equals`, `GetHashCode`,
  `ToString`, `==`, `!=`). 20 redesign rows are not stated as redesigns anywhere in the prose
  (13 in `Result<TValue,TError>`: `Bind`, `Ensure` ×2, `Failure`, `Map`, `MapError`, one `Match`
  overload, `Recover`, `RecoverWith`, `Select`, `SelectMany`, `Success`, `Zip`, `ZipWith`;
  7 in `Validation<TValue,TError>`: `Invalid`, `InvalidMany`, `Map`, `MapErrors`, one `Match`
  overload, `Valid`, `Zip`). AD-2 explicitly describes the composition verbs as keeping one
  canonical name per intent, which reads as `keep`.
- Minimal fix: add a CSV convention note (“type-level redesign marks every member of the type”)
  and state in AD-2/AD-3 that the default-state change affects all members, **or** set the
  composition/factory members to `keep` and enumerate redesign only where the prose does.

**V-02 — `decision-record.md` summary table does not reconcile with its own rows.**
- Location: `docs/next-stage/decision-record.md:22–35` vs rows E1–E142.
- Evidence: the summary claims 153 decisions (adopt 41 / adapt 19 / defer 32 / reject 61), but the
  record contains exactly 142 numbered rows (E1–E142, no gaps, no duplicates; verified by regex).
  The family allocation also does not match the section headings: F4 claims 12 rows while 22 rows
  (E46–E67) sit under the F4 heading, and F5 claims 23 while 11 rows (E68–E78) sit under F5;
  per-family decision distributions differ in every family (e.g. F6 actual adopt 6/reject 11 vs
  claimed 5/9). Even counting compound-decision rows twice yields 148, not 153.
- Minimal fix: regenerate the summary from the E rows (or replace the summary with a non-numeric
  index) and align the family assignment used by the table with the section headings.

**V-03 — 59 line-pinned citations to `docs/product-contract.md` no longer resolve.**
- Location: `decision-record.md:223` (E133), `api-decisions.md:246`, `analysis/baseline-bcl.md`
  (lines 308, 349, 357, 363, 455, 458, 488, 510), `analysis/baseline-funcky.md:368`,
  `analysis/funny-sharp-surface.md` (56, 270, 356, 390–395, 486, 528, 617, 689–696),
  `analysis/standing-constraints.md` (~25 references), `inventory/bcl.md` (384, 507, 670–674,
  721), `inventory/funny-sharp.md:395`, `call-sites.md` (751, 994, 1142, 1204).
- Evidence: the citations were written against `HEAD:docs/product-contract.md` (143 lines). The
  working-tree contract is 258 lines with new sections; e.g. E133 cites `:73–84` for the union
  deferral, but current lines 73–84 contain immutable-update text and the union text is now at
  181–192; the trim/AOT policy cited at `:86–105` now sits at 201–220; the dependency boundary
  cited at `:52–56` now sits at 158–166. Sampling confirmed the same displacement for every
  checked reference.
- Minimal fix: re-pin the final artifacts (at minimum `decision-record.md`, `api-decisions.md`,
  `standing-constraints.md`) to revised section headings (e.g. “Deliberate Deferrals”,
  “Trimming And Native AOT Policy”, “Package And Dependency Boundary”) or update the line numbers;
  keep line pins only for stable, frozen documents.

**V-04 — 17 memo decisions have no equivalent entry in `decision-record.md` or a conflicting one.**
- Location: the memo tables vs `decision-record.md`.
- Evidence: decision-bearing memo rows not cited in the record with no equivalent row:
  Funcky `F2.3` (EitherOrBoth/ZipLongest **defer**), `F2.4` (`GetOrThrow` **defer**),
  `F4.2` (`True`/`False`/`All`/`Any` **defer**), `F4.7`
  (`Cycle`/`CycleRange`/`RepeatRange`/`CycleMaterialized`/`Concat` **reject**);
  FSharp.Core `#5` (`Option.count`/`toArray`/`toList` **reject**), `#6` (`FSharpResult` carrier
  **reject**), `#8` (`ResultModule` function-first **reject**), `#22` (`EventModule`/`IEvent`
  **defer**), `#23` (`LanguagePrimitives` equality **reject**); language-ext `1.3`
  (`OptionUnsafe`/`OptionT`/`OptionAsyncT` **reject** — only `OptionAsync` is covered by E83),
  `4.4` (`Combinators` **defer**), `11.2` (`ISerializable` carrier serialization **reject** —
  E142 defers converters instead), `11.4` (`Unit` **defer** vs E98 **reject**); CFE `1.4`
  (`MaybeEqualityComparer` **defer**), `6.3` (global `ConfigureAwait` policy **reject**),
  `9.1` (value-object base classes **reject**), `11.4` (obsolete-retention policy **reject**).
- Minimal fix: add one reconciliation row or an explicit “superseded/deferred/rejected” mapping
  table in `decision-record.md` covering these rows, or amend the memos with a pointer to the
  final decision.

### Moderate

**V-05 — Six memo decisions cited with the opposite decision, without reconciliation.**
- Location/evidence: Funcky `F1.5` **adapt** (`baseline-funcky.md:24,105–115`) vs E10 **defer**
  (only the `RequiresDynamicCode` honesty aspect is adapted by E137); `F1.9` **adapt** (line 28)
  vs E14 **defer**; `F4.1` **adopt** (line 34) vs E53 **defer**; `F5.7` **adopt/adopt/adapt**
  (line 47) vs E57 **defer**; `F9.1` **defer** (line 63) vs E107 **reject**; `F4.6` **adapt**
  (line 39) vs E18 **reject** carrier / defer `Successors`. Funcky `F7.1` **adapt** vs E102
  **defer** is the one case that *is* reconciled explicitly (`decision-record.md:251–252`).
 - Minimal fix: record each downgrade/upgrade next to the E row (“memo recommended X; lead ruled Y
  because …”) or update the memo decision to “superseded”.

**V-06 — The README's supplementary-dump table and prose contradict the dumps.**
- Location: `docs/next-stage/inventory/generated/README.md:31–35, 61–64`.
- Evidence: the prose says “the `language-ext-*` family dumps … predate operator support, so
  operator members are absent there”, but the committed JSONs contain operators:
  `inv-language-ext-toplevel` 736 operators / 10 966 members (README row: 0 / 10 230),
  `inv-language-ext-typeclasses` 34 / 610 (README: 0 / 576), `inv-language-ext-pipes` 38 / 1050
  (README: 0 / 1012). The language-ext memo already records this as an open item
  (`baseline-language-ext.md:245–250`), so it is known but not fixed in the evidence file itself.
- Minimal fix: refresh the three count rows and reword the operator note to name the dumps that
  are genuinely pre-operator (the four `bcl-*` supplements) or regenerate them.

**V-07 — “Lead ruling C5/C6” cites documents that contain no rulings.**
- Location: `decision-record.md:239` (“lead ruling C5 in `analysis/standing-constraints.md`”),
  `api-decisions.md:246` (“lead ruling C6 in `analysis/standing-constraints.md`”).
- Evidence: `standing-constraints.md` §2 lists C5 and C6 as unresolved tensions that end in
  “Decision needed: …” (lines 436–448, 450–455); the word “ruling” appears nowhere in that memo.
- Minimal fix: state the rulings inline (the surrounding record text already contains C5's
  resolution) or cite them as “standing-constraints C5/C6 (resolved by this record)”.

**V-08 — `Unit` is misfiled as deferred in the constitution.**
- Location: `docs/product-contract.md:193–197` vs `decision-record.md:168` (E98 **reject**),
  `api-decisions.md:309` (G17 **reject**).
- Minimal fix: move “a public `Unit` type” out of the deferred list into an explicit rejection
  sentence.

**V-09 — Analyzer packaging is stated three ways.**
- Location: `docs/product-contract.md:138–139` (“optional, non-runtime dependency of the core
  package”), `decision-record.md:217` (E127: “Analyzer packaging in the main package with build
  assets”), `analysis/funny-sharp-surface.md:689` (OWN G9: “a separate analyzer package”).
- Minimal fix: decide in-package analyzers (Funcky model) vs a separate analyzer package, and
  make the contract, E126/E127, and G9 say the same thing.

**V-10 — Goal 20's competitor comparisons have no authorization/decision row; C5 remains open.**
- Location: `docs/goals/0020-goal.md:2` requires comparisons against FSharp.Core and “relevant
  Funcky, CSharpFunctionalExtensions, or language-ext operations”;
  `analysis/standing-constraints.md:436–448` (C5) asks for a decision; the only resolution is
  the general isolation sentence in `product-contract.md:172–175` and guardrail 1 of
  `decision-record.md:236–239`. No record row states whether competitor benchmark rows are
  required, permitted only in isolated tooling, or deferred.
- Minimal fix: add an explicit decision row (e.g. “competitor benchmark baselines — permitted
  only in the isolated evidence project, or deferred to the Goal 20 fixed-hardware runner”) and
  mirror it in the contract's performance section.

**V-11 — Unpropagated `UNVERIFIED` judgments into the final records.**
- Location: `decision-record.md:230` (E140 language-ext maintenance risk as fact; LE 11.7 marks
  it **UNVERIFIED**: no EOL statement read), `api-decisions.md:153–155` (AD-6 states the parallel
  operators “are bounded, ordered, cancellation-correct”; `baseline-bcl.md:507–513` marks
  ordering, early-disposal, and drain behavior **UNVERIFIED**).
- Minimal fix: add both items to the record/api-decisions “Unverified judgments” lists, or cite
  the test that verifies ordering/drain and mark the language-ext inference accordingly.

**V-12 — `inv-fsharp-core-focus` missing from the generated README index.**
- Evidence: the dump pair exists (96 types / 1621 members) and `inventory/fsharp-core.md:61–69`
  documents its purpose and regeneration, but `generated/README.md`'s supplementary table omits
  it.
- Minimal fix: add the row to the README table.

### Minor

- **V-13 — E142 cites `OWN G18`; the OWN memo has G1–G17.** G18 exists only in
  `api-decisions.md:310`. Fix: cite `api-decisions G18`.
- **V-14 — `call-sites.md §16.3…§16.8` citations do not name existing subsections.** §16 is an
  ordered list 1–11 with no `.x` headings (`call-sites.md:1178–1221`); the content matches list
  items 3–8. Fix: cite “§16 item 3” (or add explicit headings).
- **V-15 — Funcky memo `Chunk` line drift.** `baseline-funcky.md:271` cites
  `inv-bcl-sequences-linq.md:1454,1651`; the committed dump has `Chunk` at 1471 and 1668
  (+17 lines, the operator-regeneration drift anticipated by the brief).
- **V-16 — CFE memo `inventory:NNN` citations resolve only against the generated dump.**
  `baseline-csharpfunctionalextensions.md` cites ranges such as `inventory:469-646` while
  `inventory/csharpfunctionalextensions.md` has only 427 lines; the ranges match
  `generated/inv-csharpfunctionalextensions.md` with roughly 10 lines of drift at the mapped
  section starts (e.g. `MaybeExtensions` cited at 483–626, present at 493–637). Fix: use the
  `generated/` prefix or re-pin.
- **V-17 — `Competitors.csproj` is not marked non-packable.** Every other non-shipping project in
  the repository sets `IsPackable=false`; this one relies on being outside the solution. Fix:
  add `<IsPackable>false</IsPackable>`.
- **V-18 — OWN G7 divergence not reconciled.** `funny-sharp-surface.md:687` recommends
  **defer** completion-order coordination; `api-decisions.md:299` (G7) and E82 decide
  **adopt (constrained)**. Fix: note the supersession.
- **V-19 — Goal-enablement omissions.** The contract does not state: Goal 17's deferred
  non-empty/exact-vs-truncating-zip items (`decision-record.md:132` E72 defers them), Goal 23's
  release-notes requirement (`api-decisions.md:306` G14), or Goal 15's UnitResult traversal/HTTP
  integration (`api-decisions.md:293` G1). None blocks the goals, but the constitution is
  silent where the record decides. Fix: fold the three items into the contract's deferral/product
  lists.
- **V-20 — Record “Unverified judgments” is a subset of the memo flags.** The record lists 5
  items, `api-decisions.md` lists 5, while the memos carry ~50 flagged judgments; V-11 shows the
  propagation gap. Fix: add a cross-reference (“full list in analysis memos”) and propagate the
  decision-relevant ones.

## 3. Unverified Inventory

**Already flagged (tracking list; counts by artifact).** These do not need action beyond
maintainer tracking:

- `analysis/baseline-bcl.md` (9): `Option<T>.Some(default)` normalization; `InvalidMany(empty)`
  behavior; `SelectParallelValueAsync` ordering + early-disposal; `TraverseParallelValueAsync`
  drain-after-failure; `FirstSuccessAsync` “all failed vs caller cancelled”; `Result.Try*`
  behavior for exotic faults; which .NET 10 preview moved async LINQ in-box; OpenAPI inference
  for `IResult`; no side-by-side call sites when written.
- `analysis/baseline-csharpfunctionalextensions.md` (2): semantic-LOC counting unit is an
  analyst judgment; no benchmarks run (criterion-8 verdicts static).
- `analysis/baseline-fsharp-core.md` (3): VMR→tag commit ancestry; 10.1.401 release notes 404;
  microbenchmarks directional only.
- `analysis/baseline-funcky.md` (7): all adopted-combinator performance; EF Core translation of
  `IQueryable` bridges; AOT-clean STJ converter; consumer demand for F1.6/F2.2/F5.10/F5.13;
  list-pattern usage; naming authority; closed-generic converter feasibility.
- `analysis/baseline-language-ext.md` (6 + inline): v4 typeclass / v5 `K<M,A>` boxing cost;
  `Try*` cancellation handling; `(A)option` on `None` and truthiness operators; 4.4.9 nullable
  annotations; `Map`/`Seq`/`HashMap` persistence performance; upstream deprecation intent.
- `analysis/funny-sharp-surface.md` (8): canonical naming; maintainer accumulation-shape
  preference; production-scale Effect/StateTransition throughput; Option-comparer demand; null
  policy for `Effect.FromValue`; stability tiering; trim/AOT not re-run at this commit;
  qualitative acceptance judgments.
- `analysis/standing-constraints.md` (6): Goal 13 acceptance artifacts missing; multiple
  toolchains unverified; docs performance tables not verified against `baseline.json`;
  analyzer “no runtime dependency” feasibility; plus the historical goal-12 status.
- `call-sites.md` (6): three benchmarking gaps, two FsToolkit scope notes, one `Eff`/`Aff`
  inventory gap.
- `decision-record.md` §“Unverified judgments” (5) and `api-decisions.md` §“Unverified
  judgments” (5): maintainer acceptance of vocabulary/stability boundary/first-success shape;
  production-scale Effect/StateTransition throughput; closed-generic JSON converters; OpenAPI
  inference; consumer-demand triggers.
- Inventory/README notices: metadata-mode dumps carry no nullability; clone-only material is
  unreleased; additive changes are not implemented by Goal 14.

**Stated without an explicit UNVERIFIED flag (found by this audit, see V-11):**

- `decision-record.md:230` E140 — language-ext stable-line maintenance inference.
- `api-decisions.md:153–155` AD-6 — “bounded, ordered, cancellation-correct” parallel operators.
- `decision-record.md:22–35` — the summary counts (a numeric claim; V-02).
- `docs/product-contract.md:212–216` — the open-generic `ValueTuple` AOT limitation *is*
  backed by `docs/release-evidence/goal-12.md:99–101`, so it is not a finding.

## 4. Coverage Matrix

| Metric | Count |
| --- | ---: |
| Inventory types (core 33 + ASP.NET Core 1) | 34 |
| Types present in `decision-matrix.csv` | 34 / 34 |
| Inventory members (core 254 + ASP.NET Core 15) | 269 |
| Members present in CSV (incl. 8 operators, 3 ctors, 3 delegate, 4 enum values) | 269 / 269 |
| Missing / extra / duplicate CSV rows | 0 / 0 / 0 |
| CSV member decisions | keep 220, redesign 49, experimental 0, remove 0 |
| CSV type decisions (unique) | keep 30, redesign 4, experimental 0, remove 0 |
| Redesign rows not covered by AD-2/AD-3 prose | 20 of 49 (V-01) |
| Memo decision-bearing rows (CFE 33, FS 28, FU 48, LE 46) | 155 |
| Memo rows cited in the record | 109 |
| Uncited, semantically covered by an equivalent record row | 29 |
| Uncited/conflicting without equivalent row | 17 (V-04) + 6 cited-with-different-decision (V-05) |
| Targeted citation checks | 32 (29 in-range, 3 drift/mislabel) |
| `product-contract.md` line-pinned references audited | 59 (all written against `HEAD`, stale vs the revised contract) |
| Pinned `.nupkg` SHA256 verified | 4 / 4 recorded pins match; 5th is cross-check-only |
| Rebuilt assembly SHA256 vs `baselines.md` | 2 / 2 match |
| README-referenced dump pairs verified to exist | 22 / 22 (counts: canonical all match; 3 supplementary rows stale) |
| Call-site projects rebuilt clean (`--no-incremental`) | 4 / 4 (0 warnings, 0 errors) |
| Repository solution builds clean | 1 / 1 (0 warnings, 0 errors) |
| W1–W11 workflows with LOC table + compile evidence | 11 / 11 |
| Raw-LOC reproduction (`tools/rawloc.py`) | matches document exactly (489 total raw lines) |
| Competitor references outside the isolated evidence project | 0 |
| `UNVERIFIED` markers in the evidence set | 72 occurrences (plus 10 in the two final judgment lists) |

## 5. Not Verified

- **Maintainer acceptance** of the canonical vocabulary, stability boundary, default-state
  mechanism, and `FirstSuccessAsync` return shape — explicitly pending in
  `docs/next-stage/README.md:42` and listed as unverified; this audit cannot decide it.
- **Goal 14 completion itself** — this audit verifies the evidence set, not the maintainer's
  product judgment.
- **Runtime behavior of competitor libraries** (Funcky, CFE, language-ext, FSharp.Core): no code
  was executed; all behavioral claims remain source/XML/dump based, as the memos state.
- **Performance numbers in the memos** — no benchmarks were run by this audit; the only timing
  figures checked were the existing generated BCL/StateTransition tables cited in the documents.
- **Trim/AOT** — not re-run; `goal-12` evidence is historical.
- **EF Core translation, JSON converter feasibility, OpenAPI inference, `(A)option` semantics** —
  inherited UNVERIFIED flags, not re-tested here.
- **Completeness of the capability survey itself** (i.e. whether some external capability is
  missing from all memos) — the audit can only check the surveyed set against the record; it did
  not independently re-derive the survey from the four packages.
- **Funcky clone divergence** beyond the 3.6.0 release and all language-ext v5 material — the
  local clones were not diffed against the pinned nupkgs by this audit.
- **F# reference snippets** in `call-sites.md §15` — reference-only, not compiled (as documented).
- **Scratch evidence under `/tmp/opencode`** other than the hashed nupkgs and the four call-site
  build trees (e.g. probe projects) — not re-executed.
- **Semantic-LOC hand counts** — the rule was applied reproducibly for raw LOC, but the semantic
  S+O counts are analyst judgments by design; they were not recomputed.

## 6. Verdict (round 1 — superseded by §7.6)

**No — the Goal 14 evidence set is not yet complete and internally consistent enough to declare
the goal done.** It is close and largely credible: the member-level decision matrix covers the
inventory exactly (34/34 types, 269/269 members including operators), every recorded baseline
hash and rebuilt assembly hash matches, all five release/call-site builds are warning-clean, the
call-site suite covers W1–W11 with reproducible raw LOC and pinned competitor versions, and the
independence boundary holds in the repository (one isolated competitors project, no competitor
reference anywhere else). Before completion, the lead should fix the four material defects
(V-01 member-level redesign scope, V-02 record summary counts, V-03 stale
`product-contract.md` line citations, V-04 unrepresented memo decisions), reconcile the six
cited-with-opposite-decision rows, correct the `Unit`/analyzer-packaging statements, resolve the
Goal 20 competitor-comparison question, and refresh the stale supplementary-dump counts; after
that, `docs/next-stage/README.md`’s “Independent completeness audit” row can move to complete,
and the only remaining blocker is **maintainer acceptance** of the vocabulary and stability
boundary.

---

## 7. Re-verification (round 2)

- **Round-2 date:** 2026-09-17
- **Audited state:** same worktree as round 1 — `HEAD` = `4dbebd94b7b58648632112b7ca47c39cc517f153`;
  `M docs/product-contract.md`, untracked `docs/next-stage/` + `eng/next-stage-inventory/`, no other
  modifications. `decision-record.md` = 37 351 B, md5 `dde6dcc61006106ab9104a908bfab516`.
- **Method:** independent recomputation from the files: Python recount of every E-row against the
  §Summary table; CSV↔JSON set comparison for types and members; every README count row recomputed
  from the committed JSONs; targeted `grep`/`sed` citation checks; `sha256sum` of the four pinned
  `.nupkg` files and the rebuilt assemblies; full `dotnet build` runs (SDK 10.0.400) of the solution
  and the four call-site projects (`--no-incremental`), plus the committed competitors project.
- **Rule:** fail-closed, unchanged from round 1.

### 7.1 Round-1 finding status

| V | Status | Round-2 evidence |
| --- | --- | --- |
| V-01 | **fixed** | `api-decisions.md:35–37` declares the CSV convention (“when a type is redesigned, every member of that type carries `member_decision=redesign`”); AD-2 (`:85–88`) and AD-3 (`:93–96`) state the type-wide scope and why every `Result`/`Validation` row is marked. CSV recomputation confirms the 49 redesign rows: `Result<TValue,TError>` 25, `Validation<TValue,TError>` 18, `ConcurrentEffectExtensions` 3, `FunctionExtensions` 2 (type-level keep, AD-4), `StateTransitionExtensions` 1; every redesign type has all members redesign. No contradicting prose remains. |
| V-02 | **still open** | `decision-record.md:22–35` still shows the round-1 table (F1 6/3/5/6 … **Total 41/19/32/61 = 153**), while the record contains exactly **160** rows (E1–E160, no gaps, no duplicates). Independent recount is 37 adopt / 12 adapt / 27 defer / 84 reject (exact per-family values in §7.2). The lead’s stated target (160 rows, 37/12/27/84) matches the recount; the file does not contain it. |
| V-03 | **fixed (specified pattern) + minor residue R-1** | `grep` for `docs/product-contract.md:<line>` under `docs/next-stage/` (excluding this file): **zero matches**; re-pinned citations now use section names (`§"Product Direction"`, `§"Package And Dependency Boundary"`, `§"Deliberate Deferrals"`, `§"Trimming And Native AOT Policy"`, `§"Analyzers And Compiler Feedback"`, `§"Baseline Verification"`) that resolve to real headings. Eight stale numeric suffixes remain in mixed form — see R-1 in §7.2. |
| V-04 | **fixed** | E143–E160 exist and are numbered continuously. The “Memo reconciliation” table (`decision-record.md:252–285`, 26 rows) maps all 17 previously unrepresented memo decisions to their final rows: FU F2.3→E148, F2.4→E151, F4.2→E149, F4.7→E150; FS #5→E144, #6→E153, #8→E152, #22→E145, #23→E146; LE 1.3→E143, 4.4→E155, 11.2→E156, 11.4→E98; CFE 1.4→E147, 6.3→E157, 9.1→E158, 11.4→E159. Each final decision was checked against the memo row and the E-row text. |
| V-05 | **fixed** | The six opposite-decision rows now carry inline “memo recommended / lead ruled” notes: E10 (`:50`), E14 (`:54`), E18 (`:58`), E53 (`:118`), E57 (`:122`), E107 (`:190`); E102 (`:185`) keeps its note. The reconciliation table flags each as “(note)” and adds the OWN G7 supersession. |
| V-06 | **fixed** | All 23 count rows in `inventory/generated/README.md:56–78` were recomputed from the JSON dumps: 23/23 exact match (canonical and supplementary). Language-ext rows now read 339/10 966/736/2 939, 85/610/34/32, 63/1 050/38/141; the genuinely pre-operator rows (bcl-sse, bcl-supplement, bcl-supplement-f2f3f11, aspnetcore-http, language-ext-globalns, language-ext-effects, v5 dumps) all have 0 operators. The prose (`:31–35`) was reworded. |
| V-07 | **fixed** | No “lead ruling C5/C6” text remains anywhere under `docs/next-stage`; the cites now read “standing-constraints C5/C6, resolved by this record” (`decision-record.md:250,301`; `api-decisions.md:260`), with the ruling stated inline (E160 + independence guardrail 1; AD-10). |
| V-08 | **fixed** | `product-contract.md:203–205` rejects a public `Unit` explicitly (“rejected, not deferred”); `Unit` is no longer in the deferred list. |
| V-09 | **fixed** | `product-contract.md:141–154` states one model: analyzers ship inside the `FunnySharp` package under `analyzers/dotnet/cs` as build assets, no separate package; E126/E127 agree, and OWN G9’s separate-package recommendation is recorded as superseded (`decision-record.md:292–294`). |
| V-10 | **fixed** | E160 (`decision-record.md:250`) adopts competitor benchmark baselines for Goal 20, referencing packages only in a non-packable, non-shipping evidence project outside `FunnySharp.slnx`, with results barred from API-compatibility/acceptance use; `product-contract.md:126–128` mirrors it, and standing-constraints C5 is closed by the record. |
| V-11 | **fixed** | AD-6 (`api-decisions.md:162–170`) cites `docs/concurrency.md:28–46,73–86` and the three test files (all exist; the files carry drain/ordering tests) and states the BCL-memo uncertainty was BCL-only. E140 carries an inline UNVERIFIED flag (`decision-record.md:243`), and both “Unverified judgments” lists now include the language-ext maintenance risk and the full-inventory cross-reference. |
| V-12 | **fixed** | `inv-fsharp-core-focus.*` is listed in the supplementary table (`README.md:39`) and in the counts table (`README.md:62`: 96 types / 1 621 members / 81 operators / 0 extension members — recomputed from the JSON and matching). |
| V-13 | **fixed** | E142 now cites `api-decisions.md` G18 (`decision-record.md:245`); no `OWN G18` citation remains. |
| V-14 | **fixed** | No `§16.x` references remain in the final records (`grep` across `decision-record.md`, `api-decisions.md`, `call-sites.md`, `README.md`, `../product-contract.md`); citations now read “§16 item 3/4/5/6/7/8”, which resolves against the ordered list 1–11 in `call-sites.md §16`. |
| V-15 | **fixed** | `baseline-funcky.md:269–273` re-pins `Chunk` to `inv-bcl-sequences-linq.md:1471,1668`; the committed dump has `Chunk` at exactly those lines, and the cited `/tmp/opencode/evidence` copy is byte-identical to the committed dump. |
| V-16 | **fixed** | The CFE memo citation note (`baseline-csharpfunctionalextensions.md:13–16`) names `inventory/generated/inv-csharpfunctionalextensions.md` as the target of every `inventory:NNN` reference and records the regeneration/line-drift caveat. |
| V-17 | **fixed** | `docs/next-stage/call-sites-code/competitors/Competitors.csproj:10–11` sets `<IsPackable>false</IsPackable>` and a non-shipping description; the committed project was rebuilt `--no-incremental` with 0 warnings / 0 errors. |
| V-18 | **fixed** | OWN G7 supersession is recorded in the reconciliation table (`decision-record.md:285`): memo “defer” → final “adopt (constrained)” because Goal 18 requires completion-order results. |
| V-19 | **fixed** | `product-contract.md` now includes `UnitResult<TError>` traversal/HTTP integration (`:15–16`), release notes/versioning rules (`:52–54`), and non-empty carriers / exact-vs-truncating zip in the deferral list (`:199–205`). |
| V-20 | **fixed** | Both judgment lists now close with the memo cross-reference: `decision-record.md:318–319` (“full per-workstream `UNVERIFIED` inventory”) and `api-decisions.md:339–340`; the record also flags E140 in its row. |

### 7.2 Remaining findings

**V-02 (round-2 status: open, moderate — now resolved in round 3, see §8.1) — the
`decision-record.md` summary table was not regenerated.**

- The record contains exactly 160 decision rows, E1–E160, no gaps and no duplicates (verified by
  regex extraction). The table at `decision-record.md:22–35` still reports the round-1 figures
  (F1 6/3/5/6, F2 2/3/4/8, F3 2/2/1/4, F4 2/1/4/5, F5 8/3/6/6, F6 5/2/3/9, F7 3/1/3/6, F8 2/0/1/4,
  F9 3/1/0/3, F10 2/0/2/3, F11 6/3/3/7; **Total 41/19/32/61 = 153**).
- Independent recount, counting each row once by its primary (first-listed) decision — compound rows
  E13 defer, E18 reject, E23 reject, E32 defer, E60 defer, E75 adapt, E93 reject — and assigning rows
  by their section heading:

  | Family | adopt | adapt | defer | reject |
  | --- | ---: | ---: | ---: | ---: |
  | F1 absence | 6 | 3 | 7 | 8 |
  | F2 fail-fast | 3 | 1 | 4 | 14 |
  | F3 accumulation | 2 | 1 | 1 | 5 |
  | F4 function grammar | 3 | 0 | 8 | 13 |
  | F5 collections | 6 | 2 | 1 | 3 |
  | F6 async/concurrency | 5 | 2 | 0 | 11 |
  | F7 effects/resources | 2 | 0 | 2 | 8 |
  | F8 state | 1 | 0 | 1 | 3 |
  | F9 optics/immutability | 2 | 0 | 0 | 4 |
  | F10 HTTP | 2 | 0 | 1 | 3 |
  | F11 cross-cutting | 5 | 3 | 2 | 12 |
  | **Total** | **37** | **12** | **27** | **84** |

  This matches the lead’s stated target exactly (160 rows: adopt 37, adapt 12, defer 27, reject 84).
  The fix is a one-table replacement; no other count in the evidence set is stale (no remaining
  references to 142/153 decision counts were found).

**R-1 (new, minor — now resolved in round 3, see §8.1) — stale numeric suffixes beside re-pinned
`product-contract.md` section names.**

- The specified `docs/product-contract.md:<line>` form is gone, but eight pre-revision line pins
  survive inside section-name citations, where the section name resolves while the number now points
  at unrelated text:
  `analysis/standing-constraints.md:25` (`§"Package And Dependency Boundary",61`),
  `:34` (`,54-56,137-143`), `:41` (`,143`), `:47` (`,139`), `:56` (`,103-105`),
  `:280` (`§"Product Direction",34-35`); `analysis/baseline-funcky.md:368` (`,52-53`);
  `analysis/funny-sharp-surface.md:356` (bare `(`:73-84`)`).
- Examples: `,61` was the old “core remains BCL-only” line (now contract line 173); `:73-84` was the
  old union deferral text (now 187–198); `,103-105` was compatibility evidence, which at `HEAD` was
  already in the trimming section (now 226–228), so that label is also off.
- Fix: delete the comma-separated numeric suffixes (and correct the one label); section names are the
  durable pin. Minor because every section name resolves.

**No regressions.** Every fail-closed check that passed in round 1 still passes (see §7.3), and no
previously verified artifact changed except by the recorded fixes. Note: the committed
`Competitors.csproj` now differs from the `/tmp/opencode/callsites/competitors` scratch copy only by
the V-17 fix (`IsPackable=false` + description); the committed project was rebuilt explicitly and is
warning-clean.

### 7.3 Fail-closed checks re-run

| Check | Round-2 result |
| --- | --- |
| JSON inventories | core 33 types / 254 members; ASP.NET Core 1 type / 15 members |
| Types present in `decision-matrix.csv` | 34 / 34 (0 missing, 0 extra) |
| Members present in CSV | 269 / 269 (0 missing, 0 extra, 0 duplicate rows) |
| CSV member decisions | keep 220, redesign 49, experimental 0, remove 0 |
| CSV type decisions (unique) | keep 30, redesign 4, experimental 0, remove 0 |
| CSV member kinds | method 237, property 14, operator 8, ctor 3, delegate 3, enum-value 4 |
| Repository build (`dotnet build FunnySharp.slnx -c Release`) | exit 0, 0 warnings / 0 errors (SDK 10.0.400) |
| Call-site builds (`--no-incremental`) | idiomatic, funnysharp, funnysharp-aspnet, competitors all exit 0, 0/0 each |
| Committed `Competitors.csproj` build (extra, post-V-17) | exit 0, 0 warnings / 0 errors |
| Pinned `.nupkg` SHA256 | 4 / 4 recorded pins match `baselines.md:21–27`; the 10.0.100 cross-check has no recorded pin by design |
| Rebuilt assembly SHA256 vs `baselines.md:10–13` | 2 / 2 match (`FunnySharp.dll` `793c8532…9bf9d4`; `FunnySharp.AspNetCore.dll` `3810e1de…2f77a42`) |
| README count rows recomputed from JSON | 23 / 23 exact match |
| Reference dump pairs present | 23 / 23 (`.md` + `.json`) plus `types-languageext-4.4.9.txt` |
| Regressions | 0 |

### 7.4 Maintainer acceptance

`docs/next-stage/maintainer-acceptance.md` still reads **“Status: awaiting maintainer response”**
(questions presented 2026-09-17) and every outcome in A-1…A-8 is **pending**. This is the Goal 14
acceptance step, not an evidence defect: the audit cannot decide it. Once V-02 is regenerated
(R-1 is optional polish), maintainer acceptance of the eight decisions remains the **only external
blocker**; no other artifact is outstanding.

### 7.5 Round-2 coverage

| Metric | Count |
| --- | ---: |
| Decision-record rows (E1–E160) | 160 |
| Row distribution | adopt 37, adapt 12, defer 27, reject 84 |
| Round-1 findings fully fixed | 18 of 20 (V-01, V-04…V-20) |
| Round-1 findings partial | 1 (V-03: specified pin form gone; residue R-1) |
| Round-1 findings open | 1 (V-02) |
| `docs/product-contract.md:<line>` pins remaining | 0 |
| Residual stale numeric suffixes (R-1) | 8 |
| `§16.x` refs remaining in the final records | 0 |
| New regressions | 0 |

### 7.6 Verdict (round 2 — superseded by §8.4)

**No — one round-1 defect is still open, so the evidence set is not yet internally consistent enough
to declare the goal done.** Round 2 confirms real progress: 18 of the 20 round-1 findings are fully
fixed — the CSV level redesign scope and convention no longer contradict the prose (V-01), the
previously unrepresented memo decisions are recorded as E143–E160 with a reconciliation table and
inline deviation notes (V-04/V-05), every generated-README count matches the JSON dumps (V-06/V-12),
the standing-constraint rulings, Goal 20 benchmark authorization, AD-6 evidence citations, unverified
lists, and the G7 supersession are recorded (V-07/V-10/V-11/V-18/V-20), the constitution now rejects
a public `Unit`, fixes one analyzer-packaging model, and carries the deferred zip/UnitResult/release-
notes language (V-08/V-09/V-19), and the citation fixes are in place (V-13…V-17). Every fail-closed
check still passes: CSV coverage is exact (34/34 types, 269/269 members, zero missing/extra/duplicate
rows), the solution and all call-site projects build warning-free, and all four pinned `.nupkg` hashes
plus both rebuilt assembly hashes match `baselines.md`; no regression was found. Two items keep the
goal from closing: (1) **V-02** — `decision-record.md:22–35` still reports 153 decisions
(41/19/32/61) while the record contains 160 rows whose recounted totals are 37/12/27/84 (§7.2); the
table simply needs replacing with the values above; (2) **R-1** — eight stale numeric suffixes remain
beside correctly re-pinned `product-contract.md` section names (minor; optional to fix). After
V-02, maintainer acceptance of the eight decisions in `maintainer-acceptance.md` — still pending and
correctly listed as the only external blocker — is the sole remaining requirement for Goal 14.

---

## 8. Re-verification (round 3)

- **Round-3 date:** 2026-09-17
- **Audited state:** same worktree as rounds 1–2 — `HEAD` = `4dbebd94b7b58648632112b7ca47c39cc517f153`;
  `M docs/product-contract.md`, untracked `docs/next-stage/` + `eng/next-stage-inventory/`, no other
  modifications. `decision-record.md` = 37 431 B, md5 `d4fc35e03ce331f04b06141eb89a064d` (round 2:
  37 351 B, `dde6dcc6…`; the delta is the regenerated summary table).
- **Scope:** the two open round-2 items (V-02, R-1) plus the standing fail-closed checks.
- **Method:** independent Python recount of every E-row against the §Summary table (rows assigned by
  their `## F#` section heading, each row counted once by its first-listed decision keyword; compound
  rows E13 defer, E18 reject, E23 reject, E32 defer, E60 defer, E75 adapt, E93 reject); regex/set
  scans of every `§"…"` citation and every standalone `(:NN)` form under `docs/next-stage` (excluding
  this file); CSV↔JSON set comparison of types and members; `dotnet build FunnySharp.slnx -c Release`;
  `sha256sum` of the four pinned `.nupkg` files and the two rebuilt assemblies.
- **Rule:** fail-closed, unchanged.

### 8.1 Round-2 finding status

| V | Status | Round-3 evidence |
| --- | --- | --- |
| V-02 | **fixed** | `decision-record.md:22–35` now reports 160 decision rows — adopt 37 / adapt 12 / defer 27 / reject 84 — with per-family rows F1 6/3/7/8, F2 3/1/4/14, F3 2/1/1/5, F4 3/0/8/13, F5 6/2/1/3, F6 5/2/0/11, F7 2/0/2/8, F8 1/0/1/3, F9 2/0/0/4, F10 2/0/1/3, F11 5/3/2/12. Independent recount of E1–E160 (no gaps, no duplicates) reproduces every cell and the total exactly; the only rows whose decision cell mixes keywords are E13, E18, E23, E32, E60, E75 and E93, each counted once by its first-listed decision. No stale decision count (153/142/41/19/32/61) remains anywhere in the evidence set. |
| R-1 | **fixed** | All eight stale numeric suffixes are gone (previously at `standing-constraints.md:25,34,41,47,56,280`, `baseline-funcky.md:368`, `funny-sharp-surface.md:356`). All 60 `§"…"` citations under `docs/next-stage` (excluding this file) use 6 distinct section names, every one resolving to a real `docs/product-contract.md` heading; zero citations carry a numeric suffix, zero backtick spans combine `product-contract` with `:NN`, and zero standalone product-contract `(:NN)` refs remain. V-03 is therefore fully fixed. |

R-1 note (fail-closed): 59 bare `(:NN)` refs still exist in `inventory/*.md` and `call-sites.md`, but
each continues an explicitly named dump/source file — e.g. `inventory/funcky.md:111` anchors
`Option.Core.cs:8-105` and later writes `` (`:26`) ``; `inventory/fsharp-core.md:222–226` cites the
dump in the table header; `call-sites.md:563` names `FunctionExtensions.cs:114` and `:16` continues
it. None has a `product-contract` antecedent; this is the documents' established self-citation
convention, not a stale contract pin.

### 8.2 Fail-closed checks re-run

| Check | Round-3 result |
| --- | --- |
| JSON inventories | core 33 types / 254 members; ASP.NET Core 1 type / 15 members |
| Types present in `decision-matrix.csv` | 34 / 34 (0 missing, 0 extra) |
| Members present in CSV | 269 / 269 (0 missing, 0 extra, 0 duplicate rows) |
| CSV member decisions | keep 220, redesign 49, experimental 0, remove 0 |
| CSV type decisions (unique) | keep 30, redesign 4, experimental 0, remove 0 |
| CSV member kinds | method 237, property 14, operator 8, ctor 3, delegate 3, enum-value 4 |
| Redesign rows by type | `Result<TValue,TError>` 25, `Validation<TValue,TError>` 18, `ConcurrentEffectExtensions` 3, `FunctionExtensions` 2, `StateTransitionExtensions` 1 |
| Repository build (`dotnet build FunnySharp.slnx -c Release`) | exit 0, 0 warnings / 0 errors, 8 projects (SDK 10.0.400) |
| Pinned `.nupkg` SHA256 | 4 / 4 recorded pins match `baselines.md:24–27`; the 10.0.100 cross-check has no recorded pin by design |
| Rebuilt assembly SHA256 vs `baselines.md:10–13` | 2 / 2 match (`FunnySharp.dll` `793c8532…9bf9d4`; `FunnySharp.AspNetCore.dll` `3810e1de…2f77a42`) |
| Regressions | 0 |

### 8.3 Round-3 coverage

| Metric | Count |
| --- | ---: |
| Decision-record rows (E1–E160) | 160 |
| Row distribution | adopt 37, adapt 12, defer 27, reject 84 |
| Summary-table cells verified against the recount (11 families + total) | 12 / 12 exact |
| Round-1 findings fully fixed | 20 of 20 (V-01…V-20) |
| Open findings | 0 |
| `§"…"` citations with a stale numeric suffix (excluding this file) | 0 |
| `product-contract.md:<line>` pins / product-contract `(:NN)` refs | 0 / 0 |
| New regressions | 0 |

### 8.4 Verdict (round 3 — superseded by §9.5)

**Yes — the Goal 14 evidence set is complete and internally consistent; the only remaining blocker to
declaring the goal done is the pending maintainer acceptance, which is an external product decision.**
All 20 round-1 findings are now fully fixed: the last two open items were resolved by regenerating the
decision-record summary (V-02 — every per-family cell and the 37/12/27/84 total reproduce from the
E-rows, and no stale count survives) and by removing the eight stale numeric suffixes beside the
section-name citations (R-1 — zero stale pins and zero standalone product-contract `(:NN)` refs
remain, so V-03 is fully fixed). Every standing fail-closed check passes with zero regressions:
CSV↔JSON coverage is exact (34/34 types, 269/269 members, zero missing/extra/duplicate rows, keep 220 /
redesign 49), the solution builds warning-free (0/0, 8 projects) and the rebuilt assemblies hash to
the recorded pins, and all four pinned `.nupkg` SHA256 values match `baselines.md`. The evidence set
therefore passes independent verification; `maintainer-acceptance.md` still reads “awaiting maintainer
response” with A-1…A-8 all **pending**, and correctly remains the only external blocker — this audit
cannot decide the vocabulary, stability-boundary, or default-state choices.

---

## 9. Re-verification (round 4: post-acceptance delta)

- **Round-4 date:** 2026-09-17
- **Audited state:** same worktree as rounds 1–3 — `HEAD` =
  `4dbebd94b7b58648632112b7ca47c39cc517f153` (`main`); `M docs/product-contract.md`, untracked
  `docs/next-stage/` + `eng/next-stage-inventory/`, no other modifications (`git status --short`).
  Round-4 artifact hashes: `decision-record.md` 37 731 B, md5 `8d77ddb0…` (round 3: 37 431 B,
  `d4fc35e0…`; the delta is the E81 wording and the OWN §2.3 reconciliation bullet);
  `api-decisions.md` 22 675 B, `852e9efe…`; `decision-matrix.csv` 60 622 B, `c34bedf4…`;
  `maintainer-acceptance.md` 7 909 B, `8effb825…`; `docs/product-contract.md` 19 724 B,
  `0382132e…`.
- **Scope:** the post-acceptance delta only — the maintainer's eight decisions (A-2 taken as
  option B) and their propagation into `decision-matrix.csv`, `api-decisions.md`,
  `docs/product-contract.md`, and `decision-record.md` — plus the standing fail-closed checks.
- **Method:** independent Python recount of the CSV (member/type decisions, redesign grouped by
  type, duplicate and conflicting-row checks) and of all 160 E-rows against the §Summary table
  (escaped-pipe-aware row parsing, rows assigned by section heading, each counted once by its
  first-listed decision); JSON↔CSV set comparison of types and members; `grep`/regex scans for
  `pending`/`awaiting` acceptance status, stale pre-delta counts (keep 220 / redesign 49 / keep 30 /
  redesign 4), and `FirstSuccessAsync` + “redesign” proximity in the final records;
  `dotnet build FunnySharp.slnx -c Release` (SDK 10.0.400), then a forced `--no-incremental`
  rebuild; `sha256sum` of the four pinned `.nupkg` files and the two rebuilt assemblies.
- **Rule:** fail-closed, unchanged.

### 9.1 Delta item status

| # | Delta item | Status | Round-4 evidence |
| --- | --- | --- | --- |
| 1 | `maintainer-acceptance.md` | **pass** | `Status: accepted 2026-09-17`; 8/8 `Outcome:` lines recorded; zero `pending`/`awaiting` occurrences. A-2 = **accepted with change — option B** with the “keep it simple; `TError` can be nested” rationale, the explicit requirement to document the race contract at the method and in the concurrency guide, and the §2.3 supersession. A-4 = option A, recorded with the requested Funcky / language-ext / FSharp.Core comparison and the post-`UnitResult<TError>` revisit trigger. |
| 2 | `decision-matrix.csv` | **pass** | `FunnySharp.ConcurrentEffectExtensions` type `keep` and all three `FirstSuccessAsync` rows `keep`; 269 rows; member decisions **keep 223 / redesign 46**; redesign rows only in `Result<TValue,TError>` 25, `Validation<TValue,TError>` 18, `StateTransitionExtensions` 1, `FunctionExtensions` 2; distinct type decisions **keep 31 / redesign 3**; zero duplicate (type, kind, member) rows; no type carries conflicting `type_decision` values. |
| 3 | `api-decisions.md` | **pass** | Summary table reports keep 31 / 223 and redesign 3 types / 46 members, with the `FunctionExtensions` type-level-keep plus two-redesign-exception note; AD-6 states the keep decision (`Keep (maintainer decision A-2=B)`) and the race contract (winner `Valid`; all-typed-failure `Invalid` in input order; nested-`TError` guidance; §2.3 superseded); zero `FirstSuccessAsync` + “redesign” proximity hits. |
| 4 | `docs/product-contract.md` | **pass** | The first-success paragraph (`:119–124`) states the kept shape: “Its return shape stays `Validation<TValue, TError>`: a winner is `Valid`, and an all-typed-failure race is `Invalid` with the failures in input order; that race contract is documented explicitly at the method and in the concurrency guide.” No redesign claim remains. |
| 5 | `decision-record.md` | **pass** | E81 = `adopt (present; return shape kept per maintainer A-2=B)` with the `Validation` race-carrier sentence; “Additional reconciliations” records the OWN §2.3 supersession; the 160-row summary table (E1–E160, no gaps, no duplicates) is unchanged and the independent recount reproduces every per-family cell and the total exactly (adopt 37 / adapt 12 / defer 27 / reject 84). |
| 6 | Fail-closed checks | **pass** | See §9.2: CSV↔JSON coverage exact, build clean (incremental and forced `--no-incremental`), 4/4 `.nupkg` pins, 2/2 rebuilt-assembly hashes. |
| 7 | Round-1/2/3 findings | **no regressions** | All 20 round-1 findings and R-1 remain fixed; no stale pre-delta count (keep 220 / redesign 49 / keep 30 / redesign 4) survives anywhere in the evidence set outside this file’s historical sections. |

Memo note: `analysis/funny-sharp-surface.md` §2.3 still records the pre-decision redesign
recommendation (`:125–147`, `:610`) as a historical analysis artifact. The supersession is recorded
in the acceptance record, AD-6, E81, and the reconciliation section — the same memo-vs-final
pattern already used for OWN G7 and the V-05 rows. Not a finding.

### 9.2 Fail-closed checks re-run

| Check | Round-4 result |
| --- | --- |
| JSON inventories | core 33 types / 254 members; ASP.NET Core 1 type / 15 members |
| Types present in `decision-matrix.csv` | 34 / 34 (0 missing, 0 extra) |
| Members present in CSV | 269 / 269 (0 missing, 0 extra, 0 duplicate rows) |
| CSV member decisions | keep 223, redesign 46, experimental 0, remove 0 |
| CSV type decisions (unique) | keep 31, redesign 3, experimental 0, remove 0 |
| CSV member kinds | method 237, property 14, operator 8, ctor 3, delegate 3, enum-value 4 |
| Repository build (`dotnet build FunnySharp.slnx -c Release`) | exit 0, 0 warnings / 0 errors, 8 projects (SDK 10.0.400) |
| Forced rebuild (`--no-incremental`, extra) | exit 0, 0 warnings / 0 errors (43 s) |
| Pinned `.nupkg` SHA256 | 4 / 4 recorded pins match `baselines.md:24–27`; the 10.0.100 cross-check has no recorded pin by design |
| Rebuilt assembly SHA256 vs `baselines.md:10–13` | 2 / 2 match after the forced rebuild (`FunnySharp.dll` `793c8532…9bf9d4`; `FunnySharp.AspNetCore.dll` `3810e1de…2f77a42`) |
| Decision-record summary vs E-row recount | 12 / 12 cells exact (11 families + total; 160 rows, 37/12/27/84) |
| Regressions | 0 |

### 9.3 Round-4 coverage

| Metric | Count |
| --- | ---: |
| CSV rows | 269 |
| Member decisions | keep 223 / redesign 46 |
| Type decisions (distinct) | keep 31 / redesign 3 |
| Redesign rows by type | `Result<TValue,TError>` 25, `Validation<TValue,TError>` 18, `StateTransitionExtensions` 1, `FunctionExtensions` 2 |
| Decision-record rows / distribution | 160 / 37 adopt · 12 adapt · 27 defer · 84 reject |
| Maintainer acceptance outcomes recorded | 8 / 8 (A-1…A-8); A-2 = option B |
| `pending`/`awaiting` acceptance statements in the five synchronized artifacts | 0 |
| Stale README status cells (R-2) | 2 |
| Round-1 findings fully fixed | 20 of 20 (no regressions) |
| Open findings | 1 minor (R-2, documentation sync) |
| New regressions | 0 |

### 9.4 Findings

**R-2 (new, minor, lead-owned) — `docs/next-stage/README.md` still reports acceptance as pending
and the audit as round 3.**
- Location: `docs/next-stage/README.md:43–44`.
- Evidence: the status table reads “Independent completeness audit | round 3 complete …” and
  “Maintainer acceptance of vocabulary and stability boundary | **pending: the only open item**”,
  while `maintainer-acceptance.md` records `Status: accepted 2026-09-17` with all eight outcomes
  decided, and this round verifies the delta. The five synchronized artifacts contain zero
  `pending`/`awaiting` acceptance statements.
- Impact: the evidence set’s navigation index contradicts the authoritative acceptance record and
  the current audit round. No decision, contract, CSV, or record content is affected.
- Fix: update the two cells to “round 4 complete — verdict complete (§9)” and “accepted 2026-09-17
  (A-1…A-8; A-2=B)”. Out of scope for this audit round (the round-4 instruction limits edits to
  this file).

No other findings. Every delta item and every fail-closed check passes.

### 9.5 Verdict (round 4)

**Yes — Goal 14 is complete on the evidence and acceptance criteria.** The maintainer’s eight
decisions are recorded (A-1=A, A-2=B, A-3=A, A-4=A, A-5=A, A-6=A, A-7=A, A-8=A), the A-2 option-B
choice is propagated consistently through `maintainer-acceptance.md`, `decision-matrix.csv`,
`api-decisions.md`, `docs/product-contract.md`, and `decision-record.md`, and every recomputed count
matches: 269 CSV rows with member decisions keep 223 / redesign 46, distinct type decisions keep
31 / redesign 3, redesign members confined to `Result<TValue,TError>` (25),
`Validation<TValue,TError>` (18), `StateTransitionExtensions` (1), and `FunctionExtensions` (2), and
the 160-row decision summary (37/12/27/84) reproduces exactly. All standing fail-closed checks pass
with zero regressions: CSV↔JSON coverage is exact (34/34 types, 269/269 members, no
missing/extra/duplicate rows), the solution builds warning-free (0/0, 8 projects; forced
`--no-incremental` rebuild included) with both rebuilt assemblies hashing to the recorded pins, and
all four pinned `.nupkg` SHA256 values match `baselines.md`. The evidence set is complete and
internally consistent across the five synchronized artifacts; the only residual is **R-2**, the two
stale `README.md` status cells, a documentation sync for the lead that changes no decision or
evidence content. Once those two cells are updated (in the same commit that closes the goal), the
Goal 14 evidence set is complete, internally consistent, and accepted.
