# Reusable Tree Transformations Evidence

This record supports the maintainer's judgment for
[Goal 0041](goals/archive/0041-goal.md): whether reusable tree traversal, rewriting, or
zipper-style editing gives FunnySharp consumers a meaningful improvement over direct recursion, an
iterative visitor with an explicit stack, and an existing .NET tree API for a real configuration
task. It records the executable workload, the non-shipping prototype, six complete consumers, the
tree/DAG/cycle and mutable-content semantics, and two isolated measurement runs.

**Decision: REJECT a public tree traversal, rewrite, or zipper capability; retain the documented
iterative path-edit pattern.** The hand-written iterative consumer is the smallest complete
stack-safe solution: 359 raw lines with no adapter, generic core, or cycle-detection bookkeeping.
The generic path consumer plus its adapter is 245 raw lines, a 114-line per-consumer saving that
does not repay the 1,653-line prototype on the disclosed workload; the generic full rewrite (285
lines with adapter) visits every node, and the zipper (272 lines with adapter) allocates a cursor
and frame per navigation step. The recursive form is shorter (286 lines) but stack-unsafe; the
`JsonNode` form is the largest (434 lines) and copies the whole document. On a single override the
generic path allocated 5,104 B and took ~530 &micro;s versus 4,184 B and ~352 &micro;s for the hand
iterative; the generic full allocated 474,886 B and took ~1.37 ms; the zipper 5,464 B and ~529
&micro;s; `JsonNode` 5.16 MB and ~10.1 ms. The recursive form was lowest at 3,760 B and ~334 &micro;s
but exited 134 at 100,000 sections; the hand iterative, generic path, and zipper completed that
depth. The measured allocation and depth results below confirm the direction. The smallest useful
capability is the pattern, not a library type.

Source identity is this Goal 0041 working tree on top of commit `6abddb9`; the landing commit
becomes the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure x86_64, 4 logical cores.
- .NET SDK 10.0.401, runtime 10.0.12 x64; Release build; `TieredCompilation=false`.
- Dates: 2026-11-08 UTC. The two final runs started at 16:16:46 and 16:17:11.

Allocation is the blocking comparison; timing is directional on this shared host.

## Chosen Workload And Why It Is Representative

The workload is a typed service configuration used by a deployment overlay:

- A base configuration tree of ordinary C# records: `ConfigNode(Name, Value, Children, Indexed)`
  with `IReadOnlyList<ConfigNode>` children. A section has a null value and ordered children; a
  leaf has a value and no children. `Indexed` makes the path semantics explicit: an indexed
  section addresses children by position, a named section by label.
- 64 services. Each service has a name, a mode, eight endpoints (host, port, path, TLS), and a
  limits section (timeout, retries, max body). The fixture contains 3,074 nodes and 2,368 leaves
  at depth 5.
- The task applies an environment overlay: 32 path-addressed value replacements such as
  `services[3].endpoints[0].port` or `services[7].limits.timeout`. Each replacement must resolve to
  an existing leaf, must not target a section, must not repeat a path, and must satisfy the leaf's
  value rule (port range, positive timeout, mode vocabulary, non-empty host/path, boolean TLS).
  Independent failures accumulate in override order with a rendered location; a strict mode stops
  at the first failure. The effective tree is then validated in document order so an invalid base
  leaf is also reported.
- The result is the complete effective tree with only the changed paths rebuilt; unchanged subtrees
  are reused by reference. The original base tree is never mutated.

Configuration is the chosen task because it is the closest real task to FunnySharp's business
consumers, it naturally needs ordered traversal, failure locations, short-circuiting, and localized
persistent edits, and it exercises ordinary C# records with BCL child collections. `Location` and
`Validation` already exist for paths and accumulated failures, so the task reuses them honestly. A
syntax tree would add a parser and a second node universe; an expression-tree task is already
served by `System.Linq.Expressions.ExpressionVisitor` and would mostly re-measure that BCL facility.
Both are assessed and excluded rather than silently ignored.

## Prototype Model

The prototype is the non-packable research project
[FunnySharp.TreeTransformations](../tests/FunnySharp.TreeTransformations/). It is not referenced by
`src/` and is not part of the shipping surface.

### Adapter

`TreeNodeAdapter<TNode>` describes the caller's ordinary node type with five delegates:
`Children(node)`, `Rebuild(node, children)`, `ChildIndex(node, segment)`, `SegmentAt(node, index)`,
and `Label(node)`. Identity defaults to `ReferenceEqualityComparer.Instance`, not
`EqualityComparer<TNode>.Default`, because record structural equality is usually wrong for DAG
deduplication and can recurse without bound on a cycle. The adapter is charged to every generic
consumer and to the prototype's line count. The hand-written consumers pay their own model-specific
traversal and rebuild code instead.

