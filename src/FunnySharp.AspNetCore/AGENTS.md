# FunnySharp.AspNetCore

**Why this file:** score 22 — separately packaged ASP.NET Core integration surface; distinct domain (HTTP result mapping).

## OVERVIEW
Two public static classes. `HttpResultExtensions` has ~21 overloads mapping `Option`/`Result`/`UnitResult`/`Validation` (plus async and `TEnvironment`-scoped effect variants) to `IResult` / `Results.*` / `ProblemDetails`. `TypedHttpResultExtensions` has 24 overloads mapping the same four carriers (sync, `Task`, and `ValueTask` sources) to caller-selected `Results<...>` unions.

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Existing `IResult` mapping | `HttpResultExtensions.cs` (664 lines) |
| Typed result unions and metadata | `TypedHttpResultExtensions.cs` (24 overloads; shared sync/Task/ValueTask cores) |
| Behavioral coverage | `tests/FunnySharp.AspNetCore.Tests` (TestHost + Kestrel; typed tests in `TypedHttpResult*.cs`) |

## CONVENTIONS
- `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, not a PackageReference.
- Packable as its own nupkg (`FunnySharp.AspNetCore`), version-locked to core; README packed from repo root; snupkg + package validation like core.
- Package consumer checks (`dotnet fsi build.fsx -- -p compatibility`, `eng/harness/Compatibility.fs`) enforce:
  exactly one net10.0 dependency group, FunnySharp as the only package dependency at the matching version, AspNetCore.App as the only framework reference.
- ILLink.Tasks pinned with `PrivateAssets=all` so locked restores evaluate identically on every SDK patch.
- `ToHttpResult` / `ToHttpResultAsync` naming; `Task<IResult>` and `ValueTask<IResult>` variants both exist — keep them paired.
- `ToHttpResults` / `ToHttpResultsAsync` return `Results<...>`; default success is `Ok<T>` (`NoContent` for `UnitResult`), and callers select every other typed result. Keep the union in the return type; never erase it to `IResult`.
- Failure mapping produces `ProblemDetails` (validation failures map to `HttpValidationProblemDetails`); success mapping defaults to `Results.Ok` unless a `some`/map function is supplied.
- Dynamic `ProblemHttpResult` statuses contribute no metadata; callers declare them with `.Produces*` or use status-specific typed results. Do not synthesize declarations from `ProblemDetails.Status`.

## COMMANDS
```bash
dotnet build src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj -c Release   # build
dotnet pack src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj -c Release  # pack nupkg + snupkg
dotnet test tests/FunnySharp.AspNetCore.Tests/FunnySharp.AspNetCore.Tests.csproj # behavioral coverage
```

## ANTI-PATTERNS
- Core must not depend on ASP.NET Core (goal 0001) — the dependency arrow only points this way.
- No overloads that break the `...Async` naming grammar or the XML doc contract style used across the repo.
- Do not add middleware, DI registration, a global exception policy, or a serializer to either package.
