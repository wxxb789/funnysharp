# Call-site code copies (Goal 14)

Verbatim copies of the scratch projects used to compile `docs/next-stage/call-sites.md`.
They are evidence, not a maintained sample: project-reference paths and the NuGet feed
path are machine-specific.

## Layout

| Directory | Project | Content |
| --- | --- | --- |
| `idiomatic/` | `Idiomatic.csproj`, BCL only | W1–W10 straightforward C# baselines |
| `funnysharp/` | `FunnySharpCallSites.csproj`, `ProjectReference` to `src/FunnySharp` | W1–W10 FunnySharp 0.1.0 variants |
| `funnysharp-aspnet/` | `FunnySharpAspNetCallSites.csproj`, `ProjectReference` to `src/FunnySharp.AspNetCore` + `Microsoft.AspNetCore.App` framework reference | W11 HTTP mapping, both variants |
| `competitors/` | `Competitors.csproj`, NuGet CFE 3.7.0 + Funcky 3.6.0 + LanguageExt.Core 4.4.9 | W1, W2, W3, W3b, W4, W5, W10 competitor variants |
| `tools/` | Python 3 | `loc.py` method extractor, `rawloc.py` raw-LOC report |

## Rebuild

1. Adjust the `ProjectReference` paths in `funnysharp/*.csproj` and
   `funnysharp-aspnet/*.csproj` to your checkout (the copies contain
   `/home/azureuser/repos/funnysharp/...`).
2. Point `NuGet.config` at a folder containing the pinned nupkgs, or remove the `local`
   source and let `competitors` restore from nuget.org:
   `csharpfunctionalextensions.3.7.0.nupkg` (SHA256
   `3a7c3d5975d883a2d55b88a0786ce4f7f29f7c0f092dcbd9a245bcf8959723c8`),
   `funcky.3.6.0.nupkg`
   (`1a7ab6c6595a2f4d3bdfa83768f9054450beaffd322817644f3dea65940cc32f`),
   `languageext.core.4.4.9.nupkg`
   (`633636d9d9cb9be75581910b503fb1aac54181a10a6b8cfe9d0a3f0118347437`).
3. Build with SDK 10.0.400:

```bash
export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$DOTNET_ROOT:$PATH"
dotnet build -c Release   # in each project directory
```

The verified run produced 0 warnings and 0 errors for all four projects. Raw LOC counts
in the document come from `tools/rawloc.py`, run from this directory:

```bash
python3 tools/rawloc.py
```
