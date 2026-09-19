# Goal 16 Call-Site Comparison Evidence

Follow-up evidence for the Goal 16 decision record, in the footsteps of
`docs/next-stage/call-sites-goal-15.md`. Goal 16 is the function-grammar goal: piping,
composition, partial application, fallible composition, observation, recovery, running
aggregates, and the LINQ bridge. This document adds seven new workflows, numbered
**WF-1..WF-7** to avoid colliding with the W1-W11 numbering pinned in
`docs/next-stage/call-sites.md`, each exercising a part of that grammar against a compiled
idiomatic C# baseline.

This is evidence, not a decision. It does not replace the Goal 14 pinned comparisons and it
does not restate any W1-W11 count as authoritative: `call-sites.md` remains the record for
the W1-W11 scenarios, `call-sites-goal-15.md` remains the record for its three workflows,
and nothing in either file was modified. Both scratch projects build with 0 warnings and
0 errors (§13); every code block below is byte-identical to the committed scratch code,
and each names its file.

## 1. Provenance

| Item | Pin used here |
| --- | --- |
| Date | 2026-09-19 |
| HEAD commit | `ab9bbdb2a85049cc1ef482ebc6efc6404a741efd` (`feat(tooling): add cross-platform uv tooling beside release gate (#17)`), branch `main` |
| Goal 16 surface | the working tree of `src/FunnySharp` at evidence time — uncommitted on top of HEAD (`git status` shows uncommitted modifications from the Goal 16 implementation and sibling workstreams; the exact compiled surface is pinned by the `FunnySharp.dll` SHA256 below, identical across every build in §13); the surface exercised here is `Pipe`/`Compose`/`Curry`/`Uncurry`/`Partial`/`Flip`, `Tap`/`TapAsync`/`TapValueAsync`, `ComposeAsync`/`ComposeValueAsync`, arity-4 `Zip` combiners on `Option`/`Result`/`Validation`, `Option.Select`/`SelectMany`, `UnitResult.ToResult`, and the `Scan` family |
| FunnySharp reference | local `ProjectReference` to `/home/azureuser/repos/funnysharp/src/FunnySharp/FunnySharp.csproj` (absolute path intentionally machine-specific in the scratch copy, the Goal 14/15 pattern); built `FunnySharp.dll` SHA256 `3bed279ef52030bee45d0cd7ecfdde190d03452b9931cee3716a9f80b2321eba` |
| FunnySharp variants | `docs/next-stage/call-sites-code/funnysharp/Goal16Workflows.cs` (namespace `CallSites.FunnySharp`, class `Goal16Workflows`), compiled by `funnysharp/FunnySharpCallSites.csproj`; no Goal 14/15 file was modified |
| Idiomatic baselines | additive methods in `docs/next-stage/call-sites-code/idiomatic/Workflows.cs` (class `CallSites.Idiomatic.Goal16Workflows` plus harness types at the bottom of the file), compiled by `idiomatic/Idiomatic.csproj` |
| .NET SDK | 10.0.400 (`dotnet --version`) |

Exact build commands and results are in §13. In short: clean `bin`/`obj`, then
`dotnet build -c Release` for both projects, then a forced `-t:Rebuild` for both — all four
runs `Build succeeded. 0 Warning(s) 0 Error(s)`.

## 2. Counting rule used here

Same rule as `call-sites.md` §2: **S** is one per statement (`if`, `else`/`else if`,
loop headers, `return`, `throw`, local declaration with initializer, assignment, `using`,
`await foreach`, `yield return`, `catch`, invocation statements, and the body of an
expression-bodied member), **O** is one per operation inside a statement (method call,
`new`, indexer, `with` update, `await`, `is`/relational pattern, `?:`, `??`, `&&`, `||`,
`!`, comparison/arithmetic operator, interpolated-string hole, collection expression), and
property/field access, `ConfigureAwait(false)`, declaration headers, braces, blank lines,
and comments are excluded. Harness types and shared helper bodies are excluded from every
count, exactly as `Domain.cs` is in the Goal 14 record; their *invocations* count as
operations.

Four clarifications, stated so a reviewer can disagree with a specific element rather than
a total:

1. **One O per interpolated string, not per hole.** This is how the pinned Goal 14 rows
   reconcile exactly (W5 idiomatic 7 O and FunnySharp 10 O; W8 idiomatic 14 O), so it is
   applied here.
2. **`try`/`finally` headers count as one S each, like `catch`.** The W7 operator's
   pinned S=24 only reconciles with `try`/`finally` counted; the §2 enumeration lists
   `catch` but not `try`/`finally`.
3. **`?.` counts as one O per operator** (closest to `?:`, which §2 lists). This affects
   only the WF-7 idiomatic row; a reviewer who excludes them reads 2 O instead of 3 O.
