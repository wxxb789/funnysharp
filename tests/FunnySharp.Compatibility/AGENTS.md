# FunnySharp.Compatibility

**Why this file:** score 9, distinct domain — smoke tests built against PACKED nupkgs (trim/AOT), outside `FunnySharp.slnx`; nothing else validates published artifacts.

## OVERVIEW
The compatibility suite: one executable smoke project per package surface, orchestrated by the harness `compatibility` pipeline in `eng/harness/Compatibility.fs`.

## STRUCTURE
```
FunnySharp.Compatibility/
├── FunnySharp.Compatibility.Core/         # core smoke (Program.cs)
└── FunnySharp.Compatibility.AspNetCore/   # web smoke (Program.cs)
```
The `Run-Compatibility.ps1` orchestrator was replaced by `eng/harness/Compatibility.fs`.

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Orchestration, scenarios, safety checks | `eng/harness/Compatibility.fs` via `dotnet fsi build.fsx -- -p compatibility` (CoreTrimmed, CoreNativeAot, AspNetCoreTrimmed, AspNetCoreNativeAot) |
| Web-surface asserts | `FunnySharp.Compatibility.AspNetCore/Program.cs` (`AssertStatusAsync`, `ProblemDetails` helpers, source-gen `CompatibilityJsonContext`) |

## CONVENTIONS
- Package versions injected as MSBuild props `$(FunnySharpPackageVersion)` / `$(FunnySharpAspNetCorePackageVersion)` by the script — never hardcode them.
- SDK: `Microsoft.NET.Sdk.Web` in the AspNetCore smoke project only; plain `Microsoft.NET.Sdk` in the Core smoke. Both: `EnableTrimAnalyzer` / `EnableAotAnalyzer=true`, `IsPackable=false`; `<TrimmerRootAssembly>` gated on `'$(RootShippingAssemblies)' == 'true'`.
- Each `Program.cs` ends with `Console.WriteLine("... compatibility smoke passed.")` — the orchestrator depends on it.
- Not in `FunnySharp.slnx` — never add these projects to the solution.
- Run: `dotnet fsi build.fsx -- -p compatibility -PackageDirectory <dir-with-nupkgs> -OutputDirectory <repo>/artifacts/<sub>`; exits 1 on any failure, writes `compatibility-results.json`.

## ANTI-PATTERNS
- Never point these projects at ProjectReferences — they exist to validate packed artifacts only.
- `-OutputDirectory` must be inside `<repo>/artifacts` (script asserts; do not relax).
