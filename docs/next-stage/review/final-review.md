# Final Independent Review — Goal 14 (Adversarial, Fail-Closed)

- **Goal reviewed:** [`docs/goals/archive/0014-goal.md`](../../goals/archive/0014-goal.md)
- **Repository / branch:** `/home/azureuser/repos/funnysharp`, `goal-14/product-constitution`
- **Commit reviewed:** `0842850ae9b93bd2993c7475177d718e2e5a5230` (Goal 14 commit); survey pin
  `4dbebd94b7b58648632112b7ca47c39cc517f153`
- **Reviewer:** independent subagent with no involvement in Goal 14; read-only except this file
- **Date:** 2026-09-17
- **Rule:** fail-closed. Prose claims were not accepted; every count and hash below was
  recomputed from the files/binaries with the commands shown. The worktree was clean
  (`git status --short --branch` showed only the branch line).

## Method

- Rebuilt the shipping assemblies (`dotnet build FunnySharp.slnx -c Release`, SDK 10.0.400)
  and regenerated both FunnySharp inventories with `eng/next-stage-inventory`, diffing them
  byte-for-byte against the committed dumps.
- Wrote an independent reflection probe (in `/tmp/opencode/final-review/probe`, never in the
  repo) that enumerates exported types and public declared members of the built
  `FunnySharp.dll` and compares per-type counts against the generated JSON.
- Parsed `decision-matrix.csv` and compared it as a multiset against the JSON inventories
  (types, member kind, signature) in both directions.
- Parsed `decision-record.md` (escaped-pipe aware) for row continuity, decisions, criteria
  citations, and named-source existence; recounted the summary table against the rows.
- Verified pins/hashes independently: `sha256sum` over `/tmp/opencode/baselines/*.nupkg`,
  surviving pinned-build binaries, and NuGet flat-container version indexes.
- Re-ran all four side-by-side call-site builds in `/tmp/opencode/callsites/` and the four
  committed copies under `docs/next-stage/call-sites-code/` with
  `dotnet build -c Release --no-incremental`.
- Repository-wide scan for competitor references; `git show --name-only 0842850`.
- Re-inspected the round-1…4 audit in [`verification.md`](verification.md) and re-ran its
  fail-closed checks where they could be reproduced.

## Checklist