### Traversal

`TreeTraversal.Traverse` is an iterative pre-order or post-order walk with an explicit frame stack
and a reused path-segment buffer. It never recurses. The visitor receives the node, its depth, and
a callback-scoped `TreePathView`; `ToLocation()` allocates a `Location` only when the visitor records
a failure. `TreeVisitDecision.Stop` ends the traversal after the current callback. `MaxNodes` is a
final guard. Tree mode visits every reference occurrence; DAG mode visits each distinct node once
under the caller's identity comparer and counts later occurrences as duplicates. An ancestor repeat
is a cycle: `TreeCyclePolicy.Throw` throws `TreeCycleException` with the repeated node's location;
`Stop` reports `CycleDetected` and stops.

### Rewrite

`TreeRewrite.RewriteBottomUp` is an iterative post-order rewrite. It calls the callback for every
visited node after its children, passing the original children, the rewritten children, and the
path view. The callback returns the original node to signal an unchanged node and rebuilds a parent
only when a child reference changed. DAG mode memoizes a rewritten node by identity so a shared
node stays shared; tree mode may produce distinct instances for repeated references.
`TreeRewrite.RewriteAt` resolves one path iteratively and rebuilds exactly its ancestors, sharing
every sibling. It returns a typed `Result` failure with the failing segment's `Location`; the edit
callback may reject the resolved node (the configuration consumer uses that for a section target or
an invalid value). A no-op edit returns the original root and reports zero rebuilt nodes.

### Zipper

`Zipper<TNode>` is an immutable cursor with `Current`, `Depth`, `Path`, `Down(index)`,
`Down(segment)`, `Up()`, `Left()`, `Right()`, `Replace(node)`, `Remove()`, and `ToRoot()`.
Navigation boundaries return `Option` absence. Navigation and edit return new cursors and never
mutate the original root. `Up`, `Left`, and `Right` materialize an edited current node into its
parent before moving, so an edit is not lost by navigation. `ToRoot()` rebuilds frames from the
current node to the root, copying each ancestor's child list and sharing all siblings; the original
root is unchanged. `Remove()` returns the rebuilt parent and rejects removing the root.

### Error And Location Reporting

`ConfigError` is a diagnostic value with a kind, a `Location`, and a fixed message. Independent
override failures and effective-tree validation failures accumulate through
`Validation<ConfigNode, ConfigError>` in override order then document order; strict mode returns the
first `Result<ConfigNode, ConfigError>` failure and stops. `Location` is the existing experimental
(`FS0017`) path carrier; the prototype and tests use it with an explicit suppression and disclose
that a future public shape would need to promote or replace it. Paths are built from segments, not
concatenated application strings; a failure at `services[3].endpoints[0].port` is produced
compositionally. `Result` and `Validation` are reused as the outcome vocabulary; no new carrier is
introduced.

## Tree, DAG, And Cyclic-Graph Semantics

| Shape | Traversal | Rewrite | Zipper | Identity/equality |
| --- | --- | --- | --- | --- |
| Tree | Pre-order in child order or post-order children-before-parent; each occurrence is visited. | Each occurrence is rewritten; a shared reference reached twice becomes two instances in tree mode. | A cursor is a path occurrence; editing one path does not affect another occurrence. | Reference identity is the default. Caller-supplied equality is opt-in and only used by DAG dedup/cycle checks. |
| Shared DAG | `TreeShape.Dag` with a caller comparer visits each distinct node once (first occurrence); duplicate occurrences are counted and skipped. Tree mode visits every occurrence. | DAG mode memoizes a rewritten node by original identity/comparer, so sharing is preserved; tree mode may duplicate shared nodes. | A path occurrence is edited independently; unaffected occurrences keep the original reference. | Identity or caller equality; structural equality can merge distinct equal nodes, which is the caller's explicit policy. |
| Cyclic graph | `CyclePolicy.Throw` detects an ancestor repeat and throws a typed cycle exception; `CyclePolicy.Stop` stops and reports it; a node limit is a final guard. | A full rewrite cannot produce a finite result; cycles are rejected. A path-directed rewrite works on a finite path but does not claim the graph is acyclic. | A zipper assumes a tree/DAG; following a cycle downward is unbounded and is rejected/documented, never silently supported. | Structural equality on a cycle can recurse infinitely; it is rejected as a DAG identity policy. |

The default adapter identity is reference equality. A caller that wants structural dedup must
supply the comparer and accept that two distinct equal nodes become one visit/one rewritten node.
The evidence records the shallow record-equality case explicitly: `ConfigNode` record equality over
`IReadOnlyList` children is not a reliable deep DAG identity, so the default is reference equality
and caller-supplied equality is deliberate.

