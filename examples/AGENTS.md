# examples/ — dogfood example projects

**Earned:** score ~9 — distinct domain (three example projects; hub covers
`FunnySharp.AspNetCore.Examples` inline, score ~6).

## OVERVIEW

Three dogfood example projects: executable API examples, the docs-snippet source of truth,
and a minimal-API app for the ASP.NET Core bridges. All are in `FunnySharp.slnx`.

## WHERE TO LOOK

| Project | Role | Own file |
| --- | --- | --- |
| `FunnySharp.Examples/` | Compiling examples of the public core API; release-protocol step `examples` | `FunnySharp.Examples/AGENTS.md` |
| `FunnySharp.DocumentationSamples/` | Source of truth for docs code snippets (44 regions) | `FunnySharp.DocumentationSamples/AGENTS.md` |
| `FunnySharp.AspNetCore.Examples/` | Minimal-API dogfood app for the ASP.NET Core bridges | covered below |

## FunnySharp.AspNetCore.Examples (inline)

- `Program.cs` (205 LOC) maps 7 minimal-API endpoints exercising every
  `FunnySharp.AspNetCore` bridge: `Option.ToHttpResult`, `Result`/`UnitResult.ToHttpResult`,
  `Validation.ToHttpResult` (→ `HttpValidationProblemDetails`), `ToHttpResultAsync`,
  `Effect.ToHttpResultAsync`. Failures map to `ProblemDetails` (404/409/400).
- `--verify` prints `FunnySharp ASP.NET Core example endpoints mapped.` and exits without
  running the host — that is the release-protocol step `aspnetcore-examples`.
- Deviates from ASP.NET defaults: plain `Microsoft.NET.Sdk` + `FrameworkReference
  Microsoft.AspNetCore.App`, NOT `Microsoft.NET.Sdk.Web`.

## CONVENTIONS (apply to all three projects)

- All example projects reference the analyzer projects as `OutputItemType="Analyzer"`
  `PrivateAssets="all"` — every example must build with ZERO FunnySharp analyzer
  diagnostics (dogfooding the shipped analyzer package).
- Success is an exact stdout marker, not just exit code 0.
- All three are in `FunnySharp.slnx`; `IsPackable=false`; `TreatWarningsAsErrors` applies.

## ANTI-PATTERNS

- NEVER let an example build with a FunnySharp (FS####) analyzer diagnostic — the analyzer
  references make that a build failure; never suppress one to work around it.
- NEVER edit a guide's fenced `csharp` block without updating the matching
  `// <snippet ...>` region in `FunnySharp.DocumentationSamples` — the parity check is
  byte-exact (every line, dedented) and fails on whitespace drift.
- NEVER assert success by exit code alone — each project's contract is an exact stdout marker.

## COMMANDS

```bash
dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release
dotnet run --project examples/FunnySharp.AspNetCore.Examples -c Release -- --verify
pwsh -NoProfile -File examples/FunnySharp.DocumentationSamples/VerifyDocumentationSnippets.ps1
```
