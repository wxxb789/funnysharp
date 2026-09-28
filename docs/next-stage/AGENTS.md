# docs/next-stage/ — Goal 14 evidence set + follow-up goal evidence

Earned: score ~19 — 13 root artifacts + 4 subtrees; the capability-decision workflow hub. Goal 14 decided what FunnySharp keeps / redesigns / marks experimental / removes, and which external capabilities it adopts / adapts / defers / rejects. It implemented nothing.

## OVERVIEW

Goal 14 evidence set: pinned baselines, inventories, candidate analysis, final decision records, compiled call sites, fail-closed review, and maintainer acceptance.

## WORKFLOW (reading order, `README.md`)

`baselines.md` -> `inventory/` -> `analysis/` -> `decision-record.md` -> `api-decisions.md` (+ `decision-matrix.csv`) -> `call-sites.md` -> `review/verification.md` -> `maintainer-acceptance.md` -> `../product-contract.md`

## ROOT ARTIFACTS

| File | Role |
|------|------|
| `baselines.md` | version/commit pins, nupkg SHA256s, inventory generation method — provenance anchor |
| `decision-record.md` | FINAL external-capability decisions (adopt / adapt / defer / reject) |
| `api-decisions.md` | FINAL FunnySharp API matrix (keep / redesign / experimental / remove) |
| `decision-matrix.csv` | member-level companion of `api-decisions.md` — keep the two consistent |
| `call-sites.md` | compile-verified W1-W10 side-by-side call sites |
| `call-sites-goal-15/16/19.md` | follow-up goal gap workflows (Goal 15 UnitResult, Goal 16 function grammar, Goal 19 advanced patterns) |
| `maintainer-acceptance.md` / `maintainer-acceptance-goal-16.md` | maintainer acceptance records (vocabulary, stability boundary) |
| `advanced-pattern-curation.md` | maps AD-6..AD-9 and G6/G7/G8/G10/G17 decisions to delivered state |
| `ai-usability-goal-21.md` | Goal 21 evidence |
| `README.md` | reading order, status table, regeneration pointer |

## CONVENTIONS

- Decision vocabularies: external capabilities `adopt` / `adapt` / `defer` / `reject`; FunnySharp APIs `keep` / `redesign` / `experimental` / `remove`. Analysis memos only propose; final decisions live in `decision-record.md` / `api-decisions.md`.
- The Goal 14 record is a pinned baseline: later accepted goals ADD files (`*-goal-<N>.md`) without modifying it.
- Naming: lowercase-kebab; goal-suffixed siblings use `-goal-<N>`.
- No YAML front-matter anywhere; provenance is a title line plus an explicit pin paragraph (commit, version, SHA256, SDK/runtime).

## SUBTREES

- `analysis/` — candidate-decision memos (own AGENTS.md).
- `inventory/` — hand-written capability inventories + machine dumps (own AGENTS.md; regeneration command lives there).
- `call-sites-code/` — verbatim scratch projects behind `call-sites.md` (own AGENTS.md).
- `review/` — independent fail-closed audits; no own file (score <8). Rules that apply when appending a round: every doc opens with a fenced metadata block (goal, commit, hashes, reviewer, date); prose claims are never accepted — counts and hashes are recomputed; findings split Material / Moderate / Minor; unresolved items go under an explicit "Not Verified" section; rounds append to `verification.md` with "verdict superseded by §N" cross-references. Existing rounds are never rewritten.

## ANTI-PATTERNS

- NEVER modify a Goal 14 baseline artifact to "update" it — add a follow-up file instead.
- NEVER promote a memo's candidate decision into `decision-record.md` / `api-decisions.md` without lead/maintainer acceptance.
- Never cite an analysis memo as final authority; cite the decision record.
