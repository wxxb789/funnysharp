---
title: Reusable tree transformations feasibility
type: feat
date: 2026-11-08
artifact_contract: ce-unified-plan/v1
product_contract_source: docs/goals/archive/0041-goal.md, docs/product-contract.md, docs/grammar.md, docs/core-design.md
execution: code
---

# Reusable Tree Transformations Plan

## Goal Capsule

- **Objective:** Decide whether reusable tree traversal, rewriting, or zipper-style editing gives
  FunnySharp consumers a meaningful improvement over direct recursion, iterative visitors with an
  explicit stack, and existing .NET tree APIs for a real configuration task. Completion is a
  maintainer-ready ADOPT / ADAPT / DEFER / REJECT decision backed by an executable consumer and
  equivalent reference behavior, identifying the smallest useful capability rather than a
  recursion-scheme, optics hierarchy, or general AST framework. A supported negative finding
  completes the research; it does not claim a shipped capability.
- **Means:** One non-shipping prototype in `tests/FunnySharp.TreeTransformations` with an adapter
  for ordinary caller node types, an iterative pre/post-order traversal with lazy location paths, a
  bottom-up rewrite, a path-directed rewrite, and a zipper with navigation, edit, and root
  reconstruction. A shared typed service-configuration workload drives six complete consumers:
  direct recursion, a hand-written iterative visitor and rewrite, a generic full rewrite, a generic
  path rewrite, a zipper, and `System.Text.Json.Nodes.JsonNode`.
  A shared verifier checks exact effective trees, error order and locations, traversal order,
  short-circuiting, rebuilt/shared/visited node counts, structural sharing, mutable contents, DAG
  duplicate visits, cycle rejection, and caller-supplied equality. Two isolated Release harness
  runs report allocation, copying, rebuild cost, and depth behavior.
- **Authority:** [Goal 0041](../goals/archive/0041-goal.md) is the completion contract.
  [Product contract](../product-contract.md) already excludes a general traversal API and optics
  hierarchy; [grammar](../grammar.md) and [core design](../core-design.md) are current boundaries.
  Goal 0036 is the closest product neighbor (typed nested reshaping and locations); Goals 0039 and
  0040 are the shape references for a research outcome. This plan starts no Goal 0042 work.
- **Execution profile:** Prototype in `tests/FunnySharp.TreeTransformations/`; shared workload,
  consumers, and verifier in `tests/Shared/TreeComparison/`; xUnit v3 semantics and comparison tests
  in `tests/FunnySharp.TreeTransformations.Tests/`; measurements in
  `tests/FunnySharp.TreeTransformationsMeasurements/`. Record the decision in
  `docs/reusable-tree-transformations-evidence.md`, move the goal unchanged to archive, and update
  the goals index. No shipping project, package, API baseline, grammar, product-contract, or
  stability-inventory entry changes unless a public shape is actually adopted; the prototype is
  non-packable and not referenced by `src/`.
- **Tail ownership:** The maintainer judges whether a reusable traversal, rewrite, or zipper pays for
  its adapter, library core, and semantics. The evidence must identify where ordinary recursion, a
  hand-written iterative visitor, or the existing .NET tree API remains preferable. A negative
  decision is complete and does not claim a shipped capability.
- **Stop for a new decision if:** the only defensible prototype needs a replacement tree universe,
  reflection or property-path discovery, a general AST optimizer, automatic persistence, a
  recursion-scheme/optics hierarchy, or an ambient tree cache; or the representative workload
  shrinks to a toy chain that cannot show traversal order, failure locations, structural sharing,
  and DAG/cycle boundaries.

## Product Contract

### Chosen workload and why it is a real task

The workload is a typed service configuration used by a deployment overlay:

- A base configuration tree of ordinary C# records: `ConfigNode(Name, Value, Children)` with
  `IReadOnlyList<ConfigNode>` children. A section has a null value and named or indexed children; a
  leaf has a non-null value and no children.
