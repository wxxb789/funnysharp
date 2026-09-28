# docs/ — FunnySharp documentation tree

Earned: score ~22 — 19 hand-written guides/contracts plus 4 doc-workflow subtrees. Everything here is hand-written prose except `next-stage/inventory/generated/` (machine dumps).

## OVERVIEW

Hand-written guides and contracts for the FunnySharp public surface — usage, grammar, performance, stability — plus the goal, planning, and capability-decision evidence trees.

## STRUCTURE

```
docs/
├── *.md                 # hand-written guides + contracts (lowercase-hyphen, one topic per file)
├── goals/               # active goal contracts (0022-0024) + immutable archive (0001-0021)
├── next-stage/          # Goal 14 capability-decision evidence set (own AGENTS.md)
├── plans/               # ce-unified-plan/v1 design-time plans (own AGENTS.md)
└── release-evidence/    # historical release-run records — never edited
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Feature usage: Option / Result / UnitResult / Validation | `option.md`, `result.md`, `unit-result.md`, `validation.md` |
| Feature usage: collections / concurrency / effects / pipelines | `collections.md`, `concurrency.md`, `effects.md`, `data-pipelines.md` |
| Function grammar, state machines, immutable updates | `function-composition.md`, `state-machines.md`, `immutable-updates.md` |
| Canonical vocabulary and naming rules | `grammar.md` |
| Product boundaries (authoritative contract) | `product-contract.md` |
| Analyzer diagnostics FS1001-FS1005 | `analyzers.md` |
| ASP.NET Core integration | `aspnet-core.md` |
| Goal 22 slice comparison and evidence | `vertical-slice-call-sites.md`, `vertical-slice-evidence.md` |
| Performance policy + evidence | `performance.md` |
| Experimental-member registry | `stability-inventory.md` |
| Evergreen fail-closed release checklist | `release-readiness.md` |
| Contributor tooling (uv/Python layer) | `tooling.md` |
| Goal contracts | `goals/AGENTS.md` |
| Capability decisions + evidence workflow | `next-stage/AGENTS.md` |
| How shipped features were planned | `plans/AGENTS.md` |

## CONVENTIONS

- Guides are prose with compiling examples; the intro links the example source (`examples/FunnySharp.Examples/Program.cs` or `examples/FunnySharp.AspNetCore.Examples`).
- Body shape: `## API Shape` first, semantic sections, closing `## Deliberate Boundaries` / `## Deliberate Exclusions` stating what the API intentionally does NOT do.
- Snippet contract — the 10 primary guides (`analyzers`, `aspnet-core`, `collections`, `concurrency`, `effects`, `function-composition`, `immutable-updates`, `state-machines`, `unit-result`, `validation`): each `<!-- documentation-sample: DocumentationSamples.<Area>.<Name> -->` fenced `csharp` block must byte-match the `// <snippet ...>` region in `examples/FunnySharp.DocumentationSamples`. Change both sides together; the verifier fails on any line mismatch, missing, duplicate, or unused region.
- Non-snippet guides (e.g. `option.md`, `result.md`, `grammar.md`) link compiling examples without the byte-match contract.
- `performance.md` consolidates policy; per-topic measurement tables are generated from benchmark receipts and verified against `eng/performance/baseline.json` budgets. Allocation is blocking evidence; timing is directional.
- `stability-inventory.md` records every `[Experimental("FS####")]` member and its diagnostic ID; stable members change only through a later accepted goal.

## ANTI-PATTERNS

- NEVER hand-edit `next-stage/inventory/generated/` (machine dumps), anything under `goals/archive/`, or `release-evidence/` (immutable/historical).
- No guide ships code that does not compile — examples are built and dogfooded against the shipped analyzers.
- Do not introduce a second carrier name or a verb meaning that contradicts `product-contract.md` / `grammar.md`; those two files are the authority.

## COMMANDS

```bash
# docs snippet verification (PowerShell is release-authoritative)
pwsh -NoProfile -File examples/FunnySharp.DocumentationSamples/VerifyDocumentationSnippets.ps1
# behavior-equivalent Python port
uv run --no-project eng/tools/verify_docs_snippets.py
# performance documentation verification
pwsh -NoProfile -File eng/Generate-PerformanceDocumentation.ps1 -Verify
```

## NOTES

- `next-stage/review/` and `release-evidence/` scored below the AGENTS.md threshold (<8) and have no own file. `next-stage/review/` (fail-closed audit rounds) is covered in `next-stage/AGENTS.md`. `release-evidence/goal-12.md` is a single historical record — read-only, explicitly non-normative; current acceptance criteria live in `release-readiness.md`.
