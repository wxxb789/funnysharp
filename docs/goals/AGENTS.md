# docs/goals/ — goal contracts

Active and proposed goal contracts plus 27 archived immutable contracts; the product's completion-criteria authority.

## OVERVIEW

Active and archived goal contracts — the authoritative completion criteria every plan, audit, and acceptance cites.

## STRUCTURE

- Completed and archived: `archive/0001-goal.md` .. `archive/0027-goal.md` — immutable completion contracts; `archive/0013-goal.md` additionally carries audit status / product acceptance rules.
- Next-stage: `0028-goal.md` .. `0042-goal.md` — product outcomes and bounded research outcomes; see [README.md](README.md) for the index and provenance. Presence here does not imply activation or completion.

## FORMAT (frozen)

- Line 1: `/goal`.
- Line 2: one dense contract paragraph — the whole completion contract. Completion means the entire line-2 contract, not a favorable subset of it.
- `0013-goal.md` is the only multi-paragraph file (11 lines: audit status, acceptance rules).

## WHERE TO LOOK

| Need | Location |
|------|----------|
| A goal's full completion contract | line 2 of its `NNNN-goal.md` |
| Audit / acceptance rules for completed goals | `archive/0013-goal.md` |
| Archived audit and resolution contracts | `archive/0024-goal.md`, `archive/0025-goal.md` |
| Archived decoder and contract outcomes | `archive/0026-goal.md`, `archive/0027-goal.md` |
| Completed audit/resolution/decoder/contract evidence | `../audits/goal-24-resolution/report.md`, `../audits/goal-24-resolution/final-evidence-index.md`, `../decoder-evidence.md`, `../contract-evidence.md` |
| Next-stage goal selection | [README.md](README.md), linking `0028-goal.md` .. `0042-goal.md` |
| Which goal a plan or doc executes against | its `product_contract_source` / Authority line links back here |

## CONVENTIONS

- An active goal is complete only when every clause of its contract paragraph holds; the Goal 13 audit rules govern how completion is reviewed.
- Later goals may extend earlier ones deliberately; archived contracts are never contradicted or rewritten (0013 preserves 0001-0012 as immutable).
- New goals continue the `NNNN-goal.md` numbering; on completion a goal moves to `archive/` unchanged.

## ANTI-PATTERNS

- NEVER edit any file under `archive/` — plans, analyses, and audits cite these files as fixed reference.
- NEVER narrow a goal's contract to make it pass; an obsolete clause is a lead decision, not an edit here.
- Do not restate goal text in other documents — link the file (the whole-contract text is line 2).