- The base tree models 64 services. Each service has a name, a mode, eight endpoints (host, port,
  path, TLS), and a limits section (timeout, retries, max body). The fixture contains 3,074 nodes
  and 2,368 leaves at depth 5.
- The task applies an environment overlay: a list of path-addressed value replacements such as
  `services[3].endpoints[0].port` or `services[7].limits.timeout`. Each replacement must resolve to
  an existing leaf, must not target a section, must not repeat a path, and must satisfy the leaf's
  value rule (port range, positive timeout, mode vocabulary, non-empty host/path, boolean TLS).
  Independent failures accumulate in override order with a rendered location such as
  `services[3].endpoints[0].port`; a strict mode stops at the first failure. The effective tree is
  then validated in document order so an invalid base leaf is also reported.
- The result is the complete effective tree with only the changed paths rebuilt; unchanged subtrees
  are reused by reference. The original base tree is never mutated.

Configuration is the chosen task because it is the closest real task to FunnySharp's business
consumers, it naturally needs ordered traversal, failure locations, short-circuiting, and localized
persistent edits, and it exercises ordinary C# records with BCL child collections. `Location` and
`Validation` already exist for paths and accumulated failures, so the task can reuse them honestly.
A syntax tree would add a parser and a second node universe; an expression-tree task is already
served by `System.Linq.Expressions.ExpressionVisitor` and would mostly re-measure that BCL facility.
Both are assessed and excluded rather than silently ignored.

### Compared implementations and fairness

| Implementation | Shape | Why it is fair |
| --- | --- | --- |
| Direct recursion | Recursive path resolution, ancestor rebuild, and full validation over `ConfigNode`. | Direct, competent C#; no adapter, no generic library. It is the honest baseline for an application that already uses recursion. |
| Hand-written iterative visitor and rewrite | Explicit-stack path resolution, ancestor rebuild with arrays, and pre-order validation. | The competent stack-safe hand-written form and the comparison's competent hand-written rewrite; no generic adapter or delegate dispatch. It is the documented-pattern candidate. |
| Generic full rewrite | `TreeRewrite.RewriteBottomUp` over the adapter, applying overrides by path, plus traversal validation. | Pays the adapter, path-view, delegate, and full-traversal costs. It is the honest generic "rewrite the tree" candidate and shows the union-rebuild shape. |
| Generic path rewrite | `TreeRewrite.RewriteAt` per override over the adapter, plus `TreeTraversal.Traverse` validation. | The narrow generic candidate; it pays the adapter but not a full rewrite. |
| Zipper | `Zipper` navigation by path, `Replace`, and `ToRoot`, plus traversal validation. | The zipper candidate with explicit navigation, edit, and root reconstruction; it pays frame allocation and reconstruction. |
| `System.Text.Json.Nodes.JsonNode` | Parse the base JSON into a mutable `JsonNode`, resolve and edit paths, serialize, and validate the JSON tree. | The relevant existing .NET tree API for JSON-backed configuration. It pays parse/serialize and a bridge to the typed model; it is not a typed tree. |

Shared models, fixtures, path syntax, value rules, canonical digest, and verifier are not charged
to any consumer. Every consumer uses the same base tree, override list, value rules, error order,
and complete operation (apply plus validate). Every consumer is charged its own adapter, path
resolution, rebuild, validation, and any parse/serialize bridge. `JsonNode` is parsed fresh for
each persistent apply so the original tree is not mutated; that cost belongs to the alternative.

Assessed and not measured as competitors:

- `System.Linq.Expressions.ExpressionVisitor` is the canonical .NET expression-tree visitor, but it
  operates on the `Expression` node universe. A configuration model would need a full conversion to
  `Expression` and back, which is not a fair tree-mechanism comparison; for expression trees the BCL
  already supplies generic rewriting, so a FunnySharp replacement would add no capability.
- `Microsoft.Extensions.Configuration` is a flat key-value provider chain, not a tree with ordered
  children, structural sharing, or node identity; it cannot express this task's shape.
- `System.Collections.Immutable` supplies persistent collections, not a tree traversal or zipper;
  it is a storage mechanism a caller can use inside its own rebuild, not a competing tree API.