4. **A bare local declaration without initializer is not an S** (§2 counts "local
   declaration with initializer"); `decimal weight;` in the WF-3 idiomatic baseline is
   such a declaration.

Raw LOC is measured per method with `python3 tools/loc.py <file> <method>` (non-blank,
non-comment lines including the declaration line) — the Goal 15 practice. `rawloc.py` was
**not** extended with Goal 16 entries, mirroring Goal 15 (which also did not extend it);
its Goal 14 report still reproduces unchanged (`TOTAL raw lines counted: 505`).

**Tool disclosure.** `loc.py`'s brace scanner stops early when a brace-less
(expression-bodied) member contains a balanced interpolated-string brace such as
`{sku}`: the scanner mistakes the interpolation for an opened-and-closed method body.
Two Goal 16 methods hit this edge (WF-1 and WF-3 FunnySharp variants): the tool reports 8
raw lines for each. The tables below report the corrected count (non-blank, non-comment
lines of the method, per the §2 definition) and mark both with `*`. The same edge explains
the pinned Goal 14 W5 FunnySharp raw value of 3: the committed `TotalWithAudit` in
`funnysharp/Workflows.cs` is 5 non-blank lines, and `rawloc.py` stops at its first
interpolated-string line.

Semantic LOC is directional, hand-derived evidence, not an integer-precise metric; the S+O
breakdown is shown in every table so disagreement localizes to one element.

## 3. Summary

| Workflow | Variant | S | O | Semantic | Raw |
| --- | --- | --- | ---: | ---: | ---: |
| WF-1 multi-stage transform + observation | idiomatic C# | 5 | 6 | **11** | 13 |
| | FunnySharp (Pipe/Tap chain) | 1 | 10 | **11** | 10* |
| WF-2 build-once composed transform | idiomatic C# (spelled at each site) | 2 | 10 | **12** | 10 |
| | FunnySharp (Compose + Partial + Flip) | 4 | 14 | **18** | 17 |
| WF-3 fallible pipeline + recovery | idiomatic C# | 9 | 9 | **18** | 23 |
| | FunnySharp (Try/Bind/Recover/MapError/Match) | 5 | 19 | **24** | 22* |
| WF-4 running aggregate report | idiomatic C# | 7 | 5 | **12** | 17 |
| | FunnySharp (Scan) | 2 | 5 | **7** | 10 |
| WF-5 async composed stages | idiomatic C# | 8 | 10 | **18** | 24 |
| | FunnySharp (ComposeValueAsync/ComposeAsync) | 8 | 11 | **19** | 30 |
| WF-6 four-field form validation | idiomatic C# | 10 | 16 | **26** | 21 |
| | FunnySharp (arity-4 Zip) | 5 | 28 | **33** | 22 |
| WF-7 optional chain resolution | idiomatic C# (null-conditionals) | 1 | 3 | **4** | 4 |
| | FunnySharp (member form, canonical) | 1 | 4 | **5** | 6 |
| | FunnySharp (query form, bridge) | 1 | 2 | **3** | 6 |

Net across the seven scenarios (idiomatic 101 semantic vs FunnySharp 117, counting the
member form for WF-7): the function grammar costs ~16% more semantic LOC here and buys
typed absence/failure channels, build-once function values, and a uniform verb vocabulary.
The one density win is WF-4, where the library owns a protocol — the same shape as the
Goal 14 finding that reduction concentrates where FunnySharp owns a protocol (§16 item 1
there). Per-workflow honesty is in §4-§10; the net assessment is §11.

## 4. WF-1 — Multi-stage transform pipeline with observation

**Scenario.** Resolve the catalog product id for a raw SKU from a legacy feed: trim it,
uppercase it, audit the normalized form, then look it up in the catalog table; a missing
entry resolves to the fallback id `"UNKNOWN"`. This extends the W5 concept (one transform,
one tap) to five stages with the tap in the middle and the fallback at the end.

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# | 5 | 6 | **11** | 13 |
| FunnySharp | 1 | 10 | **11** | 10* |

Idiomatic C# — `call-sites-code/idiomatic/Workflows.cs` (class `Goal16Workflows`):

```csharp
public static string ProductIdWithAudit(
    IReadOnlyDictionary<string, string> catalog,
    string rawSku,
    Action<string> audit)
{
    var sku = rawSku.Trim().ToUpperInvariant();
    audit($"normalized sku={sku}");
    if (!catalog.TryGetValue(sku, out var productId))
    {
        return "UNKNOWN";
    }

    return productId;
}
```

FunnySharp — `call-sites-code/funnysharp/Goal16Workflows.cs`:

```csharp
public static string ProductIdWithAudit(
    IReadOnlyDictionary<string, string> catalog,
    string rawSku,
    Action<string> audit) =>
    rawSku
        .Pipe(sku => sku.Trim())
        .Pipe(sku => sku.ToUpperInvariant())
        .Tap(sku => audit($"normalized sku={sku}"))
        .Pipe(sku => catalog.GetOption(sku))
        .GetValueOr("UNKNOWN");
```

**Compile evidence.** Both variants compile in their scratch projects (§13).

**Assessment.** Semantic LOC is identical (11 = 11), so this is density-neutral, like W5
(12 → 11) before it. S/O breakdown: idiomatic 5 S (declaration, audit statement, `if`,
two returns) / 6 O (`Trim`, `ToUpperInvariant`, `audit`, interpolated string, `!`,
`TryGetValue`); FunnySharp 1 S (expression body) / 10 O (three `Pipe`, `Trim`,
`ToUpperInvariant`, `Tap`, `audit`, interpolated string, `GetOption`, `GetValueOr`).
What FunnySharp adds: no temporaries, no early-return branch, evaluation order explicit
left-to-right, and absence becomes a value — `GetOption` (`src/FunnySharp/OptionExtensions.cs:52`)
returns an `Option<string>` that the chain continues on, and `GetValueOr`
(`src/FunnySharp/Option.cs:323`) is a total exit that cannot be forgotten. `Tap`
(`src/FunnySharp/FunctionExtensions.cs:114`) observes without changing the value; `Pipe`
(`src/FunnySharp/FunctionExtensions.cs:16`) names the next transform. Honest losses: ten
operations replace six, four grammar verbs replace one `if`, readers must know that
`GetOption` also maps a stored `null` to `None`, and step-through debugging loses the
named locals — W5's honest loss, unchanged. The `Pipe` into `GetOption` must stay a
lambda because `GetOption` is an extension method and extension method groups do not
convert to delegates.

## 5. WF-2 — Reusable composed transform, built once, used many

**Scenario.** A store prices every line of a campaign with the campaign's own discount
and tax rates and rounds to the cent: line total → discount → tax → round. The pricing
transform is needed at (at least) two call sites — price a whole list of lines for the
campaign report, and price a single line for a quote — and must exist in exactly one
place, so a change to the pricing rule changes one site.

The shared binary helpers are harness in both projects and excluded from every count
(their invocations count as operations): `Scale(factor, amount)`, `RoundTo(amount, digits)`
— data-first, the natural reading for a rounding helper — and `LineTotal(line)`. In
`Goal16Workflows.cs` they are `Func` fields so `Partial`/`Flip`/`Compose` apply directly;
in `idiomatic/Workflows.cs` they are static methods.

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# (transform spelled at each site) | 2 | 10 | **12** | 10 |
| FunnySharp (built once with Compose/Partial/Flip) | 4 | 14 | **18** | 17 |

Idiomatic C# — `call-sites-code/idiomatic/Workflows.cs` (the pricing formula is spelled
inside the lambda and again in the quote method; a third call site would spell it a third
time):

