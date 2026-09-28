# docs/next-stage/analysis/ — candidate-decision analysis memos

Earned: score ~14 (distinct stage domain) — 7 memos, ~3,100 lines; the judging stage between the inventories and the decision record.

## OVERVIEW

Per-workstream memos judging pinned baselines and the FunnySharp surface against API families F1-F11 and criteria 1-8; candidate decisions only, never final.

## FILES

| Memo | Judges | Feeds |
|------|--------|-------|
| `funny-sharp-surface.md` (735 lines, keystone) | FunnySharp's own 33 types / 254 members at the pinned commit | `../api-decisions.md` + `../decision-matrix.csv`; exports gap list G1-G17 |
| `baseline-bcl.md` | .NET 10 BCL coverage per family F1-F11 | duplicate-risk vs additive-value verdicts |
| `baseline-csharpfunctionalextensions.md` | CSharpFunctionalExtensions 3.7.0 | must-not-inherit list |
| `baseline-fsharp-core.md` | FSharp.Core 10.1.401 | default/uninitialized-state + cancellation evidence |
| `baseline-funcky.md` | Funcky 3.6.0 | largest decision table; naming prior art (Goal 21 analyzers) |
| `baseline-language-ext.md` | LanguageExt.Core 4.4.9 + unreleased v5 line | recommends no structural adoption |
| `standing-constraints.md` | the constraints binding the next-stage constitution | contradictions C1.. requiring lead resolution |

## CONVENTIONS

- Naming: `baseline-<source>.md` for external baselines; `funny-sharp-surface.md` for the self-surface; `standing-constraints.md` for the constraint audit.
- Memo shape: title line tagged `(Goal 14)`, scope, evidence base, decision-vocabulary definition, per-family sections (F1-F11 / criteria 1-8), a "Not examined" section, and an "Open questions"/UNVERIFIED section.
- Provenance = explicit pin paragraph (commit, package version, nupkg SHA256, SDK/runtime) — never YAML front-matter.
- Evidence cited inline as `path:line` (`src/FunnySharp/Result.cs:57`, `docs/result.md:11-13`, `inv:§12.1`).
- Every memo is explicitly non-final: decisions here are proposals; `adopt/adapt/defer/reject` and `keep/redesign/experimental/remove` belong to `../decision-record.md` and `../api-decisions.md`.
- Unverifiable judgments are flagged **UNVERIFIED** in place — the honesty contract that `../review/verification.md` audits.
- Baseline memos carry a "Must NOT inherit" column/section; that output is first-class, not commentary.

## ANTI-PATTERNS

- NEVER edit a memo to "fix" its decision — memos are pinned evidence; supersede via the decision record or a new memo.
- NEVER cite a memo as final authority.
- Do not add a memo without the "Not examined" and UNVERIFIED/open-questions sections.

## NOTES

- No build/test runs inside the memos; corroborating dumps live in `../inventory/generated/`; regeneration command in `../README.md` and `../inventory/AGENTS.md`.
- `baseline-language-ext.md` notes its supplementary dump rows are one generation stale.