- `Newtonsoft.Json`/`JsonNode` alternatives would duplicate the `JsonNode` measurement without a
  different algorithmic claim; they are not measured.

### Tree, DAG, and cyclic-graph semantics

The prototype states one supported meaning for each shape and rejects the rest explicitly:

| Shape | Traversal | Rewrite | Zipper | Identity/equality |
| --- | --- | --- | --- | --- |
| Tree | Pre-order in child order or post-order children-before-parent; each occurrence is visited. | Each occurrence is rewritten; a shared reference reached twice becomes two instances in tree mode. | A cursor is a path occurrence; editing one path does not affect another occurrence. | Reference identity is the default. Caller-supplied equality is opt-in and only used by DAG dedup/cycle checks. |
| Shared DAG | `TreeShape.Dag` with a caller comparer visits each distinct node once (first occurrence); duplicate occurrences are counted and skipped. Tree mode visits every occurrence. | DAG mode memoizes a rewritten node by original identity/comparer, so sharing is preserved; tree mode may duplicate shared nodes. | A path occurrence is edited independently; unaffected occurrences keep the original reference. | Identity or caller equality; structural equality can merge distinct equal nodes, which is the caller's explicit policy. |
| Cyclic graph | `CyclePolicy.Throw` detects an ancestor repeat and throws a typed cycle exception; `CyclePolicy.Stop` stops and reports it; a node limit is a final guard. | A full rewrite cannot produce a finite result; cycles are rejected. A path-directed rewrite works on a finite path but does not claim the graph is acyclic. | A zipper assumes a tree/DAG; following a cycle downward is unbounded and is rejected/documented, never silently supported. | Structural equality on a cycle can recurse infinitely; it is rejected as a DAG identity policy. |

The default adapter identity is `ReferenceEqualityComparer.Instance`, not
`EqualityComparer<TNode>.Default`, because C# records often have recursive structural equality that
is wrong for DAG dedup and can overflow on cycles. A caller that wants structural dedup must supply
the comparer and accept that two distinct equal nodes become one visit/one rewritten node.

### Traversal order, failure locations, and short-circuiting

- Pre-order visits a node before its children in child order; post-order visits children in child
  order before the parent. The prototype's fixtures pin the exact sequence and depth for a small
  tree and for the 3,074-node configuration.
- The traversal passes a `TreePathView` valid only during the callback. The view exposes depth and
  segments; `ToLocation()` allocates a `Location` only when the visitor records a failure. A
  per-node `Location` object is a disclosed cost row, not the default.
- A visitor returns `Continue` or `Stop`. `Stop` ends the traversal after the current callback and
  reports the visited count; no later node or child is examined. Validation's strict mode uses the
  same stop path; accumulation continues.
- `RewriteAt` returns `Result<TNode, TreeEditError>` with `PathNotFound`, `IndexOutOfRange`, or
  `NotAValue` (a section target) and a `Location` for the failing segment. A path-directed edit
  does not visit unrelated nodes; its failure location is the deepest resolved segment.

### Structural sharing versus mutable node contents

- The sharing rule is explicit: a rebuild callback returns the original node when its subtree did
  not change; the parent copies its child list only when at least one child reference changed; all
  unchanged child references are reused. A path rewrite rebuilds exactly the ancestors on the path
  and shares every sibling subtree by reference.
- Sharing is reference sharing, not deep immutability. `ConfigNode` has immutable properties but
  its `Children` may be a caller-owned mutable `List<ConfigNode>`; mutating that list after a build
  is visible through every root that shares it. The prototype never clones or freezes leaves. The
  zipper captures the child-list reference at navigation time and copies the list only when it
  rebuilds; it does not detect later mutation.
- The evidence measures rebuilt nodes, shared sibling references, allocated bytes, and the cost of
  rebuilding a changed path. It does not claim that sharing makes a mutable tree immutable.

### Error and location reporting

