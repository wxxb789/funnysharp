# FunnySharp.DocumentationSamples/ — docs snippet source of truth

**Earned:** score ~8 — distinct domain (docs-guide snippet parity, byte-exact).

## OVERVIEW

The compiled source of every `csharp` code block in the 10 primary guides. Snippets are
extracted from this project and byte-compared against `docs/*.md` — edit here, then the doc.

## WHERE TO LOOK

| Need | Location |
| --- | --- |
| Snippet verifier | `eng/harness/DocsSnippets.fs` via `dotnet fsi build.fsx -- -p verify-docs-snippets` |
| Sample files | one per guide area: `AnalyzerSamples`, `AspNetCoreSamples`, `CollectionsSamples`, `ConcurrencySamples`, `EffectSamples`, `FunctionCompositionSamples`, `ImmutableUpdateSamples`, `StateMachineSamples`, `UnitResultSamples`, `ValidationSamples` |
| Region markers | `// <snippet DocumentationSamples.<Area>.<Name>>` … `// </snippet>` — 44 regions total; the marker is the consumed interface |
| Docs counterpart | fenced `csharp` blocks tagged `<!-- documentation-sample: ... -->` in the 10 primary guides |

## CONVENTIONS

- Snippets are extracted DEDENTED and compared byte-for-byte (every line) — whitespace and
  comment placement inside a region matter. A snippet must appear in exactly one guide;
  duplicate, unused, or missing regions all fail the verifier.
- Samples are `internal static` classes with `private static` snippet methods — code exists
  to be quoted, not called; the project builds (net10.0, `IsPackable=false`) mainly so it
  compiles and analyzes clean.
- Analyzer dogfooding applies: analyzer + code-fix projects referenced as analyzers, so the
  samples must build with zero FunnySharp diagnostics.

## ANTI-PATTERNS

- NEVER remove the `#pragma warning disable FS100x` blocks wrapping deliberate-misuse
  snippets in `AnalyzerSamples.cs` — each carries a "Deliberate misuse" comment; removing
  the pragma breaks the samples-clean invariant.
- NEVER edit a guide's fenced block without updating the matching region here (or the
  verifier fails the parity check).

## COMMANDS

```bash
dotnet build examples/FunnySharp.DocumentationSamples/FunnySharp.DocumentationSamples.csproj -c Release
dotnet fsi build.fsx -- -p verify-docs-snippets
```
