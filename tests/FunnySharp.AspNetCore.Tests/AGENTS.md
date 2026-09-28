# FunnySharp.AspNetCore.Tests

**Why this file:** score 22 — in-process ASP.NET Core suite (TestHost + real Kestrel) for `HttpResultExtensions`; distinct domain (HTTP semantics).

## OVERVIEW
xUnit v3 tests for the FunnySharp.AspNetCore package via `Microsoft.AspNetCore.TestHost` and real Kestrel; 3 test files, ~1.2k LOC.

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Result -> HTTP mapping (white-box) | `HttpResultExtensionsTests.cs` (676 lines) |
| UnitResult mapping | `UnitResultHttpResultExtensionsTests.cs` |
| Cancellation over a real server | `KestrelCancellationTests.cs` (nested `KestrelApplication : IAsyncDisposable`) |

## CONVENTIONS
- Links `tests/FunnySharp.Tests/CountingValueTaskSource.cs` into `TestSupport/` — keep that file's signature in sync when editing it on either side.
- Requires `FrameworkReference Microsoft.AspNetCore.App` and `Microsoft.AspNetCore.TestHost` 10.0.11.
- Test app hosts are private nested classes (`TestApplication`, `KestrelApplication`) implementing `IAsyncDisposable`.
- Same suite conventions as `tests/FunnySharp.Tests`: one `public sealed class` per file, prose test names, classic asserts, no skips.
- No new test-project dependencies beyond `Microsoft.AspNetCore.TestHost` + framework — keep the dependency group the release gate checks.

## COMMANDS
```bash
dotnet test tests/FunnySharp.AspNetCore.Tests/FunnySharp.AspNetCore.Tests.csproj
dotnet test tests/FunnySharp.AspNetCore.Tests/FunnySharp.AspNetCore.Tests.csproj -c Release
```

## ANTI-PATTERNS
- No `WebApplicationFactory` / extra host indirection — tests hit `IResult` mapping and status codes directly via `AssertStatusAsync`-style helpers.
- Do not add a project reference to other test projects; shared source moves by Compile-include link only.