## Structural Sharing Versus Mutable Node Contents

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

## Compared Implementations And Fairness

| Implementation | Shape | Fairness treatment |
| --- | --- | --- |
| Direct recursion | Recursive path resolution, ancestor rebuild, and full validation over `ConfigNode`. | Direct, competent C#; no adapter, no generic library. It is the honest baseline for an application that already uses recursion and the child-process depth probe. |
| Hand-written iterative visitor and rewrite | Explicit-stack path resolution, ancestor rebuild with arrays, and pre-order validation. | The competent stack-safe hand-written form and the comparison's competent hand-written rewrite; no generic adapter or delegate dispatch. It is the documented-pattern candidate. |
| Generic full rewrite | `TreeRewrite.RewriteBottomUp` over the adapter, applying overrides by path, plus traversal validation. | Pays the adapter, path-view, delegate, and full-traversal costs. It is the honest generic "rewrite the tree" candidate and shows the union-rebuild shape. |
| Generic path rewrite | `TreeRewrite.RewriteAt` per override over the adapter, plus `TreeTraversal.Traverse` validation. | The narrow generic candidate; it pays the adapter but not a full rewrite. |
| Zipper | `Zipper` navigation by path, `Replace`, and `ToRoot`, plus traversal validation. | The zipper candidate with explicit navigation, edit, and root reconstruction; it pays frame allocation and reconstruction. |
| `System.Text.Json.Nodes.JsonNode` | Parse the base JSON into a mutable `JsonNode`, resolve and edit paths, validate JSON leaves, and convert back to the typed tree. | The relevant existing .NET tree API for JSON-backed configuration. It pays a fresh parse, JSON validation, and the typed bridge; it does not preserve typed-node sharing. |

Shared models, fixtures, path syntax, value rules, canonical digest, and verifier are not charged
to any consumer. Every consumer uses the same base tree, override list, value rules, error order,
and complete operation (apply plus validate). Every consumer is charged its own adapter, path
resolution, rebuild, validation, and any parse/convert bridge. `JsonNode` is parsed fresh for each
persistent apply so the original tree is not mutated; that cost belongs to the alternative.

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

## Measurements

Command:

```bash
dotnet run --project tests/FunnySharp.TreeTransformationsMeasurements/FunnySharp.TreeTransformationsMeasurements.csproj -c Release
```

The harness first runs the complete comparison (765 checks), then measures the precreated base
tree. Allocation is from `GC.GetAllocatedBytesForCurrentThread`; time is the best of three internal
rounds after warmup. Two complete process runs are shown; the work ledger and depth results are
deterministic and identical in both runs. Timing is directional on this shared host; allocation is
the blocking comparison.

### Work ledger

One complete apply plus validation. `Visited` counts path resolution, rewrite, and validation
nodes; `Rebuilt` counts new nodes; `Shared` counts sibling references copied without rebuilding;
`Applied` counts valid overrides that changed a leaf; `Errors` counts recorded failures.

| Scenario | Consumer | Visited | Rebuilt | Shared | Applied | Errors | Outcome |
| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| Single valid override | Direct recursion | 3,080 | 6 | 76 | 1 | 0 | valid |
| Single valid override | Iterative explicit stack | 3,080 | 6 | 76 | 1 | 0 | valid |
| Single valid override | Generic full rewrite | 6,154 | 6 | 76 | 1 | 0 | valid |
| Single valid override | Generic path rewrite | 3,080 | 6 | 76 | 1 | 0 | valid |
| Single valid override | Zipper | 3,080 | 6 | 76 | 1 | 0 | valid |
| Single valid override | `JsonNode` | 3,080 | 3,074 | 0 | 1 | 0 | valid |
| 32 valid overrides | Direct recursion | 3,258 | 184 | 2,380 | 32 | 0 | valid |
| 32 valid overrides | Iterative explicit stack | 3,258 | 184 | 2,380 | 32 | 0 | valid |
| 32 valid overrides | Generic full rewrite | 6,332 | 112 | 358 | 32 | 0 | valid |
| 32 valid overrides | Generic path rewrite | 3,258 | 184 | 2,380 | 32 | 0 | valid |
| 32 valid overrides | Zipper | 3,258 | 184 | 2,380 | 32 | 0 | valid |
| 32 valid overrides | `JsonNode` | 3,258 | 3,074 | 0 | 32 | 0 | valid |
| Mixed overlay | Direct recursion | 3,275 | 189 | 2,448 | 33 | 4 | invalid |
| Mixed overlay | Iterative explicit stack | 3,275 | 189 | 2,448 | 33 | 4 | invalid |
| Mixed overlay | Generic full rewrite | 6,353 | 114 | 359 | 33 | 4 | invalid |
| Mixed overlay | Generic path rewrite | 3,263 | 189 | 2,448 | 33 | 4 | invalid |
| Mixed overlay | Zipper | 3,275 | 189 | 2,448 | 33 | 4 | invalid |
| Mixed overlay | `JsonNode` | 3,275 | 3,074 | 0 | 33 | 4 | invalid |
| Full validation | Direct recursion | 3,074 | 0 | 0 | 0 | 0 | valid |
| Full validation | Iterative explicit stack | 3,074 | 0 | 0 | 0 | 0 | valid |
| Full validation | Generic full rewrite | 3,074 | 0 | 0 | 0 | 0 | valid |
| Full validation | Generic path rewrite | 3,074 | 0 | 0 | 0 | 0 | valid |
| Full validation | Zipper | 3,074 | 0 | 0 | 0 | 0 | valid |
| Full validation | `JsonNode` | 3,074 | 0 | 0 | 0 | 0 | valid |