- `ConfigError` is a diagnostic value with a kind, a `Location`, and a message. Independent override
  failures and effective-tree validation failures accumulate through
  `Validation<ConfigNode, ConfigError>` in override order then document order; strict mode returns
  the first `Result<ConfigNode, ConfigError>` failure and stops.
- `Location` is the existing experimental (`FS0017`) path carrier; the prototype and tests use it
  with an explicit suppression and disclose that a future public shape would need to promote or
  replace it. Paths are built from segments, not concatenated application strings; a failure at
  `services[3].endpoints[0].port` is produced compositionally.
- `Result` and `Validation` are reused as the outcome vocabulary; no new carrier is introduced.

### Zipper semantics

- A `Zipper<TNode>` is an immutable cursor over the adapter: `Current`, `Depth`, `Path`,
  `Down(index)`, `Up()`, `Left()`, `Right()`, `Replace(node)`, `Remove()`, and `ToRoot()`.
  Navigation boundaries return `Option<TNode>`-shaped absence (`None`), never an exception;
  `Replace` and `Remove` return a new cursor and never mutate the original root.
- `ToRoot()` rebuilds frames from the current node to the root, copying each ancestor's child list
  and sharing all siblings; the original root is unchanged. Root replacement is the zero-frame
  case. `Remove()` removes the current node from its parent and returns the parent cursor; removing
  the root is rejected with `None` or a typed result.
- The zipper navigates by child position, not by equality; a shared DAG node reachable by two paths
  has two independent cursors, and editing one path does not affect the other. A cycle is not
  detected; the zipper's contract assumes a tree/DAG and the evidence documents the rejected cycle
  case rather than running an unbounded navigation loop.

### Adapter and consumer-code accounting

The generic adapter is `Children(node)`, `Rebuild(node, children)`, `ChildIndex(node, segment)`,
`Label(node)`, and `Identity`. The adapter is charged to every generic consumer and to the
prototype's own line count. The hand-written consumers pay their own model-specific traversal and
rebuild code instead. The evidence reports raw source lines for the prototype, each consumer, each
adapter, the shared fixtures, and the verifier; lines include braces and comments and are not a LOC
target.

### Measurement protocol

- **Environment disclosed:** host OS, kernel, architecture, logical cores, .NET SDK/runtime,
  Release build, `TieredCompilation=false`, UTC date. Allocation is the blocking comparison; timing
  is directional on this shared host.
- **Workloads:** (a) one valid path override, (b) 32 valid overrides, (c) a mixed overlay with a
  missing path, a section target, an invalid value, and a duplicate path, (d) full validation of the
  3,074-node tree, (e) a deep chain at 1,000 / 5,000 / 10,000 / 20,000 / 50,000 / 100,000 levels,
  (f) a shared DAG with duplicate occurrences, (g) a cyclic graph, and (h) a mutable child-list
  fixture.
- **Metrics:** allocated bytes per operation, directional nanoseconds per operation, visited nodes,
  rebuilt nodes, shared sibling references, applied overrides, accumulated errors, and consumer
  source lines. Work counts are deterministic and asserted by the shared verifier.
- **Stack behavior:** the recursive baseline is run in a child process at each depth so a stack
  overflow is observed as an exit code without killing the harness; the iterative, generic, and
  zipper forms run in-process at depth 100,000 and report success or a typed limit failure. The
  harness never runs a recursive crash in its own process.
- **Two runs:** the complete harness is run twice in isolation; both runs are recorded. The harness
  verifies parity before measuring. It is never run concurrently with the test suite.
- **Identity:** no custom fingerprints or receipts. Source identity is the Goal 0041 working tree;
  the landing commit becomes the durable identity.

### Scope markers

**Included:** non-shipping adapter/traversal/rewrite/zipper prototype; shared typed configuration
workload; six complete consumers; semantics tests; depth, allocation, copying, rebuild, DAG,
cycle, and mutable-content evidence; evidence record and goal close.

**Deferred to follow-up work (only if a later goal reopens):** a public tree API, a public adapter,
a path type, a zipper, a batched rewrite protocol, async trees, cancellation, persistence, and any
reflection/property-path discovery.

