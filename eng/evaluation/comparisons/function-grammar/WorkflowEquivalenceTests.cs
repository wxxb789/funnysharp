using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks.Sources;

namespace FunctionGrammarComparisons;

public sealed class WorkflowEquivalenceTests
{
    [Theory]
    [InlineData(" abc ", "ID")]
    [InlineData("missing", "UNKNOWN")]
    [InlineData("null", "UNKNOWN")]
    public void Wf1NormalizesObservesAndResolvesTheSamePublicValue(string input, string expected)
    {
        var catalog = new Dictionary<string, string> { ["ABC"] = "ID", ["NULL"] = null! };
        var leftAudit = new List<string>();
        var rightAudit = new List<string>();

        Assert.Equal(expected, IdiomaticWorkflows.ProductIdWithAudit(catalog, input, leftAudit.Add));
        Assert.Equal(expected, FunnySharpWorkflows.ProductIdWithAudit(catalog, input, rightAudit.Add));
        Assert.Single(leftAudit);
        Assert.Equal(leftAudit, rightAudit);
    }

    [Fact]
    public void Wf1ObservationHappensBeforeLookupOnBothSides()
    {
        foreach (var workflow in ProductWorkflows())
        {
            var catalog = new Dictionary<string, string>();
            var calls = 0;
            var output = workflow(catalog, " abc ", _ =>
            {
                calls++;
                catalog["ABC"] = "ADDED";
            });

            Assert.Equal("ADDED", output);
            Assert.Equal(1, calls);
        }
    }

    [Fact]
    public void Wf1PreservesObserverExceptionIdentity()
    {
        var expected = new InvalidOperationException("observer");
        foreach (var workflow in ProductWorkflows())
        {
            Assert.Same(expected, Assert.Throws<InvalidOperationException>(() =>
                workflow(new Dictionary<string, string>(), "a", _ => throw expected)));
        }
    }

    [Fact]
    public void Wf2UsesOneConstructedFunctionForBatchAndQuoteWithEqualMaterializedValues()
    {
        OrderLine[] lines = [new(2, 10m), new(1, 1.005m)];
        var left = IdiomaticWorkflows.CampaignAdjustment(0.9m, 1.2m);
        var right = FunnySharpWorkflows.CampaignAdjustment(0.9m, 1.2m);
        var leftBatch = IdiomaticWorkflows.PriceCampaignLines(left, lines);
        var rightBatch = FunnySharpWorkflows.PriceCampaignLines(right, lines);

        Assert.Equal([21.60m, 1.09m], leftBatch);
        Assert.Equal(leftBatch, rightBatch);
        Assert.Equal(leftBatch[0], IdiomaticWorkflows.QuoteCampaignLine(left, lines[0]));
        Assert.Equal(rightBatch[0], FunnySharpWorkflows.QuoteCampaignLine(right, lines[0]));
        Assert.Equal(leftBatch, IdiomaticWorkflows.PriceCampaignLines(left, lines));
        Assert.Equal(rightBatch, FunnySharpWorkflows.PriceCampaignLines(right, lines));
        Assert.Empty(IdiomaticWorkflows.PriceCampaignLines(left, []));
        Assert.Empty(FunnySharpWorkflows.PriceCampaignLines(right, []));

        var leftOther = IdiomaticWorkflows.CampaignAdjustment(1m, 1m);
        var rightOther = FunnySharpWorkflows.CampaignAdjustment(1m, 1m);
        Assert.Equal(20m, IdiomaticWorkflows.QuoteCampaignLine(leftOther, lines[0]));
        Assert.Equal(20m, FunnySharpWorkflows.QuoteCampaignLine(rightOther, lines[0]));
        Assert.Throws<OverflowException>(() => left(decimal.MaxValue));
        Assert.Throws<OverflowException>(() => right(decimal.MaxValue));
    }