The single override rebuilds six nodes (the leaf and five ancestors) and shares 76 sibling
references. The 32 overrides rebuild 184 nodes and share 2,380 references sequentially; the full
rewrite rebuilds the union once (112 nodes) and shares 358. The full rewrite's lower rebuild count
is real, but its per-node traversal, rewritten arrays, and path objects cost more allocation and
time (below). The `JsonNode` consumer rebuilds all 3,074 typed nodes because it converts the whole
JSON tree back; it preserves no reference sharing. The generic path consumer's mixed-overlay
`Visited` is 3,263 rather than 3,275 because a failed `RewriteAt` does not add the failed path's
visits to the consumer ledger; the rebuilt/shared/applied/error counts match.

### Allocation and directional time, run 1

| Scenario | Consumer | Allocated B/op | Time ns/op |
| --- | --- | ---: | ---: |
| Single valid override | Direct recursion | 3,760.4 | 333,819.0 |
| Single valid override | Iterative explicit stack | 4,184.4 | 351,156.0 |
| Single valid override | Generic full rewrite | 474,886.0 | 1,468,910.0 |
| Single valid override | Generic path rewrite | 5,103.6 | 535,311.0 |
| Single valid override | Zipper | 5,463.6 | 525,526.0 |
| Single valid override | `JsonNode` | 5,162,051.2 | 10,234,910.0 |
| 32 valid overrides | Direct recursion | 36,824.4 | 369,361.0 |
| 32 valid overrides | Iterative explicit stack | 41,520.4 | 382,372.0 |
| 32 valid overrides | Generic full rewrite | 483,222.0 | 1,385,825.0 |
| 32 valid overrides | Generic path rewrite | 47,151.6 | 560,374.0 |
| 32 valid overrides | Zipper | 57,967.6 | 566,566.0 |
| 32 valid overrides | `JsonNode` | 5,199,338.4 | 9,850,215.0 |
| Mixed overlay | Direct recursion | 39,240.4 | 115,331.0 |
| Mixed overlay | Iterative explicit stack | 44,440.4 | 128,316.0 |
| Mixed overlay | Generic full rewrite | 484,990.0 | 1,135,025.0 |
| Mixed overlay | Generic path rewrite | 50,871.6 | 308,052.0 |
| Mixed overlay | Zipper | 62,447.6 | 310,211.0 |
| Mixed overlay | `JsonNode` | 5,202,818.8 | 9,611,675.0 |
| Full validation | Direct recursion | 2,496.1 | 329,620.0 |
| Full validation | Iterative explicit stack | 2,776.1 | 351,565.4 |
| Full validation | Generic full rewrite | 3,543.6 | 521,148.0 |
| Full validation | Generic path rewrite | 3,537.5 | 520,986.8 |
| Full validation | Zipper | 3,537.5 | 522,894.4 |
| Full validation | `JsonNode` | 4,351,376.8 | 8,070,998.0 |

### Allocation and directional time, run 2

