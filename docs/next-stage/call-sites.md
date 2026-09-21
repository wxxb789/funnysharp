# Representative Side-By-Side Call Sites (Goal 14)

Evidence for the Goal 14 decision record: eleven business workflows written three ways
wherever an honest third way exists — (a) straightforward idiomatic C# on the BCL,
(b) the same workflow on FunnySharp 0.1.0, (c) the closest honest equivalent in
CSharpFunctionalExtensions 3.7.0, Funcky 3.6.0, or LanguageExt.Core 4.4.9. Every C#
line shown as compiled evidence was compiled in a scratch project before being copied
here. F# is reference-only and was not compiled (`§15`).

This file is evidence, not a decision. Where a FunnySharp variant does not exist today
(W4 `UnitResult`, W6 location paths), the gap and its owning goal are named rather than
patched over.

## 1. Provenance and pins

| Item | Pin used here |
| --- | --- |
| .NET SDK | 10.0.400 (`dotnet --version`) |
| .NET runtime | `Microsoft.NETCore.App 10.0.11`, `Microsoft.AspNetCore.App 10.0.11` |
| FunnySharp | commit `4dbebd94b7b58648632112b7ca47c39cc517f153`, version 0.1.0, local `ProjectReference` to `src/FunnySharp` and `src/FunnySharp.AspNetCore`; `FunnySharp.dll` SHA256 `793c8532333776ed6dc2bada5457c264ab0c6eab8a150628fa54d5d67a9bf9d4` |
| CSharpFunctionalExtensions | 3.7.0, nupkg SHA256 `3a7c3d5975d883a2d55b88a0786ce4f7f29f7c0f092dcbd9a245bcf8959723c8` |
| Funcky | 3.6.0, nupkg SHA256 `1a7ab6c6595a2f4d3bdfa83768f9054450beaffd322817644f3dea65940cc32f` |
| LanguageExt.Core | 4.4.9 (`netstandard2.0` asset), nupkg SHA256 `633636d9d9cb9be75581910b503fb1aac54181a10a6b8cfe9d0a3f0118347437` |
| Transitive package | Microsoft.Bcl.AsyncInterfaces 7.0.0 (LanguageExt.Core only), nuget.org |
| NuGet sources | local folder feed with the three pinned nupkgs + nuget.org for the transitive dependency |

No competitor package is referenced from any FunnySharp call site: the three competitor
packages exist only in the `competitors` scratch project, and no competitor type appears
in the FunnySharp variants. FunnySharp remains dependency-free here.

## 2. Counting rule

**Semantic LOC** is a hand count of meaning-bearing elements in the workflow unit
(the entry method plus the private helpers that exist only to serve it):

- **S** — +1 for each statement: `if`, `else`/`else if`, `foreach`/`for`/`while` header,
  `return`, `throw`, local declaration with initializer, assignment, `using`/`await using`,
  `await foreach` header, `yield return`, `catch`, and one for the body of an
  expression-bodied member.
- **O** — +1 for each operation inside those statements: method call, `new`, indexer,
  `with` update, `await`, `is`/relational pattern, `?:`, `??`, `&&`, `||`, `!`,
  comparison/arithmetic operator, interpolated-string hole, and collection expression.
- **Excluded**: `using` directives, namespaces, type/member declaration headers, braces,
  blank lines, comments, attributes, parameter and return types, `ConfigureAwait(false)`,
  `.AsTask()`, and property/field access.

**Raw LOC** is machine-counted: non-blank, non-comment lines of the same workflow unit,
including its declaration line. The counting script is in
`docs/next-stage/call-sites-code/tools/rawloc.py` (it imports `loc.py`); semantic counts
are hand-derived and the S+O breakdown is shown in every table so a reviewer can
disagree with a specific element rather than with the total.

The domain model (`Address`, `Customer`, `Order`, `OrderError` variants, repository
interfaces) is shared harness in all three scratch projects and is excluded from every
count. Minimal API endpoint registration is not counted in W11; the mapping functions
are.

Caveat: semantic LOC is a directional measure, not an integer-precise metric. Where it
contradicts intuition, the prose says so (W3 is the main case).

## 3. Summary

| Workflow | Variant | S | O | Semantic | Raw |
| --- | --- | ---: | ---: | ---: | ---: |
| W1 dictionary + fallback | idiomatic C# | 3 | 3 | **6** | 9 |
| | FunnySharp | 1 | 5 | **6** | 9 |
| | Funcky 3.6.0 | 1 | 4 | **5** | 4 |
| W2 fail-fast pipeline | idiomatic C# | 11 | 15 | **26** | 27 |
| | FunnySharp | 6 | 22 | **28** | 28 |
| | CFE 3.7.0 | 6 | 21 | **27** | 21 |
| W3 accumulating validation | idiomatic C# | 8 | 11 | **19** | 17 |
| | FunnySharp | 4 | 19 | **23** | 17 |
| | language-ext 4.4.9 | 4 | 18 | **22** | 15 |
| | CFE 3.7.0 (alt) | 4 | 28 | **32** | 20 |
| W4 unit-result delete | idiomatic C# | 6 | 8 | **14** | 15 |
| | FunnySharp (workaround) | 6 | 11 | **17** | 15 |
| | CFE 3.7.0 | 6 | 11 | **17** | 15 |
| W5 tap / observation | idiomatic C# | 5 | 7 | **12** | 8 |
| | FunnySharp | 1 | 10 | **11** | 3 |
| | CFE 3.7.0 | 1 | 12 | **13** | 3 |
| W6 traversal + index | idiomatic C# | 11 | 18 | **29** | 27 |
| | FunnySharp (workaround) | 5 | 18 | **23** | 16 |
| W7 bounded parallel stream | idiomatic C# (operator + consumer) | 24 | 22 | **46** | 65 |
| | FunnySharp (consumer only) | 4 | 3 | **7** | 14 |
| W8 first success + timeout | idiomatic C# | 11 | 14 | **25** | 25 |
| | FunnySharp | 4 | 10 | **14** | 14 |
| W9 env + resource scope | idiomatic C# | 3 | 6 | **9** | 11 |
| | FunnySharp | 2 | 8 | **10** | 14 |
| W10 nested record update | idiomatic C# | 1 | 4 | **5** | 9 |
| | FunnySharp | 4 | 12 | **16** | 4 + 17 |
| | language-ext 4.4.9 | 4 | 12 | **16** | 4 + 18 |
| W11 HTTP mapping | idiomatic C# | 13 | 21 | **34** | 43 |
| | FunnySharp.AspNetCore | 4 | 17 | **21** | 33 |

For W10 the "raw" column splits entry method / one-time lens definitions, because the
definitions amortize across every later update.

Net: FunnySharp is decisively smaller where the library owns a protocol (W7, W8, W11),
modestly smaller for composition and traversal (W5, W6), roughly neutral for typed
fail-fast work at this scale (W2, W4), and larger than the direct BCL expression where a
single small object is updated or a three-field form is checked once (W3, W9, W10
first use).

## 4. W1 — Dictionary lookup with fallback

**Scenario.** Read `request.timeoutSeconds` from `IReadOnlyDictionary<string,string>`;
absent or non-numeric input yields 30.

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# | 3 | 3 | 6 | 9 |
| FunnySharp | 1 | 5 | 6 | 9 |
| Funcky 3.6.0 | 1 | 4 | 5 | 4 |

Idiomatic C# — `call-sites-code/idiomatic/Workflows.cs`:

```csharp
public static int TimeoutSeconds(IReadOnlyDictionary<string, string> config)
{
    if (config.TryGetValue("request.timeoutSeconds", out var raw) &&
        int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
    {
        return seconds;
    }

    return 30;
}
```

