# FunnySharp.Tests

**Why this file:** score 31 — primary xUnit v3 suite (50 test files, ~17.7k LOC) for the core library.

## OVERVIEW
C# xUnit v3 behavioral tests for the FunnySharp core assembly. Attribute-driven only: `[Fact]` / `[Theory]` + `[InlineData]`; no collections, traits, or member/class data.

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Enumeration laziness asserts | `ProbeEnumerable.cs` (internal counting `IEnumerable<T>`) |
| ValueTask consumption asserts | `CountingValueTaskSource.cs` (also linked into AspNetCore.Tests) |
| Public-surface freeze | `AdvancedPatternCurationTests.cs` (exact member lists, forbidden names) |
| Package boundary | `PackageBoundaryTests.cs` (reflection: platform-only references) |
| Perf manifest shape | `PerformanceManifestTests.cs` (validates `eng/performance/*.json`) |
| Repo-root discovery | `../Shared/TestRepositoryRoot.cs` — Compile-include linked, not referenced |

## CONVENTIONS
- One `public sealed class` per file; class name == file name; suffix grammar `*Tests` / `*AsyncTests` / `*BoundaryTests` / `*InteropTests` / `*TraversalTests`.
- Test method names are long multi-clause prose — deliberate; do not shorten to `Method_Scenario_Expectation`.
- `Xunit` is a global using (csproj) — no per-file `using Xunit`.
- Classic asserts only (`Assert.Equal/True/Throws/ThrowsAsync/Same`); no FluentAssertions, Shouldly, or snapshots.
- Sync test methods are `public void`; async are `public async Task` — never `ValueTask`.
- Meta tests couple this project to repo structure (`docs/`, `eng/`) via `TestRepositoryRoot.Find()`.
- Helpers are `internal` standalone non-`*Tests` files (`ProbeEnumerable.cs`, `CountingValueTaskSource.cs`) or in-file nested classes; no shared test assembly.

## ANTI-PATTERNS
- No skipped tests: no `Skip=` on any `[Fact]`/`[Theory]`.
- Never re-introduce rejected vocabulary (deferred AD-6..AD-9 type names) or `Where` on carrier types — curation tests fail the build.
- A comparer observing a null item is a test invariant violation (throws), never a `None`.