| Scenario | Consumer | Allocated B/op | Time ns/op |
| --- | --- | ---: | ---: |
| Single valid override | Direct recursion | 3,760.4 | 334,596.0 |
| Single valid override | Iterative explicit stack | 4,184.4 | 352,704.0 |
| Single valid override | Generic full rewrite | 474,886.0 | 1,370,075.0 |
| Single valid override | Generic path rewrite | 5,103.6 | 529,731.0 |
| Single valid override | Zipper | 5,463.6 | 529,320.0 |
| Single valid override | `JsonNode` | 5,162,082.0 | 10,084,135.0 |
| 32 valid overrides | Direct recursion | 36,824.4 | 370,712.0 |
| 32 valid overrides | Iterative explicit stack | 41,520.4 | 383,801.0 |
| 32 valid overrides | Generic full rewrite | 483,222.0 | 1,425,020.0 |
| 32 valid overrides | Generic path rewrite | 47,151.6 | 563,072.0 |
| 32 valid overrides | Zipper | 57,967.6 | 576,020.0 |
| 32 valid overrides | `JsonNode` | 5,199,390.4 | 10,101,320.0 |
| Mixed overlay | Direct recursion | 39,240.4 | 115,654.0 |
| Mixed overlay | Iterative explicit stack | 44,440.4 | 131,043.0 |
| Mixed overlay | Generic full rewrite | 484,990.0 | 1,166,125.0 |
| Mixed overlay | Generic path rewrite | 50,871.6 | 311,405.0 |
| Mixed overlay | Zipper | 62,447.6 | 315,541.0 |
| Mixed overlay | `JsonNode` | 5,202,822.8 | 9,744,820.0 |
| Full validation | Direct recursion | 2,496.1 | 329,634.4 |
| Full validation | Iterative explicit stack | 2,776.1 | 351,540.4 |
| Full validation | Generic full rewrite | 3,543.6 | 525,601.0 |
| Full validation | Generic path rewrite | 3,537.5 | 523,654.4 |
| Full validation | Zipper | 3,537.5 | 518,817.8 |
| Full validation | `JsonNode` | 4,351,376.8 | 8,094,890.0 |

The allocation rows reproduce across the two runs except small `JsonNode` parse-buffer variance
(about 31 B/op). Directional timing varies by a few percent.

### Headline

- **Single valid override:** the recursive form is lowest (3,760 B, ~334 &micro;s); the hand
  iterative is 11% higher (4,184 B, ~352 &micro;s); the generic path is 22% higher than the hand
  iterative (5,104 B, ~530 &micro;s); the zipper is 31% higher (5,464 B, ~529 &micro;s); the generic
  full is 113x the hand iterative (474,886 B, ~1.37 ms); `JsonNode` is 1,234x (5.16 MB, ~10.1 ms).
- **32 valid overrides:** the recursive form is lowest (36,824 B, ~371 &micro;s); the hand iterative
  is 13% higher (41,520 B, ~384 &micro;s); the generic path is 14% higher than the hand iterative
  (47,152 B, ~563 &micro;s); the zipper is 40% higher (57,968 B, ~576 &micro;s); the generic full is
  11.6x (483,222 B, ~1.43 ms); `JsonNode` is 125x (5.20 MB, ~10.1 ms).
- **Full validation:** the recursive/hand iterative forms allocate 2.5-2.8 KB and take ~330-352
  &micro;s; the generic path, zipper, and full rewrite allocate 3.5 KB and take ~520 &micro;s;
  `JsonNode` allocates 4.35 MB and takes ~8.1 ms. Validation dominates the complete operation's
  time; the apply-side path edit difference is smaller than the validation floor.
- **The full rewrite's lower rebuild count does not lower its cost:** it rebuilds 112 nodes versus
  the sequential 184, but its per-node callback, rewritten array, and path object make it 11-113x
  the hand iterative's allocation and ~4x its time on the complete operation.
- **The zipper's cursor is not free:** navigation allocates a cursor and frame per step; the single
  and batch rows are 31-40% above the hand iterative, and the 100,000-section path is 2.0x the
  iterative allocation and 3.2x its time.
- **The `JsonNode` alternative is a full-document tool:** a fresh parse, JSON validation, and typed
  conversion copy the whole tree; it is the largest consumer and the highest allocation by three
  orders of magnitude.

### Recursive depth probe

The recursive consumer is run in a child process at each depth so a stack overflow is observed as
an exit code without killing the harness. Both runs produced the same table.

| Depth | Outcome | Detail |
| ---: | --- | --- |
| 1,000 | ok | `ok 1000 12669221103291512064` |
| 5,000 | ok | `ok 5000 11642841854773999968` |
| 10,000 | ok | `ok 10000 15891619942456390532` |
| 20,000 | ok | `ok 20000 2114407325392460140` |
| 50,000 | ok | `ok 50000 17782949777744564540` |
| 100,000 | exit 134 | stack overflow in `Program.Main` |

The recursive form succeeds through 50,000 sections on this host and exits 134 (stack overflow) at
100,000. The iterative, generic path, and zipper forms complete the 100,000-section edit. This is
the supported depth distinction: the documented iterative pattern is required when the structure can
exceed the call stack; recursion remains valid for the shallow trees where it is shortest.

### Deep path at 100,000 sections

One operation per consumer after the tree and path are built; allocation is the operation's
allocation only.

| Consumer | Result | Allocated B run 1 | Time ms run 1 | Allocated B run 2 | Time ms run 2 |
| --- | --- | ---: | ---: | ---: | ---: |
| Iterative explicit stack | ok | 18,789,416 | 13.4 | 18,789,416 | 13.6 |
| Generic path rewrite | ok | 29,021,816 | 32.8 | 29,021,816 | 37.1 |
| Zipper | ok | 37,821,736 | 45.0 | 37,821,736 | 43.6 |