FunnySharp — `call-sites-code/funnysharp/Workflows.cs`:

```csharp
public static int TimeoutSeconds(IReadOnlyDictionary<string, string> config) =>
    config.GetOption("request.timeoutSeconds")
        .Bind(raw => Option.FromTry<int>(
            (out int seconds) => int.TryParse(
                raw,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out seconds)))
        .GetValueOr(30);
```

Funcky 3.6.0 — `call-sites-code/competitors/W1Funcky.cs`:

```csharp
public static int TimeoutSeconds(IReadOnlyDictionary<string, string> config) =>
    config.GetValueOrNone("request.timeoutSeconds")
        .SelectMany(raw => raw.ParseInt32OrNone(CultureInfo.InvariantCulture))
        .GetOrElse(30);
```

**Compile evidence.** Idiomatic and FunnySharp variants compile in
`idiomatic/Idiomatic.csproj` and `funnysharp/FunnySharpCallSites.csproj`; Funcky variant
in `competitors/Competitors.csproj`. `dotnet build -c Release` succeeded for all three
(§17.2).

**Assessment.** Semantic LOC is identical (6) to the idiomatic baseline, so the value
here is not density. FunnySharp removes the `out var` leaking into method scope and the
two-return control flow; absence becomes a value that composes with the rest of the
pipeline, and a malformed value is deliberately folded into absence by `Option.FromTry`
(`src/FunnySharp/Option.cs:62`). Signature clarity improves: `GetValueOr` is total and
cannot be forgotten (`src/FunnySharp/Option.cs:212`). Honest losses: four FunnySharp
operations (`GetOption`, `FromTry`, `Bind`, `GetValueOr`) replace two BCL calls, and
readers must know that `GetOption` also maps a stored `null` to `None`
(`src/FunnySharp/OptionExtensions.cs:42-44`); Funcky reaches 5 semantic LOC because it
ships a `ParseInt32OrNone` bridge that FunnySharp does not have, so the parse-bridge
family is a real Funcky advantage for dictionary-sourced configuration. Performance
character: `Option<T>` is a readonly struct; `FromTry` and `GetOption` do not allocate
beyond the dictionary's own lookup. F# reference: `optionInt |> Option.defaultValue 0`
(`§15`).

## 5. W2 — Parse-then-validate fail-fast pipeline with typed errors

**Scenario.** Create an invoice: reject empty customer/payload and non-positive line
quantities, look the customer up asynchronously, fail fast with one typed `OrderError`,
otherwise build the invoice.

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# | 11 | 15 | 26 | 27 |
| FunnySharp | 6 | 22 | 28 | 28 |
| CFE 3.7.0 | 6 | 21 | 27 | 21 |

Idiomatic C#:

```csharp
public static async Task<InvoiceOutcome> CreateInvoiceAsync(
    OrderRequest request,
    ICustomerRepository repository,
    CancellationToken cancellationToken)
{
    if (request.CustomerId.Length == 0)
    {
        return InvoiceOutcome.Failed(new InvalidPayload("CustomerId is required."));
    }

    if (request.Lines.IsEmpty)
    {
        return InvoiceOutcome.Failed(new NoLines(request.OrderId));
    }

    foreach (var line in request.Lines)
    {
        if (line.Quantity <= 0)
        {
            return InvoiceOutcome.Failed(new InvalidQuantity(line.Sku, line.Quantity));
        }
    }

    var customer = await repository.FindCustomerAsync(request.CustomerId, cancellationToken);
    if (customer is null)
    {
        return InvoiceOutcome.Failed(new CustomerNotFound(request.CustomerId));
    }

    return InvoiceOutcome.Ok(Domain.BuildInvoice(customer, request));
}
```

FunnySharp (entry plus the two helpers the pipeline requires; the helper bodies are
counted):

```csharp
public static async Task<Result<Invoice, OrderError>> CreateInvoiceAsync(
    OrderRequest request,
    ICustomerRepository repository,
    CancellationToken cancellationToken)
{
    var customer = await Result<OrderRequest, OrderError>.Success(request)
        .Ensure(r => r.CustomerId.Length > 0, _ => new InvalidPayload("CustomerId is required."))
        .Ensure(r => !r.Lines.IsEmpty, _ => new NoLines(request.OrderId))
        .Ensure(r => r.Lines.All(line => line.Quantity > 0), InvalidQuantityFor)
        .BindAsync(r => FindCustomerAsync(repository, r.CustomerId, cancellationToken))
        .ConfigureAwait(false);
    return customer.Map(c => Domain.BuildInvoice(c, request));
}

public static async Task<Result<Customer, OrderError>> FindCustomerAsync(
    ICustomerRepository repository,
    string customerId,
    CancellationToken cancellationToken)
{
    var customer = await repository.FindCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
    return customer is null
        ? Result<Customer, OrderError>.Failure(new CustomerNotFound(customerId))
        : Result<Customer, OrderError>.Success(customer);
}

private static OrderError InvalidQuantityFor(OrderRequest request)
{
    var invalid = request.Lines.First(line => line.Quantity <= 0);
    return new InvalidQuantity(invalid.Sku, invalid.Quantity);
}
```

CFE 3.7.0:

```csharp
public static async Task<Result<Invoice, OrderError>> CreateInvoiceAsync(
    OrderRequest request,
    ICustomerRepository repository,
    CancellationToken cancellationToken)
{
    var customer = await repository.FindCustomerAsync(request.CustomerId, cancellationToken)
        .ConfigureAwait(false);
    return Result.Success<OrderRequest, OrderError>(request)
        .Ensure(r => r.CustomerId.Length > 0, new InvalidPayload("CustomerId is required."))
        .Ensure(r => !r.Lines.IsEmpty, new NoLines(request.OrderId))
        .Ensure(r => r.Lines.All(line => line.Quantity > 0), InvalidQuantityFor(request))
        .Bind(_ => customer is null
            ? Result.Failure<Customer, OrderError>(new CustomerNotFound(request.CustomerId))
            : Result.Success<Customer, OrderError>(customer))
        .Map(c => Domain.BuildInvoice(c, request));
}
```

**Compile evidence.** All three variants compiled in their scratch projects (`§17.2`).

**Assessment.** Semantic LOC is slightly *higher* than the idiomatic baseline (28 vs 26):
the pipeline buys signature-level short-circuit semantics and a typed failure channel but
pays for `Result<T,E>.Success`/`Failure` factories and the `Ensure` predicates. What is
removed outside the count is the custom `InvoiceOutcome` type and the `(Invoice?, OrderError?)`
nullability convention — a real reduction in concept count even when statement count does
not move. Semantics added: failure is a value, not an exception, and `Ensure` cannot be
skipped by a later stage; cancellation is not converted to a domain failure because
`Result`'s async boundary only catches non-`OperationCanceledException` exceptions
(`src/FunnySharp/Result.cs:40`). Honest losses: the mixed sync/async chain is awkward —
`BindAsync` returns `Task<Result<...>>` and 0.1.0 has no `Map` on task carriers, so the
chain must be awaited before continuing (an intermediate compile of the single-expression
form failed with `CS1061: Task<Result<Customer, OrderError>> does not contain Map`;
`ResultExtensions.MapAsync` is defined on `Result` at `src/FunnySharp/ResultExtensions.cs:66`,
not on `Task`). CFE has the same seam and additionally needs the non-generic static
`Result.Success<T,E>`/`Result.Failure<T,E>` factories. Performance character: `Ensure`,
`Map`, and `Bind` are struct-passing operations with no per-success allocation; one
failure object is allocated per rejected request. Alloc/throughput claims beyond that
were not measured here (**UNVERIFIED: no benchmark run in this workstream**).

