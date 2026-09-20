# Stability Inventory

FunnySharp ships a stable 0.1.0 surface plus a tracked set of experimental members. Every
experimental member carries `[Experimental("FS####")]` with the diagnostic ID recorded here, an
entry in this inventory, no compatibility promise, and removal without a breaking-change process
([stability boundary](next-stage/api-decisions.md)). Stable members change only through a later
accepted goal.

Consumers opt into an experimental member by suppressing its diagnostic ID
(`#pragma warning disable FS####` or `NoWarn`); the diagnostic makes the experimental status
visible at every use site.

| Diagnostic ID | Member | Status | Entered | Owner goal | Notes |
| --- | --- | --- | --- | --- | --- |
| FS0017 | `Location` (src/FunnySharp/Location.cs) | Experimental | 2026-09-20 | Goal 17 | Compositional traversal location context (decision G6: adopt experimental first; E44). |
| FS0017 | `EnumerablePipelineExtensions`.`Traverse` located list overloads (Option/Result/UnitResult/Validation) | Experimental | 2026-09-20 | Goal 17 | Selector receives `root.At(index)`. |
| FS0017 | `EnumerablePipelineExtensions`.`Traverse` located dictionary overloads | Experimental | 2026-09-20 | Goal 17 | Selector receives `root.Key(key)` plus key and value. |
| FS0017 | `AsyncSequenceExtensions`.`TraverseAsync`/`TraverseValueAsync` located overloads | Experimental | 2026-09-20 | Goal 17 | Async located traversal; token forwarded to the enumerator. |