The iterative form is the cheapest deep path. The generic path pays the `RewriteAt` frame array and
the traversal validation; the zipper pays a cursor and frame per navigation step plus reconstruction.
The recursive reference does not complete this depth (above).

### Tree, DAG, cycle, and mutable-content boundaries

Both runs produced the same table.

| Case | Observation |
| --- | --- |
| DAG traversal, tree shape | 8 visits |
| DAG traversal, DAG shape | 5 visits, 1 duplicate |
| DAG rewrite, tree shape | two instances |
| DAG rewrite, DAG shape | shared |
| DAG path edit | 4 rebuilt, 2 shared |
| Cycle traversal | throws |
| Cycle stop policy | detected=True, visits=2 |
| Mutable child list | digest changed=True, shared children=2 |

The DAG tree-mode traversal visits the shared section and its leaves twice; DAG mode visits each
distinct node once and counts the second occurrence. The tree-mode rewrite produces two instances;
DAG mode preserves one shared instance. A path edit changes one occurrence and leaves the other
sharing the original. A cycle throws or stops with a typed result; a whole rewrite always throws. The
mutable child list is visible through every root that shares it; sharing is not deep immutability.

## Consumer Expression And Maintenance

| Artifact | Raw lines | Notes |
| --- | ---: | --- |
| Prototype | 1,653 | Adapter, path types, iterative traversal, bottom-up/path rewrites, zipper, options, diagnostics; research-only |
| Adapter | 46 | Children, rebuild, child index, segment, label, identity for `ConfigNode` |
| Direct recursion | 286 | Recursive resolve, rebuild, and validation; shortest typed consumer, stack-unsafe at depth |
| Hand-written iterative visitor and rewrite | 359 | Explicit-stack resolve, rebuild, and validation; no adapter or generic core |
| Generic full rewrite | 239 + 46 adapter = 285 | Full traversal, path lookup per node, union rebuild, traversal validation |
| Generic path rewrite | 199 + 46 adapter = 245 | Path rewrite per override plus traversal validation |
| Zipper | 226 + 46 adapter = 272 | Navigate, replace, reconstruct, traversal validation |
| `JsonNode` | 434 | Parse, resolve, edit, validate JSON, convert to typed nodes |
| Shared workload and verifier | 1,337 | Models, fixtures, rules, canonicalizer, diagnostics, six-consumer verifier; not charged |
| Tests | 604 | Prototype and consumer semantics plus the shared comparison |

Raw lines include braces and XML comments; this is not a LOC target. The prototype's 1,653 lines
are the price of the generic adapter/traversal/rewrite/zipper family; the path rewrite alone is a
smaller fraction, but it still depends on the adapter and the traversal validation. The generic
path consumer's 114-line saving versus the hand-written iterative consumer is real per consumer;
it is not repaid by one application, and the generic form additionally pays the traversal's
cycle-detection set and the adapter at runtime.

## Workload Guidance

**Where the documented pattern pays:**

- Persistent, path-addressed edits to a typed tree with full validation. Resolve each path
  iteratively, edit the leaf, rebuild only the ancestors, and share every sibling by reference.
- Deep trees. The explicit stack and ancestor loop are stack-safe at 100,000 sections (iterative
  18.8 MB/13.6 ms; generic path 29.0 MB/37.1 ms; zipper 37.8 MB/43.6 ms). The recursive reference
  succeeds through 50,000 and exits 134 at 100,000 on this host.
- DAG occurrences. A path edit changes one occurrence and leaves the other occurrence sharing the
  original node; the full traversal can optionally deduplicate by an explicit identity comparer.
- Explicit cycle rejection. An ancestor set turns a cycle into a typed diagnostic; a path-directed
  edit is safe on a finite path but does not claim the graph is acyclic.
- Failure locations. Build a `Location` only when an error is recorded; use `Validation` for
  independent failures and `Result` for strict first-failure behavior. `Location` appends one
  segment at a time, so a failure at extreme depth is O(depth^2); ordinary configuration depths
  pay nothing on the success path.
- Structural sharing. Return the original node when its subtree is unchanged; copy a parent's child
  list only when a child reference changed. The unchanged siblings stay reference-equal: a single
  override rebuilt 6 nodes and shared 76 references; 32 sequential overrides rebuilt 184 and shared
  2,380; the full rewrite rebuilt the union (112) and shared 358; `JsonNode` rebuilt all 3,074 and
  shared none.

**Where ordinary recursion, the hand-written iterative visitor, or the existing .NET tree API
remains preferable:**