## 6. W3 — Independent form-field validation accumulating all errors

**Scenario.** Validate email, password, and age independently and return every error, not
just the first.

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# | 8 | 11 | 19 | 17 |
| FunnySharp | 4 | 19 | 23 | 17 |
| language-ext 4.4.9 | 4 | 18 | 22 | 15 |
| CFE 3.7.0 (alternative) | 4 | 28 | 32 | 20 |

Idiomatic C#:

```csharp
public static IReadOnlyList<string> ValidateSignup(SignupForm form)
{
    var errors = new List<string>();
    if (string.IsNullOrWhiteSpace(form.Email) || !form.Email.Contains('@'))
    {
        errors.Add("email must contain '@'");
    }

    if (form.Password.Length < 12)
    {
        errors.Add("password must have at least 12 characters");
    }

    if (form.Age is < 18 or > 130)
    {
        errors.Add("age must be between 18 and 130");
    }

    return errors;
}
```

FunnySharp (entry plus the three validators it composes):

```csharp
public static Validation<SignupForm, string> ValidateSignup(SignupForm form) =>
    ValidateEmail(form.Email)
        .Zip(ValidatePassword(form.Password))
        .Zip(ValidateAge(form.Age))
        .Map(parts => new SignupForm(parts.First.First, parts.First.Second, parts.Second));

private static Validation<string, string> ValidateEmail(string email) =>
    !string.IsNullOrWhiteSpace(email) && email.Contains('@')
        ? Validation<string, string>.Valid(email)
        : Validation<string, string>.Invalid("email must contain '@'");

private static Validation<string, string> ValidatePassword(string password) =>
    password.Length is >= 12
        ? Validation<string, string>.Valid(password)
        : Validation<string, string>.Invalid("password must have at least 12 characters");

private static Validation<int, string> ValidateAge(int age) =>
    age is >= 18 and <= 130
        ? Validation<int, string>.Valid(age)
        : Validation<int, string>.Invalid("age must be between 18 and 130");
```

language-ext 4.4.9:

```csharp
using LanguageExt;

public static Validation<string, SignupForm> ValidateSignup(SignupForm form) =>
    (ValidateEmail(form.Email), ValidatePassword(form.Password), ValidateAge(form.Age))
        .Apply((email, password, age) => new SignupForm(email, password, age));

private static Validation<string, string> ValidateEmail(string email) =>
    !string.IsNullOrWhiteSpace(email) && email.Contains('@')
        ? Validation<string, string>.Success(email)
        : Validation<string, string>.Fail(Prelude.Seq<string>(["email must contain '@'"]));
```

(the password and age validators follow the same shape; the full file is
`call-sites-code/competitors/W3LanguageExt.cs`.)

CFE 3.7.0 alternative (CFE has no `Validation` type; accumulation is expressed by
combining `UnitResult<E>` values with a composer):

```csharp
public static Result<SignupForm, IReadOnlyList<string>> ValidateSignup(SignupForm form) =>
    Result.Combine(
            new[]
            {
                ValidateEmail(form.Email),
                ValidatePassword(form.Password),
                ValidateAge(form.Age),
            },
            errors => (IReadOnlyList<string>)errors.SelectMany(list => list).ToArray())
        .Bind<SignupForm, IReadOnlyList<string>>(() => Result.Success<SignupForm, IReadOnlyList<string>>(form));
```

**Compile evidence.** FunnySharp variant compiles in `funnysharp/FunnySharpCallSites.csproj`;
both competitor variants compile in `competitors/Competitors.csproj` (`§17.2`). The
language-ext form was the hardest to discover: the two-argument `Validation<FAIL,SUCCESS>`
has no `Apply`, and accumulation comes from `LanguageExt.ValidationSeqExtensions.Apply`
tuple overloads, whose `FAIL` must be wrapped in `Seq<FAIL>` for `Fail(...)`.

**Assessment.** This is the one workflow where FunnySharp is clearly *larger* than the
idiomatic baseline at this scale (23 vs 19), and the language-ext form is smaller again
(22) because its tuple `Apply` avoids the nested-pair `Map`. The honest read: for three
inline field rules checked once, a `List<string>` and three `if`s is hard to beat. What
FunnySharp adds: errors are typed per field, validators are reusable and independently
testable, and accumulation order is documented and deterministic (left-to-right,
`src/FunnySharp/Validation.cs:177-211`). What it removes: the mutable error list, the
"forgot to add the error" bug class, and the convention that the return value means
"empty list = valid". Honest losses: the applicative encoding requires per-field methods
returning `Validation<_,string>`; `parts.First.First` is a readability wart caused by
`Zip` returning nested tuples; and there is no curried `Apply`, so tuple-style applicative
composition is not available. `Validation` deliberately has no `Bind` that would falsely
promise accumulation for dependent computations (`docs/goals/archive/0015-goal.md:2`); callers who
need sequencing must move to `Result`. Performance character: `Validation<T,E>` stores a
snapshot `IReadOnlyList<TError>` on failure; `Zip` concatenates error arrays, so each
recombination allocates proportionally to the number of accumulated errors. No allocation
measurements were taken here (**UNVERIFIED: no benchmark run in this workstream**).
`docs/product-contract.md` states the same accumulation contract at the product level
(lines 32-36).

## 7. W4 — Delete-or-notify, success carries no value

**Scenario.** Delete an order and notify; failure carries an `OrderError`; success carries
nothing. **FunnySharp 0.1.0 has no `UnitResult<TError>`** — a `grep UnitResult src/`
returns nothing, while Goal 15 names it as one of the four canonical carriers
(`docs/goals/archive/0015-goal.md:2`).

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# | 6 | 8 | 14 | 15 |
| FunnySharp (current workaround) | 6 | 11 | 17 | 15 |
| CFE 3.7.0 (`UnitResult<E>`) | 6 | 11 | 17 | 15 |

Idiomatic C# (the `null`-means-success convention is the weakness):

```csharp
public static async Task<OrderError?> DeleteOrderAsync(
    string orderId,
    IOrderStore store,
    INotifier notifier,
    CancellationToken cancellationToken)
{
    var existing = await store.FindAsync(orderId, cancellationToken);
    if (existing is null)
    {
        return new CustomerNotFound(orderId);
    }

    await store.DeleteAsync(orderId, cancellationToken);
    await notifier.NotifyAsync($"order {orderId} deleted", cancellationToken);
    return null;
}
```

FunnySharp 0.1.0, closest current workaround (dummy `bool` payload is the cost):

```csharp
public static async Task<Result<bool, OrderError>> DeleteOrderAsync(
    string orderId,
    IOrderStore store,
    INotifier notifier,
    CancellationToken cancellationToken)
{
    var existing = await store.FindAsync(orderId, cancellationToken).ConfigureAwait(false);
    if (existing is null)
    {
        return Result<bool, OrderError>.Failure(new CustomerNotFound(orderId));
    }

    await store.DeleteAsync(orderId, cancellationToken).ConfigureAwait(false);
    await notifier.NotifyAsync($"order {orderId} deleted", cancellationToken).ConfigureAwait(false);
    return Result<bool, OrderError>.Success(true);
}
```

CFE 3.7.0, the shape the missing carrier should eventually have:

```csharp
using CSharpFunctionalExtensions;

public static async Task<UnitResult<OrderError>> DeleteOrderAsync(
    string orderId,
    IOrderStore store,
    INotifier notifier,
    CancellationToken cancellationToken)
{
    var existing = await store.FindAsync(orderId, cancellationToken).ConfigureAwait(false);
    if (existing is null)
    {
        return UnitResult.Failure<OrderError>(new CustomerNotFound(orderId));
    }

    await store.DeleteAsync(orderId, cancellationToken).ConfigureAwait(false);
    await notifier.NotifyAsync($"order {orderId} deleted", cancellationToken).ConfigureAwait(false);
    return UnitResult.Success<OrderError>();
}
```

**Compile evidence.** Idiomatic and FunnySharp variants compile in their projects; CFE
variant in `competitors/Competitors.csproj` (`§17.2`).

**Assessment.** The idiomatic side is the smallest (14) because `OrderError?` uses an
existing BCL convention, but that convention is lossy: `null` success and "forgot to
check" are indistinguishable to the compiler. The FunnySharp workaround is honest but
adds a dummy `true` payload — precisely the "dummy values" Goal 15 says to eliminate
(`docs/goals/archive/0015-goal.md:2`) — and costs 3 more semantic operations than the baseline.
CFE proves the target shape exists without the dummy and at identical cost to the
workaround. Decision gap: `UnitResult<TError>` is a Goal 15 commitment and is absent from
the 0.1.0 surface; the call-site evidence here is the concrete motivation. This must
appear in `docs/next-stage/decision-record.md` as a redesign/keep item, not as a call-site
workaround. Learning cost: both result-based variants ask the reader to know that
`Result<bool, E>` here means "no payload"; until `UnitResult` exists, that intent lives
in a comment.

## 8. W5 — Composed data transforms with observation

**Scenario.** Compute a discounted line total while emitting audit events at each
intermediate value.

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# | 5 | 7 | 12 | 8 |
| FunnySharp | 1 | 10 | 11 | 3 |
| CFE 3.7.0 | 1 | 12 | 13 | 3 |

Idiomatic C#:

```csharp
public static decimal TotalWithAudit(OrderLine line, Action<string> audit)
{
    var total = line.Quantity * line.UnitPrice;
    audit($"line={line.Sku} total={total}");
    var discounted = total * Discount(line.Sku);
    audit($"discounted={discounted}");
    return discounted;
}
```

FunnySharp:

```csharp
public static decimal TotalWithAudit(OrderLine line, Action<string> audit) =>
    (line.Quantity * line.UnitPrice)
        .Tap(total => audit($"line={line.Sku} total={total}"))
        .Pipe(total => total * Discount(line.Sku))
        .Tap(discounted => audit($"discounted={discounted}"));
```

CFE 3.7.0 (its `Tap` exists only on carriers, so it must wrap the value in `Maybe`):

```csharp
public static decimal TotalWithAudit(OrderLine line, Action<string> audit) =>
    Maybe<decimal>.From(line.Quantity * line.UnitPrice)
        .Tap(total => audit($"line={line.Sku} total={total}"))
        .Map(total => total * Discount(line.Sku))
        .Tap(discounted => audit($"discounted={discounted}"))
        .GetValueOrDefault();
```

**Compile evidence.** All three variants compile (`§17.2`).

**Assessment.** FunnySharp removes both temporaries and makes evaluation order explicit
left-to-right; `Tap` observes without changing the value (`src/FunnySharp/FunctionExtensions.cs:114`)
and `Pipe` composes the next transform (`:16`). The raw-line difference (8 → 3) is larger
than the semantic difference (12 → 11) because the fluent chain reflows, which is exactly
why raw LOC is reported separately. Honest losses: step-through debugging loses named
locals; a statement-bodied `Tap` lambda can hide arbitrary side effects inside what looks
like a data pipeline, so teams need a convention about observation lambdas being pure
audit. CFE's carrier-wrapping shows the cost of implementing `Tap` only on monads rather
than as a BCL-value extension: 13 semantic for the same two observations. F# reference:
`Option.iter` is the analogous observation primitive, but with the same side-effect
caution (`§15`).

## 9. W6 — Traversal with index/path context

**Scenario.** Parse rows of `sku:qty:price`; the failure identifies the failing row.
This is the workflow Goal 17 requires to compose paths such as
`customers[17].addresses[2].postalCode` without application-side string assembly
(`docs/goals/archive/0017-goal.md:2`).

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# | 11 | 18 | 29 | 27 |
| FunnySharp (current workaround) | 5 | 18 | 23 | 16 |

Idiomatic C# (entry + parser):

```csharp
public static Parsed<IReadOnlyList<OrderLine>> ParseAll(IReadOnlyList<string> rows)
{
    var lines = new List<OrderLine>(rows.Count);
    for (var index = 0; index < rows.Count; index++)
    {
        var parsed = ParseLine(rows[index]);
        if (!parsed.IsSuccess)
        {
            parsed.TryGetError(out var error);
            return Parsed<IReadOnlyList<OrderLine>>.Fail(
                new LocatedError($"rows[{index}]", error!));
        }

        lines.Add(parsed.Value!);
    }

    return Parsed<IReadOnlyList<OrderLine>>.Ok(lines);
}

public static Parsed<OrderLine> ParseLine(string row)
{
    var parts = row.Split(':');
    if (parts.Length != 3 ||
        !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) ||
        !decimal.TryParse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
    {
        return Parsed<OrderLine>.Fail(new InvalidPayload($"cannot parse row '{row}'"));
    }

    return Parsed<OrderLine>.Ok(new OrderLine(parts[0], quantity, price));
}
```

FunnySharp today (the index must be threaded manually; `Traverse` passes only the item):

```csharp
public static Result<IReadOnlyList<OrderLine>, OrderError> ParseAll(IReadOnlyList<string> rows) =>
    rows.Select((row, index) => (row, index))
        .Traverse(item => ParseLine(item.row)
            .MapError(error => (OrderError)new LocatedError($"rows[{item.index}]", error)));
```

**Compile evidence.** Both variants compile (`§17.2`).

**Assessment.** FunnySharp removes the manual accumulator/short-circuit loop: `Traverse`
returns the first failure and the successful list in source order
(`src/FunnySharp/SequenceExtensions.cs:86-107`), a 6-semantic-LOC entry against 15 for the
loop. The unavoidable loss is context: `Traverse`'s selector receives only the item, so
the index must be threaded through a tuple and the path string assembled by hand. This
workaround composes one level; it cannot produce nested paths such as
`customers[17].addresses[2].postalCode` compositionally, which Goal 17 requires
(`docs/goals/archive/0017-goal.md:2`). Decision gap: traversal location context is a Goal 17
commitment; the current `Traverse` surface has no index/key/path overload, and
`MapError` rewrites the error after the fact. Learning cost: a reader must know that
`Traverse` is eager, single-pass, and fail-fast; the XML contract says so
(`src/FunnySharp/SequenceExtensions.cs:29-35`). No competitor variant was compiled:
CFE 3.7.0 has no `Traverse`/`Sequence` for collections, Funcky has `WhereSelect` (a
filter/map for options) without a failure channel, and language-ext's `Validation.Apply`
tuple overloads accumulate but carry no index context either — the gap is not
competitor-specific. F# reference: `List.traverseResult` in FsToolkit.ErrorHandling is the
community equivalent; it is **not** part of FSharp.Core and was not surveyed here
(**UNVERIFIED: FsToolkit.ErrorHandling is outside the pinned baselines**).

## 10. W7 — Bounded parallel ordered fetch over `IAsyncEnumerable` with cancellation

