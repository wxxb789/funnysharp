# Next-stage goal contracts

These goals capture the maintainer-accepted direction discussed in October 2026: pragmatic functional and type-directed programming that reduces complete consumer code, exposes boundary cases and can reduce actual execution work. The repository baseline inspected for this proposal is [473648f](https://github.com/wxxb789/funnysharp/commit/473648f15336cf12bece8f48aef8469545957e51).

Each numbered file is an independently addressable outcome contract, written using the attached Define Goal skill and the existing `/goal` plus single-paragraph format. This index is navigation and provenance; the entire paragraph in the linked file is the completion authority. Creating or merging these documents does not activate or complete any goal.

Read [Core Design](../core-design.md) for the shared design intent when pursuing a goal. It guides abstraction and engineering choices across the series; each goal retains its own completion contract, and prospective capabilities remain distinct from shipped behavior.

## Completed And Archived

| Goal | Outcome | Completion evidence |
| --- | --- | --- |
| [0024](archive/0024-goal.md) | Independent fail-closed audit of Goals 14-23 | [final resolution report](../audits/goal-24-resolution/report.md), [evidence index](../audits/goal-24-resolution/final-evidence-index.md) |
| [0025](archive/0025-goal.md) | Resolution of all 69 original audit findings at the frozen `d8744` candidate | [final ledger](../audits/goal-24-resolution/findings.json), [report](../audits/goal-24-resolution/report.md) |
| [0026](archive/0026-goal.md) | Composable decoding into trusted domain values | [decoder evidence](../decoder-evidence.md), [compiling example](../../examples/FunnySharp.Examples/DecoderExamples.cs) |

The 0024/0025 final ledger records 69 `ACCEPTED_VERIFIED` rows, 0 open, the original 40 FAIL/29 UNVERIFIED origins preserved, and an independent whole-goal gate recommendation of APPROVE at product `d8744c934d86833f9817245ecc7c78b1177b8b76`. Goal 0026 is verified by the decoder tests, three-way consumer comparison, and allocation/timing evidence.

## Next-Stage Goals

| Goal | Outcome | Kind |
| --- | --- | --- |
| [0027](0027-goal.md) | Interpretable contracts and honest derivation | Product outcome |
| [0028](0028-goal.md) | Validated, inspectable partial changes | Product outcome |
| [0029](0029-goal.md) | Composable batched reads | Product outcome |
| [0030](0030-goal.md) | Domain invariants and composable typed errors | Product outcome |
| [0031](0031-goal.md) | Typed ASP.NET Core boundaries | Product outcome |
| [0032](0032-goal.md) | Reusable checks with explicit failure policy | Product outcome |
| [0033](0033-goal.md) | Short-circuiting fallible reduction | Product outcome |
| [0034](0034-goal.md) | Composable one-pass summaries | Product outcome |
| [0035](0035-goal.md) | Explicit cardinality and constrained joins | Product outcome |
| [0036](0036-goal.md) | Typed nested data reshaping and provenance | Product outcome |
| [0037](0037-goal.md) | Composable domain programs and deterministic simulation | Product outcome |
| [0038](0038-goal.md) | Bounded concurrency with per-key ordering | Product outcome |
| [0039](0039-goal.md) | Feasibility of compile-time fused execution | Research outcome |
| [0040](0040-goal.md) | Feasibility of incremental domain computation | Research outcome |
| [0041](0041-goal.md) | Feasibility of reusable tree transformations | Research outcome |
| [0042](0042-goal.md) | Feasibility of inspectable named operation plans | Research outcome |

The numbers provide a useful reading and selection order, not a requirement to finish every earlier goal before an independent outcome can be pursued. Each file names material relationships where relevant. Supply a selected file's contents to the receiving agent one at a time; planning and execution belong to that agent and its harness. Reassess a distant research goal against the capabilities and priorities present when it is selected. Research completion establishes a supported decision, including a negative decision where warranted; it does not establish that a product feature shipped. The product outcomes retain their stated acceptance conditions.

The completed [0024](archive/0024-goal.md), [0025](archive/0025-goal.md), and [0026](archive/0026-goal.md) contracts are archived unchanged with the evidence linked above. The new directions deliberately permit focused reconsideration of earlier defer/reject decisions, such as inspectable changes, scope-local batched reads and incremental computation. A capability change must reconcile the affected current [product contract](../product-contract.md), [grammar](../grammar.md) and [stability inventory](../stability-inventory.md) within that goal's scope, retaining historical evidence and rationale. These prospective goals do not silently redefine the behavior of the currently shipped API.

The shared product constraints are BCL interoperability, optional immutability, meaningful sync/async use, explicit error and cancellation semantics, a thin I/O boundary and no new core runtime dependency. Performance and expression claims concern complete, semantically equivalent consumer workflows, including adapters and type declarations. Generated or derived artifacts are not independent proof of the business specification. There are no invented speedup percentages, LOC targets or completion deadlines. Maintainer judgment and the concrete evidence in each contract determine acceptance.

Validation follows the repository's current AGENTS.md: relevant evidence belongs to the changed behavior; heavy suites, evaluation cohorts, platform checks and benchmarks require the existing explicit selection. These files add no global audit, new CI gate, source fingerprint scheme or mandatory release ceremony. Goal identity and later evidence use Git revisions.

## Design references

These are selective primary references for the agreed direction, not dependencies or feature-parity checklists. The examples and API spellings in the conversation were exploratory and are not frozen by these contracts.

- [Clojure spec](https://clojure.org/guides/spec), [MoonBit derivation](https://docs.moonbitlang.com/en/stable/language/derive.html) and [FsCheck](https://fscheck.github.io/FsCheck/QuickStart.html): interpretable constraints, generated inputs and shrinking (0027).
- [Ecto.Changeset](https://hexdocs.pm/ecto/Ecto.Changeset.html), [ReScript optional fields](https://rescript-lang.org/docs/manual/record/) and [null/undefined interoperation](https://rescript-lang.org/docs/manual/null-undefined-option/): precise partial changes (0028).
- [Elixir Dataloader](https://hexdocs.pm/dataloader/Dataloader.html) and [.NET DataLoader](https://chillicream.com/docs/hotchocolate/fetching-data/batching/dataloader): batched data requirements and existing alternatives (0029).
- [Scala opaque types](https://docs.scala-lang.org/scala3/reference/other-new-features/opaques.html), [OCaml modules](https://ocaml.org/docs/modules) and [MoonBit error handling](https://docs.moonbitlang.com/en/stable/language/error-handling.html): domain boundaries and typed composition, with host-language limits (0030).
- [ASP.NET Core response types](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0) and [CSharpFunctionalExtensions Check](https://github.com/vkhorikov/CSharpFunctionalExtensions/blob/master/CSharpFunctionalExtensions/Result/Methods/Extensions/Check.cs): compare existing idioms before adding an adapter or operation (0031-0032).
- [Rust Iterator::try_fold](https://doc.rust-lang.org/std/iter/trait.Iterator.html#method.try_fold) and [Haskell foldl](https://github.com/Gabriella439/foldl): source short-circuiting and composable reductions (0033-0034).
- [dplyr join relationships](https://dplyr.tidyverse.org/reference/mutate-joins.html), [tidyr rectangling](https://tidyr.tidyverse.org/articles/rectangle.html) and [aligned unnesting](https://tidyr.tidyverse.org/reference/unnest_longer.html): explicit shape and cardinality contracts (0035-0036).
- [Elm commands and subscriptions](https://guide.elm-lang.org/effects/) and [Cmd implementation](https://github.com/elm/core/blob/master/src/Platform/Cmd.elm): composable domain decisions and execution boundaries (0037).
- [GenStage](https://hexdocs.pm/gen_stage/GenStage.html), [Task.Supervisor](https://hexdocs.pm/elixir/Task.Supervisor.html) and [TPL Dataflow](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/dataflow-task-parallel-library): bounded coordination and the limits of translating BEAM behavior to .NET (0038).
- [Chez Scheme syntactic extension](https://cisco.github.io/ChezScheme/csug10.0/syntax.html), [Nanopass](https://nanopass.org/), [Clojure transducers](https://clojure.org/reference/transducers), [Roslyn source generators](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.md) and [Hyperlinq](https://github.com/NetFabric/NetFabric.Hyperlinq): specialization, semantic preservation and comparison baselines (0039).
- [Jane Street Incremental](https://github.com/janestreet/incremental), its [introduction](https://blog.janestreet.com/introducing-incremental/) and [DynamicData](https://github.com/reactivemarbles/DynamicData): self-adjusting calculations and existing .NET alternatives (0040).
- [Clojure zipper](https://clojure.github.io/clojure/clojure.zip-api.html) and [Ecto.Multi](https://hexdocs.pm/ecto/Ecto.Multi.html): bounded investigations of tree transformations and inspectable operations (0041-0042).
