# FunnySharp Next-Stage Product Constitution And Capability Decision Record

This directory is the Goal 14 evidence set. Goal 14 is a product and scope decision
goal: it decides what FunnySharp keeps, redesigns, marks experimental, or removes,
and which external capabilities it adopts, adapts, defers, or rejects. It does not
implement features.

## Reading order

1. [`baselines.md`](baselines.md) — version- and commit-pinned survey inputs, hashes,
   inventory generation method, and limitations.
2. [`inventory/`](inventory/) — human-readable capability inventories per baseline and
   for FunnySharp itself; [`inventory/generated/`](inventory/generated/) holds the
   machine-generated public API dumps.
3. [`analysis/`](analysis/) — per-workstream analysis memos with candidate decisions
   and open questions.
4. [`decision-record.md`](decision-record.md) — external capability decisions
   (`adopt` / `adapt` / `defer` / `reject`) with rationale and evidence.
5. [`api-decisions.md`](api-decisions.md) — the FunnySharp API matrix
   (`keep` / `redesign` / `experimental` / `remove`), with the complete member-level
   matrix in [`decision-matrix.csv`](decision-matrix.csv).
6. [`call-sites.md`](call-sites.md) — compile-verified representative side-by-side
   call sites.
7. [`review/verification.md`](review/verification.md) — independent completeness and
   fidelity audit, including every judgment left explicitly unverified.
8. [`maintainer-acceptance.md`](maintainer-acceptance.md) — the acceptance record for the
   canonical vocabulary and stability boundary (Goal 14 requirement).
9. [`../product-contract.md`](../product-contract.md) — the next-stage product
   constitution (canonical vocabulary, stability boundary, scope rejections).

## Status

| Artifact | Status |
| --- | --- |
| Baseline pins, hashes, generation method | complete |
| Generated inventories | complete (operator-bearing regeneration) |
| FunnySharp surface inventory + analysis | complete |
| Baseline analyses (CFE, Funcky, FSharp.Core, language-ext, BCL) | complete |
| Side-by-side call sites (`call-sites.md` + `call-sites-code/`) | complete (compiled, 0 warnings) |
| Standing-constraints audit | complete |
| Decision record + API matrix | complete (`decision-record.md`, `api-decisions.md`, `decision-matrix.csv`) |
| Product-contract revision | complete (`../product-contract.md`) |
| Independent completeness audit | round 4 complete — all findings resolved, verdict pass (see `review/verification.md` §9) |
| Maintainer acceptance of vocabulary and stability boundary | **accepted 2026-09-17** (A-1..A-8; A-2=B) — see `maintainer-acceptance.md` |

## Regeneration

```bash
uv run --no-project eng/tools/inventory.py \
  <baseline-root> \
  "$HOME/.dotnet/packs/Microsoft.NETCore.App.Ref/10.0.11/ref/net10.0" \
  "$HOME/next-stage-evidence"
```

See [`baselines.md`](baselines.md) for pins and the exact filters used for each dump.
