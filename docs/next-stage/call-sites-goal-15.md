# Goal 15 Call-Site Comparison Evidence

Follow-up evidence for the Goal 14 decision record. `docs/next-stage/call-sites.md` named two
concrete call-site problems: W4 had no carrier for "success carries no value" and shipped a
dummy `bool` payload, and W3 had to index nested tuples (`parts.First.First`) because `Zip`
only produced pairs. Goal 15 adds `UnitResult<TError>` and bounded arity-2/3 `Zip` combiners,
so this document re-runs W4 and W3 against that surface and adds a third workflow,
`SubmitOrderAsync`, that crosses from a value-producing pipeline to a no-value command.

This is evidence, not a decision. It does not replace the Goal 14 record: `call-sites.md`
remains the pinned comparison for the W1-W11 scenarios listed there. Only W3 and W4 are
re-compared against compiled idiomatic baselines; the `SubmitOrderAsync` idiomatic baseline
is inline and illustrative, not compiled (stated again in `§5` and `§7`).

## 1. Provenance

| Item | Pin used here |
| --- | --- |
| Goal 14 base commit | `2834fee` (`docs: add next-stage product constitution and capability decisions (#16)`) |
| Goal 15 call-site worktree | branch `g15-callsites` based on `c1a5198` (Goal 15 implementation/tests line); evidence commit is the one that adds this file |
| Goal 15 surface | `UnitResult<TError>` (`src/FunnySharp/UnitResult.cs`, `UnitResultExtensions.cs`) plus bounded arity-2/3 `Zip` combiners for `Option`, `Result`, and `Validation` (`src/FunnySharp/{Option,Result,Validation}.cs`); the surface is described by `docs/goals/archive/0015-goal.md` |
| FunnySharp reference | local `ProjectReference` to `/home/azureuser/repos/funnysharp/src/FunnySharp/FunnySharp.csproj` (main checkout, which contains the Goal 15 API); the absolute path is intentionally machine-specific in the scratch copy |
| Scratch project | `docs/next-stage/call-sites-code/funnysharp/FunnySharpCallSites.csproj` (net10.0, Release) |
| .NET SDK | 10.0.400 (`dotnet --version`) |

Exact build command and result:

