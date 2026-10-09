# FunnySharp

[English](README.md) | [简体中文](README_zh-cn.md)

FunnySharp 是一个面向 .NET 10 的实用主义、BCL 优先的函数式编程库。
仅当目标定义了其行为和验证证据时，才会添加功能 API。

权威的设计和依赖边界记录在[产品契约 (product contract)](https://github.com/wxxb789/funnysharp/blob/main/docs/product-contract.md) 中。
当前的 fail-closed 发布门禁及其显式证据清单记录在[发布就绪状态 (release readiness)](https://github.com/wxxb789/funnysharp/blob/main/docs/release-readiness.md) 中。

## 快速入门 (Quick Start)

```shell
dotnet add package FunnySharp              # 核心载体、语法、管道与代码分析器
dotnet add package FunnySharp.AspNetCore   # Minimal API 结果映射
```

两个包均以 `net10.0` 为目标平台。[快速入门 (quick start)](https://github.com/wxxb789/funnysharp/blob/main/docs/quick-start.md) 按序展示了每个 API 面的规范用法——Option、Result、UnitResult、Validation、共享语法、集合与遍历、管道与流式处理、异步并发、副作用与资源、状态机与不可变更新、分析器反馈以及 ASP.NET Core 映射——并链接到对应的专项指南。

[版本控制 (Versioning)](https://github.com/wxxb789/funnysharp/blob/main/docs/versioning.md) 说明了版本控制规则和提交的公共 API 基准；[发布说明 (release notes)](https://github.com/wxxb789/funnysharp/blob/main/docs/release-notes.md) 载有 0.1.0 预览版 API 面以及支持的运行时声明。

## 性能表现 (Performance)

每个与性能相关的稳定操作都具备详尽记录的复杂度、枚举、内存分配、装箱、具现化、缓冲和异步调度特征，并以针对原生 BCL、惯用 LINQ、FSharp.Core 和固定版本函数式替代库的实测对比为支撑。分配预算超标和复杂度回归将直接阻断发布；在固定硬件执行器就位之前，托管环境的耗时数据保持方向性参考。

- [统一性能指南 (Consolidated performance guidance)](https://github.com/wxxb789/funnysharp/blob/main/docs/performance.md)

## 语法表 (Grammar)

一套紧凑的动词语法规范支配着所有动词：在每个适用该含义的载体上，动词都保持唯一定义和可预测的输出结构，因此求值顺序、短路机制、异常处理、取消信号、枚举和具现化行为均由签名和契约自然确定。

- [权威动词语法表 (Authoritative grammar table)](https://github.com/wxxb789/funnysharp/blob/main/docs/grammar.md)

## Roslyn 代码分析器 (Analyzers)

编译器诊断反馈直接随核心包附带分发：每个对 `FunnySharp` 的引用都会将分析器程序集添加到 `analyzers/dotnet/cs` 目录下，无需任何额外安装，且不引入运行时依赖。诊断规则可拦截未初始化的语义载体（`FS1001`）、静默丢弃的计算结果（`FS1002`）、忽略的 `TryGet*` 存在性判定（`FS1003`）、阻塞的 `ValueTask`（`FS1004`）以及对异步释放资源的同步释放（`FS1005`）；每条诊断均可按需抑制，并附带误报处置策略与免除机制说明。

- [诊断文档 (Diagnostic documentation)](https://github.com/wxxb789/funnysharp/blob/main/docs/analyzers.md)

## 函数组合 (Function Composition)

FunnySharp 为管道式调用、从左至右函数组合、柯里化（currying）、部分应用（partial application）、参数翻转以及副作用观测提供了一组精炼的标准委托扩展。配套的 `Task` 与 `ValueTask` 组合与观测扩展（`ComposeAsync`/`ComposeValueAsync`、`TapAsync`/`TapValueAsync`）可在不产生 sync-over-async 的前提下完整保留异步执行特性。

- [语义与性能证据 (Semantics and performance evidence)](https://github.com/wxxb789/funnysharp/blob/main/docs/function-composition.md)
- [可编译示例 (Compiling examples)](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Option 载体

`Option<T>` 显式表示值的存在（`Some`）或缺失（`None`），提供安全的属性检查、同步组合，以及针对可空类型、Try 模式、字典、`Task` 和 `ValueTask` 的专用桥接。故障和取消操作仍保持为标准异步失败，而不会被静默转为缺失。LINQ `Select`/`SelectMany` 作为 `Map`/`Bind` 的次要别名用于支持查询语法；为了防止语义混淆，未提供 `Where` 动词。

- [语义说明 (Semantics)](https://github.com/wxxb789/funnysharp/blob/main/docs/option.md)
- [可编译示例 (Compiling examples)](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Result 载体

`Result<TValue, TError>` 显式表示成功值或强类型错误，支持快速短路（fail-fast）的映射、绑定、校验、恢复、组合、LINQ 查询语法、Option 互操作以及配套的 `Task`/`ValueTask` 异步组合。显式的 `Try` 边界保留取消语义，除非调用方特意映射为领域错误，否则保留原始异常。

- [语义说明 (Semantics)](https://github.com/wxxb789/funnysharp/blob/main/docs/result.md)
- [可编译示例 (Compiling examples)](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## UnitResult 载体

`UnitResult<TError>` 针对无需携带有效负载的命令、删除与通知操作表示成功或类型化失败，无需虚拟返回值或引入公共的 `Unit` 类型。它提供对应的检查、向 `Result` 的映射、绑定、校验、错误恢复、错误映射、组合、短路序列遍历以及相匹配的 `Task`/`ValueTask` 组合。

- [语义说明 (Semantics)](https://github.com/wxxb789/funnysharp/blob/main/docs/unit-result.md)
- [可编译示例 (Compiling examples)](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## 副作用与延迟执行 (Effects)

`Effect<T>` 与 `Effect<TEnvironment, T>` 为标准 .NET 操作提供了轻量的延迟执行边界。它们基于 `ValueTask` 进行组合，使依赖关系与资源生命周期显式化，并在不引入副作用运行时或依赖注入（DI）容器的情况下保留标准的异常抛出与取消信号。

- [语义与性能证据 (Semantics and performance evidence)](https://github.com/wxxb789/funnysharp/blob/main/docs/effects.md)
- [可编译示例 (Compiling examples)](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## 并发控制 (Concurrency)

FunnySharp 支持在保持原始顺序或完成顺序下对 `IAsyncEnumerable<T>` 执行显式有界并行映射与遍历，并支持在冷态的 `Effect<Result<TValue, TError>>` 操作中进行首个成功者竞争选择（first-success）。这些 API 遵循标准 .NET 取消、异常、`ValueTask`、`Channel` 与 `TimeProvider` 机制，无需额外的并发运行时或调度程序。

- [语义与性能证据 (Semantics and performance evidence)](https://github.com/wxxb789/funnysharp/blob/main/docs/concurrency.md)
- [可编译示例 (Compiling examples)](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## 验证与多错误累积 (Validation)

`Validation<TValue, TError>` 表示有效的值或一个及以上的领域错误。它适用于所有独立分支都应完整执行并按确定性顺序报告所有错误的场景；在需要遇错即短路（fail-fast）的场合，请使用 `Option<T>`、`Result<TValue, TError>` 或 `UnitResult<TError>`。

- [语义与共享遍历行为 (Semantics and shared traversal behavior)](https://github.com/wxxb789/funnysharp/blob/main/docs/validation.md)
- [可编译示例 (Compiling examples)](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## 数据流管道 (Data Pipelines)

FunnySharp 将管道操作保持在标准 .NET 载体上。对于常规的投影、过滤、扁平化、排序和显式具现化，直接使用 LINQ 和 .NET 10 异步 LINQ。`Choose` 为同步与异步流提供了融合的 Option 过滤映射，`Scan` 提供了流式累计聚合，而 span 与 memory 辅助方法则直接写入调用方持有的存储或就地转换。

- [语义、生命周期规则与性能证据 (Semantics, lifetime rules, and performance evidence)](https://github.com/wxxb789/funnysharp/blob/main/docs/data-pipelines.md)
- [可编译数据清洗示例 (Compiling data-cleaning examples)](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## 集合扩展 (Collections)

FunnySharp 遵从标准 .NET 集合和序列生态，仅补充 BCL 语义模糊的操作：`*OrNone` 基数访问（`FirstOrNone`、`LastOrNone`、`SingleOrNone`、`ElementAtOrNone`、`MinOrNone`、`MaxOrNone`）、无异常且无需种子的 `NonEmpty<T>` 保证、单趟 `Partition` 谓词/载体分割、精确与截断拉链组合（`ZipExact`、`ZipExactOrNone`）、容器及 `IParsable` 解析桥接、`WhereNotNull` 以及遍历上下文信息——索引、键名以及组合式的 `Location` 上下文，使得遍历错误能够携带如 `customers[17].addresses[2].postalCode` 的精确路径而无需应用代码拼接。

- [语义、载体行为矩阵与设计边界 (Semantics, carrier behavior matrix, and deliberate exclusions)](https://github.com/wxxb789/funnysharp/blob/main/docs/collections.md)
- [可编译批量验证与清洗示例 (Compiling data-cleaning and batch-validation examples)](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## 不可变数据更新 (Immutable Updates)

`Lens<TSource, TFocus>` 与 `Optional<TSource, TFocus>` 为完全存在或可能缺失的嵌套属性更新提供了精简、可组合的抽象。它们与记录（record）的 `with` 表达式以及调用方选择的 BCL 不可变集合操作原生配合，不引入额外的集合类型体系或隐式内存拷贝。

- [语义、BCL 集合指引与性能证据 (Semantics, BCL collection guidance, and performance evidence)](https://github.com/wxxb789/funnysharp/blob/main/docs/immutable-updates.md)
- [可编译不可变更新示例 (Compiling immutable-update examples)](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## 状态机 (State Machines)

FunnySharp 将纯状态转换与有限状态工作流建模为显式状态、输出命令、无效事件、转换失败、未定义转移、组合与回放。状态转换核心保持同步且确定，无需直接执行副作用；调用方自主决定如何执行输出命令中的异步操作。

- [语义、回放规则与异步边界 (Semantics, replay rules, and async boundary)](https://github.com/wxxb789/funnysharp/blob/main/docs/state-machines.md)
- [可编译审批工作流示例 (Compiling approval-workflow example)](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## ASP.NET Core 集成

`FunnySharp.AspNetCore` 是一个独立的 Minimal API 扩展包。它将显式的 `Option`、`Result`、`UnitResult`、`Validation`、`Task`、`ValueTask` 和 `Effect` 计算结果映射为调用方选定的 `IResult` 和符合 RFC 规范的 `ProblemDetails`，避免将纯 BCL 核心包与 ASP.NET Core 产生耦合。

- [集成语义与端点示例 (Integration semantics and endpoint examples)](https://github.com/wxxb789/funnysharp/blob/main/docs/aspnet-core.md)
- [可编译 Minimal API 示例 (Compiling Minimal API example)](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.AspNetCore.Examples/Program.cs)

## 验证构建 (Verify)

```bash
dotnet fsi build.fsx -- -p release \
  -AttemptId local-full-1 \
  -CompatibilityRuntimeIdentifier win-x64 \
  -CompatibilityPackageFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json \
  -DistributionFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json
```

验证器会拒绝脏工作区、复用的尝试标识、已发布或不明确的包版本、不安全的生成输出路径或非隔离的还原。它执行锁定的无缓存还原、Release 构建、xUnit 测试、两组可执行示例运行、打包、代码格式验证、语义基准测试预检、协议测试套件、生成表格一致性验证、兼容性运行，以及在完整模式下执行全部 BenchmarkDotNet 套件与内存分配策略核验。兼容性结果仅对其记录的 SDK、运行时补丁、操作系统、RID 和规范包哈希有效；有关当前支持范围与 Native AOT 限制，请参见[产品契约 (product contract)](https://github.com/wxxb789/funnysharp/blob/main/docs/product-contract.md)。

GitHub release 验证公开了四个稳定的必要状态检查：`release / win-x64`、`release / linux-x64`、`release / osx-arm64` 以及 `release / osx-x64-consumer`。仓库规则集（ruleset）回读是独立的运行凭据；若无该凭据，Goal 13 的产品验收将保持未通过状态。

如需免除 PowerShell 的本地预检查，请参阅[工具链说明 (tooling)](https://github.com/wxxb789/funnysharp/blob/main/docs/tooling.md)。

## 性能基准测试 (Benchmark)

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*'
```

## 开源许可证 (License)

FunnySharp 基于 [MIT 许可证 (MIT License)](LICENSE) 开源发布。
