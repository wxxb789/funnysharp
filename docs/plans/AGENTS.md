# docs/plans/ — ce-unified-plan/v1 design-time plans

Earned: score ~9 (distinct domain) — 16 plans, ~7,000 lines, all carrying the rigid `ce-unified-plan/v1` artifact contract.

## OVERVIEW

Design-time plans for the Option abstraction, release-acceptance restoration, uv tooling, the Goal 17 collection surface, Goal 26 decoding, the Goal 33-35 sequence features, Goal 36 nested reshaping, Goal 37 domain programs, Goal 38 bounded per-key concurrency, Goal 39 compile-time fusion, Goal 40 incremental computation, Goal 41 tree transformations, and Goal 42 typed operation plans — how shipped features and research outcomes were planned and verified.

## WHERE TO LOOK

| Plan | Scope |
|------|-------|
| `2026-08-31-1518-feat-focused-option-abstraction-plan.md` | `Option<T>` abstraction: factories, composition, bridges, tests, benchmarks |
| `2026-09-03-2223-fix-release-acceptance-plan.md` | cancellation-defect, performance-evidence, and release-gate restoration |
| `2026-09-18-1508-feat-cross-platform-uv-tooling-plan.md` | Python+uv tooling layer beside the frozen PowerShell release protocol |
| `2026-09-20-1540-feat-collection-safety-plan.md` | Goal 17 collection surface: `*OrNone` family, `NonEmpty<T>`, `Partition`, `ZipExact` |
| `2026-10-30-1156-feat-composable-decoding-plan.md` | Goal 26 decoder surface, error model, three-way consumer comparison, and measurements |
| `2026-11-07-0519-feat-fallible-reduction-plan.md` | Goal 33 seeded fallible fold, source short-circuit semantics, async-source path, reference comparison, and measurements |
| `2026-11-07-1807-feat-one-pass-summaries-plan.md` | Goal 34 reusable count/sum/extrema/domain summaries, `Zip`/`MapResult`, one-pass and async evaluation, MapAccum non-adoption, reference comparison, and measurements |
| `2026-11-08-0144-feat-strict-cardinality-joins-plan.md` | Goal 35 strict-cardinality keyed lookups and exactly-one joins: many and unique indexes, complete-input versus matched-key uniqueness, buffered and streaming shapes, structured positions, comparison, and measurements |
| `2026-11-08-1345-feat-nested-data-reshaping-plan.md` | Goal 36 typed nested-data expansion: single, aligned, and explicit Cartesian shapes, null/empty/unequal/rejection policies, by-value provenance, sync/async sources, comparison, and measurements |
| `2026-11-08-1544-feat-domain-programs-simulation-plan.md` | Goal 37 composable domain programs: pure `Update`, parent-aware `Lift`, `OrElse`/`Replay`, explicit-time deterministic simulation, late/duplicate/stale/rejected/failure scenarios, direct-switch comparison, and measurements |
| `2026-11-08-1716-feat-bounded-concurrency-ordering-plan.md` | Goal 38 bounded per-key concurrency: API shape, key equality, null keys, delivery order, hot-key fairness, explicit admission/buffer bounds, state release, cleanup semantics, competent BCL comparison, and measurements |
| `2026-11-08-1201-feat-compile-time-fused-execution-plan.md` | Goal 39 compile-time fused execution research: bounded summary/decoder subset, static field recognition, diagnostics, semantic preservation, comparison, generation/build cost, and ADAPT summary-only decision |
| `2026-11-08-1428-feat-incremental-domain-computation-plan.md` | Goal 40 incremental domain computation research: static graph prototype, staged publication, purity/cutoff/error/observer semantics, node-versus-collection treatment, shared pricing workload, full/hand/graph/cache comparison, and ADAPT static-subset decision |
| `2026-11-08-1535-feat-reusable-tree-transformations-plan.md` | Goal 41 reusable tree transformation research: typed configuration workload, adapter, iterative traversal, bottom-up and path rewrites, zipper, recursion/iterative/generic/`JsonNode` comparison, tree/DAG/cycle/mutability semantics, depth and allocation evidence |
| `2026-11-09-0528-feat-typed-operation-plans-plan.md` | Goal 42 typed operation plan research: SQLite order-placement workload, typed plan with named/dependency metadata, direct `Result`/`await` and EF Core comparisons, cancellation/callback/partial/commit semantics, external-effect boundary, two measurement runs |

## CONVENTIONS (ce-unified-plan/v1 — every new plan follows it)

- Filename: `YYYY-MM-DD-HHMM-<type>-<slug>-plan.md` (`type`: feat or fix; slug lowercase-hyphen).
- YAML front matter required: `title`, `type`, `date`, `artifact_contract: ce-unified-plan/v1`, `product_contract_source`, `execution: code` (plus `deepened`, `artifact_readiness` when applicable).
- Body sections, in order: Goal Capsule, Product Contract, Planning Contract, Implementation Units, Verification Contract, Definition of Done. Short plans may omit Units/DoD when signatures are frozen tables.
- Goal Capsule keys: Objective / Means / Authority / Execution profile / Tail ownership / Stop conditions ("Stop for a new decision if ...").
- Units: `### U<n>. <Imperative title>` with Goal / Requirements / Dependencies / Files / Approach / Execution note / Patterns to follow / Test scenarios / Verification. Dependencies cite unit IDs; execution order is stated when it differs from numeric order.
- IDs: requirements `R1..`, acceptance examples `AE1..`, key flows `F1..`, key technical decisions `KTD1..` (each "Governs R...").
- Accepted decisions marked `(session-settled: user-approved/user-directed ...)`. Scope markers: `**Included**` / `**Deferred to Follow-Up Work**` / `**Outside this product's identity**`.
- Verification Contract is a table (Gate / Command / Applies to / Completion evidence); Definition of Done splits per-unit completion (table) and global completion (bullets).
- Traceability: units cite which `R`/`AE`/`KTD` they satisfy; test scenarios carry tags like `(happy path)`, `(edge)`, `(error path)`, `(integration)`.
- Plans execute against the immutable contracts in `docs/goals/archive/` and the product boundaries in `docs/product-contract.md`; both are linked in `product_contract_source`.

## ANTI-PATTERNS

- Nothing under `docs/plans` is imported, built, or referenced by source — do not add code here.
- NEVER loosen the contract (drop front matter, reorder sections) when adding a plan.
- Deferred work goes to the plan's follow-up section / `TODO.md` — never silently into scope.