- Shallow one-off trees. A recursive `with` rewrite is the shortest (286 lines) and fastest form
  (3,760 B/~334 &micro;s single; 36,824 B/~371 &micro;s batch); the stack risk is irrelevant below
  the observed depth. The hand iterative form is the stack-safe choice (4,184 B/~352 &micro;s;
  41,520 B/~384 &micro;s).
- A single localized edit with no full-tree validation. The path-directed iterative edit is
  O(depth) (4,184 B/~352 &micro;s single); the generic full rewrite is O(nodes) and allocated
  474,886 B/~1.37 ms; the generic path rewrite and zipper were 5,104 B/~530 &micro;s and
  5,464 B/~529 &micro;s.
- Whole-tree transformations with no path addressing. A direct recursive `with` rewrite or a
  hand-written visitor is simpler than the adapter, path views, and options. The generic full
  rewrite's lower union rebuild count (112 versus the sequential 184) did not offset its per-node
  rewritten arrays and path objects: 474,886 B/~1.37 ms single and 483,222 B/~1.43 ms batch.
- JSON documents. `System.Text.Json.Nodes.JsonNode` is the existing .NET tree API; it is convenient
  when the application already owns a mutable JSON document, but a fresh parse and typed conversion
  copy the whole tree and do not preserve typed-node sharing (5.16 MB/~10.1 ms single; 5.20 MB/
  ~10.1 ms batch; 4.35 MB/~8.1 ms validation; 434 lines).
- Expression trees. `System.Linq.Expressions.ExpressionVisitor` already supplies the generic
  expression-tree rewrite; a configuration adapter to `Expression` is not a fair comparison and a
  replacement adds no capability.
- Interactive navigation. A zipper is defensible when a caller repeatedly moves around one tree
  and reuses the cursor; for a batch of path edits, the immutable frame chain allocates per step
  and `ToRoot` rebuilds the path (5,464 B/~529 &micro;s single; 57,968 B/~576 &micro;s batch;
  37.8 MB/43.6 ms at 100,000 sections), while the path-directed iterative rewrite is smaller and
  faster (4,184 B/~352 &micro;s; 41,520 B/~384 &micro;s; 18.8 MB/13.6 ms).
- Applications that already have a competent iterative visitor. The generic adapter, path views,
  and options add a learning and allocation surface without changing the domain algorithm: the
  generic path rewrite allocated 5,104 B/~530 &micro;s single and 47,152 B/~563 &micro;s batch versus
  4,184 B/~352 &micro;s and 41,520 B/~384 &micro;s, and the prototype is 1,653 lines.

## Maintainer Judgment

**REJECT a public tree traversal, rewrite, or zipper capability.** The evidence supports a
documented application pattern, not a library type:

- The hand-written iterative consumer is the smallest complete stack-safe solution (359 lines) and
  needs no adapter, generic core, or cycle-detection bookkeeping. The recursive form (286 lines) is
  shorter and was the lowest allocation/time on shallow trees (3,760 B/~334 &micro;s single;
  36,824 B/~371 &micro;s batch) but exited 134 at 100,000 sections; the hand-written iterative form
  is the documented stack-safe pattern (4,184 B/~352 &micro;s single; 41,520 B/~384 &micro;s batch;
  18.8 MB/13.6 ms at 100,000 sections).
- The generic path consumer plus adapter (245 lines) saves 114 lines per consumer versus the
  hand-written iterative form, but the prototype is 1,653 lines and the generic path still pays the
  adapter and traversal at runtime: 5,104 B/~530 &micro;s versus 4,184 B/~352 &micro;s for a single
  override and 47,152 B/~563 &micro;s versus 41,520 B/~384 &micro;s for 32. The generic full rewrite
  (285 lines) visits every node and allocates a rewritten array per node (474,886 B/~1.37 ms single;
  483,222 B/~1.43 ms batch); the zipper (272 lines) allocates a cursor and frame per navigation step
  (5,464 B/~529 &micro;s single; 57,968 B/~576 &micro;s batch; 37.8 MB/43.6 ms at 100,000 sections).
  None repays its core on this workload.
- The existing .NET `JsonNode` alternative is the largest consumer (434 lines) and copies the whole
  document; it is a JSON-document tool, not a typed persistent-tree mechanism.
- Structural sharing is a discipline, not a library guarantee: return the original node when the
  subtree is unchanged and copy only changed ancestors. The measured reference sharing is real, but
  it is reference sharing over possibly mutable child collections, not deep immutability.
- Tree, DAG, and cycle meanings are explicit: reference identity by default; an optional caller
  comparer for DAG dedup; an ancestor repeat is a rejected cycle for a whole rewrite and a typed
  throw/stop for a traversal; a zipper assumes a tree/DAG and does not detect cycles.
