# Next-Stage Baseline Pins

This document records the exact inputs surveyed for Goal 14 and how the inventories
were produced. It is the provenance anchor for every decision in the
[decision record](decision-record.md) and the [API decision matrix](api-decisions.md).

- Survey date: **2026-09-17**
- FunnySharp pin: commit `4dbebd94b7b58648632112b7ca47c39cc517f153` (`main`, package
  version 0.1.0). The assembly hashes below are reproducible only for a Release build of that
  exact commit (the informational version embeds `0.1.0+<commit-sha>` and the PDB path is
  embedded, so a rebuild of another commit does not reproduce the bytes even with
  `Deterministic=true`). Release build outputs:
  - `src/FunnySharp/bin/Release/net10.0/FunnySharp.dll` — SHA256
    `793c8532333776ed6dc2bada5457c264ab0c6eab8a150628fa54d5d67a9bf9d4`
  - `src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.dll` — SHA256
    `3810e1de6991830cbd374b273f03838cc04948cd48ff3dcbfb09e82321f77a42`
  - Build: `dotnet build FunnySharp.slnx -c Release`, .NET SDK 10.0.400, no warnings,
    no errors.
- Toolchain: .NET SDK **10.0.400**, host runtime **10.0.11**, reference pack
  `Microsoft.NETCore.App.Ref/10.0.11/ref/net10.0`, RID `linux-x64`, Ubuntu 24.04.

## External pins