**Scenario.** Fetch a price for every SKU with at most four requests in flight, yield
results in source order, and stop promptly on cancellation.

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# (operator + consumer) | 24 | 22 | 46 | 65 |
| FunnySharp (consumer only) | 4 | 3 | 7 | 14 |

Idiomatic C# operator + consumer (`Channel` + linked CTS + local producer; 53 raw lines
for the operator alone):

```csharp
public static async IAsyncEnumerable<TResult> SelectParallelAsync<TResult>(
    IAsyncEnumerable<string> source,
    int maxConcurrency,
    Func<string, CancellationToken, Task<TResult>> selector,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    using var concurrency = new SemaphoreSlim(maxConcurrency);
    var channel = Channel.CreateBounded<Task<TResult>>(maxConcurrency);
    var producer = ProduceAsync();
    try
    {
        await foreach (var pending in channel.Reader.ReadAllAsync(operation.Token))
        {
            try
            {
                yield return await pending.ConfigureAwait(false);
            }
            finally
            {
                concurrency.Release();
            }
        }
    }
    finally
    {
        operation.Cancel();
        try
        {
            await producer.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    async Task ProduceAsync()
    {
        try
        {
            await foreach (var item in source.WithCancellation(operation.Token).ConfigureAwait(false))
            {
                await concurrency.WaitAsync(operation.Token).ConfigureAwait(false);
                var pending = selector(item, operation.Token);
                await channel.Writer.WriteAsync(pending, operation.Token).ConfigureAwait(false);
            }

            channel.Writer.Complete();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            channel.Writer.TryComplete(exception);
        }
    }
}

public static async Task<int> SumFetchedAsync(
    IAsyncEnumerable<string> skus,
    Func<string, CancellationToken, Task<int>> fetch,
    CancellationToken cancellationToken)
{
    var total = 0;
    await foreach (var price in SelectParallelAsync(skus, 4, fetch, cancellationToken))
    {
        total += price;
    }

    return total;
}
```

FunnySharp consumer (the operator is library code):

```csharp
public static async Task<int> SumFetchedAsync(
    IAsyncEnumerable<string> skus,
    Func<string, CancellationToken, ValueTask<int>> fetch,
    CancellationToken cancellationToken)
{
    var total = 0;
    await foreach (var price in skus
        .SelectParallelValueAsync(4, fetch)
        .WithCancellation(cancellationToken))
    {
        total += price;
    }

    return total;
}
```

**Compile evidence.** Both variants compile (`§17.2`); the FunnySharp operator itself is
part of the referenced local library and was built as part of the same solution.

**Assessment.** This is the largest consumer-side reduction in the set (46 → 7 semantic;
65 → 14 raw): an entire correctness-sensitive concurrency protocol is replaced by one
operator call. The baseline grew in review (31/49 → 46/65 semantic/raw): bounding
started selectors to `maxConcurrency` needs its own semaphore slot, and surfacing an
upstream producer fault needs its own complete-with-exception path — precisely the
admission-control and failure-drain work the library owns for the consumer. FunnySharp
removes admission control, ordering, backpressure, linked
cancellation, and the failure-drain protocol from application code; the documented
contract is ordered output, at most `maxConcurrency` started selectors, first-failure
stop/cancel/drain, and the caller's token forwarded exactly
(`src/FunnySharp/ParallelAsyncEnumerableExtensions.cs:65-75`, product statement at
`docs/product-contract.md §"Product Direction"`). Honest losses: the complexity moved into 530 lines of
library code (`src/FunnySharp/ParallelAsyncEnumerableExtensions.cs`), so application
teams trade implementation control for a behavioral contract they must trust and
upgrade; a bug in the operator becomes a bug in every consumer. Performance character:
the library uses `Channel` backpressure and a custom `IValueTaskSource<bool>` enumerator,
which is a deliberate allocation/throughput trade that only benchmarks can validate —
none were run here (**UNVERIFIED: no benchmark run; see `eng/performance/baseline.json`
for existing measured data, not consulted for this call site**). Learning cost: consumers
must know the difference between `SelectParallelValueAsync` (bounded, ordered, stream)
and `TraverseParallelValueAsync` (materialized, failure/accumulation variants). No
competitor variant: none of CFE 3.7.0, Funcky 3.6.0, or LanguageExt.Core 4.4.9 exposes a
bounded ordered `IAsyncEnumerable` map in its surveyed public surface.

## 11. W8 — First success across cold operations with timeout

**Scenario.** Race several cold invoice providers; return the first success, cancel and
drain losers, honor a timeout and a caller token, and keep typed failures distinct from
faults and cancellation.

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# (`Task.WhenAny`) | 11 | 14 | 25 | 25 |
| FunnySharp | 4 | 10 | 14 | 14 |

Idiomatic C# baseline:

```csharp
public static async Task<Invoice> FirstInvoiceAsync(
    IReadOnlyList<Func<CancellationToken, Task<Invoice>>> providers,
    TimeSpan timeout,
    CancellationToken cancellationToken)
{
    using var timeoutSource = new CancellationTokenSource(timeout, TimeProvider.System);
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken,
        timeoutSource.Token);
    var pending = providers.Select(provider => provider(linked.Token)).ToList();
    while (pending.Count > 0)
    {
        var completed = await Task.WhenAny(pending).ConfigureAwait(false);
        pending.Remove(completed);
        try
        {
            return await completed.ConfigureAwait(false);
        }
        catch (Exception) when (!linked.IsCancellationRequested)
        {
        }
    }

    cancellationToken.ThrowIfCancellationRequested();
    throw new TimeoutException($"No invoice provider succeeded within {timeout}.");
}
```

FunnySharp:

```csharp
public static async Task<Invoice> FirstInvoiceAsync(
    IReadOnlyList<Func<CancellationToken, Task<Invoice>>> providers,
    TimeSpan timeout,
    CancellationToken cancellationToken)
{
    var effects = providers.Select(provider =>
        Effect.FromTask<Result<Invoice, string>>(
            async token => Result<Invoice, string>.Success(await provider(token).ConfigureAwait(false))));
    var outcome = await effects.FirstSuccessAsync(timeout, cancellationToken).ConfigureAwait(false);
    return outcome.Match(
        invoice => invoice,
        errors => throw new InvalidOperationException(
            $"All providers failed: {string.Join("; ", errors)}"));
}
```

**Compile evidence.** Both variants compile (`§17.2`).

**Assessment.** FunnySharp removes the entire race protocol: `FirstSuccessAsync` starts
every cold effect, returns the first observed success as a valid `Validation`, cancels and
drains remaining work after a winner, maps all-typed-failure to an invalid `Validation`
with errors in input order, and throws `TimeoutException` on timeout or
`OperationCanceledException` for the caller token
(`src/FunnySharp/ConcurrentEffectExtensions.cs:53-85`, implementation `:115-293`). The
baseline's weaknesses are visible in its own code: it does not cancel or drain losers, it
swallows ordinary faults (`catch (Exception) when (!linked.IsCancellationRequested)`), and
it cannot distinguish "all providers returned typed failures" from "everything faulted".
Those are real semantic additions, not just LOC. Honest losses: providers must be cold
`Effect<Result<T,E>>` values, so already-started `Task`s cannot participate; all-typed
failure surfaces as a `Validation` that the caller must `Match`, which reuses the
accumulation carrier for a non-accumulation meaning; and a `TimeoutException` from
`FirstSuccessAsync` can be confused with a domain timeout unless callers separate the
channels. Performance character: all effects start concurrently and losers are awaited
during drain, so a slow loser delays return only until its cancellation is observed —
no hidden `Task.Run`, and the token is threaded to started effects. No competitor variant:
none of the three packages offers first-success coordination in the surveyed surface.