**Outside this product's identity:** a replacement tree universe, a recursion-scheme or optics
hierarchy, a general AST optimizer, automatic persistence, an ambient tree cache, or a serializer.

## Planning Contract

### Requirements

- **R1:** Add a non-packable `tests/FunnySharp.TreeTransformations` prototype with
  `TreeNodeAdapter<TNode>`, `TreePath`/`TreePathSegment`/`TreePathView`, iterative
  `TreeTraversal.Traverse`, `TreeRewrite.RewriteBottomUp`, `TreeRewrite.RewriteAt`, `Zipper<TNode>`,
  `TreeTraversalOptions<TNode>`, and typed cycle/limit/edit errors. It must not be referenced by
  `src/`.
- **R2:** Implement the adapter with children, rebuild, child index, label, and identity; reject null
  delegates and null child lists with documented exceptions; default identity is reference equality.
- **R3:** Implement iterative pre-order and post-order traversal with document order, depth, lazy
  path views, `Continue`/`Stop`, `MaxNodes`, tree/DAG shape, cycle throw/stop, and duplicate-visit
  counts. No recursion in the traversal.
- **R4:** Implement bottom-up rewrite that calls the rewrite callback for every visited node, reuses
  the original node when the callback returns it, and rebuilds only ancestors whose child
  references changed; DAG mode memoizes rewritten nodes by identity/comparer and preserves sharing.
- **R5:** Implement path-directed `RewriteAt` with iterative resolution, `Result` failures with
  `Location`, root replacement, and reference-preserving rebuild of exactly the path ancestors.
- **R6:** Implement the immutable zipper with `Down`/`Up`/`Left`/`Right`/`Replace`/`Remove`/`ToRoot`,
  `Option` navigation absence, path/location rendering, sibling sharing, and documented stale
  mutable-list and DAG/cycle semantics.
- **R7:** Add the shared typed configuration workload: models, fixtures, path syntax, value rules,
  canonical digest, error values, override lists, deep chain, DAG, cycle, and mutable child-list
  fixtures. The fixtures are deterministic and shared by tests and measurements.
- **R8:** Implement six complete consumers against the same workload: recursive, iterative,
  generic full rewrite, generic path rewrite, zipper, and `JsonNode`. Each
  applies the overlay, validates the effective tree, accumulates errors in the fixed order, and
  reports visited/rebuilt/shared/error counts. The generic consumers include the adapter.
- **R9:** Add a shared verifier that checks exact effective trees, canonical digests, error kinds and
  paths/order, traversal order, short-circuit counts, rebuilt/shared/visited counts, structural
  sharing by reference, mutable-list aliasing, DAG duplicate visits and sharing, cycle rejection,
  caller-supplied equality, and root immutability. The verifier is called by both the xUnit suite and
  the measurement harness.
- **R10:** Add deterministic xUnit v3 tests for every promise above, including null guards, malformed
  paths, root edits, remove-root rejection, navigation boundaries, cycle throw/stop, limit guard,
  path failures, and fail-fast versus accumulate behavior. No skipped tests.
- **R11:** Add a measurement executable that verifies parity, prints environment and workload shape,
  reports consumer line counts, applies one/32/mixed overrides, validates the full tree, measures
  depth behavior with a child-process recursive probe, measures DAG/cycle/mutable fixtures, and can
  be run twice in isolation.
- **R12:** Write the evidence record with the decision, workload rationale, model and semantics,
  fairness assessment, both runs' tables, consumer expression, workload guidance, non-adoptions,
  residual risks, and an explicit statement that no public capability changed. Move the goal
  unchanged to archive and update the goals index/navigation.
- **R13:** Run the required gates and record real exit codes: build, test, format, docs snippets,
  API baseline, examples, and the measurement harness twice.

### Non-goals

- No shipping tree API, adapter, path type, zipper, package, analyzer, grammar, product-contract, or
  API-baseline change.
- No replacement tree universe, recursion scheme, optics hierarchy, general AST optimizer, or
  automatic persistence.
