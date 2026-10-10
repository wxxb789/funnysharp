# FunnySharp: 面向 C# 与 .NET 10 的实用主义、BCL 优先函数式编程库

[![NuGet FunnySharp](https://img.shields.io/nuget/v/FunnySharp.svg?style=flat-square&label=FunnySharp)](https://www.nuget.org/packages/FunnySharp)
[![NuGet FunnySharp.AspNetCore](https://img.shields.io/nuget/v/FunnySharp.AspNetCore.svg?style=flat-square&label=FunnySharp.AspNetCore)](https://www.nuget.org/packages/FunnySharp.AspNetCore)
[![Target Framework](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)
[![Zero Runtime Dependencies](https://img.shields.io/badge/dependencies-0-brightgreen.svg?style=flat-square)](#设计原则)

[English](README.md) | [简体中文](README_zh-cn.md)

**FunnySharp** 是一款专为 C# 13 和 .NET 10 打造的现代、高性能、**BCL 优先（Base Class Library First）**的实用函数式编程库。它提供了零堆内存分配的语义载体（`Option<T>`、`Result<TValue, TError>`、`UnitResult<TError>`、`Validation<TValue, TError>`）、铁路导向编程（Railway-Oriented Programming, ROP）、内置开箱即用的 Roslyn 编译器分析器、高阶函数组合、数据流管道、纯状态机以及无缝的 ASP.NET Core Minimal API 映射——无需引入任何第三方运行时依赖，不重构原生集合，不强加复杂的外部类型体系。

---

## 目录 (Table of Contents)

- [为什么选择 FunnySharp？](#为什么选择-funnysharp)
- [核心特性](#核心特性)
- [包与安装](#包与安装)
- [快速入门与代码示例](#快速入门与代码示例)
  - [Option：杜绝空引用异常与安全空值处理](#1-optiont空值安全与安全访问)
  - [Result 与 UnitResult：铁路导向编程 (ROP)](#2-result-与-unitresult铁路导向编程-rop)
  - [Validation：应用函子多错误并行累积](#3-validation多错误并行累积校验)
  - [内置 Roslyn 静态分析器：编译期安全防护](#4-内置-roslyn-静态分析器fs1001fs1005)
  - [函数组合与管道式调用](#5-函数组合与管道式调用)
  - [数据流管道：融合操作与流式处理](#6-数据流管道choose-scan-partition)
  - [ASP.NET Core Minimal API 快速集成](#7-aspnet-core-minimal-api-无缝集成)
- [核心语义载体一览](#核心语义载体一览)
- [正交动词语法体系](#正交动词语法体系)
- [零外部依赖与严格性能预算](#零外部依赖与严格性能预算)
- [构建与验证体系](#构建与验证体系)
- [文档与深入指南](#文档与深入指南)
- [开源许可证](#开源许可证)

---

## 为什么选择 FunnySharp？

在 C# 中实践函数式编程时，开发者往往面临两种痛点：要么引入庞大的 Monad 框架，被迫适应与原生 .NET 割裂的自定义集合和类型体系（陡峭的学习曲线与隐藏的垃圾回收压力）；要么在不同微服务中手写简陋脆弱的 `Result` 类。

FunnySharp 采用 **BCL 优先的实用主义哲学**：

1. **原生拥抱 .NET 10 与 C# 13**：完全构建于 BCL 原生基础之上（`ValueTask`、`IAsyncEnumerable<T>`、`ReadOnlySpan<T>`、`CancellationToken` 和标准委托）。不造重复的调度器轮子，不搞千层 Monad 抽象包装。
2. **零运行时依赖（Zero Runtime Dependencies）**：核心包绝不引入任何外部第三方包，保持依赖树极致纯净。
3. **随包附带编译期防线**：Roslyn 分析器直接打包在 NuGet 核心包内。无需额外配置，未初始化的 struct、被忽略的错误、阻碍线程池的 sync-over-async 阻塞在编码阶段就被 IDE 和编译器直接拦截。
4. **严格的分配预算与可测性能**：每个关键热路径均设有 BenchmarkDotNet 内存分配预算与回归基线，且直接与原生手写 C#、LINQ 及 FSharp.Core 进行实测对比。
5. **正交统一的动词语法**：一个动词在所有载体上具备唯一且完全一致的行为语义（`Map`、`Bind`、`Ensure`、`Match`、`Recover`、`Filter`）。

---

## 核心特性

- 🛡️ **四大权威语义载体**：
  - `Option<T>`：显式表达值的存在与缺失，彻底告别 `NullReferenceException`。
  - `Result<TValue, TError>`：遇错即短路（Fail-fast）的业务流载体，显式强类型领域错误。
  - `UnitResult<TError>`：专为写操作、删除、通知等无返回值命令打造，杜绝虚拟返回值或伪 `Unit` 侵入。
  - `Validation<TValue, TError>`：应用函子多错误并行累积，按确定性顺序收集表单、DTO 和批量输入的所有失败项。
- 🚦 **铁路导向编程 (Railway-Oriented Programming)**：流畅的链式调用（`Map`、`Bind`、`Ensure`、`Recover`、`OrElse`、`Tap`），完美支持原生 `Task` 与 `ValueTask` 异步组合。
- 🔍 **开箱即用的 Roslyn 诊断分析器 (`FS1001`–`FS1005`)**：自动排查未正确初始化的载体、静默丢弃的错误结果、未消费的 Try 模式返回值以及错误的 `ValueTask` 阻塞。
- ⚡ **高性能流式处理**：融合式 `Choose`（单趟完成过滤与投影，避免中间分配）、`Scan`（流式累加聚合）与 `Partition`，全面覆盖 `IEnumerable<T>`、`IAsyncEnumerable<T>` 和 `ReadOnlySpan<T>`。
- 🌐 **ASP.NET Core Minimal API 适配包**：优雅地将领域 `Result` 和 `Validation` 结果投影为标准 `IResult` 和符合 RFC 7807 / RFC 9457 规范的 `ProblemDetails`。
- ⚙️ **纯状态机与副作用隔离**：基于不可变转换与输出事件建模状态流，与异步副作用执行（`Effect<T>`, `Effect<TEnvironment, T>`）清晰解耦。
- 🧩 **光学元件与不可变更新**：轻量级 `Lens<TSource, TFocus>` 与 `Optional<TSource, TFocus>`，原生协同 C# 记录类型的 `with` 表达式。

---

## 包与安装

通过 .NET CLI 或 NuGet 包管理器进行安装：

### 核心包（载体、语法、分析器、流式管道）
```shell
dotnet add package FunnySharp
```

### ASP.NET Core 集成包（Minimal API 结果与 ProblemDetails 映射）
```shell
dotnet add package FunnySharp.AspNetCore
```

*开发环境要求：.NET 10.0 SDK（目标框架为 `net10.0`）。*

---

## 快速入门与代码示例

### 1. `Option<T>`：空值安全与安全访问

消灭 `null` 带来的歧义与隐藏隐患。`Option<T>` 明确区分有值（`Some`）与无值（`None`）：

```csharp
using FunnySharp;

// 构造：通过值或标准 Try 模式构造
Option<string> name = Option.Some("Alice");
Option<string> empty = Option.None;
Option<int> parsed = int.TryParse("128", out var val) ? Option.Some(val) : Option.None;

// 安全变换、过滤与模式解包
string greeting = name
    .Map(n => n.ToUpperInvariant())
    .Filter(n => n.StartsWith("A"))
    .Match(
        some: n => $"你好, {n}!",
        none: () => "你好, 访客!"
    );

// 兜底回退
string displayName = empty.GetValueOr("匿名用户");
string lazyName = empty.GetValueOrElse(() => ComputeFallbackName());
```

### 2. `Result` 与 `UnitResult`：铁路导向编程 (ROP)

通过流畅的链式调用组织业务管道，遇错自动短路并安全向下传递错误信息：

```csharp
using FunnySharp;

public record User(int Id, string Email, bool IsActive);
public record UserError(string Code, string Message);

public Result<User, UserError> ProcessUser(string rawEmail)
{
    return ValidateEmail(rawEmail)
        .Ensure(email => !email.EndsWith("@blocked.com"), new UserError("BlockedDomain", "不允许使用的邮箱域名。"))
        .Bind(FindUserByEmail)
        .Tap(user => LogAudit(user.Id));
}

// UnitResult 专门用于无返回值的命令（Command/Mutation）
public UnitResult<UserError> DeactivateAccount(int userId)
{
    return FindUser(userId)
        .ToUnitResult()
        .Ensure(() => CheckPermission(userId), new UserError("Forbidden", "权限不足。"))
        .Bind(() => DeleteSession(userId));
}
```

### 3. `Validation`：多错误并行累积校验

与一旦遇错立即短路的 `Result` 不同，`Validation<TValue, TError>` 会并行执行所有独立规则并按确定性顺序收集所有失败信息，非常适合用户输入校验、注册表单与批量导入：

```csharp
using FunnySharp;

public record RegisterRequest(string Username, string Password, int Age);

public Validation<RegisterRequest, string> ValidateRegistration(RegisterRequest req)
{
    var validUser = req.Username.Length >= 3
        ? Validation<string, string>.Valid(req.Username)
        : Validation<string, string>.Invalid("用户名长度至少为 3 个字符。");

    var validPass = req.Password.Length >= 8
        ? Validation<string, string>.Valid(req.Password)
        : Validation<string, string>.Invalid("密码长度至少为 8 个字符。");

    var validAge = req.Age >= 18
        ? Validation<int, string>.Valid(req.Age)
        : Validation<int, string>.Invalid("必须年满 18 周岁。");

    // 组合所有校验，错误按顺序收集累加
    return validUser.ZipWith(validPass, validAge, (u, p, a) => new RegisterRequest(u, p, a));
}
```

### 4. 内置 Roslyn 静态分析器（`FS1001`–`FS1005`）

FunnySharp 将 Roslyn 分析器直接打包在核心 NuGet 包中，无需单独安装扩展，为项目提供直接的编译期防线：

| 诊断规则 | 级别 | 拦截场景 | 为什么这很重要 |
|:---:|:---:|---|---|
| **FS1001** | **错误 (Error)** | 禁止通过 `default` 或 `new()` 创建语义载体 | 语义载体要求具备明确的状态（`Some`/`None`, `Success`/`Failure`）。 |
| **FS1002** | **警告 (Warning)** | 拦截未处理/静默丢弃的载体返回值 | 防止在函数式调用链中意外丢弃错误或未检查失败状态。 |
| **FS1003** | **警告 (Warning)** | 强制检查 `TryGet*` 方法的布尔返回值 | 杜绝直接消费未经验证的提取变量。 |
| **FS1004** | **警告 (Warning)** | 严禁在 FunnySharp 返回的 `ValueTask` 上进行同步阻塞（`.Result`, `.Wait()`） | 彻底消除线程池饥饿与 sync-over-async 死锁隐患。 |
| **FS1005** | **警告 (Warning)** | 拦截对异步释放资源（`IAsyncDisposable`）的同步释放调用 | 确保异步资源得到完整、正确的异步生命周期释放。 |

### 5. 函数组合与管道式调用

利用标准委托扩展，书写直观、高内聚、易维护的现代函数式流水线：

```csharp
using FunnySharp;

Func<string, string> trim = s => s.Trim();
Func<string, string> lowercase = s => s.ToLowerInvariant();
Func<string, string> sanitize = trim.Compose(lowercase);

// 通过 Pipe 流式传递值
var result = "  Hello FunnySharp  "
    .Pipe(sanitize)
    .Pipe(s => $"[清洗结果: {s}]");
// 输出: "[清洗结果: hello funnysharp]"

// 柯里化与部分应用 (Currying & Partial Application)
Func<int, int, int> add = (a, b) => a + b;
var addFive = add.Curry()(5);
int fifteen = addFive(10); // 15
```

### 6. 数据流管道：`Choose`, `Scan`, `Partition`

扩展标准 LINQ，为 `IEnumerable<T>`、`IAsyncEnumerable<T>` 以及内存切片带来单趟融合流算子：

```csharp
using FunnySharp;

var inputs = new[] { "10", "abc", "25", "40", "invalid" };

// 单趟融合过滤映射 (Zero-allocation 过滤掉非法项并解析)
IEnumerable<int> numbers = inputs.Choose(s => int.TryParse(s, out var n) ? Option.Some(n) : Option.None);
// 结果: 10, 25, 40

// 单趟按条件划分 (Partition)
var (adults, minors) = users.Partition(u => u.Age >= 18);

// 流式累计扫描聚合 (Scan)
IEnumerable<int> runningTotal = numbers.Scan(0, (acc, n) => acc + n);
// 结果: 0, 10, 35, 75
```

### 7. ASP.NET Core Minimal API 无缝集成

通过 `FunnySharp.AspNetCore`，轻松将领域层的 `Result`、`Validation` 和 `Effect` 映射为强类型 HTTP 响应及符合 RFC 规范的 `ProblemDetails`：

```csharp
using FunnySharp;
using FunnySharp.AspNetCore;

var app = WebApplication.Create();

app.MapGet("/users/{id:int}", (int id, IUserService service) =>
{
    return service.FindUser(id)
        .ToHttpResult(
            onSuccess: user => Results.Ok(user),
            onNone: () => Results.NotFound($"用户 {id} 不存在")
        );
});

app.MapPost("/orders", (CreateOrderDto dto, IOrderService service) =>
{
    return service.CreateOrder(dto)
        .ToHttpResult(
            onSuccess: order => Results.Created($"/orders/{order.Id}", order),
            onFailure: error => Results.BadRequest(new ProblemDetails
            {
                Title = "创建订单失败",
                Detail = error.Message,
                Status = StatusCodes.Status400BadRequest
            })
        );
});
```

---

## 核心语义载体一览

| 载体类型 | 状态表示 | 短路 / 累加 | 默认值 (Default) 约定 | 主要应用场景 |
|---|---|:---:|:---:|---|
| **`Option<T>`** | 存在 (`Some`) 或 缺失 (`None`) | 短路 | 合法 `None` | 取代 `null`，表达查询无结果、字典索引缺失、安全类型解析。 |
| **`Result<TValue, TError>`** | 强类型成功或强类型失败 | 短路 | 未初始化 (触发 FS1001) | 业务工作流、领域操作逻辑、可能会失败的计算。 |
| **`UnitResult<TError>`** | 强类型命令成功或失败 | 短路 | 未初始化 (触发 FS1001) | 修改、删除、事件发布等无返回值的写操作。 |
| **`Validation<TValue, TError>`** | 有效值或多个累积错误 | 累加 | 未初始化 (触发 FS1001) | 表单验证、复合规则校验、数据契约校验与批量入库。 |
| **`Effect<T>` / `Effect<TEnv, T>`** | 包含环境依赖的延迟执行计算 | 延迟求值 | 未初始化 | 冷异步工作流、环境依赖显式化与资源生命周期安全治理。 |

---

## 正交动词语法体系

FunnySharp 采用正交、一致的语法规范。只要掌握了动词，就能准确预知在所有载体上的执行行为：

```
          ┌──────────────────────────────────────────────────────────────┐
          │                      FunnySharp 动词语法规范                  │
          ├───────────────┬──────────────────────────────────────────────┤
          │ Map           │ 在载体内部变换成功值                           │
          │ Bind          │ 扁平化链接返回相同载体的函数 (Monadic FlatMap) │
          │ Ensure        │ 校验断言，不满足则转入指定错误                   │
          │ Match         │ 穷尽式模式解包提取                             │
          │ Recover       │ 遇错恢复：从失败状态回退至成功值                 │
          │ RecoverWith   │ 遇错恢复：从失败状态回退至另一个载体             │
          │ Filter        │ 满足断言保留，否则转换为 None                   │
          │ Tap           │ 旁路观测值或错误（触发副作用/记录日志）           │
          │ Zip / ZipWith │ 组合多个独立的载体并聚合计算结果                 │
          └───────────────┴──────────────────────────────────────────────┘
```

---

## 零外部依赖与严格性能预算

FunnySharp 在构思之初便服务于高吞吐、低延迟的现代云原生架构：

- **严苛的内存分配预算**：CI 持续运行 BenchmarkDotNet 进行内存指标核验，零堆分配路径受到强回归防护。
- **杜绝 Sync-Over-Async**：异步管道完整传递取消令牌与异常，不发生线程池阻塞死锁。
- **修剪与 Native AOT 友好**：满足现代化发布要求（配置 `<IsTrimmable>true</IsTrimmable>`）。
- **零反射设计**：所有载体与管道操作基于静态类型委托与内联 struct 实现，JIT 优化友好。

查看完整的[性能指标与测量证据文档](docs/performance.md)。

---

## 构建与验证体系

FunnySharp 采用 Fail-Closed（遇错即止）的开发验证门禁，保障代码跨 Windows、Linux、macOS 平台稳定可靠：

```bash
# 构建整个解决方案
dotnet build FunnySharp.slnx

# 运行完整 xUnit 测试套件
dotnet test FunnySharp.slnx

# 运行 harness 发布门禁校验流程（跳过长时间基准测试）
dotnet fsi build.fsx -- -p release -SkipBenchmarks

# 执行完整 BenchmarkDotNet 性能基准测试套件
dotnet run --project benchmarks/FunnySharp.Benchmarks -c Release -- --filter '*'
```

---

## 文档与深入指南

查阅详尽的官方设计指南、规范契约与示例：

- 📖 [产品契约 (Product Contract)](docs/product-contract.md) — 权威架构准则与依赖设计边界
- 📖 [快速入门指南 (Quick Start)](docs/quick-start.md) — 端到端用法导航
- 📖 [权威动词语法表 (Authoritative Grammar)](docs/grammar.md) — 动词与载体行为全量速查表
- 📖 [Option 指南 (Option Guide)](docs/option.md) — `Option<T>` 深度解析
- 📖 [Result 指南 (Result Guide)](docs/result.md) — `Result<TValue, TError>` 业务实践
- 📖 [UnitResult 指南 (UnitResult Guide)](docs/unit-result.md) — 命令式工作流规范
- 📖 [Validation 指南 (Validation Guide)](docs/validation.md) — 多错误累积校验
- 📖 [静态分析器指南 (Analyzers Guide)](docs/analyzers.md) — FS1001–FS1005 规则解析与配置
- 📖 [ASP.NET Core 指南 (ASP.NET Core Guide)](docs/aspnet-core.md) — Minimal API 与 RFC 规范 ProblemDetails
- 📖 [集合扩展与遍历 (Collections & Traversal)](docs/collections.md) — 序列操作、`NonEmpty<T>` 与 `Location`
- 📖 [数据流管道 (Data Pipelines)](docs/data-pipelines.md) — 流式处理、`Choose` 与 `Scan`
- 📖 [并发控制指南 (Concurrency Guide)](docs/concurrency.md) — 有界并行与竞争调度
- 📖 [状态机 (State Machines)](docs/state-machines.md) — 纯状态转换工作流
- 📖 [不可变更新与光学元件 (Immutable Updates)](docs/immutable-updates.md) — Lens 与 Optional
- 📖 [性能指南 (Performance Guidance)](docs/performance.md) — 内存分配预算与基准测试方法

---

## 开源许可证

FunnySharp 遵循 [MIT 开源许可证 (MIT License)](LICENSE)。