```csharp
public static IReadOnlyList<decimal> PriceCampaignLines(
    IReadOnlyList<OrderLine> lines,
    decimal discountRate,
    decimal taxRate) =>
    lines.Select(line => RoundTo(Scale(taxRate, Scale(discountRate, LineTotal(line))), 2)).ToArray();

public static decimal QuoteCampaignLine(
    OrderLine line,
    decimal discountRate,
    decimal taxRate) =>
    RoundTo(Scale(taxRate, Scale(discountRate, LineTotal(line))), 2);
```

FunnySharp — `call-sites-code/funnysharp/Goal16Workflows.cs` (both entries; the composed
transform is built once per campaign by `CampaignAdjustment` and applied at each site):

```csharp
public static IReadOnlyList<decimal> PriceCampaignLines(
    IReadOnlyList<OrderLine> lines,
    decimal discountRate,
    decimal taxRate)
{
    var adjust = CampaignAdjustment(discountRate, taxRate);
    return lines.Select(line => adjust(LineTotal(line))).ToArray();
}

public static decimal QuoteCampaignLine(
    OrderLine line,
    decimal discountRate,
    decimal taxRate) =>
    CampaignAdjustment(discountRate, taxRate)(LineTotal(line));

private static Func<decimal, decimal> CampaignAdjustment(decimal discountRate, decimal taxRate) =>
    Scale.Partial(discountRate)
        .Compose(Scale.Partial(taxRate))
        .Compose(RoundTo.Flip().Partial(2));
```

**Compile evidence.** Both variants compile in their scratch projects (§13).

**Assessment.** This is an honest density loss: 18 vs 12 semantic (+6), and the raw count
is higher too (17 vs 10). S/O breakdown: idiomatic 2 S / 10 O (the formula's four calls
appear twice); FunnySharp 4 S (declaration, return, two expression bodies) / 14 O
(`CampaignAdjustment` twice, `Select`, two composed-function invocations, `LineTotal`
twice, `ToArray`, three `Partial`, two `Compose`, one `Flip`). What the grammar buys is the build-once
semantics the scenario asks for: the pricing formula exists in exactly one place
(`CampaignAdjustment`), it is a *value* — one `Func<decimal, decimal>` per campaign that
can be stored in a configuration object, passed to a higher-order API, or composed with a
further stage without writing another wrapper — and the binding story is explicit at the
build site: `Scale` is config-first so `Partial` binds the rate directly
(`src/FunnySharp/FunctionExtensions.cs:82`), while `RoundTo` is data-first so `Flip`
reorders before `Partial` binds the digits (`src/FunnySharp/FunctionExtensions.cs:99`).
`Flip`'s honest reason to exist is exactly this: `Partial` binds the first argument, so a
helper whose configuration argument comes second needs `Flip` first; a codebase that
standardizes on config-first helpers rarely needs it. `Curry`/`Uncurry` were not needed
at this call site (`Partial` covers binding); reported rather than forced in.

The obvious idiomatic fix is extraction into a named method, which removes the duplication
at no semantic cost. Inline and illustrative only (not compiled, not part of
`idiomatic/Idiomatic.csproj`; the Goal 15 §5 precedent), it counts 3 S / 9 O = **12**
semantic and 12 raw (5 + 5 + 2, measured by running `tools/loc.py` on a temporary copy of
the exact snippet text):

```csharp
private static decimal AdjustCampaignPrice(decimal price, decimal discountRate, decimal taxRate) =>
    RoundTo(Scale(taxRate, Scale(discountRate, price)), 2);
```

with both call sites passing `AdjustCampaignPrice(LineTotal(line), discountRate, taxRate)`.
So the honest conclusion: for a fixed multi-stage transform with named parameters, the
named method is the idiomatic answer and is smaller (12 vs 18); `Compose`/`Partial` earn
their keep only when the transform must remain a function value (per-campaign
configuration, further composition, point-free hand-off), and they cost 6 semantic
elements plus delegate allocations for that privilege.

## 6. WF-3 — Fallible pipeline with recovery and error mapping

**Scenario.** Compute a shipping quote from raw input: parse the weight text (a parse
failure is a typed error and the default quote 4.90 applies), look up the zone rate (an
unknown zone is a typed error; the quote recovers to the flat rate 0.90 per unit and
still scales with the weight), and round to the cent. Every failure is audited. This is
the "fallible composition stays `Bind`" evidence for decision E74
(`docs/next-stage/decision-record.md:146`): no Kleisli operator, no wrapper type.

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# | 9 | 9 | **18** | 23 |
| FunnySharp | 5 | 19 | **24** | 22* |

Idiomatic C# — `call-sites-code/idiomatic/Workflows.cs`:

```csharp
public static decimal ShippingQuote(
    IReadOnlyDictionary<string, decimal> zoneRates,
    string rawWeight,
    string zone,
    Action<string> audit)
{
    decimal weight;
    try
    {
        weight = decimal.Parse(rawWeight, CultureInfo.InvariantCulture);
    }
    catch (FormatException)
    {
        audit($"quote failed: weight '{rawWeight}' is not a number");
        return 4.90m;
    }

    if (!zoneRates.TryGetValue(zone, out var rate))
    {
        audit($"quote failed: zone '{zone}' is not configured; pricing at the flat rate");
        rate = 0.90m;
    }

    return Math.Round(rate * weight, 2, MidpointRounding.AwayFromZero);
}
```

FunnySharp — `call-sites-code/funnysharp/Goal16Workflows.cs` (the `QuoteError` hierarchy
is harness at the bottom of the file, excluded from the count):

```csharp
public static decimal ShippingQuote(
    IReadOnlyDictionary<string, decimal> zoneRates,
    string rawWeight,
    string zone,
    Action<string> audit) =>
    Result.Try<decimal, QuoteError>(
            () => decimal.Parse(rawWeight, CultureInfo.InvariantCulture),
            _ => new UnparsableWeight($"weight '{rawWeight}' is not a number"))
        .Bind(weight => zoneRates.GetOption(zone)
            .ToResult<decimal, QuoteError>(() => new UnknownZone($"zone '{zone}' is not configured"))
            .Recover(error =>
            {
                audit($"quote failed: {error.Message}; pricing at the flat rate");
                return 0.90m;
            })
            .Map(rate => Math.Round(rate * weight, 2, MidpointRounding.AwayFromZero)))
        .MapError(error => error.Message)
        .Match(quote => quote, message =>
        {
            audit($"quote failed: {message}");
            return 4.90m;
        });
```

**Compile evidence.** Both variants compile in their scratch projects (§13).

**Assessment.** An honest density loss: 24 vs 18 semantic (+6), mirroring the W2 finding
that a typed pipeline is roughly neutral-to-larger at small scale. S/O breakdown:
idiomatic 9 S (`try`, assignment, `catch`, audit, return, `if`, audit, assignment,
return; the bare `decimal weight;` is not an S per clarification 4 in §2) / 9 O
(`Parse`, two audits, two interpolated strings, `!`, `TryGetValue`, `Round`, `*`);
FunnySharp 5 S (expression body, plus the `Recover` and `Match` lambda blocks' two
statements each) / 19 O (`Try`, `Parse`, two `new`, four interpolated strings (two in the
error factories, two in the audits), `Bind`, `GetOption`, `ToResult`, `Recover`, two
audits, `Map`, `Round`, `*`, `MapError`, `Match`). What the chain adds over the baseline: the failure channel is typed
(`UnparsableWeight`, `UnknownZone` are values, not exceptions or conventions), the
exception boundary is one named stage (`Result.Try` converts every non-cancellation
exception, `src/FunnySharp/Result.cs:31` — broader than the baseline's
`catch (FormatException)`, a real semantic difference, reported), recovery is a named
intermediate stage that keeps the pipeline running (`Recover`,
`src/FunnySharp/Result.cs:621`), error mapping happens at a named stage (`MapError`,
`src/FunnySharp/Result.cs:567`), and `Match` (`src/FunnySharp/Result.cs:495`) is the
single boundary back to a plain `decimal`. What it removes: the `try`/`catch` control
flow, the definite-assignment dance (`decimal weight;` assigned inside `try`), and the
convention that an exception is the error channel for a value the method must return.
Honest losses: +6 semantic elements; two audit sites (inside `Recover` and inside
`Match`'s failure arm) mirror the baseline's `catch`/`if` split rather than collapsing
it; and one real API friction was observed while compiling: `ToResult`'s `TError` is
inferred from the error factory's return type and `Result<TValue,TError>` is invariant,
so the intended error type must be restated as explicit type arguments
(`ToResult<decimal, QuoteError>`) — without them the compiler rejects the chain with
`CS0029: cannot convert Result<decimal, UnknownZone> to Result<decimal, QuoteError>`.
This is the same family of friction Goal 15 §6 recorded for the async seam. A second
friction was observed in the same method while compiling: the natural terminal form
ending in `.Recover(message => { ...; return 4.90m; })` does not compile, because
`Recover` returns `Result<TValue, TError>` rather than the recovered value
(`CS0029: cannot convert Result<decimal, string> to decimal`); the working form routes
the terminal boundary through `Match`, which is why the chain reads
`MapError` → `Match` with `Recover` as an intermediate stage. E74 evidence
proper: the fallible composition itself is one `Bind` whose argument is a sub-pipeline;
no Kleisli operator is missed at this call site, and every verb's short-circuit behavior
is readable from the chain.

## 7. WF-4 — Running aggregate report

**Scenario.** Build the running-balance statement of an account from a ledger of signed
`Transaction` amounts and an opening balance: one balance per transaction, in order. The
closing balance (what the BCL `Aggregate` produces for the same seed and accumulator) is
computed alongside as the equivalence oracle; per the `Scan` contract, the last element
of the running report equals it for a non-empty ledger, the seed is never yielded, and an
empty ledger yields no elements
(`src/FunnySharp/EnumerablePipelineExtensions.cs:36-53`).

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# | 7 | 5 | **12** | 17 |
| FunnySharp | 2 | 5 | **7** | 10 |

The counted unit is both methods on both sides; `ClosingBalance` is identical in the two
projects (BCL `Aggregate`), so the delta comes entirely from `RunningBalances`.

Idiomatic C# — `call-sites-code/idiomatic/Workflows.cs` (explicit state variable plus a
materialized list):