- No reflection, property-path discovery, serializer, or schema inference.
- No ambient/cross-call tree cache, async tree API, cancellation, or scheduler.
- No claim that reference sharing implies deep immutability or that a DAG/cycle is supported beyond
  the recorded meanings.
- No changes to Goals 0036, 0039, or 0040 behavior; their shapes are references only.

## Implementation Units

### U1. Prototype core and semantics

- **Goal:** A buildable, non-shipping tree prototype implementing R1-R6.
- **Requirements:** R1, R2, R3, R4, R5, R6.
- **Dependencies:** none.
- **Files:** `tests/FunnySharp.TreeTransformations/`.
- **Approach:** `TreeNodeAdapter<TNode>`; `TreePathSegment`/`TreePath`/`TreePathView`;
  `TreeTraversal` with explicit frames and a path segment buffer; `TreeRewrite` with iterative
  post-order and path-directed resolution; `Zipper<TNode>` with immutable frames; typed
  `TreeEditError`, `TreeCycleException`, and `TreeTraversalLimitException`. Use `Location` only when
  an error is created; suppress `FS0017` explicitly in the files that use it.
- **Execution note:** traversal frames are structs; path views are valid only during the callback;
  no recursion in traversal, rewrite, or root reconstruction. The adapter validates null children and
  delegates at entry.
- **Patterns to follow:** ordinary C# records/readonly structs, `ArgumentNullException`/
  `ArgumentException` guards, `Result`/`Option`/`Validation` vocabulary, no static mutable state.
- **Test scenarios:** pre/post order, depth, lazy locations, stop, max nodes, tree/DAG duplicate
  visits, cycle throw/stop, reference/custom equality, bottom-up rewrite sharing, path rewrite
  success/failure/root, zipper navigation boundaries/replace/remove/root sharing, mutable lists.
- **Verification:** prototype build and focused semantics tests.

### U2. Shared workload, consumers, and verifier

- **Goal:** The typed configuration workload and six complete consumers, plus the shared verifier.
- **Requirements:** R7, R8, R9.
- **Dependencies:** U1.
- **Files:** `tests/Shared/TreeComparison/`.
- **Approach:** `ConfigNode`, `ConfigFixtures`, `ConfigKernel`, `ConfigOverride`,
  `ConfigError`, `ConfigEditStats`, `IConfigEditor`, the six consumers, and
  `TreeComparison.Verify`. The base tree is 64 services with eight endpoints and four fields per
  endpoint; overrides are deterministic; the deep chain, DAG, cycle, and mutable-list fixtures are
  separate. The recursive consumer uses recursion; the iterative consumer uses explicit stacks;
  the generic consumers use the adapter; the zipper consumer uses `Zipper`; the `JsonNode`
  consumer parses, edits, and converts a uniform JSON tree.
- **Execution note:** every consumer returns the same `Validation`/`Result` shape and resets stats
  per operation. The shared canonicalizer is not charged to any consumer. The `JsonNode` consumer
  parses fresh per apply and validates JSON leaves with the same rules.
- **Patterns to follow:** existing shared comparison consumers; internal types; no test framework
  dependency in shared code; deterministic fixtures.
- **Test scenarios:** valid single/32 overrides, mixed failures, full validation, fail-fast,
  structural sharing, rebuilt/shared counts, DAG, cycle, mutable list, equality, root immutability.
- **Verification:** shared verifier plus xUnit comparison tests.

### U3. xUnit v3 semantics and comparison tests

- **Goal:** Executable coverage of every prototype and consumer promise.
- **Requirements:** R10.
- **Dependencies:** U1, U2.
- **Files:** `tests/FunnySharp.TreeTransformations.Tests/`.
- **Approach:** Split by concern: path/traversal/rewrite/zipper/config-comparison. Call the shared
  verifier once; add focused boundary tests for null guards, malformed paths, root edits, remove-root,
  navigation absence, cycle/limit, and fail-fast counts.
