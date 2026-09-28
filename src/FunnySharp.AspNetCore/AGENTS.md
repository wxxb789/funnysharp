# FunnySharp.AspNetCore

**Why this file:** score 22 — separately packaged ASP.NET Core integration surface; distinct domain (HTTP result mapping).

## OVERVIEW
Single type `HttpResultExtensions`: ~21 overloads mapping `Option`/`Result`/`UnitResult`/`Validation` (plus async and `TEnvironment`-scoped effect variants) to `IResult` / `Results.*` / `ProblemDetails`.

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Everything | `HttpResultExtensions.cs` (536 lines — the only source file) |
| Behavioral coverage | `tests/FunnySharp.AspNetCore.Tests` (TestHost + Kestrel) |

## CONVENTIONS
- `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, not a PackageReference.
- Packable as its own nupkg (`FunnySharp.AspNetCore`), version-locked to core; README packed from repo root; snupkg + package validation like core.
- Release gate (`eng/Verify-Release.ps1`) enforces: exactly one net10.0 dependency group, FunnySharp as the only package dependency at the matching version, AspNetCore.App as the only framework reference.
- ILLink.Tasks pinned with `PrivateAssets=all` so locked restores evaluate identically on every SDK patch.
- `ToHttpResult` / `ToHttpResultAsync` naming; `Task<IResult>` and `ValueTask<IResult>` variants both exist — keep them paired.
- Failure mapping produces `ProblemDetails` (validation failures map to `HttpValidationProblemDetails`); success mapping defaults to `Results.Ok` unless a `some`/map function is supplied.

## COMMANDS
```bash
dotnet build src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj -c Release   # build
dotnet pack src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj -c Release  # pack nupkg + snupkg
dotnet test tests/FunnySharp.AspNetCore.Tests/FunnySharp.AspNetCore.Tests.csproj # behavioral coverage
```

## ANTI-PATTERNS
- Core must not depend on ASP.NET Core (goal 0001) — the dependency arrow only points this way.
- No overloads that break the `...Async` naming grammar or the XML doc contract style used across the repo.