| # | Requirement (from the goal) | Verdict | Evidence (command → result) |
| --- | --- | --- | --- |
| 1 | Current FunnySharp public surface examined without silent omissions; generated inventory + full keep/redesign/experimental/remove matrix | **pass** | `inv-funny-sharp-core.md/.json` and `inv-funny-sharp-aspnetcore.md/.json` exist (title pins commit 4dbebd9). Regeneration from a fresh Release build: `dotnet run … eng/next-stage-inventory` → `diff` byte-identical for both `.md`; regenerated JSON `==` committed JSON. Independent probe → `types=33 probeMembers=254 mismatchedTypes=0`. JSON↔CSV set comparison → 34/34 types (33 core + 1 ASP.NET), 269/269 members, **0 missing, 0 extra, 0 duplicate rows** (only formatting delta: JSON `Undefined` vs CSV `Undefined = 0`). CSV decisions: keep 223 / redesign 46; distinct types keep 31 / redesign 3; every decision ∈ {keep, redesign} (a subset of the allowed vocabulary). Kinds: method 237, property 14, operator 8, enum-value 4, ctor 3, delegate 3. Note: `.json` dumps are intentionally untracked (`inventory/generated/.gitignore:1`), and `generated/README.md:10-13` says so. |
| 2 | .NET/BCL, FSharp.Core, Funcky, CFE, language-ext baselines pinned with hash + inventory + analysis memo | **pass** | `baselines.md` records pins; `sha256sum /tmp/opencode/baselines/*.nupkg` → 4/4 match exactly (`3a7c3d59…`, `1a7ab6c6…`, `cf1f4e69…`, `633636d9…`). Clones match: `git -C ~/repos/funcky rev-parse HEAD` = `133ba5ba…`; `git -C ~/repos/language-ext` = `2f0e3628…` (`v5.0.0-beta-77-11-g2f0e3628`). NuGet flat-container: `fsharp.core` lists `10.1.401` as the last 10.x stable and `11.0.100`; `language-ext` index → **404**; `languageext.core` lists `4.4.9` as the last stable with `5.0.0-alpha.*`/`5.0.0-beta-*` only. ≥15 API spot checks against the dumps: BCL `Task.WhenEach` (inv-bcl-async-concurrency.md:890), `Chunk` at exactly 1471/1668, `FrozenDictionary/FrozenSet`; FSharp.Core `FSharpOption<T>` = class (null-as-None) with `Value`/static `None`, `FSharpResult` = struct with unchecked `ResultValue`/`ErrorValue`, `FSharpValueOption` = struct; Funcky `Option.FromBoolean` ×3 (first returns `Option<Unit>`), `Unit` readonly struct, 221 `Parse*` signatures (~200 claim); CFE `Maybe` `op_Implicit(T? value)`/`op_Implicit(Maybe _)` (lines 660–661), four interfaces `IResult/IValue/IError/IUnitResult`, `ValueObject/Entity/SimpleValueObject/EnumValueObject`; language-ext `Prelude` = 2284 members, `Option<A>` = 89 members/12 operators, `Either`/`Fin`/`Validation` = 41/29/14 operators, 49 global-namespace extension classes, `OptionAsync` exposes `Task<bool> IsSome`. Analysis memos exist for all five baselines plus FunnySharp. |
| 3 | Every relevant external capability adopt/adapt/defer/reject with rationale citing C1–C8 or a named source; memo decision tables represented | **pass** | `decision-record.md` parser → exactly **160 rows E1–E160**, no gaps, no duplicates; every row's decision begins with adopt/adapt/defer/reject (compound rows E13, E18, E23, E32, E60, E75, E81, E93, E109, E138 counted by first-listed decision); 160/160 cite at least one of C1–C8 and at least one named source. Summary table recounted: 12/12 cells exact (F1 6/3/7/8 … total 37/12/27/84). Memo representation: all CFE/FS/FU/LE decision-row IDs are either cited in the record or mapped by the 26-row `Memo reconciliation` table; all 26 mappings are keyword-consistent with their final E rows; BCL memo carries coverage verdicts only ("nothing here is a decision", `baseline-bcl.md:30`); FunnySharp memo's 34 type rows + G1–G17 map to AD-1…AD-10 and api-decisions G1–G18, with the one divergence (`ConcurrentEffectExtensions` "redesign (return type)") superseded by the recorded A-2=B. Three+ sampled mappings accurate: FU F2.4 defer→E151 reject, LE 11.4 `Unit` defer→E98 reject, FS #22 defer→E145 defer, CFE 9.1 reject→E158 reject. |
| 4 | Decisions based on the eight criteria | **pass** | C1–C8 defined in `api-decisions.md:9–20`. All 160 record rows cite criteria and existing sources (checked programmatically; the only regex false positive was `CFE 3.7.0` as a version). 11-row sample across F1–F11 verified: E1/E6/E16 (F1, adopt), E27 (F2, reject), E38 (F3, reject), E47 (F4, reject), E69 (F5, adopt), E82 (F6, adopt constrained), E98 (F7, reject), E117 (F9, adopt present), E127 (F11, adapt) — every stated criterion and every cited memo row/section exists. The CSV itself does not carry a criteria column (criterion attribution lives in AD prose and the record), which is consistent with its documented schema. |
| 5 | One canonical vocabulary and stability boundary accepted by the maintainer; documents agree on A-2 and Unit | **pass** | `docs/product-contract.md` has `## Canonical Vocabulary` (line 10) and `## Stability Boundary` (line 42), and states the four carriers, one-verb-per-meaning, no second carrier. `maintainer-acceptance.md` → `Status: accepted 2026-09-17`, 8/8 `Outcome:` lines, A-2 = "accepted with change — option B" with rationale "keep it simple; `TError` can be nested". Cross-document A-2 agreement: maintainer-acceptance §A-2, `api-decisions.md` AD-6 ("Keep (maintainer decision A-2=B)"), `product-contract.md:119–128` ("return shape stays `Validation<TValue, TError>`: a winner is `Valid` … `Invalid` … in input order"), `decision-record.md` E81, CSV `ConcurrentEffectExtensions` keep + 3 `FirstSuccessAsync` rows keep. The A-2 documentation obligation is real: race contract present in `src/FunnySharp/ConcurrentEffectExtensions.cs` XML docs and `docs/concurrency.md` §"First Successful Effect". Unit: rejected, not deferred (`product-contract.md:209–211`; E98 reject; G17 reject; A-4=A). |
| 6 | Unresolved qualitative judgments remain explicitly unverified; no pending acceptance | **pass** (one stale-list inconsistency, F-1) | `grep -ro UNVERIFIED docs/next-stage docs/product-contract.md` → **88** occurrences (77 excluding the audit file), plus the `decision-record.md` and `api-decisions.md` "Unverified judgments" lists. `grep -nE 'pending\|awaiting' maintainer-acceptance.md` → none. Two samples: (a) `baseline-csharpfunctionalextensions.md:166` — criterion-2 semantic-LOC counting unit is an analyst convention, reproducible under the `call-sites.md §2` rule; (b) `funny-sharp-surface.md:148` — "the maintainer may prefer to keep the accumulation shape; the qualitative preference is not derivable from evidence", later decided by A-2=B. Both are genuine judgment calls, not hidden factual gaps. |
| 7 | Independence: no competitor dependency outside the isolated evidence project; no carrier conversion / compatibility / migration surface | **pass** | Repository-wide project scan (`*.csproj/*.props/*.targets/*.slnx`) → competitor `PackageReference`s exist only in `docs/next-stage/call-sites-code/competitors/Competitors.csproj` (CFE 3.7.0, Funcky 3.6.0, LanguageExt.Core 4.4.9; `IsPackable=false`); zero in `src/`, `tests/`, `examples/`, `benchmarks/`. No competitor `using`/qualified-name usage in shipping/test/demo code. `FunnySharp.slnx` lists only the 8 shipping/test/sample projects and does not reference the competitors project. `product-contract.md:184–187` states the isolation rule; `:36` forbids carrier conversion. No conversion/compatibility/migration API in `src/`. Core package has zero `PackageReference`; ASP.NET Core package uses only a `FrameworkReference` + `ProjectReference`. |
| 8 | Rejections preserved: custom runtime, scheduler, replacement collection universe, pervasive immutability, HKT/monad-transformer hierarchy, premature general DU | **pass** | `product-contract.md:37–40` (scope rejections), `:180–183` (custom scheduler/fiber/HKT), `:193–204` (premature general unions, union compatibility layer), `:105–107` (default contracts), plus `decision-record.md` E64/E65/E99/E110/E116/E132/E133 and `api-decisions.md` G3/G8/G16/G17. All six rejections remain in force in the accepted constitution. |
| 9 | Evidence types: pinned inventories, representative side-by-side call sites, recorded rationales, complete decision matrix | **pass** | Pinned inventories: 4/4 nupkg SHA256 verified (above); DLL-API dump regeneration byte-identical. Call sites: `dotnet build -c Release --no-incremental` in `/tmp/opencode/callsites/{idiomatic,funnysharp,funnysharp-aspnet,competitors}` → **4/4 exit 0, 0 warnings / 0 errors**; same command in the four committed copies → **4/4 exit 0, 0/0**; repository solution → 8 projects, 0/0. Rationales: 160 record rows + 10 AD sections + 6 analysis memos, every cited source exists. Complete matrix: 269 rows, exact match to JSON, zero missing/extra/duplicate. |
| 10 | Not an implementation task | **pass** | `git show --name-only 0842850` → 75 files: 71 under `docs/`, 4 under `eng/next-stage-inventory/`; **0** files under `src/`, `tests/`, `examples/`, `benchmarks/`. The API/commodity changes are recorded decisions, not implementations. |
| 11 | Goal-state claim: the round-1…4 audit verdicts in `review/verification.md` are supported by the artifacts | **pass with two missed minor items** | Re-verified the audited facts: 269/269 CSV coverage, keep 223/redesign 46, types 31/3, 160 rows 37/12/27/84, `§"…"` citations = 60 over 6 real `product-contract.md` headings with 0 numeric suffixes, 0 stale pre-delta counts, README dump-count rows match the JSON (all four language-ext rows included), `Chunk` at 1471/1668, `Competitors.csproj IsPackable=false`, README R-2 cells now read round 4/verdict pass and accepted 2026-09-17. The audit did not catch F-1 and F-2 below. Its DLL-hash claim (2/2 match) is true only at the audited pin state (see O-2). |