- The product contract already excludes a general traversal API and optics hierarchy. This research
  does not justify changing that boundary.

Non-adoptions:

- No public tree API, adapter, path type, zipper, package, analyzer, grammar, product-contract,
  stability-inventory, or API-baseline change. `src/` is untouched.
- No replacement tree universe, recursion scheme, optics hierarchy, general AST optimizer,
  serializer, reflection/property-path discovery, or automatic persistence.
- No ambient tree cache, async tree API, cancellation, or scheduler.
- No claim that record structural equality is a deep DAG identity; the default is reference
  identity and caller-supplied equality is explicit. No claim that reference sharing implies deep
  immutability.
- No `ExpressionVisitor` measurement beyond assessment; it serves the `Expression` node universe,
  and a configuration conversion would not be a fair tree-mechanism comparison.
- No change to Goals 0036, 0039, or 0040 behavior; their shapes are references only.

Conditions for a later reconsideration:

1. A real application needs the same path-addressed edit, DAG identity, cycle diagnostics, and
   lazy location behavior across several unrelated node types, so the adapter and traversal core
   are reused rather than written once.
2. The generic traversal can offer an explicit no-cycle fast path or otherwise remove the active
   set from the common tree case, and the full rewrite can avoid a rewritten array per node.
3. A public shape can promote or replace the experimental `Location` and fit the product contract's
   existing traversal/optics exclusion through an accepted goal.
4. Measurements on that application show a complete-workflow allocation or code-size gain, not only
   a per-consumer line saving.

## Residual Risks And Open Questions

- The workload is synthetic though representative. Production configuration trees may be shallower
  (favoring recursion) or wider (changing child-list copying), and syntax/expression trees may have
  different node shapes.
- The prototype's generic traversal always maintains an active-path set for cycle detection, so the
  measured generic allocation includes that safety cost. A production generic library could offer a
  no-cycle fast path; the evidence does not claim the prototype is an optimized upper bound.
- `TreeRewrite.RewriteBottomUp` allocates a rewritten array for every visited node; a production
  implementation could use a lazy array or a child-count-aware buffer. The measured full rewrite is
  an honest prototype, not the best possible full rewrite.
- The zipper's immutable frame chain allocates a cursor and a frame per navigation step; a mutable
  cursor could reduce allocation but would change the navigation semantics and aliasing story.
- The `JsonNode` consumer converts back to typed nodes so the verifier can compare the complete
  typed result. An application that stops at JSON would avoid the conversion but would also lose
  typed validation and reference sharing; the measured cost is the complete typed task.
- `Location` is experimental (`FS0017`); a future public pattern or capability would need to promote
  or replace it. The prototype and tests use an explicit suppression.
- `Location` appends one segment at a time. An exploratory eager implementation built a location
  per resolved segment and allocated 120 GB and took 54-74 s on a 100,000-section path; the final
  code builds a location only on failure, and the successful deep path is O(depth). A failure at
  extreme depth still builds the location by appending the prefix (O(depth^2)); the evidence does
  not measure a 100,000-section failure. A future public shape should use a segment-list or
  rendered-path diagnostic for deep failures, or add a bulk `Location` constructor.
- Record structural equality over `IReadOnlyList` children is not a reliable deep DAG identity; the
  evidence pins the shallow behavior and uses reference identity by default. A caller that supplies
  structural equality accepts merging distinct equal nodes and must not use it on a cycle.
- Timing is directional on this shared host; allocation is the blocking comparison. No trim, Native
  AOT, package-consumer, or multi-OS evidence is claimed because no shipping package was produced.
- The child-process depth probe observes the default main-thread stack of this host; another stack
  size or runtime changes the exact overflow depth, not the direction.

## Gate Evidence

Final gate results for this working tree:

- `dotnet fsi build.fsx -- -p build`: exit 0; 0 warnings, 0 errors (6:26.73).
- `dotnet fsi build.fsx -- -p test`: PENDING.
- `dotnet fsi build.fsx -- -p format`: PENDING.
- `dotnet fsi build.fsx -- -p verify-docs-snippets`: PENDING.
- `dotnet fsi build.fsx -- -p verify-api-baseline`: PENDING.
- `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release`: PENDING.
- Measurement harness, run 1 at 2026-11-08 16:16:46 UTC: exit 0; 765 parity checks; tables above.
- Measurement harness, run 2 at 2026-11-08 16:17:11 UTC: exit 0; 765 parity checks; tables above.

Run the harness with:

```shell
dotnet run --project tests/FunnySharp.TreeTransformationsMeasurements/FunnySharp.TreeTransformationsMeasurements.csproj -c Release
```

No commit, push, or history rewrite was performed. The working tree is left for review. No Goal
0042 work started.
