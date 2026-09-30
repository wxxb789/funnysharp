# docs/goals/ — goal contracts

Earned: score ~16 — 1 active goal plus 23 archived immutable contracts; the product's completion-criteria authority.

## OVERVIEW

Active and archived goal contracts — the authoritative completion criteria every plan, audit, and acceptance cites.

## STRUCTURE

- Active: `0024-goal.md` — the independent fail-closed audit of Goals 14–23.
- `archive/`: `0001-goal.md` .. `0023-goal.md` — completed goals kept as immutable reference; `0013-goal.md` additionally carries audit status / product acceptance rules.

## FORMAT (frozen)

- Line 1: `/goal`.
- Line 2: one dense contract paragraph — the whole completion contract. Completion means the entire line-2 contract, not a favorable subset of it.
- `0013-goal.md` is the only multi-paragraph file (11 lines: audit status, acceptance rules).

## WHERE TO LOOK

| Need | Location |
|------|----------|
| A goal's full completion contract | line 2 of its `NNNN-goal.md` |
| Audit / acceptance rules for completed goals | `archive/0013-goal.md` |
| Active goal text | `0024-goal.md` |
| Which goal a plan or doc executes against | its `product_contract_source` / Authority line links back here |

## CONVENTIONS

- An active goal is complete only when every clause of its contract paragraph holds; the Goal 13 audit rules govern how completion is reviewed.
- Later goals may extend earlier ones deliberately; archived contracts are never contradicted or rewritten (0013 preserves 0001-0012 as immutable).
- New goals continue the `NNNN-goal.md` numbering; on completion a goal moves to `archive/` unchanged.

## ANTI-PATTERNS

- NEVER edit any file under `archive/` — plans, analyses, and audits cite these files as fixed reference.
- NEVER narrow a goal's contract to make it pass; an obsolete clause is a lead decision, not an edit here.
- Do not restate goal text in other documents — link the file (the whole-contract text is line 2).