```bash
$ dotnet build docs/next-stage/call-sites-code/funnysharp/FunnySharpCallSites.csproj -c Release --nologo
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

A forced clean rebuild (`-t:Rebuild`) of the same project also produced `0 Warning(s)`,
`0 Error(s)`. The Goal 15 variants are in
`docs/next-stage/call-sites-code/funnysharp/Goal15Workflows.cs` (namespace
`CallSites.FunnySharp`, class `Goal15Workflows`); no Goal 14 file was modified.

## 2. Counting rule used here

Same rule as `call-sites.md` §2: **S** is one per statement (including expression-bodied
member bodies), **O** is one per operation inside a statement (method call, `new`, `await`,
`is`/relational pattern, `?:`, `!`, `&&`/`||`, comparison/arithmetic operator, interpolated
string hole, indexer, collection expression), and property/field access, `ConfigureAwait(false)`,
declaration headers, and comments are excluded. Raw LOC is machine-counted by
`docs/next-stage/call-sites-code/tools/loc.py` (imported unchanged from `python3`).

The idiomatic and 0.1.0-workaround rows below are quoted from the Goal 14 tables
(`call-sites.md` §3, §6, §7), which used the same rule; the Goal 15 rows and the
`SubmitOrderAsync` pair are hand-counted here with the S/O split shown so a reviewer can
disagree with one element instead of the total.

## 3. W4 — Delete-or-notify, success carries no value

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# (Goal 14) | 6 | 8 | **14** | 15 |
| FunnySharp 0.1.0 workaround (Goal 14) | 6 | 11 | **17** | 15 |
| FunnySharp Goal 15 (`UnitResult<TError>`) | 6 | 11 | **17** | 15 |

Idiomatic C# — `call-sites-code/idiomatic/Workflows.cs`:

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

FunnySharp Goal 15 — `call-sites-code/funnysharp/Goal15Workflows.cs`:

```csharp
public static async Task<UnitResult<OrderError>> DeleteOrderAsync(
    string orderId,
    IOrderStore store,
    INotifier notifier,
    CancellationToken cancellationToken)
{
    var existing = await store.FindAsync(orderId, cancellationToken).ConfigureAwait(false);
    if (existing is null)
    {
        return UnitResult<OrderError>.Failure(new CustomerNotFound(orderId));
    }

    await store.DeleteAsync(orderId, cancellationToken).ConfigureAwait(false);
    await notifier.NotifyAsync($"order {orderId} deleted", cancellationToken).ConfigureAwait(false);
    return UnitResult<OrderError>.Success();
}
```

**What changed from the Goal 14 workaround.** The control flow is identical; only the two
boundary values changed, and they no longer fabricate a value:

```diff
-        return Result<bool, OrderError>.Failure(new CustomerNotFound(orderId));
+        return UnitResult<OrderError>.Failure(new CustomerNotFound(orderId));
...
-        return Result<bool, OrderError>.Success(true);
+        return UnitResult<OrderError>.Success();
```

The semantic count is unchanged (6 S / 11 O, raw 15). The win is not density:
`Result<bool, OrderError>` forced every reader and every consumer to interpret a `bool`
that means nothing, and the compiler could not distinguish "succeeded with `false`" from a
legitimate value. `UnitResult<OrderError>` makes the no-payload contract part of the
signature, keeps the `CustomerNotFound` failure object typed and intact, and cannot leak a
dummy success value. The `null`-means-success idiomatic baseline is still the smallest
element count at 14, but that convention is unverifiable at compile time.

## 4. W3 — Independent field validation accumulating all errors

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# (Goal 14) | 8 | 11 | **19** | 17 |
| FunnySharp 0.1.0 workaround (Goal 14) | 4 | 19 | **23** | 17 |
| FunnySharp Goal 15 (arity-3 `Zip`) | 4 | 17 | **21** | 17 |

Idiomatic C# — `call-sites-code/idiomatic/Workflows.cs`:

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

FunnySharp Goal 15 — `call-sites-code/funnysharp/Goal15Workflows.cs` (entry plus the three
validators it composes; the validators are unchanged from the Goal 14 variant):

```csharp
public static Validation<SignupForm, string> ValidateSignup(SignupForm form) =>
    ValidateEmail(form.Email).Zip(
        ValidatePassword(form.Password),
        ValidateAge(form.Age),
        (email, password, age) => new SignupForm(email, password, age));

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

**What changed from the Goal 14 workaround.** The entry expression loses one `Zip`, one
`Map`, and all nested tuple indexing:

```diff
-    ValidateEmail(form.Email)
-        .Zip(ValidatePassword(form.Password))
-        .Zip(ValidateAge(form.Age))
-        .Map(parts => new SignupForm(parts.First.First, parts.First.Second, parts.Second));
+    ValidateEmail(form.Email).Zip(
+        ValidatePassword(form.Password),
+        ValidateAge(form.Age),
+        (email, password, age) => new SignupForm(email, password, age));
```

Counted unit: entry 1 S / 5 O plus validators 3 S / 12 O, total 4 S / 17 O = 21 semantic
(Goal 14 workaround: entry 1 S / 7 O plus the same 3 S / 12 O = 23). Raw LOC stays at 17
because the removed chaining is exchanged for line breaks in the arity-3 call. The
idiomatic baseline (8 S / 11 O) remains the smallest at this three-field scale; what the
Goal 15 form adds over it is typed per-field errors, reusable validators, and guaranteed
error retention, and what it now removes is the pair-nesting wart the Goal 14 record
called out. Error order is visible in the combiner argument order: operands are supplied
email, password, age; `Zip` accumulates in left-to-right operand order
(`src/FunnySharp/Validation.cs:281-314`), and the combiner parameters `(email, password,
age)` mirror that order, so a reader can see the error order at the call site.

## 5. Extra Goal 15 workflow — `SubmitOrderAsync` (value pipeline, command boundary)