## Findings (ordered by severity)

**F-1 (minor — stale "Unverified judgments" list; documentation contradiction).**
`decision-record.md` "Unverified judgments in this record" still says
"- Maintainer acceptance of the vocabulary and stability boundary (`api-decisions.md`)." and
"- Retry policy choice (E102): … the maintainer arbitrates." Both are resolved:
`maintainer-acceptance.md:8` records acceptance on 2026-09-17 with A-1=A, A-2=B, A-3=A, A-4=A,
A-5=A, A-6=A, A-7=A (retry = defer), A-8=A, and `api-decisions.md:330` records the same.
Minimal fix: replace the first bullet with the recorded acceptance, and reword the second to
state that A-7 accepted the deferral (drop "the maintainer arbitrates").

**F-2 (minor — wrong cross-reference).**
`api-decisions.md:209` says "a general retry layer stays out of the stable surface for this
stage (see the decision record, E58)". The retry decision is `E102` (`decision-record.md:185`);
E58 is the unrelated `Split`/`Intersperse`/… row (`:123`). Minimal fix: change `E58` to `E102`.

Neither finding changes a decision, the CSV, the contract text, or the acceptance record.

### Observations (not defects)

- **O-1 — tool description vs implementation.** `eng/next-stage-inventory/README.md:6` says the
  tool lists "the public and protected surface", but `Program.cs:235` uses
  `BindingFlags.Public` only. Measured impact: both FunnySharp assemblies declare **0**
  protected members, so the Goal 14 matrix is unaffected; baseline dumps do omit e.g. CFE's
  protected abstract `GetEqualityComponents` (`inv-csharpfunctionalextensions.md:1401–1416`),
  which the CFE memo covers from source instead. Fix only the wording (or add protected
  emission for family-level completeness).
