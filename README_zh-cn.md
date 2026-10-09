# FunnySharp

[![NuGet FunnySharp](https://img.shields.io/nuget/v/FunnySharp.svg?style=flat-square&label=FunnySharp)](https://www.nuget.org/packages/FunnySharp)
[![NuGet FunnySharp.AspNetCore](https://img.shields.io/nuget/v/FunnySharp.AspNetCore.svg?style=flat-square&label=FunnySharp.AspNetCore)](https://www.nuget.org/packages/FunnySharp.AspNetCore)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg?style=flat-square)](https://dotnet.microsoft.com/)

**[English](README.md)** | **[简体中文](README_zh-cn.md)**

> **实用主义、BCL 优先、面向现代 C# 与 .NET 10 的高性能函数式编程库。**  
> 零第三方运行时依赖 · 拒绝庞杂难用的单子类型宇宙 · 内置专属 Roslyn 代码分析器。

FunnySharp 将 **铁路导向编程（Railway Oriented Programming / ROP）**、**显式强类型错误处理**、**多错误累积验证**、**纯状态机转换** 和 **轻量数据管道** 等现代函数式编程实践优雅地引入 C# 与 .NET 10，无需引入庞大的外来体系，更不会割裂现有的 BCL 编码习惯。

---

## 🌟 为什么选择 FunnySharp？

在 .NET 生态中，现有的函数式编程库往往存在两个极端：要么盲目照搬 Haskell 式的范畴论概念（引入 `Either`、`Try`、`IO`、Monad Transformers 或高阶类型模拟），严重脱离 C# 实际开发体验且产生沉重的内存分配开销；要么设计缺乏严密的契约边界与编译器安全保障。

FunnySharp 采用 **BCL 优先（BCL-first）的实用主义路线**：

- ⚡ **零第三方运行时依赖**：核心库 `FunnySharp` 保持 0 运行时依赖，完全原生面向 `net10.0`。
- 🎯 **四大经典语义载体（Carriers）**：仅提供清晰分工的 4 个载体——`Option<T>`、`Result<TValue, TError>`、`UnitResult<TError>`、`Validation<TValue, TError>`。拒绝冗余重复的类型（不搞 `Maybe`、不搞 `Either`、不搞 `Fin`）。
- 🛡️ **内置专属 Roslyn 分析器（FS1001～FS1005）**：随核心 NuGet 包分发，无需额外配置，即可在编译阶段捕捉未初始化载体、静默丢弃计算结果、忽略 `TryGet` 返回值、阻塞 `ValueTask` 等常见隐患。
- 🚀 **内存分配极致优化，完美支持 Native AOT**：所有载体基于 `readonly struct` 实现，全面把控对象分配与装箱开销，所有发布程序集均标记为可剪裁（`IsTrimmable=true`）。
- 🌐 **原生无缝集成 ASP.NET Core Minimal APIs**：提供独立的 `FunnySharp.AspNetCore` 包，一行代码将领域运算结果映射为 `IResult` 及符合 RFC 规范的 `ProblemDetails`。

---

## 📦 安装与引用

通过 .NET CLI 或 NuGet 包管理器进行安装：

```shell
# 核心函数式载体、统一语法表、集合与管道扩展、Roslyn 分析器（0 运行时依赖）
dotnet add package FunnySharp

# 可选：ASP.NET Core Minimal API 结果映射与 ProblemDetails 支持
dotnet add package FunnySharp.AspNetCore
```

---

## 🚀 核心功能快速预览

### 1. 铁路导向编程：`Option<T>` 与 `Result<TValue, TError>`

告别随处可见的 `NullReferenceException` 与滥用业务异常引发的性能损耗，用强类型掌控成功与失败分支：

```csharp
using FunnySharp;

// 1. 安全处理空值与存在性
Option<string> rawInput = Option.Some("  SKU-4289  ");

// 2. 链式管道操作：Map 映射与 Filter 过滤
Option<string> cleanedSku = rawInput
    .Map(static s => s.Trim())
    .Filter(static s => s.StartsWith("SKU-"));

// 3. 平滑桥接：将 Option 转换为携带显式领域错误的 Result
Result<string, string> validSku = cleanedSku
    .ToResult("SKU 必须以 'SKU-' 开头且不能为空。");

// 4. 短路校验：Ensure 断言校验
Result<string, string> finalized = validSku
    .Ensure(static sku => sku.Length <= 10, "SKU 长度超出最大限制。");

// 5. 穷尽模式匹配：Match 获取终态结果
string message = finalized.Match(
    onSuccess: static sku => $"订单商品就绪: {sku}",
    onFailure: static err => $"校验未通过: {err}"
);
```

---

### 2. 独立多错误累积验证：`Validation<TValue, TError>`

`Result` 在遇到第一个错误时即刻短路（Fail-fast）；而在表单提交或批量数据校验时，`Validation` 能够在确定性顺序下收集**全部**错误：

```csharp
using FunnySharp;

record RegisterUserRequest(string Username, string Email, int Age);
record User(string Username, string Email, int Age);

Validation<string, string> vUsername = ValidateUsername(request.Username);
Validation<string, string> vEmail = ValidateEmail(request.Email);
Validation<int, string> vAge = ValidateAge(request.Age);

// 组合 3 个独立校验：如果存在失败，将完整收集所有错误信息
Validation<User, string> result = vUsername.Zip(
    vEmail,
    vAge,
    static (name, email, age) => new User(name, email, age)
);

result.Match(
    onValid: static user => Console.WriteLine($"用户创建成功: {user.Username}"),
    onInvalid: static errors => Console.WriteLine($"校验错误列表:\n - {string.Join("\n - ", errors)}")
);
```

---

### 3. 轻量化副作用与资源生命周期边界：`Effect<T>`

基于标准 `ValueTask` 组合异步与延迟副作用，并显式管理资源生命周期：

```csharp
using FunnySharp;

Effect<string> readConfig = Effect.FromSync(() => File.OpenRead("appsettings.json"))
    .UsingAsync(async stream =>
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    });

string configJson = await readConfig.RunAsync();
```

---

### 4. 高性能单趟数据流管道：`Choose` 与 `Scan`

在标准 `IEnumerable<T>` 与 `IAsyncEnumerable<T>` 上实现融合的高效筛选映射与累计求和：

```csharp
using FunnySharp;

int[] rawValues = [ 10, 0, 25, -5, 40 ];

// Choose: 结合 Option<T> 实现单趟 filter + map 融合处理
IEnumerable<int> positiveValues = rawValues
    .Choose(static n => n > 0 ? Option.Some(n) : Option.None<int>());

// Scan: 流式累计聚合（Running Aggregate）
IEnumerable<int> runningTotals = positiveValues
    .Scan(0, static (acc, val) => acc + val);
// 输出序列: [10, 35, 75]
```

---

### 5. ASP.NET Core Minimal API 深度映射

仅需一行，即可将函数式处理结果转化为 HTTP 200 OK、404 Not Found 或符合 RFC 7807 规范的 `ProblemDetails`：

```csharp
using FunnySharp;
using FunnySharp.AspNetCore;

app.MapGet("/api/products/{id}", (string id, IProductService service) =>
{
    // 将领域层返回的 Result<Product, DomainError> 映射为 Minimal API IResult
    return service.FindProduct(id)
        .ToHttpResult(
            onSuccess: product => TypedResults.Ok(product),
            onError: error => TypedResults.NotFound(new { error.Message })
        );
});
```

---

## 🛡️ 开箱即用的 Roslyn 代码分析器

FunnySharp 将分析器直接内置于核心 NuGet 包中，在编写代码时为类型安全保驾护航：

| 规则 ID | 严重程度 | 说明 |
| :--- | :--- | :--- |
| **`FS1001`** | **错误 (Error)** | **禁止使用未初始化载体**：禁止使用 `default` 或 `new Result<...>()`，防止在运行时抛出未初始化异常。 |
| **`FS1002`** | **警告 (Warning)** | **禁止静默丢弃计算结果**：防止遗漏对 `Option`、`Result`、`Validation`、`Effect` 等运算结果的接收或匹配，避免逻辑静默丢失。 |
| **`FS1003`** | **警告 (Warning)** | **必须检查 `TryGet*` 结果**：警告直接丢弃 `TryGetValue`、`TryGetError` 布尔返回值的行为。 |
| **`FS1004`** | **警告 (Warning)** | **禁止同步阻塞 `ValueTask`**：阻止调用 `.Result` 或 `.GetAwaiter().GetResult()`，避免线程池饥饿与死锁。 |
| **`FS1005`** | **警告 (Warning)** | **异步资源须使用 UsingAsync**：提示针对实现了 `IAsyncDisposable` 的资源改用 `UsingAsync` 进行释放。 |

---

## 🧩 核心设计理念与架构

### 四大核心载体规范

| 载体类型 | 底层结构 | 核心语义与职责 | 默认状态行为 |
| :--- | :--- | :--- | :--- |
| `Option<T>` | `readonly struct` | 显式表达存在（`Some`）或安全缺失（`None`）。 | 有效的 `None` |
| `Result<TValue, TError>` | `readonly struct` | 表达成功或强类型错误，采用短路执行（Fail-fast）。 | 未初始化状态（触发 FS1001） |
| `UnitResult<TError>` | `readonly struct` | 表达无返回值的命令执行结果（成功或带类型失败）。 | 未初始化状态（触发 FS1001） |
| `Validation<TValue, TError>` | `readonly struct` | 表达独立多分支校验，确定性聚合所有错误信息。 | 未初始化状态（触发 FS1001） |

### 统一严谨的动词语法（Grammar）

所有载体遵循一致的动词命名契约：
- **`Map`**：同步转换内部值，保持载体外形不变。
- **`Bind`**：扁平化链接返回相同载体的函数（单子扁平绑定）。
- **`Ensure`**：断言判断；条件不满足时转换为指定的错误状态。
- **`Match`**：终态穷尽解包，要求调用方显式处理所有分支。
- **`Tap` / `TapAsync`**：用于执行副作用观测（如日志记录、指标上报），不改变管道中的载体值。

---

## 📖 深度文档索引

查阅全面的架构规范、性能基准及语义规则：

- 📘 [**快速入门指南 (Quick Start)**](docs/quick-start.md) — 各 API 模块的完整入门指引。
- 📐 [**产品契约 (Product Contract)**](docs/product-contract.md) — 项目设计原则、边界与技术选型约束。
- 🔤 [**统一语法表 (Grammar)**](docs/grammar.md) — 动词表、签名规则与操作语义。
- 📦 [**Option 指南**](docs/option.md) | [**Result 指南**](docs/result.md) | [**UnitResult 指南**](docs/unit-result.md) | [**Validation 指南**](docs/validation.md)
- ⚙️ [**副作用与资源管理 (Effects)**](docs/effects.md) — 基于 `ValueTask` 的可组合延迟计算。
- 🔀 [**并发与数据流 (Concurrency)**](docs/concurrency.md) — 有界并发映射与异步流式协调。
- 🗃️ [**集合与遍历 (Collections)**](docs/collections.md) — `*OrNone` 基数访问、`NonEmpty<T>` 保证及上下文位置感知。
- ⚡ [**性能指南与预算 (Performance)**](docs/performance.md) — 内存分配预算与基准性能对照。
- 🌐 [**ASP.NET Core 集成指南**](docs/aspnet-core.md) — Minimal API 映射与 ProblemDetails 处理。
- 🔍 [**Roslyn 分析器指南**](docs/analyzers.md) — 分析器规则清单、配置及例外抑制说明。

---

## 🔬 质量保障与基准测试

运行针对 BCL 与主流实现的高性能基准测试：

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj -c Release -- --filter '*'
```

运行全量单元测试与集成测试（xUnit v3，要求 100% 通过且零跳过测试）：

```shell
dotnet test FunnySharp.slnx
```

验证文档代码示例的字节级一致性：

```shell
dotnet fsi build.fsx -- -p verify-docs-snippets
```

---

## 📄 开源许可证

FunnySharp 采用 [MIT 开源许可证](LICENSE)。