## 12. W9 — Environment + resource scope with cancellation

**Scenario.** Open a session from an explicit environment, load order lines, and dispose
the session even on failure or cancellation.

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# (`await using`) | 3 | 6 | 9 | 11 |
| FunnySharp | 2 | 8 | 10 | 14 |

Idiomatic C#:

```csharp
public static async Task<decimal> LoadOrderTotalAsync(
    PricingEnvironment environment,
    string orderId,
    CancellationToken cancellationToken)
{
    await using var session = await environment.Connections
        .OpenSessionAsync(cancellationToken)
        .ConfigureAwait(false);
    var lines = await session.LoadOrderLinesAsync(orderId, cancellationToken).ConfigureAwait(false);
    return lines.Sum(line => line.Quantity * line.UnitPrice);
}
```

FunnySharp:

```csharp
public static Task<decimal> LoadOrderTotalAsync(
    PricingEnvironment environment,
    string orderId,
    CancellationToken cancellationToken)
{
    var effect = Effect
        .FromTask<PricingEnvironment, DbSession>(
            (env, token) => env.Connections.OpenSessionAsync(token))
        .UsingAsync(session => Effect
            .FromTask<PricingEnvironment, IReadOnlyList<OrderLine>>(
                (_, token) => session.LoadOrderLinesAsync(orderId, token))
            .Map(lines => lines.Sum(line => line.Quantity * line.UnitPrice)));
    return effect.RunAsync(environment, cancellationToken).AsTask();
}
```

**Compile evidence.** Both variants compile (`§17.2`).

**Assessment.** FunnySharp is *larger* here (10 vs 9 semantic; 14 vs 11 raw) and that is
the honest result: for one acquisition and one use, `await using` is already excellent.
What the effect encoding adds is deferral and composition: the acquisition/use/disposal
protocol is a value that can be stored, retried, or scheduled before anything runs;
`UsingAsync` guarantees disposal in a `finally` even when the use effect fails, and turns
a null acquisition into a typed `InvalidOperationException` rather than a
`NullReferenceException` (`src/FunnySharp/EffectResourceExtensions.cs:102-122`). The
environment parameter makes the dependency explicit instead of captured. Honest losses:
one extra vocabulary (`Effect<TEnvironment,T>`, `RunAsync`, `AsTask`) for a workflow the
BCL handles with a keyword; `RunAsync` must be called by the consumer or nothing happens,
which is a new failure mode (silently-building an effect and never running it); the
environment is threaded manually rather than via DI. Recommendation signal for Goal 14:
adoption is justified where effects are composed or retried, not for single-scope code.
No competitor variant was compiled: CFE 3.7.0 has no effect/resource carrier; language-ext
has `Eff`/`Aff` (`ToEff` appears on `Validation`, `LanguageExt.Core.xml`), but its
resource/`use` combinators were not surveyed or compiled here
(**UNVERIFIED: no Eff/Aff inventory was generated in this workstream**).

## 13. W10 — Nested record update with immutable collections

**Scenario.** Set a nested postal code and append to a nested immutable tag list on an
`Order` record graph.

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# (`with`) | 1 | 4 | 5 | 9 |
| FunnySharp (entry + one-time lens definitions) | 4 | 12 | 16 | 4 + 17 |
| language-ext 4.4.9 (entry + one-time lens definitions) | 4 | 12 | 16 | 4 + 18 |

Idiomatic C#:

```csharp
public static Order ReviewPostalCode(Order order, string postalCode) =>
    order with
    {
        Customer = order.Customer with
        {
            PrimaryAddress = order.Customer.PrimaryAddress with { PostalCode = postalCode },
            Tags = order.Customer.Tags.Add("address-reviewed"),
        },
    };
```

FunnySharp (entry + the three lenses; two updates are needed because 0.1.0 has no
multi-focus combinator):

```csharp
public static Order ReviewPostalCode(Order order, string postalCode) =>
    OrderTags.Update(
        OrderAddress.Compose(AddressPostalCode).Update(order, _ => postalCode),
        tags => tags.Add("address-reviewed"));

private static readonly Lens<Order, Address> OrderAddress =
    Lens.Create<Order, Address>(
        order => order.Customer.PrimaryAddress,
        (order, address) => order with
        {
            Customer = order.Customer with { PrimaryAddress = address },
        });

private static readonly Lens<Address, string> AddressPostalCode =
    Lens.Create<Address, string>(
        address => address.PostalCode,
        (address, postalCode) => address with { PostalCode = postalCode });

private static readonly Lens<Order, ImmutableArray<string>> OrderTags =
    Lens.Create<Order, ImmutableArray<string>>(
        order => order.Customer.Tags,
        (order, tags) => order with
        {
            Customer = order.Customer with { Tags = tags },
        });
```

language-ext 4.4.9 (its `Lens` has no `Compose`; composition is `Prelude.lens(...)`):

```csharp
public static Order ReviewPostalCode(Order order, string postalCode) =>
    OrderTags.Update(
        tags => tags.Add("address-reviewed"),
        lens(OrderAddress, AddressPostalCode).Update(_ => postalCode, order));

private static readonly Lens<Order, Address> OrderAddress =
    Lens<Order, Address>.New(
        order => order.Customer.PrimaryAddress,
        address => order => order with
        {
            Customer = order.Customer with { PrimaryAddress = address },
        });
```

**Compile evidence.** All three variants compile (`§17.2`).

**Assessment.** For a single update, nested `with` is unbeatable (5 semantic, 9 raw). The
lens version costs 11 semantic in one-time definitions (17 raw) but the call site is then
5 semantic and, more importantly, reusable: `OrderAddress.Compose(AddressPostalCode)`
names a read/write path that can be passed around, tested once, and applied to every
update site, while `with` expressions are re-spelled at every site and silently drift.
What FunnySharp adds: composable named focus with `Lens.Create`/`Compose`/`Update`
(`src/FunnySharp/Optics.cs:17,99,84`), with setter delegates owned by the caller — the
contract explicitly keeps copying/aliasing with the caller and provides no lent-view or
persistent-collection universe (`docs/product-contract.md §"Product Direction"`). Honest losses: 17 raw
lines of definitions for a workflow that may only update once; lens laws are caller
obligations, so a bad getter/setter pair compiles and misbehaves; there is no traversal
or multi-focus combinator, so the second field (tags) needs a second, sequential update.
language-ext is the mirror image: 18 raw definitions, no `Compose` on `Lens`, and a
curried `SetF` shape that is harder to read; its composition helper `Prelude.lens(...)`
compensates. Performance character: both variants are `with`-based copies; `ImmutableArray<T>.Add`
copies the tag array (O(n)) in both cases, and the lens adds only delegate indirection.
Serialization impact: lenses are static delegates and are not serialized; the record graph
stays `System.Text.Json`-compatible. No F# reference is needed here; F# records update
natively.

## 14. W11 — Minimal API mapping Option/Result/Validation/Effect to HTTP results

**Scenario.** Three minimal-API endpoints: 404 for a missing customer, 400 with a problem
detail for a rejected order, and a validation problem for a bad signup form; plus an
effect-based order creation that uses the request-abort token.

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# (`IResult` branches) | 13 | 21 | 34 | 43 |
| FunnySharp.AspNetCore | 4 | 17 | 21 | 33 |

Idiomatic C# (`call-sites-code/funnysharp-aspnet/Workflows.cs`):