`SubmitOrderAsync` exercises the value-to-command boundary: a fail-fast `Result`
composition that produces a value (the invoice) and ends as a no-value command through
`ToUnitResultAsync`. Goal 15's `ToUnitResultAsync` is defined on
`Task<Result<TValue, TError>>` (`src/FunnySharp/UnitResultExtensions.cs:36`).

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# (inline, illustrative, **not compiled**) | 12 | 10 | **22** | 28 |
| FunnySharp Goal 15 (entry + 2 private helpers) | 5 | 24 | **29** | 26 |

FunnySharp Goal 15 — `call-sites-code/funnysharp/Goal15Workflows.cs`:

```csharp
public static Task<UnitResult<OrderError>> SubmitOrderAsync(
    OrderRequest request,
    ICustomerRepository repository,
    CancellationToken cancellationToken) =>
    Result<OrderRequest, OrderError>.Success(request)
        .Ensure(r => r.CustomerId.Length > 0, _ => new InvalidPayload("CustomerId is required."))
        .Ensure(r => !r.Lines.IsEmpty, _ => new NoLines(request.OrderId))
        .Ensure(r => r.Lines.All(line => line.Quantity > 0), InvalidQuantityFor)
        .BindAsync(r => ResolveInvoiceAsync(request, repository, r.CustomerId, cancellationToken))
        .ToUnitResultAsync();

private static async Task<Result<Invoice, OrderError>> ResolveInvoiceAsync(
    OrderRequest request,
    ICustomerRepository repository,
    string customerId,
    CancellationToken cancellationToken)
{
    var customer = await repository.FindCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
    return customer is null
        ? Result<Invoice, OrderError>.Failure(new CustomerNotFound(customerId))
        : Result<Invoice, OrderError>.Success(Domain.BuildInvoice(customer, request));
}

private static OrderError InvalidQuantityFor(OrderRequest request)
{
    var invalid = request.Lines.First(line => line.Quantity <= 0);
    return new InvalidQuantity(invalid.Sku, invalid.Quantity);
}
```

Idiomatic C# baseline, inline and illustrative only (not compiled, not part of
`idiomatic/Idiomatic.csproj`; the compiled idiomatic baselines are W3 and W4 above). It
produces the value and discards it at the module boundary with the usual `OrderError?`
null-means-success convention:

```csharp
public static async Task<OrderError?> SubmitOrderAsync(
    OrderRequest request,
    ICustomerRepository repository,
    CancellationToken cancellationToken)
{
    if (request.CustomerId.Length == 0)
    {
        return new InvalidPayload("CustomerId is required.");
    }

    if (request.Lines.IsEmpty)
    {
        return new NoLines(request.OrderId);
    }

    foreach (var line in request.Lines)
    {
        if (line.Quantity <= 0)
        {
            return new InvalidQuantity(line.Sku, line.Quantity);
        }
    }

    var customer = await repository.FindCustomerAsync(request.CustomerId, cancellationToken);
    if (customer is null)
    {
        return new CustomerNotFound(request.CustomerId);
    }

    _ = Domain.BuildInvoice(customer, request);
    return null;
}
```

The inline baseline is hand-counted at 12 S / 10 O = 22; its raw column (28) was measured
by running `tools/loc.py` on a temporary copy of the exact snippet text, because the
snippet is not a compiled project file. The Goal 15 variant counts its entry (1 S / 13 O)
plus the two private helpers (`ResolveInvoiceAsync` 2 S / 8 O, `InvalidQuantityFor`
2 S / 3 O) = 5 S / 24 O = 29 semantic; raw 26 from `tools/loc.py` on the committed file.
As in Goal 14's W2, the pipeline is denser per line but pays for typed-error helper
plumbing; at this scale the direct `OrderError?` guard list is smaller. The gain is the
single explicit boundary: the pipeline produces `Invoice` through `Result`, and
`ToUnitResultAsync` discards only the successful value while preserving the failure object
and short-circuit behavior.

## 6. Assessment

- **Carrier (W4):** no dummy value exists anywhere in the workflow or its signature; success
  is stateless and cannot be misread as a payload; the failure case still carries the exact
  `OrderError` object. The idiomatic `OrderError?` convention saves three semantic elements
  but cannot distinguish success from "forgot to check", which the carrier can.