```csharp
public static IReadOnlyList<decimal> RunningBalances(
    IReadOnlyList<Transaction> transactions,
    decimal openingBalance)
{
    var balances = new List<decimal>(transactions.Count);
    var balance = openingBalance;
    foreach (var transaction in transactions)
    {
        balance += transaction.Amount;
        balances.Add(balance);
    }

    return balances;
}

public static decimal ClosingBalance(
    IReadOnlyList<Transaction> transactions,
    decimal openingBalance) =>
    transactions.Aggregate(openingBalance, (balance, transaction) => balance + transaction.Amount);
```

FunnySharp — `call-sites-code/funnysharp/Goal16Workflows.cs`:

```csharp
public static IReadOnlyList<decimal> RunningBalances(
    IReadOnlyList<Transaction> transactions,
    decimal openingBalance) =>
    transactions
        .Scan(openingBalance, (balance, transaction) => balance + transaction.Amount)
        .ToArray();

public static decimal ClosingBalance(
    IReadOnlyList<Transaction> transactions,
    decimal openingBalance) =>
    transactions.Aggregate(openingBalance, (balance, transaction) => balance + transaction.Amount);
```

**Compile evidence.** Both variants compile in their scratch projects (§13).

**Assessment.** The one clear density win of this set: 7 vs 12 semantic, 10 vs 17 raw.
S/O breakdown: idiomatic 7 S (two declarations, `foreach`, assignment, `Add` statement,
return, expression body of `ClosingBalance`) / 5 O (`new`, `+=`, `Add`, `Aggregate`,
`+`); FunnySharp 2 S (two expression bodies) / 5 O (`Scan`, `+`, `ToArray`,
`Aggregate`, `+`). `Scan` (`src/FunnySharp/EnumerablePipelineExtensions.cs:45`) owns the
running-aggregate protocol — seed never yielded, one accumulated value per element in
source order, empty source yields nothing, last value equals `Aggregate` — which is
exactly the "library owns a protocol" shape where Goal 14 found the big reductions.
Honest losses: `Scan` is deferred, so `ToArray` is needed to match the idiomatic eager
list (counted on the FunnySharp side); the idiomatic loop is already excellent at this
scale (12 semantic is not much, and every C# reader knows it cold); and the
`Aggregate`-equivalence and seed/empty contracts are cited from the source contract, not
executed — this workstream compiled the shapes only (**UNVERIFIED: no runtime execution;
no benchmark run**). The async family exists on the same contract (`Scan` over
`IAsyncEnumerable` and `ScanValueAsync` ±token,
`src/FunnySharp/AsyncEnumerablePipelineExtensions.cs:93,123,150`) and was not exercised
by a workflow here.

## 8. WF-5 — Async composed stages

**Scenario.** A quote pipeline resolves a customer's contract rate and then applies the
margin rules; both stages are cancellation-aware I/O. The ValueTask pipeline is built
once as a function value and applied to every customer of a batch; a Task-returning
variant of the same two stages serves a legacy audit exporter that requires a
`Task<decimal>`-producing function. The naming rule is visible at the call site:
`ComposeValueAsync` joins ValueTask-returning callbacks and `ComposeAsync` joins
Task-returning callbacks (`src/FunnySharp/FunctionExtensions.cs:199,176`) — the
`ValueAsync` suffix means "ValueTask-returning callback" everywhere else in the API, and
here it does in composition too. `IRateBook`/`IMarginRules` are harness (bottom of each
file, excluded from counts).

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# (sequential awaits) | 8 | 10 | **18** | 24 |
| FunnySharp (composed function values) | 8 | 11 | **19** | 30 |

Idiomatic C# — `call-sites-code/idiomatic/Workflows.cs`:

```csharp
public static async Task<IReadOnlyList<decimal>> QuoteAllAsync(
    IRateBook rates,
    IMarginRules margins,
    IReadOnlyList<string> customerIds,
    CancellationToken cancellationToken)
{
    var quotes = new List<decimal>(customerIds.Count);
    foreach (var customerId in customerIds)
    {
        var rate = await rates.RateForValueAsync(customerId, cancellationToken).ConfigureAwait(false);
        var quoted = await margins.ApplyValueAsync(rate, cancellationToken).ConfigureAwait(false);
        quotes.Add(quoted);
    }

    return quotes;
}

public static async Task<decimal> QuoteForAuditAsync(
    IRateBook rates,
    IMarginRules margins,
    string customerId,
    CancellationToken cancellationToken)
{
    var rate = await rates.RateForAsync(customerId, cancellationToken).ConfigureAwait(false);
    return await margins.ApplyAsync(rate, cancellationToken).ConfigureAwait(false);
}
```

FunnySharp — `call-sites-code/funnysharp/Goal16Workflows.cs`:

```csharp
public static Func<string, CancellationToken, ValueTask<decimal>> QuoteValuePipeline(
    IRateBook rates,
    IMarginRules margins) =>
    new Func<string, CancellationToken, ValueTask<decimal>>(rates.RateForValueAsync)
        .ComposeValueAsync(margins.ApplyValueAsync);

public static Func<string, CancellationToken, Task<decimal>> QuoteAuditPipeline(
    IRateBook rates,
    IMarginRules margins) =>
    new Func<string, CancellationToken, Task<decimal>>(rates.RateForAsync)
        .ComposeAsync(margins.ApplyAsync);

public static async Task<IReadOnlyList<decimal>> QuoteAllAsync(
    IRateBook rates,
    IMarginRules margins,
    IReadOnlyList<string> customerIds,
    CancellationToken cancellationToken)
{
    var quote = QuoteValuePipeline(rates, margins);
    var quotes = new List<decimal>(customerIds.Count);
    foreach (var customerId in customerIds)
    {
        quotes.Add(await quote(customerId, cancellationToken).ConfigureAwait(false));
    }

    return quotes;
}

public static Task<decimal> QuoteForAuditAsync(
    IRateBook rates,
    IMarginRules margins,
    string customerId,
    CancellationToken cancellationToken) =>
    QuoteAuditPipeline(rates, margins)(customerId, cancellationToken);
```

**Compile evidence.** Both variants compile in their scratch projects (§13).

**Assessment.** Roughly neutral on semantic LOC (19 vs 18, +1) and worse on raw (30 vs
24). S/O breakdown: idiomatic 8 S (six statements in the batch method, two in the audit
method) / 10 O (four awaits with four calls, `new`, `Add`); FunnySharp 8 S (two
expression-bodied pipeline builders, five statements in the batch method, one
expression-bodied audit entry) / 11 O (two `new Func`, `ComposeValueAsync`,
`ComposeAsync`, `QuoteValuePipeline`, `new`, `Add`, `await`, `quote` invoke,
`QuoteAuditPipeline`, invoke). What the grammar buys: the pipelines are function values
built once and applied many times, the per-element loop body is a single call instead of
two awaits with a temp, and the Task-flavored audit variant is *derived by composition*
(`ComposeAsync` over the Task-returning stage methods) rather than re-spelled as a
second hand-written async method — the naming rule makes the flavor difference readable
at the build site. Honest losses: stage 1 must be lifted into an explicit
`new Func<...>` because an instance method group has no natural type in
extension-receiver position, while stage 2's method group infers fine in argument
position — an asymmetry observed while compiling (both stages would need the lift if
`ComposeAsync`'s parameters were not inferable); one indirection layer per pipeline; and
the idiomatic sequential awaits remain the smallest honest expression of a fixed
two-stage pipeline (18 semantic, 24 raw). No benchmark was run
(**UNVERIFIED: no benchmark run; the composed forms allocate delegates per pipeline build,
the idiomatic forms do not**).

## 9. WF-6 — Four-field form validation

**Scenario.** Validate a four-field account form — email, password, age, country —
independently and return every error, not just the first. This re-uses the W3 form-scale
scenario shape with a fourth field, which lands exactly on the bound of the applicative
helper: AD-3 permits a "bounded applicative helper (arity ≤ 4, explicit argument order)"
([API decisions, AD-3](api-decisions.md#ad-3-accumulation--validation-validationtvalueterror-validationextensions)). `AccountForm` is harness at the bottom of each
file, excluded from counts.

| Variant | S | O | Semantic | Raw |
| --- | --- | ---: | ---: | ---: |
| Idiomatic C# | 10 | 16 | **26** | 21 |
| FunnySharp (arity-4 `Zip`) | 5 | 28 | **33** | 22 |

(The FunnySharp O column splits 6 O in the entry expression + 22 O in the four
validators; the full breakdown is in the assessment.)

Idiomatic C# — `call-sites-code/idiomatic/Workflows.cs`:

```csharp
public static IReadOnlyList<string> ValidateAccount(AccountForm form)
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

    if (form.Country.Length != 2 || !form.Country.All(char.IsUpper))
    {
        errors.Add("country must be a two-letter uppercase code");
    }

    return errors;
}
```

FunnySharp — `call-sites-code/funnysharp/Goal16Workflows.cs` (entry plus the four
validators it composes; the validators are at the bottom of the file):

```csharp
public static Validation<AccountForm, string> ValidateAccount(AccountForm form) =>
    ValidateEmail(form.Email).Zip(
        ValidatePassword(form.Password),
        ValidateAge(form.Age),
        ValidateCountry(form.Country),
        (email, password, age, country) => new AccountForm(email, password, age, country));
```

```csharp
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

private static Validation<string, string> ValidateCountry(string country) =>
    country.Length == 2 && country.All(char.IsUpper)
        ? Validation<string, string>.Valid(country)
        : Validation<string, string>.Invalid("country must be a two-letter uppercase code");
```

**Compile evidence.** Both variants compile in their scratch projects (§13).

**Assessment.** The W3 result, restated at four fields with the Goal 16 surface: the
idiomatic loop is smaller (26 vs 33 semantic). S/O breakdown: idiomatic 10 S (declaration,
four `if`s, four `Add` statements, return) / 16 O (`new`, four condition clusters totaling
11 operations — `IsNullOrWhiteSpace`, `||`, `!`, `Contains`, `<`, `<`, `>`, `!=`, `||`,
`!`, `All` — plus four `Add`); FunnySharp 5 S (entry expression body plus
one per validator) / 28 O (entry: four validator calls, `Zip`, `new` = 6; validators:
`!`, `&&`, `IsNullOrWhiteSpace`, `Contains`, `?:`, `Valid`, `Invalid` = 7; `is >=`,
`?:`, `Valid`, `Invalid` = 4; `>=`, `<=`, `?:`, `Valid`, `Invalid` = 5; `==`, `&&`,
`All`, `?:`, `Valid`, `Invalid` = 6). Counting-convention note: the Goal 14/15 tables
counted the W3 validators at 4 O each; under that convention this variant is 5 S / 22 O =
27 — the conclusion (no LOC win at form scale) is the same under either convention, and
AD-3 already records that "`Validation` has no numeric-LOC advantage at small scale...
Recorded so no 'always shorter' claim is made". What the arity-4 `Zip`
(`src/FunnySharp/Validation.cs:323`) adds over the Goal 15 arity-3 form: the fourth field
joins without nesting a second `Zip` or paying the tuple tax that Goal 14's W3 record
flagged (`parts.First.First`); error order stays visible in the argument order — operands
are passed email, password, age, country, the combiner receives
`(email, password, age, country)` in the same order, and the library accumulates
left-to-right in operand order, so the call-site text alone tells the reader the error
order. The bound is visible at exactly this call site: a fifth field exceeds the arity-4
combiner, and the caller must either nest `Zip`s (reintroducing the tuple tax) or regroup
the form — AD-3's deliberate limit, now with concrete evidence at its edge. Honest
losses: +7 semantic (rule-as-written), per-field validator methods are 16 of the 22 raw
lines, and the "empty list means valid" convention the idiomatic form relies on is
replaced by a type the reader must know.

## 10. WF-7 — Option query chain, member chain, null-conditionals

**Scenario.** Resolve the promo label for a SKU: the catalog may not contain the SKU, and
a catalog item may carry no attached `PromoCampaign`. Absence at any step yields no
label. The same nullable-reference domain shape (`CatalogItem.Sku`, `CatalogItem.Promo`
of type `PromoCampaign?`) is harness on both sides, so the FunnySharp variants convert at
the boundary with `ToOption` (`src/FunnySharp/OptionExtensions.cs:14`).

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# (null-conditionals) | 1 | 3 | **4** | 4 |
| FunnySharp member form (canonical) | 1 | 4 | **5** | 6 |
| FunnySharp query form (LINQ bridge) | 1 | 2 | **3** | 6 |

Idiomatic C# — `call-sites-code/idiomatic/Workflows.cs`:

```csharp
public static string? PromoLabel(
    IReadOnlyDictionary<string, CatalogItem> catalog,
    string sku) =>
    catalog.GetValueOrDefault(sku)?.Promo?.Label;
```

FunnySharp member form — `call-sites-code/funnysharp/Goal16Workflows.cs`:

```csharp
public static Option<string> PromoLabel(
    IReadOnlyDictionary<string, CatalogItem> catalog,
    string sku) =>
    catalog.GetOption(sku)
        .Bind(item => item.Promo.ToOption())
        .Map(promo => promo.Label);
```

FunnySharp query form — `call-sites-code/funnysharp/Goal16Workflows.cs` (compiles through
the new `SelectMany`/`Select` LINQ members, `src/FunnySharp/Option.cs:397,383`):

```csharp
public static Option<string> PromoLabelQuery(
    IReadOnlyDictionary<string, CatalogItem> catalog,
    string sku) =>
    from item in catalog.GetOption(sku)
    from promo in item.Promo.ToOption()
    select promo.Label;
```

**Compile evidence.** All three variants compile in their scratch projects (§13).

**Assessment.** The idiomatic null-conditional chain is the smallest honest expression:
4 semantic (1 S; `GetValueOrDefault` + two `?.` = 3 O, counting each `?.` per
clarification 3 in §2 — a reviewer who excludes them reads 2 O). The member form costs
one more element (5) for
typed absence; the query form is the *smallest* of the three (3) because its `SelectMany`
machinery is compiler-generated and only the written operations count (`GetOption`,
`ToOption`). That is exactly why canonicality is not decided by this count. The member
form is canonical because it is the vocabulary the rest of the API speaks — `Filter`,
`Zip`, `GetValueOr`, `OrElse` continue a member chain, while the query form dead-ends:
any of those operations forces the reader back to member calls mid-expression. The bridge
is available on `Option`, `Result`, and `Effect`. It remains secondary because it covers
only dependent-projection chains and cannot express the rest of the
grammar — consistent with the Goal 16 statement that "any LINQ syntax bridge remains
secondary to the canonical vocabulary" and with AD-4 keeping the member vocabulary
([API decisions, AD-4](api-decisions.md#ad-4-function-grammar--functionextensions)). Honest losses: `string?` carries absence with
zero vocabulary while `Option<string>` pays `ToOption()` at the nullable boundary plus
one grammar verb per step, and the query form's desugaring (an implicit
`SelectMany(item => ..., (item, promo) => ...)`) means the reader must know LINQ
translation rules to know what runs.

## 11. Assessment

- **The function grammar is not a density win at these scales.** One modest win (WF-4,
  12 → 7), one dead-even case (WF-1, 11 = 11), and five cases costing +1 to +7 semantic
  elements (WF-2 +6, WF-3 +6, WF-5 +1, WF-6 +7, WF-7 +1 for the canonical member form).
  Across the seven scenarios the FunnySharp forms total 117 semantic vs the idiomatic
  101. This mirrors the Goal 14 finding that typed outcomes are roughly LOC-neutral at
  small scale and that reduction concentrates where the library owns a protocol
  (`call-sites.md` §16 items 1-2): `Scan` (WF-4) is the protocol-shaped member of this
  set and it is the one clear win.
- **What the grammar adds where it costs.** WF-3 buys a typed failure channel, a named
  exception boundary, a named intermediate recovery, and a single exit boundary — the
  E74 evidence that fallible composition stays `Bind` with no missed Kleisli operator at
  this call site. WF-6 buys typed per-field errors, reusable validators, and error order
  visible in the argument order at the arity-4 bound. WF-2 and WF-5 buy build-once
  function values: the transform/pipeline is a value that can be stored, passed, or
  further composed, and the Task/ValueTask flavors are derived by parallel verbs rather
  than duplicated methods.
- **Where it is neutral or worse, said plainly.** For a fixed pipeline with named
  parameters, a named method beats `Compose` (WF-2: 12 vs 18, and the idiomatic
  extraction fix also lands at 12); for a fixed two-stage async pipeline, sequential
  awaits beat the composed function value (WF-5: 18 vs 19); for a nullable-reference
  chain, `?.` beats both FunnySharp forms (WF-7: 4 vs 5/3, with the query form's smaller
  count being an artifact of compiler-generated machinery, not a density win).
- **API frictions observed while compiling (reported, not fixed).** `ToResult` requires
  explicit type arguments when the intended error type is a supertype of the factory's
  return (`Result<TValue,TError>` is invariant; `CS0029` otherwise — WF-3). Instance
  method groups must be lifted into `new Func<...>` in extension-receiver position but
  infer fine in argument position (WF-5) — an asymmetry a reader must learn. `Recover`
  does not leave the carrier; the exit to a plain value is `Match` (WF-3). `Flip` is
  needed only because `Partial` binds the first argument and shared helpers read
  data-first (WF-2). `Pipe` into an extension method (`GetOption`) must stay a lambda
  because extension method groups do not convert (WF-1).
- **Vocabulary rules visible at the call sites.** `ValueAsync` = ValueTask-returning
  callback, in composition as everywhere else (WF-5: `ComposeValueAsync` vs
  `ComposeAsync`). The applicative helper is bounded at arity 4 with explicit argument
  order (WF-6, AD-3). The LINQ bridge is secondary: member form canonical, query form a
  bridge that dead-ends outside dependent projections (WF-7).
- **Tooling note for reviewers of raw LOC.** The `loc.py` brace scanner stops early on
  interpolated strings inside expression-bodied members (§2); the two affected raw values
  here are corrected and marked, and the same edge explains the pinned W5 FunnySharp raw
  value of 3 (actual 5). Nothing else in the Goal 14 raw report changes.

## 12. Limitations

- Semantic LOC is directional, hand-derived evidence, not an integer-precise metric; the
  S+O breakdown is printed so disagreement localizes to one element. The four counting
  clarifications this document adds to `call-sites.md` §2 are stated in §2 and applied
  consistently.
- `docs/next-stage/call-sites.md` (Goal 14) and `docs/next-stage/call-sites-goal-15.md`
  remain the pinned records for their scenarios; no W1-W11 count is restated here as
  authoritative, and no file from those records was modified. The Goal 16 idiomatic
  baselines live in additive methods in `idiomatic/Workflows.cs`; the FunnySharp
  variants live in a new `funnysharp/Goal16Workflows.cs`.
- The evidence is compile-time only. No runtime execution, benchmark, allocation, or
  throughput measurement was performed: the `Scan`/`Aggregate` equivalence, the `Zip`
  left-to-right error order, `Recover`/`Match` behavior, and the async pipelines'
  behavior are cited from source contracts, not from observed runs (**UNVERIFIED: no
  runtime execution; no benchmark run in this workstream**).
- No competitor variants were compiled: this document compares the Goal 16 surface
  against idiomatic C# only, per its scope. Funcky/CFE/language-ext positions for these
  shapes remain whatever the Goal 14 record says for adjacent workflows.
- The WF-2 extraction-fix variant is inline illustrative code, not compiled; its raw
  count was measured by running `tools/loc.py` on a temporary copy of the exact snippet
  text (the Goal 15 §5 precedent), and its semantic count is hand-derived.
- The Goal 16 surface is the working tree of `src/FunnySharp` (uncommitted at evidence
  time; §1). The scratch `ProjectReference` and its absolute path are machine-specific by
  design, as in the Goal 14/15 records.
- `rawloc.py` was not extended with Goal 16 entries (mirroring Goal 15); raw LOC was
  measured per method with `tools/loc.py`, and its interpolated-string edge case is
  disclosed in §2 with corrected values where it fired.

## 13. Compile evidence

Environment (`export PATH="$HOME/.dotnet:$PATH"` first; `dotnet` is not on `PATH` in this
environment):

```
$ dotnet --version
10.0.400
```

Both scratch projects were built clean (`bin`/`obj` removed first), Release configuration,
then force-rebuilt, in this order (run verbatim in the repository):

```bash
export PATH="$HOME/.dotnet:$PATH"          # dotnet is not on PATH in this environment
export DOTNET_CLI_UI_LANGUAGE=en           # English build output for the record
cd /home/azureuser/repos/funnysharp/docs/next-stage/call-sites-code/funnysharp
rm -rf bin obj && dotnet build -c Release --nologo    # Build succeeded. 0 Warning(s) 0 Error(s)
dotnet build -c Release --nologo -t:Rebuild           # Build succeeded. 0 Warning(s) 0 Error(s)
cd ../idiomatic
rm -rf bin obj && dotnet build -c Release --nologo    # Build succeeded. 0 Warning(s) 0 Error(s)
dotnet build -c Release --nologo -t:Rebuild           # Build succeeded. 0 Warning(s) 0 Error(s)
```

Output (identical shape for all four runs; the `funnysharp` build also compiles the
referenced local `src/FunnySharp` project, i.e. the working-tree Goal 16 surface):

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

| Scratch project | Target | Result | Warnings | Errors |
| --- | --- | --- | ---: | ---: |
| `funnysharp` (local `ProjectReference` to working-tree `src/FunnySharp`) | `net10.0` library | succeeded | 0 | 0 |
| `funnysharp` forced rebuild (`-t:Rebuild`) | `net10.0` library | succeeded | 0 | 0 |
| `idiomatic` (BCL only) | `net10.0` library | succeeded | 0 | 0 |
| `idiomatic` forced rebuild (`-t:Rebuild`) | `net10.0` library | succeeded | 0 | 0 |

The built `FunnySharp.dll` SHA256 is recorded in §1. No compiler warnings were
suppressed, and no `#pragma` appears in either scratch file. `rawloc.py` still reproduces
the Goal 14 raw report unchanged (`TOTAL raw lines counted: 505`) after the Goal 16
additions, confirming the additive methods did not disturb the pinned evidence.
