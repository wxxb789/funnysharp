# docs/next-stage/call-sites-code/ — verbatim call-site scratch projects

Earned: score ~18 — 5 project subtrees + tools; the compiled evidence behind `docs/next-stage/call-sites.md`.

## OVERVIEW

Verbatim scratch projects — idiomatic C#, FunnySharp, ASP.NET Core, and competitor variants — that were compiled to produce `call-sites.md`; evidence, not samples.

## LAYOUT

| Directory | Project | Content |
|-----------|---------|---------|
| `idiomatic/` | BCL only | W1-W10 straightforward C# baselines |
| `funnysharp/` | ProjectReference `src/FunnySharp` | W1-W10 FunnySharp variants |
| `funnysharp-aspnet/` | ProjectReference `src/FunnySharp.AspNetCore` | W11 HTTP mapping, both variants |
| `competitors/` | NuGet CFE 3.7.0 + Funcky 3.6.0 + LanguageExt.Core 4.4.9 | W1-W5, W3b, W10 competitor variants |
| `tools/` | — | retired: `loc.py` and `rawloc.py` are now `eng/harness/Loc.fs`, run as `dotnet fsi build.fsx -- -p rawloc` / `-- -p loc-extract` |

Root: `README.md` (rebuild steps), `NuGet.config` (local feed for the pinned competitor nupkgs).

## REBUILD

1. Adjust the `ProjectReference` paths in `funnysharp/*.csproj` and `funnysharp-aspnet/*.csproj` to your checkout.
2. Point `NuGet.config` at a folder with the pinned nupkgs (SHA256s in `README.md`) or remove the `local` source.
3. Build per project with SDK 10.0.400:

```bash
export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$DOTNET_ROOT:$PATH"
dotnet build -c Release
```

The verified run produced 0 warnings and 0 errors for all four projects.

## CONVENTIONS

- These are VERBATIM copies of the scratch projects used to compile `call-sites.md` — evidence, not maintained samples. `ProjectReference` paths are absolute and machine-specific (`/home/azureuser/repos/funnysharp/...`); adjust before rebuilding. `README.md` lists the pinned nupkg SHA256s.
- Workflow tags: `W1`..`W11` in comments; Goal 15/16/19 files use `WF-*` identifiers and reference `AD-*` decisions.
- None of the projects are in `FunnySharp.slnx` or the release pipeline; `competitors/` is never shipped.
- `rawloc` counts non-blank, non-`//` lines per method and runs from the repository root: `dotnet fsi build.fsx -- -p rawloc` (the ported `eng/harness/Loc.fs`).

## ANTI-PATTERNS

- NEVER refactor, rename, or "fix" code here — the verbatim property IS the evidence; a change invalidates `call-sites.md` and its review.
- NEVER add these projects to the solution or any release artifact.
- Do not treat `competitors/` code as API guidance — it is comparison evidence against pinned versions only.