- **Combiner (W3):** the arity-3 `Zip` removes nested tuples from the entry line and two
  operations (entry 7 O → 5 O, total 23 → 21) while keeping all three validators independent
  and reusable. The `parts.First.First` readability tax named by Goal 14 is gone.
- **Value/command boundary (`SubmitOrderAsync`):** `ToUnitResultAsync` is the single, explicit
  drop point from a value-producing chain to a command; the value (`Invoice`) is genuinely
  produced, and only the success payload is discarded.
- **Fail-fast vs accumulation boundary is preserved:** `SubmitOrderAsync` stays on `Result`
  because its checks are sequential (line validation must precede the dependent customer
  lookup) and must short-circuit; `ValidateSignup` stays on `Validation` because the three
  field rules are independent and all errors must survive. `Validation` still has no false
  `Bind` promise.
- **Error order is visible in the combiner argument order:** operands are passed email,
  password, age and the combiner receives `(email, password, age)`; library accumulation is
  left-to-right in the same order, so the call site text alone tells the reader the order in
  which field errors will appear.
- **Honest costs:** per-field validation needs a dedicated validator method per field
  (three helpers, 12 of the 17 raw lines in W3); the carrier and combiner vocabulary
  (`UnitResult<OrderError>`, `Zip(second, third, combine)`, uninitialized-throw contract)
  is one more thing a new reader must learn; and the library still cannot continue a fluent
  chain after `BindAsync` — see the friction note below.
- **API friction observed (reported, not fixed):** the natural single-expression form
  `...BindAsync(...).MapAsync(customer => Domain.BuildInvoice(customer, request)).ToUnitResultAsync()`
  does not compile because there is no `Map`/`MapAsync` on `Task<Result<TValue, TError>>` in
  this surface; the compiler's diagnostic was `CS1503: Argument 1: cannot convert from
  'System.Threading.CancellationToken' to 'CallSites.FunnySharp.Customer'` inside the
  `MapAsync` lambda, which points at an inapplicable overload rather than at the missing
  task-carrier member. The evidence works around it with the `ResolveInvoiceAsync` helper,
  which returns `Task<Result<Invoice, OrderError>>` and keeps the final
  `ToUnitResultAsync` conversion. This is the same async seam Goal 14 §16.4 recorded, and
  Goal 15's `ToUnitResultAsync` does not remove it from the value pipeline (`ToUnitResult`
  on an already-awaited `Result` would, but then the async conversion is lost).

## 7. Limitations

- Semantic LOC is directional, hand-derived evidence, not an integer-precise metric; the
  S/O split is printed so disagreement localizes to one element. Idiomatic and 0.1.0
  workaround rows are quoted from the Goal 14 tables and carry that record's numbers.
- `docs/next-stage/call-sites.md` (Goal 14) remains the pinned historical record for the
  listed W1-W11 scenarios; nothing in it was modified, and no W1-W11 count is restated here
  as authoritative.
- No benchmark, allocation, throughput, or runtime behavior claim is made. The evidence is
  compile-time only; `Zip` ordering and `ToUnitResultAsync` semantics are cited from the
  implementation and spec, not measured.
- The `SubmitOrderAsync` idiomatic baseline is inline illustrative code, not compiled; only
  W3 and W4 have compiled idiomatic baselines (`idiomatic/Idiomatic.csproj`).
- The scratch `ProjectReference` and its absolute path are machine-specific by design.

## 8. Compile evidence

`docs/next-stage/call-sites-code/funnysharp/Goal15Workflows.cs` is compiled by
`docs/next-stage/call-sites-code/funnysharp/FunnySharpCallSites.csproj`. Running

```bash
$ dotnet build docs/next-stage/call-sites-code/funnysharp/FunnySharpCallSites.csproj -c Release --nologo
```

completed with `Build succeeded. 0 Warning(s) 0 Error(s)` (and again with `-t:Rebuild`),
against the Goal 15 `FunnySharp` referenced from
`/home/azureuser/repos/funnysharp/src/FunnySharp/FunnySharp.csproj`.