    [Theory]
    [InlineData("2.5", "EU", "3.00", 0)]
    [InlineData("2.5", "unknown", "2.25", 1)]
    [InlineData("bad", "EU", "4.90", 1)]
    [InlineData("bad", "unknown", "4.90", 1)]
    public void Wf3ReturnsEqualQuotesAndAuditOutput(
        string weight,
        string zone,
        string expected,
        int expectedAudits)
    {
        var rates = new Dictionary<string, decimal> { ["EU"] = 1.2m };
        var leftAudit = new List<string>();
        var rightAudit = new List<string>();
        var left = IdiomaticWorkflows.ShippingQuote(rates, weight, zone, leftAudit.Add);
        var right = FunnySharpWorkflows.ShippingQuote(rates, weight, zone, rightAudit.Add);

        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), left);
        Assert.Equal(left, right);
        Assert.Equal(expectedAudits, leftAudit.Count);
        Assert.Equal(leftAudit, rightAudit);
    }

    [Fact]
    public void Wf3ParseOverflowAndDownstreamArithmeticOverflowAreNotFormatFailures()
    {
        var rates = new Dictionary<string, decimal> { ["EU"] = 1.2m };
        foreach (var workflow in ShippingWorkflows())
        {
            var audit = new List<string>();
            Assert.Throws<OverflowException>(() => workflow(rates, new string('9', 80), "EU", audit.Add));
            Assert.Empty(audit);
            Assert.Throws<OverflowException>(() =>
                workflow(rates, decimal.MaxValue.ToString(CultureInfo.InvariantCulture), "EU", audit.Add));
            Assert.Empty(audit);
        }
    }

    [Fact]
    public void Wf3ObserverFaultsAndCancellationRemainOutsideTheParseBoundary()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Exception[] errors = [new InvalidOperationException("audit"), new OperationCanceledException(cancellation.Token)];
        var rates = new Dictionary<string, decimal>();
        foreach (var workflow in ShippingWorkflows())
        {
            foreach (var expected in errors)
            {
                foreach (var weight in new[] { "bad", "2" })
                {
                    Assert.Same(expected, Record.Exception(() =>
                        workflow(rates, weight, "missing", _ => throw expected)));
                }
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Wf4MaterializesOnceExcludesTheSeedAndAgreesWithTheClosingFold(int count)
    {
        Transaction[] all = [new("a", 2m), new("b", -1m), new("c", 4m)];
        decimal[] expected = [12m, 11m, 15m];
        var values = all.Take(count).ToArray();
        var leftSource = new OnceSource(values);
        var rightSource = new OnceSource(values);
        var left = IdiomaticWorkflows.RunningBalances(leftSource, 10m);
        var right = FunnySharpWorkflows.RunningBalances(rightSource, 10m);

        Assert.Equal(expected.Take(count), left);
        Assert.Equal(left, right);
        Assert.Equal(1, leftSource.Enumerations);
        Assert.Equal(1, rightSource.Enumerations);
        Assert.Equal(1, leftSource.Disposals);
        Assert.Equal(1, rightSource.Disposals);
        var leftFold = new OnceSource(values);
        var rightFold = new OnceSource(values);
        var closing = IdiomaticWorkflows.ClosingBalance(leftFold, 10m);
        Assert.Equal(count == 0 ? 10m : expected[count - 1], closing);
        Assert.Equal(closing, FunnySharpWorkflows.ClosingBalance(rightFold, 10m));
        Assert.Equal(1, leftFold.Enumerations);
        Assert.Equal(1, rightFold.Enumerations);
        Assert.Equal(1, leftFold.Disposals);
        Assert.Equal(1, rightFold.Disposals);
    }

    [Fact]
    public void Wf4ArithmeticFailureDisposesEachSource()
    {
        var left = new OnceSource([new Transaction("overflow", 1m)]);
        var right = new OnceSource([new Transaction("overflow", 1m)]);
        Assert.Throws<OverflowException>(() => IdiomaticWorkflows.RunningBalances(left, decimal.MaxValue));
        Assert.Throws<OverflowException>(() => FunnySharpWorkflows.RunningBalances(right, decimal.MaxValue));
        Assert.Equal(1, left.Disposals);
        Assert.Equal(1, right.Disposals);
    }

    [Fact]
    public void Wf4SourceFailureIdentityIsPreserved()
    {
        var expected = new InvalidOperationException("source");
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() =>
            IdiomaticWorkflows.RunningBalances(new BrokenSource(expected), 0m)));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() =>
            FunnySharpWorkflows.RunningBalances(new BrokenSource(expected), 0m)));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task Wf5BothAwaitableKindsPreserveOrderExactTokensAndSingleConsumption(
        bool funny,
        bool task,
        bool pending)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var first = new CountingSource();
        var second = new CountingSource();
        if (!pending)
        {
            first.Complete(3m);
            second.Complete(6m);
        }

        var stages = new Stages((_, _) => first.Create(), (_, _) => second.Create());
        var quote = BuildQuote(funny, task, stages);
        Assert.Empty(stages.Trace);
        var operation = quote("a", cancellation.Token);
        try
        {
            if (pending)
            {
                await Bounded(first.Subscribed.Task);
                Assert.Equal(["rate:a"], stages.Trace.Select(entry => entry.Stage));
                first.Complete(3m);
                await Bounded(second.Subscribed.Task);
                Assert.Equal(["rate:a", "margin"], stages.Trace.Select(entry => entry.Stage));
                second.Complete(6m);
            }

            Assert.Equal(6m, await Bounded(operation));
            Assert.Equal(["rate:a", "margin"], stages.Trace.Select(entry => entry.Stage));
            Assert.All(stages.Trace, entry => Assert.Equal(cancellation.Token, entry.Token));
            Assert.Equal(1, first.Consumptions);
            Assert.Equal(1, second.Consumptions);
        }
        finally
        {
            first.Complete(3m);
            second.Complete(6m);
            await Bounded(operation);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Wf5FaultsCancellationAndShortCircuitingMatchForBothStages(bool funny, bool task)
    {
        for (var mode = 0; mode < 7; mode++)
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            cancellation.Cancel();
            var expected = new InvalidOperationException("stage");
            var stages = new Stages(
                (_, token) => mode switch
                {
                    0 => ValueTask.FromException<decimal>(expected),
                    2 => throw expected,
                    4 => ValueTask.FromCanceled<decimal>(token),
                    _ => ValueTask.FromResult(3m),
                },
                (_, token) => mode switch
                {
                    1 => ValueTask.FromException<decimal>(expected),
                    3 => throw expected,
                    5 => ValueTask.FromCanceled<decimal>(token),
                    _ => ValueTask.FromResult(6m),
                });
            var quote = BuildQuote(funny, task, stages);
            Task<decimal>? operation = null;
            Assert.Null(Record.Exception(() => { operation = quote("a", cancellation.Token); }));
            if (mode < 4)
            {
                Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => Bounded(operation!)));
            }
            else if (mode < 6)
            {
                var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Bounded(operation!));
                Assert.Equal(cancellation.Token, actual.CancellationToken);
            }
            else
            {
                // A canceled token alone does not preempt callbacks which ignore it.
                Assert.Equal(6m, await Bounded(operation!));
            }

            Assert.Equal(
                mode is 0 or 2 or 4 ? new[] { "rate:a" } : new[] { "rate:a", "margin" },
                stages.Trace.Select(entry => entry.Stage));
            Assert.All(stages.Trace, entry => Assert.Equal(cancellation.Token, entry.Token));
        }
    }

    [Fact]
    public async Task Wf5ConstructedFunctionsAreReusableAcrossBatchAndIndividualCalls()
    {
        foreach (var funny in new[] { false, true })
        {
            var stages = new Stages(
                (id, _) => ValueTask.FromResult((decimal)id.Length),
                (rate, _) => ValueTask.FromResult(rate * 2m));
            var value = funny
                ? FunnySharpWorkflows.QuoteValuePipeline(stages, stages)
                : IdiomaticWorkflows.QuoteValuePipeline(stages, stages);
            var task = funny
                ? FunnySharpWorkflows.QuoteAuditPipeline(stages, stages)
                : IdiomaticWorkflows.QuoteAuditPipeline(stages, stages);
            Assert.Empty(stages.Trace);
            var token = TestContext.Current.CancellationToken;
            var batch = funny
                ? FunnySharpWorkflows.QuoteAllAsync(value, ["a", "bb"], token)
                : IdiomaticWorkflows.QuoteAllAsync(value, ["a", "bb"], token);
            Assert.Equal([2m, 4m], await Bounded(batch));
            Assert.Equal(6m, await Bounded(value("ccc", token).AsTask()));
            var audit = funny
                ? FunnySharpWorkflows.QuoteForAuditAsync(task, "dddd", token)
                : IdiomaticWorkflows.QuoteForAuditAsync(task, "dddd", token);
            Assert.Equal(8m, await Bounded(audit));
            audit = funny
                ? FunnySharpWorkflows.QuoteForAuditAsync(task, "e", token)
                : IdiomaticWorkflows.QuoteForAuditAsync(task, "e", token);
            Assert.Equal(2m, await Bounded(audit));
            Assert.Equal(10, stages.Trace.Count);
            Assert.All(stages.Trace, entry => Assert.Equal(token, entry.Token));
        }
    }

    [Fact]
    public void Wf6AllSixteenFieldCombinationsHaveEqualValuesOrderedErrorsAndWireOutput()
    {
        string[] codes = ["E_EMAIL", "E_PASSWORD", "E_AGE", "E_COUNTRY"];
        for (var mask = 0; mask < 16; mask++)
        {
            var form = new AccountForm(
                (mask & 1) == 0 ? "a@b" : "bad",
                (mask & 2) == 0 ? "abcdefghijkl" : "short",
                (mask & 4) == 0 ? 18 : 17,
                (mask & 8) == 0 ? "US" : "us");
            var expectedErrors = Enumerable.Range(0, 4)
                .Where(bit => (mask & (1 << bit)) != 0)
                .Select(bit => codes[bit])
                .ToArray();
            var left = IdiomaticWorkflows.ValidateAccount(form);
            var right = FunnySharpWorkflows.ValidateAccount(form);

            Assert.Equal(mask == 0 ? form : null, left.Value);
            Assert.Equal(left.Value, right.Value);
            Assert.Equal(expectedErrors, left.Errors);
            Assert.Equal(left.Errors, right.Errors);
            // Serialized neutral DTO output is machine-consumed boundary evidence.
            Assert.Equal(JsonSerializer.Serialize(left), JsonSerializer.Serialize(right));
        }
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, null)]
    [InlineData(2, null)]
    [InlineData(3, null)]
    [InlineData(4, "Sale")]
    [InlineData(5, "")]
    public void Wf7ConvertsAllAbsenceCasesAndPresentLabelsToTheSameNullableBoundary(int scenario, string? expected)
    {
        var catalog = new Dictionary<string, CatalogItem>();
        if (scenario != 0)
        {
            catalog["sku"] = scenario switch
            {
                1 => null!,
                2 => new CatalogItem(null),
                3 => new CatalogItem(new PromoCampaign(null)),
                4 => new CatalogItem(new PromoCampaign("Sale")),
                _ => new CatalogItem(new PromoCampaign("")),
            };
        }

        Assert.Equal(expected, IdiomaticWorkflows.PromoLabel(catalog, "sku"));
        Assert.Equal(expected, FunnySharpWorkflows.PromoLabel(catalog, "sku"));
    }

    private static Func<IReadOnlyDictionary<string, string>, string, Action<string>, string>[] ProductWorkflows() =>
        [IdiomaticWorkflows.ProductIdWithAudit, FunnySharpWorkflows.ProductIdWithAudit];

    private static Func<IReadOnlyDictionary<string, decimal>, string, string, Action<string>, decimal>[] ShippingWorkflows() =>
        [IdiomaticWorkflows.ShippingQuote, FunnySharpWorkflows.ShippingQuote];

    private static Task<T> Bounded<T>(Task<T> operation) =>
        operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    private static Task Bounded(Task operation) =>
        operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    // Oracle adapter only. The actual implementations retain both native signatures.
    private static Func<string, CancellationToken, Task<decimal>> BuildQuote(bool funny, bool task, Stages stages)
    {
        if (task)
        {
            return funny
                ? FunnySharpWorkflows.QuoteAuditPipeline(stages, stages)
                : IdiomaticWorkflows.QuoteAuditPipeline(stages, stages);
        }

        var quote = funny
            ? FunnySharpWorkflows.QuoteValuePipeline(stages, stages)
            : IdiomaticWorkflows.QuoteValuePipeline(stages, stages);
        return (id, token) => quote(id, token).AsTask();
    }

    private sealed class Stages(
        Func<string, CancellationToken, ValueTask<decimal>> rate,
        Func<decimal, CancellationToken, ValueTask<decimal>> margin) : IRateBook, IMarginRules
    {
        public List<(string Stage, CancellationToken Token)> Trace { get; } = [];

        public ValueTask<decimal> RateForValueAsync(string customerId, CancellationToken cancellationToken)
        {
            Trace.Add(($"rate:{customerId}", cancellationToken));
            return rate(customerId, cancellationToken);
        }

        public ValueTask<decimal> ApplyValueAsync(decimal value, CancellationToken cancellationToken)
        {
            Trace.Add(("margin", cancellationToken));
            return margin(value, cancellationToken);
        }

        public Task<decimal> RateForAsync(string customerId, CancellationToken cancellationToken) =>
            RateForValueAsync(customerId, cancellationToken).AsTask();

        public Task<decimal> ApplyAsync(decimal value, CancellationToken cancellationToken) =>
            ApplyValueAsync(value, cancellationToken).AsTask();
    }

    private sealed class CountingSource : IValueTaskSource<decimal>
    {
        private ManualResetValueTaskSourceCore<decimal> source = new() { RunContinuationsAsynchronously = true };
        private int consumptions;
        private bool completed;

        public TaskCompletionSource Subscribed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Consumptions => Volatile.Read(ref consumptions);

        public ValueTask<decimal> Create() => new(this, source.Version);

        public void Complete(decimal value)
        {
            if (!completed)
            {
                completed = true;
                source.SetResult(value);
            }
        }

        public decimal GetResult(short token)
        {
            if (Interlocked.Increment(ref consumptions) != 1)
            {
                throw new InvalidOperationException("ValueTask consumed twice");
            }

            return source.GetResult(token);
        }

        public ValueTaskSourceStatus GetStatus(short token) => source.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            source.OnCompleted(continuation, state, token, flags);
            Subscribed.TrySetResult();
        }
    }

    private sealed class OnceSource(IReadOnlyList<Transaction> values) : IEnumerable<Transaction>
    {
        public int Enumerations { get; private set; }

        public int Disposals { get; private set; }

        public IEnumerator<Transaction> GetEnumerator()
        {
            if (++Enumerations != 1)
            {
                throw new InvalidOperationException("Source enumerated twice");
            }

            return Enumerate().GetEnumerator();
        }

        private IEnumerable<Transaction> Enumerate()
        {
            try
            {
                foreach (var value in values)
                {
                    yield return value;
                }
            }
            finally
            {
                Disposals++;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class BrokenSource(Exception error) : IEnumerable<Transaction>
    {
        public IEnumerator<Transaction> GetEnumerator() => throw error;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
