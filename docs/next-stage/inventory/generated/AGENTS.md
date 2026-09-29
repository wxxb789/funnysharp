# docs/next-stage/inventory/generated/ — machine-generated API dumps

Earned: score ~16 (48 files, file-count weight) — this file exists mainly as a guard: everything here is MACHINE-GENERATED evidence.

## OVERVIEW

Machine-generated public-API dumps of FunnySharp, the .NET 10 BCL, and the pinned competitor baselines — verbatim tool output, never hand-edited.

## WHAT THIS IS

- Verbatim public-API dumps produced by the inventory toolchain:
  `dotnet fsi build.fsx -- -p generate-inventory <baseline-root> <ref-pack-dir> <output-dir>`
  (pins and per-dump filters in `../../baselines.md`; method summary in `../../README.md` "Regeneration").
- Sources: FunnySharp core + AspNetCore, .NET 10 BCL (several filtered slices), CSharpFunctionalExtensions 3.7.0, Funcky 3.6.0 (+ its analyzers), FSharp.Core 10.1.401, LanguageExt.Core 4.4.9 (+ the unreleased v5 line and its type lists).

## NAMING

- `inv-<source>[-<focus>].md` — the tracked dump, readable form (e.g. `inv-funny-sharp-core.md`, `inv-bcl-async-concurrency.md`, `inv-fsharp-core-focus.md`).
- `inv-<source>[-<focus>].json` — the same dump in machine-readable form; NOT tracked (`.gitignore` excludes `*.json`), created only by regenerating locally (`dotnet fsi build.fsx -- -p generate-inventory <baseline-root> <ref-pack-dir> <output-dir>`).
- BCL comes as filtered slices (async-concurrency, collections-immutable, language-errors, sequences-linq, sse, supplements), not one dump.
- `types-languageext-4.4.9.txt` — raw type list; `README.md` — dump manifest.

## WHERE TO LOOK

- Prefer `.md` for reading and for scripted comparison — the `.json` counterpart exists only after local regeneration.
- Cite a dump the way the analysis memos do — by short alias and section (`inv:§12.1`, `csharp-out:44-46`) plus the pin from `../../baselines.md`.
- Verify a dump is current by comparing its SHA256 against the one recorded in the parent inventory or `../../baselines.md`.

## CONVENTIONS

- NEVER hand-edit, reformat, or trim anything here; any change invalidates the SHA256 pins recorded in `../<inventory>.md` and `../../baselines.md` and the decisions that cite them.
- Do not read these wholesale (~6.5 MB tracked). Use the parent hand-written inventories as the index; open a dump only when a decision cites it.
- Regeneration overwrites `inv-*` files; a new target comes from the tool's target table, never from a manual copy.
- A FunnySharp dump's bytes reproduce only for a Release build of the pinned commit (informational version embeds the commit SHA).

## ANTI-PATTERNS

- NEVER regenerate without re-checking output against the pins in `../../baselines.md`.
- Never treat a competitor dump as compatibility guidance — these are capability-survey evidence for Goal 14 decisions only.
