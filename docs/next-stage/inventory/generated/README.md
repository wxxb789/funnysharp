# Generated Next-Stage Inventories

These files are machine-generated evidence for Goal 14. Do not hand-edit them.

They were produced by `eng/next-stage-inventory/` from the version- or
commit-pinned inputs recorded in [`../../baselines.md`](../../baselines.md).
Regenerate the canonical set with `eng/next-stage-inventory/generate.sh`.

Generated dumps list the public/protected surface including operators; nullable
reference annotations appear in runtime-mode dumps only. The `.md` dumps and the count
tables below are tracked; the `.json` machine dumps are not tracked (they are larger and
fully regenerable with `generate.sh`). The independent audit ran its member-coverage and
count checks against the working-tree JSON at the pinned state.

## Canonical dumps

| File | Input | Mode |
| --- | --- | --- |
| `inv-funny-sharp-core.*` | `FunnySharp.dll` 0.1.0 build at commit `4dbebd9` | runtime |
| `inv-funny-sharp-aspnetcore.*` | `FunnySharp.AspNetCore.dll` 0.1.0 build at commit `4dbebd9` | runtime |
| `inv-bcl-sequences-linq.*` | `System.Linq`, `System.Linq.AsyncEnumerable`, `System.Runtime`, `System.Collections`, `System.Memory`, `System.Buffers`, `System.Threading.Tasks.Extensions` ref assemblies | metadata |
| `inv-bcl-collections-immutable.*` | `System.Collections.Immutable`, `System.Collections`, `System.Collections.Concurrent`, `System.Linq` ref assemblies | metadata |
| `inv-bcl-async-concurrency.*` | `System.Runtime`, `System.Threading.Channels`, `System.Threading.Tasks.Parallel`, `System.Threading.Tasks`, `System.Threading` ref assemblies | metadata |
| `inv-bcl-language-errors.*` | `System.Runtime`, `System.Runtime.Extensions`, `System.Linq.Expressions` ref assemblies | metadata |
| `inv-fsharp-core.*` | FSharp.Core 10.1.401 (`lib/netstandard2.1`) | runtime |
| `inv-funcky.*` | Funcky 3.6.0 (`lib/net10.0`) | runtime |
| `inv-funcky-analyzers.*` | Funcky 3.6.0 `Funcky.BuiltinAnalyzers.dll` | metadata |
| `inv-csharpfunctionalextensions.*` | CSharpFunctionalExtensions 3.7.0 (`lib/net8.0`) | runtime |
| `inv-language-ext.*` | LanguageExt.Core 4.4.9 (`lib/netstandard2.0`), top-level `LanguageExt` namespace | metadata |
| `types-languageext-4.4.9.txt` | LanguageExt.Core 4.4.9 | type-name list |

## Supplementary dumps

These extend the canonical set for families cited by the analyses. They were
generated during the survey with the same tool. Rows marked *(pre-operator)* were
generated before operator support was added to the tool, so operator members are
absent there; the canonical dumps are authoritative for operator evidence, and the
language-ext family dumps were regenerated with operator support after the audit.

| File | Input | Mode |
| --- | --- | --- |
| `inv-fsharp-core-focus.*` | FSharp.Core 10.1.401 focused re-dump (96 types; `<Type>Module` names corrected) | runtime |
| `inv-bcl-sse.*` *(pre-operator)* | BCL `System.Net.ServerSentEvents` | metadata |
| `inv-bcl-supplement.*` *(pre-operator)* | BCL Stopwatch/GC/AsyncLocal/IServiceProvider/trim/JSON supplement | metadata |
| `inv-bcl-supplement-f2f3f11.*` *(pre-operator)* | BCL F2/F3/F11 supplement (DataAnnotations/ExceptionDispatchInfo/Unsafe) | metadata |
| `inv-aspnetcore-http.*` *(pre-operator)* | `Microsoft.AspNetCore.Http` result surface 10.0.11 | metadata |
| `inv-language-ext-toplevel.*` | LanguageExt.Core 4.4.9 all-family dump | metadata |
| `inv-language-ext-globalns.*` *(pre-operator)* | LanguageExt.Core 4.4.9 global-namespace extension classes | metadata |
| `inv-language-ext-typeclasses.*` | LanguageExt.Core 4.4.9 typeclass/ClassInstances machinery | metadata |
| `inv-language-ext-pipes.*` | LanguageExt.Core 4.4.9 pipes | metadata |
| `inv-language-ext-effects.*` *(pre-operator)* | LanguageExt.Core 4.4.9 effect types | metadata |
| `inv-language-ext-v5-toplevel.*` *(pre-operator)* | `~/repos/language-ext` HEAD `2f0e362`, **unreleased v5 development line** | runtime |
| `inv-language-ext-v5-traits.*` *(pre-operator)* | v5 development traits/`K<M,A>` machinery | runtime |

Type and member counts at the pinned survey state (latest regeneration):

| Dump | Types | Members | Operators | Extension members |
| --- | ---: | ---: | ---: | ---: |
| funny-sharp-core | 33 | 254 | 8 | 98 |
| funny-sharp-aspnetcore | 1 | 15 | 0 | 15 |
| csharpfunctionalextensions | 42 | 1872 | 31 | 1664 |
| funcky | 85 | 1156 | 21 | 760 |
| funcky-analyzers | 2 | 6 | 0 | 0 |
| fsharp-core (filtered) | 102 | 1625 | 81 | 0 |
| fsharp-core-focus | 96 | 1621 | 81 | 0 |
| language-ext (top-level namespace) | 159 | 6745 | 115 | 2939 |
| language-ext-toplevel | 339 | 10966 | 736 | 2939 |
| language-ext-globalns *(pre-operator)* | 49 | 2555 | 0 | 2544 |
| language-ext-typeclasses | 85 | 610 | 34 | 32 |
| language-ext-pipes | 63 | 1050 | 38 | 141 |
| language-ext-effects *(pre-operator)* | 1 | 3 | 0 | 0 |
| language-ext-v5-toplevel *(pre-operator)* | 444 | 9559 | 0 | 1356 |
| language-ext-v5-traits *(pre-operator)* | 150 | 795 | 0 | 33 |
| bcl-sequences-linq | 73 | 1502 | 17 | 680 |
| bcl-collections-immutable | 55 | 843 | 4 | 52 |
| bcl-async-concurrency | 86 | 919 | 12 | 8 |
| bcl-language-errors | 127 | 1956 | 52 | 70 |
| bcl-sse *(pre-operator)* | 5 | 18 | 0 | 0 |
| bcl-supplement *(pre-operator)* | 27 | 359 | 0 | 25 |
| bcl-supplement-f2f3f11 *(pre-operator)* | 10 | 104 | 0 | 0 |
| aspnetcore-http *(pre-operator)* | 11 | 223 | 0 | 0 |

`baselines.md` records how the 102-type `fsharp-core` filter works; `bcl.md` and
`fsharp-core.md` explain why the supplementary and focus dumps exist.