- **O-2 — DLL SHA256 pins are commit- and path-specific.** The recorded
  `FunnySharp.dll = 793c8532…` matches the surviving pinned-build copies at
  `/tmp/opencode/callsites/{funnysharp,funnysharp-aspnet}/bin/Release/net10.0/FunnySharp.dll`
  (and `/tmp/opencode/optprobe/bin/…`), i.e. it is a genuine build of commit 4dbebd9. A rebuild
  at HEAD `0842850` in place yields `b8caf08b…`, and a clone of `4dbebd9` under `/tmp` yields
  `e88f3720…`: `AssemblyInformationalVersion` embeds `0.1.0+<commit-sha>` and the PDB path is
  embedded, so `Deterministic=true` still produces different bytes. `baselines.md` does not
  state this condition. The substantive surface check passes regardless (regenerated dump is
  byte-identical). Minimal fix: note the exact build state required to reproduce the hash, or
  record the informational version next to the hash.
- **O-3 — untracked JSON.** The `.json` inventories exist in the worktree and were used for the
  matrix comparison, but are gitignored by design; a fresh clone cannot independently repeat the
  CSV↔JSON check without re-running `eng/next-stage-inventory/generate.sh`. This is documented
  (`inventory/generated/README.md:10–13`), so it is a reproducibility limitation rather than a
  missing artifact.

## Could Not Verify

- The **completeness of the external capability survey beyond the reconciled memo/record set**:
  I did not re-derive the capability list from every package (the round-1 audit says the same).
  Coverage of the six analysis memos against the record is exact, but a capability absent from
  all memos would not have been detected.
- **Maintainer acceptance as an external fact**: only the recorded attestation
  (`maintainer-acceptance.md`) exists; there is no independent signature or session receipt.
- **Competitor runtime behavior and performance**: no competitor code was executed; this review
  inherited the memos' own UNVERIFIED scope (no benchmarks run).
- **Trim/AOT and OpenAPI claims**: historical or forward-looking, not re-run here.
- **Semantic-LOC counts**: not recomputed (analyst counts; the counting rule is documented).
- **`inv-*` JSON dumps in a fresh clone**: absent by design; verified only in this worktree plus
  fresh regeneration equality.
- **Original bytes of the pinned assemblies at an arbitrary path**: no build at another path
  reproduces the recorded SHA256 (see O-2); I verified the surviving pinned-build copies instead.

## Verdict

**Goal 14 completed and correct.** All ten goal requirements pass with independently reproduced
evidence: the 269-member matrix covers the complete 33+1-type public surface with zero
missing/extra/duplicate rows (confirmed by an independent reflection probe), all 160 external
capability rows are decided and sourced with a summary that recounts exactly, all five baselines
are version/hash-pinned with matching nupkg hashes and verified API claims, the vocabulary and
stability boundary are recorded as accepted with A-2=B propagated consistently (and the race
contract actually documented in source and guide), Unit is rejected rather than deferred,
independence and the six scope rejections hold, and the Goal 14 commit touches only `docs/` and
the evidence tool. The two minor defects found (F-1 stale acceptance bullet in `decision-record.md`,
F-2 wrong `E58`→`E102` cross-reference in `api-decisions.md`) are documentation defects outside
every decision, matrix, and acceptance statement; they should be fixed, but they do not make the
goal incomplete or incorrect.