```csharp
public static IResult GetCustomerIdiomatic(
    IReadOnlyDictionary<string, Customer> customers,
    string customerId)
{
    if (!customers.TryGetValue(customerId, out var customer))
    {
        return Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "customer-not-found",
            detail: $"No customer '{customerId}' exists.");
    }

    return Results.Ok(customer);
}

public static async Task<IResult> CreateOrderIdiomaticAsync(
    OrderRequest request,
    IOrderService service,
    HttpContext context)
{
    if (request.Total is < 0 or > 100_000)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "amount-out-of-range",
            detail: $"Total {request.Total} must be between 0 and 100000.");
    }

    var order = await service.CreateAsync(request, context.RequestAborted);
    return Results.Created($"/orders/{order.Id}", order);
}

public static IResult SignUpIdiomatic(SignupForm form)
{
    var errors = new Dictionary<string, string[]>();
    if (!form.Email.Contains('@'))
    {
        errors["email"] = ["email must contain '@'"];
    }

    if (form.Password.Length < 12)
    {
        errors["password"] = ["password must have at least 12 characters"];
    }

    return errors.Count == 0
        ? Results.Created("/signup", form)
        : Results.ValidationProblem(errors);
}
```

FunnySharp.AspNetCore:

```csharp
using FunnySharp;
using FunnySharp.AspNetCore;

public static IResult GetCustomer(
    IReadOnlyDictionary<string, Customer> customers,
    string customerId) =>
    customers.GetOption(customerId).ToHttpResult(
        () => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "customer-not-found",
            Detail = $"No customer '{customerId}' exists.",
        });

public static IResult CreateOrder(Result<Order, OrderError> result) =>
    result.ToHttpResult(
        error => new ProblemDetails
        {
            Status = error.Status,
            Title = error.Title,
            Detail = error.Detail,
        },
        order => Results.Created($"/orders/{order.Id}", order));

public static IResult SignUp(Validation<SignupForm, string> validation) =>
    validation.ToHttpResult(
        errors => new HttpValidationProblemDetails(
            new Dictionary<string, string[]> { ["form"] = errors.ToArray() })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "validation-failed",
        },
        form => Results.Created("/signup", form));

public static ValueTask<IResult> CreateOrderFromEffectAsync(
    Effect<Result<Order, OrderError>> effect,
    HttpContext context) =>
    effect.ToHttpResultAsync(
        context,
        error => new ProblemDetails
        {
            Status = error.Status,
            Title = error.Title,
            Detail = error.Detail,
        },
        order => Results.Created($"/orders/{order.Id}", order));
```

**Compile evidence.** Idiomatic and FunnySharp variants both compile in
`funnysharp-aspnet/FunnySharpAspNetCallSites.csproj`, which references the local
`FunnySharp.AspNetCore` project and the `Microsoft.AspNetCore.App` framework reference
(`§17.2`).

**Assessment.** The mapping package removes per-endpoint branch plumbing: each outcome
type has exactly one mapping call, and the failure-to-`ProblemDetails` seam is explicit
and typed (`src/FunnySharp.AspNetCore/HttpResultExtensions.cs:20` for `Option`, `:38` for
`Result`, `:56` for `Validation`, `:204` for `Effect` with `HttpContext`). The effect
overload is the strongest addition: it runs the effect with `context.RequestAborted`
(`:212`), so request cancellation is wired without the consumer passing a token, while
`CreateOrderIdiomaticAsync` must remember `context.RequestAborted`. The validation mapping
also removes the hand-built `Dictionary<string,string[]>`: `Validation` already carries a
snapshot list, and `ToHttpResult` maps it to `HttpValidationProblemDetails`. Honest
losses: the `ProblemDetails` bodies are still built by lambdas at each endpoint, so
semantic LOC stays higher than the number of mapping calls; the validation mapping
collapses all field errors into a single `"form"` key because the error list is
untyped — per-field dictionary keys require a richer error type, which a vanilla
`Validation<T,string>` cannot express; and FunnySharp.AspNetCore is a second package with
an ASP.NET Core framework reference, which the product contract permits but which must
stay optional (`docs/product-contract.md §"Package And Dependency Boundary"`). No competitor variant exists: CFE 3.7.0,
Funcky 3.6.0, and LanguageExt.Core 4.4.9 have no ASP.NET Core integration in the pinned
assemblies.

## 15. F# reference snippets (not compiled)

Reference only, from pinned Microsoft Learn pages; no F# project was created and no F#
claim in this document depends on compilation.

**Results** — `https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/results`,
source commit `e7add279618e90c6d3a3e379dd4d502e1017c88d`, retrieved 2026-09-17. The
fail-fast pipeline shape that W2 mirrors:

```fsharp
let validateRequest reqResult =
    reqResult
    |> Result.bind validateName
    |> Result.bind validateEmail
```

**Options** — `https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/options`,
source commit `e3ae3bdeecda6b7f300fe22a5fcd53e22d0c43c8`, retrieved 2026-09-17. The
fallback shape that W1 mirrors:

```fsharp
let optionInt = None
let defaultInt = optionInt |> Option.defaultValue 0
// defaultInt is 0
```

**Accumulation** — FSharp.Core has no accumulating applicative for `Result`; the Results
page above documents `Result.bind` (fail-fast) only. The applicative validation pattern in
F# is provided by the community `FsToolkit.ErrorHandling` library, which is not among the
pinned baselines and was not surveyed
(**UNVERIFIED: FsToolkit.ErrorHandling is outside the pinned baselines**).

## 16. Cross-cutting observations

1. **Reduction is concentrated where FunnySharp owns a protocol.** W7 (46→7), W8 (25→14),
   and W11 (34→21) are the only workflows with double-digit semantic savings, and all
   three are cases where the library implements a coordination/cancellation/mapping
   protocol that application code would otherwise hand-roll.
2. **Typed outcomes are roughly LOC-neutral at small scale** (W2 26→28, W4 14→17) and
   become favorable with composition; their gain is compile-time verifiability and
   elimination of conventions, not statement count.
3. **Accumulation is the exception**: at three fields, `Validation` costs more semantic
   LOC than a `List<string>` loop (W3 19→23). The justification for `Validation` is typed
   reusable validators and guaranteed error retention, not density.
4. **The async seam is the sharpest friction point found.** `BindAsync` returns
   `Task<Result<...>>` and there is no `Map` on task carriers, so a single mixed
   sync/async chain cannot be written as one expression; the same limitation exists in
   CFE. This is call-site evidence for a redesign candidate in the decision record.
5. **`UnitResult<TError>` is the clearest concrete API gap** (W4): Goal 15 commits to it
   (`docs/goals/archive/0015-goal.md:2`) and the current workaround uses a dummy payload.
6. **Location context is the second concrete gap** (W6): Goal 17 requires compositional
   paths (`docs/goals/archive/0017-goal.md:2`) and `Traverse` has no index/key/path surface, so
   even the workaround cannot express nesting.
7. **`Effect`/resource/effect-based HTTP mapping are wins only when composed.** W9 is
   larger than `await using` (9→10) and only pays off with reuse/retry; the W11 effect
   overload is a pure win because it also wires `RequestAborted` (`:204-213`).
8. **Optics are a one-time-cost decision.** W10's lenses cost 17 raw definitions for one
   update; the product contract's deliberately small `Lens`/`Optional` surface
   (`docs/product-contract.md §"Product Direction"`) means consumers should define lenses only for paths
   updated in more than one place.
