# FunnySharp.Compatibility

**Why this file:** score 9, distinct domain — smoke tests built against PACKED nupkgs (trim/AOT), outside `FunnySharp.slnx`; nothing else validates published artifacts.

## OVERVIEW
PowerShell-orchestrated compatibility suite: one executable smoke project per package surface, run only via `Run-Compatibility.ps1`.

## STRUCTURE
```
FunnySharp.Compatibility/
├── Run-Compatibility.ps1                  # orchestrator (480 lines)
├── FunnySharp.Compatibility.Core/         # core smoke (Program.cs, 118 lines)
└── FunnySharp.Compatibility.AspNetCore/   # web smoke (Program.cs, 80 lines)
```

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Orchestration, scenarios, safety checks | `Run-Compatibility.ps1` (CoreTrimmed, CoreNativeAot, AspNetCoreTrimmed, AspNetCoreNativeAot) |
| Web-surface asserts | `FunnySharp.Compatibility.AspNetCore/Program.cs` (`AssertStatusAsync`, `ProblemDetails` helpers, source-gen `CompatibilityJsonContext`) |

## CONVENTIONS
- Package versions injected as MSBuild props `$(FunnySharpPackageVersion)` / `$(FunnySharpAspNetCorePackageVersion)` by the script — never hardcode them.
- `Microsoft.NET.Sdk.Web`, `EnableTrimAnalyzer` / `EnableAotAnalyzer=true`, `IsPackable=false`; `<TrimmerRootAssembly>` gated on `'$(RootShippingAssemblies)' == 'true'`.
- Each `Program.cs` ends with `Console.WriteLine("... compatibility smoke passed.")` — the orchestrator depends on it.
- Not in `FunnySharp.slnx` — never add these projects to the solution.
- Run: `pwsh tests/FunnySharp.Compatibility/Run-Compatibility.ps1 -PackageDirectory <dir-with-nupkgs> -OutputDirectory <repo>/artifacts/<sub>`; exits 1 on any failure, writes `compatibility-results.json`.

## ANTI-PATTERNS
- Never point these projects at ProjectReferences — they exist to validate packed artifacts only.
- `-OutputDirectory` must be inside `<repo>/artifacts` (script asserts; do not relax).