- **Execution note:** one public sealed class per file; classic asserts; no skipped tests; no
  recursive stack-overflow test in the suite (the measurement harness owns the child-process probe).
- **Test scenarios:** R10 list; DAG equality; mutable child list; zipper root reconstruction; error
  order and paths.
- **Verification:** `dotnet test` on the new project and the solution gate.

### U4. Measurement harness

- **Goal:** Honest allocation, copying, rebuild, depth, DAG, and cycle measurements.
- **Requirements:** R11.
- **Dependencies:** U2.
- **Files:** `tests/FunnySharp.TreeTransformationsMeasurements/`.
- **Approach:** Verify parity first; print environment and workload shape; report raw source line
  counts; use the shared allocation harness for one/32/mixed overrides and full validation; measure
  rebuilt/shared/visited counts; run the recursive depth probe in child processes; run the iterative,
  generic, and zipper forms at depth 100,000; report DAG/cycle/mutable fixtures; print two complete
  process runs.
- **Execution note:** never run concurrently with the test suite; one heavy process at a time. The
  depth probe must not crash the parent. The measured operations include the complete apply plus
  validation where the row says so.
- **Test scenarios:** one override, 32 overrides, mixed overlay, full validation, deep chain,
  DAG duplicate/sharing, cycle rejection, mutable list.
- **Verification:** two isolated harness runs with verified parity.

### U5. Evidence, decision, and close

- **Goal:** Maintainer-ready evidence and goal closure.
- **Requirements:** R12, R13.
- **Dependencies:** U4.
- **Files:** `docs/reusable-tree-transformations-evidence.md`, `docs/goals/README.md`,
  `docs/goals/archive/0041-goal.md`, `docs/goals/AGENTS.md`, `docs/AGENTS.md`,
  `docs/plans/AGENTS.md`.
- **Approach:** Record the model, fairness assessment, both measurement runs, consumer expression,
  workload guidance, tree/DAG/cycle and mutability semantics, non-adoptions, residual risks, and the
  ADOPT/ADAPT/DEFER/REJECT judgment. Move the goal unchanged. State explicitly that no public
  capability changed for a negative or research-only decision; reconcile product contract/grammar/
  stability only if a public shape was actually adopted.
- **Execution note:** preserve the goal file byte-for-byte; verify with `cmp` before and after the
  move.
- **Test scenarios:** none beyond gates.
- **Verification:** final gate transcript and working-tree review.

## Verification Contract

| Gate | Command | Applies to | Completion evidence |
| --- | --- | --- | --- |
| Build | `dotnet fsi build.fsx -- -p build` | all projects | 0 warnings, 0 errors |
| Test | `dotnet fsi build.fsx -- -p test` | all suites including new tests | 0 failed, 0 skipped |
| Format | `dotnet fsi build.fsx -- -p format` | C# | clean |
| Docs snippets | `dotnet fsi build.fsx -- -p verify-docs-snippets` | primary guides | unchanged count unless a guide is added |
| API baseline | `dotnet fsi build.fsx -- -p verify-api-baseline` | shipping assemblies | 0 delta; no public capability changed |
| Examples | `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release` | examples | passed |
| Measurement | `dotnet run --project tests/FunnySharp.TreeTransformationsMeasurements/FunnySharp.TreeTransformationsMeasurements.csproj -c Release` | comparison | two complete isolated runs, parity verified |

## Definition of Done

- **U1-U3:** the prototype, shared workload, six consumers, verifier, and tests build and run; the
  traversal order, locations, short-circuit, sharing, DAG, cycle, equality, mutable-content, and
  zipper promises hold; unsupported shapes are explicit.
- **U4:** the harness reports actual allocation, copying, rebuild counts, consumer lines, and depth
  behavior for every compared implementation, twice.
- **U5:** the evidence states the decision with reasoning and non-adoptions, the goal is archived
  unchanged, navigation is updated, and no public capability is claimed if the decision is not an
  adopt/adapt with a public shape.
- **Global:** every required gate has a real recorded result; the working tree is left uncommitted;
  no Goal 0042 work starts.