9. **Competitor positioning from the compiled set.** Funcky has the best micro-ergonomics
   for dictionary + parse (W1, 5 vs 6); CFE has the carrier FunnySharp lacks
   (`UnitResult<E>`) and typed-error `Result<T,E>` pipelines, but no `Validation`, no
   traversal, no optics, and no HTTP integration; language-ext has tuple-`Apply`
   accumulation (22 in W3) and lens composition via `Prelude.lens`, but its `Lens` lacks
   `Compose`, its `Fail` requires `Seq<FAIL>`, and its two-arity `Apply` is an extension
   whose monoid behavior is easy to miss. None of the three offers bounded ordered async
   mapping, first-success coordination, or resource-scoped effects at the call sites
   tested.
10. **Raw LOC and semantic LOC disagree in both directions.** Fluent chains compress
    raw lines far more than semantic operations (W5 raw 8→3, semantic 12→11), and the
    idiomatic channel operator is 53 raw lines, which would overstate its semantic cost.
    The semantic metric is the one to use for the Goal 14 criterion.
11. **All scratch builds are warning-clean.** Every variant compiled with 0 warnings and
    0 errors in Release, with no suppressions, which means the evidence does not depend
    on nullable or unused-code pragmas (`§17`).

## 17. Compile evidence appendix

### 17.1 Environment

```
$ dotnet --version
10.0.400
$ dotnet --list-runtimes        # relevant entries
Microsoft.AspNetCore.App 10.0.11 [/home/azureuser/.dotnet/shared/Microsoft.AspNetCore.App]
Microsoft.NETCore.App 10.0.11 [/home/azureuser/.dotnet/shared/Microsoft.NETCore.App]
```

FunnySharp was built from `main` at `4dbebd94b7b58648632112b7ca47c39cc517f153`
(`git rev-parse HEAD`), version 0.1.0. Scratch projects referenced the sources directly:

```xml
<ProjectReference Include="/home/azureuser/repos/funnysharp/src/FunnySharp/FunnySharp.csproj" />
<ProjectReference Include="/home/azureuser/repos/funnysharp/src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj" />
```

The copied project files under `call-sites-code/` contain these absolute paths verbatim;
adjust them before re-running elsewhere. `call-sites-code/NuGet.config` points at a local
folder feed containing the three pinned nupkgs plus nuget.org for
`Microsoft.Bcl.AsyncInterfaces 7.0.0`.

### 17.2 Commands and results

All builds were clean rebuilds (`bin/` and `obj/` removed first), Release configuration,
in this order:

```bash
export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$DOTNET_ROOT:$PATH"
cd /tmp/opencode/callsites/idiomatic        && dotnet build -c Release   # exit 0
cd /tmp/opencode/callsites/funnysharp       && dotnet build -c Release   # exit 0
cd /tmp/opencode/callsites/funnysharp-aspnet && dotnet build -c Release # exit 0
cd /tmp/opencode/callsites/competitors      && dotnet build -c Release   # exit 0
```

Output summary (representative, from the `competitors` log; the other three logs differ
only in paths and elapsed time):

```
  Restored /tmp/opencode/callsites/competitors/Competitors.csproj (...)
  Competitors -> /tmp/opencode/callsites/competitors/bin/Release/net10.0/Competitors.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

| Scratch project | Target | Result | Warnings | Errors |
| --- | --- | --- | ---: | ---: |
| `idiomatic` (BCL only) | `net10.0` library | succeeded | 0 | 0 |
| `funnysharp` (local `ProjectReference`) | `net10.0` library | succeeded | 0 | 0 |
| `funnysharp-aspnet` (local `FunnySharp.AspNetCore` + `Microsoft.AspNetCore.App`) | `net10.0` library | succeeded | 0 | 0 |
| `competitors` (CFE 3.7.0, Funcky 3.6.0, LanguageExt.Core 4.4.9) | `net10.0` library | succeeded | 0 | 0 |

Restored competitor dependency graph: `CSharpFunctionalExtensions 3.7.0`, `Funcky 3.6.0`,
`LanguageExt.Core 4.4.9`, `Microsoft.Bcl.AsyncInterfaces 7.0.0`. The three pinned nupkgs
were hashed locally and match the pins in §1.

### 17.3 Exact project files used

`idiomatic/Idiomatic.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <RootNamespace>CallSites.Idiomatic</RootNamespace>
    <OutputType>Library</OutputType>
  </PropertyGroup>
</Project>
```

`competitors/Competitors.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <RootNamespace>CallSites.Competitors</RootNamespace>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CSharpFunctionalExtensions" Version="3.7.0" />
    <PackageReference Include="Funcky" Version="3.6.0" />
    <PackageReference Include="LanguageExt.Core" Version="4.4.9" />
  </ItemGroup>
</Project>
```

The FunnySharp projects differ only in `RootNamespace`, `OutputType`, and the
`ProjectReference`/`FrameworkReference` items shown in §17.1; the verbatim files are in
`call-sites-code/funnysharp/FunnySharpCallSites.csproj` and
`call-sites-code/funnysharp-aspnet/FunnySharpAspNetCallSites.csproj`.

### 17.4 Raw LOC tooling

`call-sites-code/tools/` contains `loc.py` (extract a method body by name) and
`rawloc.py` (batch report used for the raw column). Run from `call-sites-code/`:

```bash
python3 tools/rawloc.py
```

Semantic LOC was counted by hand under the §2 rule; the per-workflow S+O breakdown is in
the tables so any disagreement can be localized to one element.

## 18. Not examined

- **F# compilation**: F# snippets are quoted from pinned Microsoft Learn pages only; no
  F# project or FSharp.Core compilation was performed (`§15`).
- **Benchmarks and allocation measurement**: no `BenchmarkDotNet` or allocation run was
  made in this workstream; all performance characterizations are structural, and
  quantitative claims beyond the source-level notes are marked UNVERIFIED. Existing
  measured data in `eng/performance/baseline.json`, `benchmarks/`, and the generated
  `docs/*.md` tables was not consulted for these call sites.
- **language-ext `Eff`/`Aff`**: W9's closest language-ext equivalent was not compiled and
  its resource/`use` surface was not inventoried (`UNVERIFIED`).
- **Funcky analyzers**: Funcky 3.6.0 ships Roslyn analyzers and code fixes; their effect
  on these workflows (e.g., dictionary bridge or tap diagnostics) was not observed
  because the scratch builds do not run analyzer-driven code fixes.
- **CFE `Maybe`/`Result` async surfaces beyond W2**: only the APIs used by the compiled
  variants were inspected; the full async extension set was not enumerated.
- **`System.Text.Json` behavior** of the carriers, `FrozenDictionary`/`FrozenSet`
  interactions, and trimming/Native AOT behavior of these call sites were not tested.
- **W7/W8 behavior under faulting sources**: the compiled code exercises the shapes, but
  no runtime execution (only compilation) was performed; behavioral claims come from the
  XML/source contracts, not from observed runs.

## 19. Files

Scratch roots (not in the repository):

- `/tmp/opencode/callsites/idiomatic/` — BCL-only baselines.
- `/tmp/opencode/callsites/funnysharp/` — FunnySharp W1–W10.
- `/tmp/opencode/callsites/funnysharp-aspnet/` — W11 on FunnySharp.AspNetCore.
- `/tmp/opencode/callsites/competitors/` — W1, W2, W3, W3b, W4, W5, W10 on the three
  competitor packages.
- `/tmp/opencode/callsites/build-*.log` — raw build logs.
- `/tmp/opencode/callsites/raw-loc-report.txt` — raw counter output.

Committed copies (verbatim, machine-specific paths preserved):

- `docs/next-stage/call-sites-code/README.md` — build instructions for the copy.
- `docs/next-stage/call-sites-code/{idiomatic,funnysharp,funnysharp-aspnet,competitors}/`.
- `docs/next-stage/call-sites-code/tools/`.
