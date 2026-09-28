# docs/plans/ — ce-unified-plan/v1 design-time plans

Earned: score ~9 (distinct domain) — 4 plans, ~1,760 lines, all carrying the rigid `ce-unified-plan/v1` artifact contract.

## OVERVIEW

Design-time plans for the Option abstraction, release-acceptance restoration, uv tooling, and the Goal 17 collection surface — how shipped features were planned and verified.

## WHERE TO LOOK

| Plan | Scope |
|------|-------|
| `2026-08-31-1518-feat-focused-option-abstraction-plan.md` | `Option<T>` abstraction: factories, composition, bridges, tests, benchmarks |
| `2026-09-03-2223-fix-release-acceptance-plan.md` | cancellation-defect, performance-evidence, and release-gate restoration |
| `2026-09-18-1508-feat-cross-platform-uv-tooling-plan.md` | Python+uv tooling layer beside the frozen PowerShell release protocol |
| `2026-09-20-1540-feat-collection-safety-plan.md` | Goal 17 collection surface: `*OrNone` family, `NonEmpty<T>`, `Partition`, `ZipExact` |

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