| Baseline | Pin | SHA256 (`.nupkg`) | Asset used | Notes |
| --- | --- | --- | --- | --- |
| .NET / BCL | SDK 10.0.400, runtime 10.0.11, ref pack `10.0.11/ref/net10.0` | n/a (SDK install) | reference assemblies + XML docs | repo target is `net10.0`; ASP.NET Core surface comes from the `Microsoft.AspNetCore.App` 10.0.11 framework reference |
| [FSharp.Core](https://www.nuget.org/packages/FSharp.Core) | **10.1.401** | `cf1f4e69fc2d1351af9fa9923ac90ed466088730a00867cbe0eb1b3b2c68963e` | `lib/netstandard2.1/FSharp.Core.dll` + XML | latest 10.x stable line. NuGet also lists 11.0.100, which aligns with the .NET 11 toolchain, not the `net10.0` target; it is deliberately outside this baseline. 10.0.100 was downloaded only as a cross-check and is not surveyed. |
| [Funcky](https://www.nuget.org/packages/Funcky) | **3.6.0** | `1a7ab6c6595a2f4d3bdfa83768f9054450beaffd322817644f3dea65940cc32f` | `lib/net10.0/Funcky.dll` + XML, `analyzers/dotnet/cs/*`, `README.md` | ships Roslyn analyzers and code fixes as package assets; depends on `System.Text.Json` 5.0.2 and `System.Collections.Immutable` 1.7.1 (netstandard2.0 group). Local clone `~/repos/funcky` at `133ba5bac4cb57861a3b2fbea49a98f0aa9a953a` (2026-09-15) may be ahead of the 3.6.0 release; clone-only material is marked unreleased in the analysis. |
| [CSharpFunctionalExtensions](https://www.nuget.org/packages/CSharpFunctionalExtensions) | **3.7.0** | `3a7c3d5975d883a2d55b88a0786ce4f7f29f7c0f092dcbd9a245bcf8959723c8` | `lib/net8.0/CSharpFunctionalExtensions.dll` + XML | no local clone; upstream `vkhorikov/CSharpFunctionalExtensions` cited from the pinned release when reachable. |
| [LanguageExt.Core](https://www.nuget.org/packages/LanguageExt.Core) | **4.4.9** (stable) | `633636d9d9cb9be75581910b503fb1aac54181a10a6b8cfe9d0a3f0118347437` | `lib/netstandard2.0/LanguageExt.Core.dll` + XML | The historical `language-ext` package id no longer resolves on nuget.org; `LanguageExt.Core` is the current released package. The v5 line exists only as `5.0.0-beta-*` and is not a compatibility target. Local clone `~/repos/language-ext` at `2f0e3628242889774d4141960a35671a0280051f` (2026-07-29) tracks the **unreleased v5 development line**; it is surveyed separately and never treated as released. |

Package acquisition used the NuGet v3 flat-container endpoint
(`https://api.nuget.org/v3-flatcontainer/{id}/{version}/{id}.{version}.nupkg`) and
`unzip`/Python `zipfile` extraction. Published hashes above are of the downloaded
`.nupkg` files.

## Inventory generation

All inventories are generated from the actual assemblies with the repository's
inventory tool (`eng/next-stage-inventory/`, run instructions in its README):

```bash
dotnet run -c Release --project eng/next-stage-inventory -- \
  --assembly <path.dll> [--resolve-dir <dir>]... \
  [--include '<regex on full type name>'] [--exclude '<regex>'] \
  [--mode runtime|metadata] [--core-dir <ref-pack-dir>] \
  --out-md <path.md> --out-json <path.json> --title "<text>"
```

- `runtime` mode loads the assembly in a resolving `AssemblyLoadContext` and reports
  nullable reference annotations via `NullabilityInfoContext`.
- `metadata` mode uses `MetadataLoadContext`; it is required for reference
  assemblies and for packages whose transitive dependencies are not extracted, and
  it does not report nullability.
- Nullability in the runtime-mode dumps is a decoded annotation record, not a
  promise about behavior; the contract text in `docs/*.md` remains authoritative.

Generated, committed dumps live in `inventory/generated/`:

| Dump | Input | Mode | Filter |
| --- | --- | --- | --- |
| `inv-funny-sharp-core.md/.json` | `FunnySharp.dll` (pinned build) | runtime | none (33 types) |
| `inv-funny-sharp-aspnetcore.md/.json` | `FunnySharp.AspNetCore.dll` (pinned build) | runtime | none (1 type) |
| `inv-bcl-sequences-linq.md/.json` | `System.Linq`, `System.Linq.AsyncEnumerable`, `System.Runtime`, `System.Collections`, `System.Memory`, `System.Buffers`, `System.Threading.Tasks.Extensions` ref assemblies | metadata | LINQ, generic sequence interfaces, span/memory |
| `inv-bcl-collections-immutable.md/.json` | `System.Collections.Immutable`, `System.Collections`, `System.Collections.Concurrent`, `System.Linq` ref assemblies | metadata | immutable/frozen/concurrent collections |
| `inv-bcl-async-concurrency.md/.json` | `System.Runtime`, `System.Threading.Channels`, `System.Threading.Tasks.Parallel`, `System.Threading.Tasks`, `System.Threading` ref assemblies | metadata | Task/ValueTask, channels, cancellation, timers |
| `inv-bcl-language-errors.md/.json` | `System.Runtime`, `System.Runtime.Extensions`, `System.Linq.Expressions` ref assemblies | metadata | delegates, nullable, exceptions, CodeAnalysis attributes |
| `inv-fsharp-core.md/.json` | `FSharp.Core.dll` 10.1.401 | runtime | Option/ValueOption/Result/Choice/modules/Async/MailboxProcessor/TaskBuilder (102 types) |
| `inv-funcky.md/.json` | `Funcky.dll` 3.6.0 | runtime | none (85 types) |
| `inv-funcky-analyzers.md/.json` | `Funcky.BuiltinAnalyzers.dll` 3.6.0 | metadata (Roslyn assemblies from SDK `Roslyn/bincore`) | none (2 analyzer types) |
| `inv-csharpfunctionalextensions.md/.json` | `CSharpFunctionalExtensions.dll` 3.7.0 | runtime | none (42 types) |
| `inv-language-ext.md/.json` | `LanguageExt.Core.dll` 4.4.9 | metadata | types in the top-level `LanguageExt` namespace (159 types) |

Type inventory files used for selection:
`/tmp/opencode/evidence/types-*.txt` during the survey run. The sub-namespaces
`LanguageExt.ClassInstances`, `LanguageExt.TypeClasses`, `LanguageExt.Pipes`,
`LanguageExt.Common`, and `LanguageExt.Thunks` are recorded by type-name list only;
their counts and representative names are cited in the language-ext analysis as
evidence of the HKT/typeclass footprint and are not reproduced member-by-member.

## Regeneration

The survey inputs are third-party packages and local clones, not repository
artifacts. To regenerate:

1. Download the `.nupkg` files listed above and verify their SHA256.
2. Extract them to a local tree.
3. Build the FunnySharp Release assemblies from the pinned commit.
4. Run `uv run --no-project eng/tools/inventory.py <baseline-root> <ref-pack-dir> <output-dir>`
   (the orchestrator encodes the exact commands, filters, and output names).

Because package feeds are mutable, regeneration is only equivalent when the pin
hashes match. A regenerated dump that differs from a committed dump is a new
baseline and requires a new decision review.

## Deliberate limitations

- The .NET 11 preview/runtime surface is out of scope; FunnySharp targets `net10.0`.
- Funcky clone content newer than 3.6.0 and all language-ext v5 content are marked
  as unreleased development and are never cited as released behavior.
- Metadata-mode dumps carry no nullability annotations.
- Third-party repositories are cited at the commits recorded above; a moving
  branch was never treated as evidence.
