# docs/next-stage/inventory/ — capability inventories

Earned: score ~17 — 6 hand-written inventories plus the `generated/` dump tree; the evidence base every analysis memo and decision cites.

## OVERVIEW

Capability inventories per baseline: hand-written F1-F11 surveys of the BCL, competitors, and FunnySharp itself, plus the machine-generated public-API dumps they summarize.

## SPLIT

- Hand-written (this directory): `bcl.md`, `csharpfunctionalextensions.md`, `fsharp-core.md`, `funcky.md`, `funny-sharp.md`, `language-ext.md` — curated, readable capability inventories organized by API families F1-F11, one per baseline plus FunnySharp itself.
- Machine-generated (`generated/`, own AGENTS.md): verbatim `inv-*.{md,json}` public-API dumps — never hand-edited.

## CONVENTIONS

- Each hand-written inventory pins its evidence: source commit or package version, plus the SHA256 of every machine dump it summarizes (`funny-sharp.md` pins `generated/inv-funny-sharp-core.md/.json`, etc.).
- `funny-sharp.md` covers F1-F11 for the FunnySharp surface at the pinned commit (`4dbebd9`, version 0.1.0). FunnySharp assembly hashes are byte-reproducible only for a Release build of that exact commit.
- Family labels F1-F11 match `../analysis/` memos and the decision record — keep the vocabulary aligned.

## REGENERATION

```bash
uv run --no-project eng/tools/inventory.py \
  <baseline-root> \
  "$HOME/.dotnet/packs/Microsoft.NETCore.App.Ref/10.0.11/ref/net10.0" \
  "$HOME/next-stage-evidence"
```

Pins, per-dump filters, and limitations: `../baselines.md`. The tool drives the C# dumper (`eng/next-stage-inventory/`) over its target table and exits non-zero on any target failure — partial evidence is never reported as success. Generated output is copied verbatim into `generated/`.

## ANTI-PATTERNS

- NEVER edit `generated/` by hand — regenerate.
- NEVER cite a dump without its pin (version/commit + SHA256 from `../baselines.md`).
- Do not renumber or rename the F1-F11 family labels; downstream documents cite them.
