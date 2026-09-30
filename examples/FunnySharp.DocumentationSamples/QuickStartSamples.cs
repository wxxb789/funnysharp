using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunnySharp.DocumentationSamples;

internal static class QuickStartSamples
{
    private static void CreateCarriers()
    {
        // <snippet DocumentationSamples.QuickStart.OptionResult>
        Option<string> sku = Option.Some("SKU-1");
        Result<int, string> quantity = sku
            .Map(static text => text.Length)
            .ToResult("The SKU is absent.");

        UnitResult<string> stored = UnitResult<string>.Success()
            .Ensure(() => quantity.IsSuccess, "The quantity is missing.");
        Validation<int, string> price = Validation<int, string>.Valid(
            quantity.Match(static value => value, static _ => 0));
        // </snippet>
    }

    private static void ApplySharedGrammar()
    {
        // <snippet DocumentationSamples.QuickStart.SharedVerbs>
        Option<int> doubled = Option.Some(4)
            .Map(static value => value * 2)
            .Bind(static value => Option.Some(value + 1));

        int total = doubled
            .ToResult("missing")
            .Ensure(static value => value > 0, "not positive")
            .Match(static value => value, static _ => 0);
        // </snippet>
    }

    private static void QueryCollections()
    {
        // <snippet DocumentationSamples.QuickStart.Cardinality>
        var quantities = new[] { 2, 4, 6 };

        Option<int> first = quantities.FirstOrNone();
        Option<NonEmpty<int>> nonEmpty = quantities.ToNonEmptyOrNone();
        var (even, odd) = quantities.Partition(static value => value % 2 == 0);
        Option<IReadOnlyList<(int First, int Second)>> pairs =
            quantities.ZipExactOrNone(new[] { 10, 20, 30 });
        // </snippet>
    }

    private static void FilterAndScan()
    {
        // <snippet DocumentationSamples.QuickStart.ChooseScan>
        var quantities = new[] { 0, 2, 0, 4 };

        IEnumerable<int> present = quantities
            .Choose(static value => value == 0 ? Option.None<int>() : Option.Some(value));
        IEnumerable<int> running = present.Scan(0, static (total, value) => total + value);
        // </snippet>
    }

    private static void FilterAndScanAsync(IAsyncEnumerable<int> source)
    {
        // <snippet DocumentationSamples.QuickStart.AsyncChooseScan>
        IAsyncEnumerable<int> present = source.Choose(
            static value => value % 2 == 0 ? Option.Some(value) : Option.None<int>());
        IAsyncEnumerable<int> running = present.Scan(0, static (total, value) => total + value);
        // </snippet>
    }

    private static IAsyncEnumerable<int> DoubleInParallel(IAsyncEnumerable<int> source)
    {
        // <snippet DocumentationSamples.QuickStart.ParallelMap>
        return source.SelectParallelValueAsync(
            maxConcurrency: 4,
            static item => new ValueTask<int>(item * 2));
        // </snippet>
    }

    private static async Task CreateEffectAsync()
    {
        // <snippet DocumentationSamples.QuickStart.CreateEffect>
        Effect<string> greeting = Effect.FromSync(() => "hello")
            .Map(static text => text.ToUpperInvariant());

        string value = await greeting.RunAsync();
        // </snippet>
    }

    private static async Task ScopeResourcesAsync()
    {
        // <snippet DocumentationSamples.QuickStart.ScopeResources>
        Effect<int> syncHandle = Effect.FromSync(() => new Handle())
            .Using(static handle => Effect.FromValue(handle.Value));
        Effect<long> asyncStream = Effect.FromSync(() => new MemoryStream())
            .UsingAsync(static stream => Effect.FromValue(stream.Length));

        int syncLength = await syncHandle.RunAsync();
        long asyncLength = await asyncStream.RunAsync();
        // </snippet>
    }

    private static void DefineTransition(Account account)
    {
        // <snippet DocumentationSamples.QuickStart.StateTransition>
        StateTransition<Account, AuditCommand> submit = current =>
            StateChange<Account, AuditCommand>.To(
                current with { Status = AccountStatus.Submitted },
                new StoreAccount(current.Id));
        // </snippet>
    }

    private static void UpdateNested(Account account)
    {
        // <snippet DocumentationSamples.QuickStart.NestedUpdate>
        var profile = Lens.Create<Account, Profile>(
            value => value.Profile,
            (value, next) => value with { Profile = next });
        var city = Lens.Create<Profile, string>(
            value => value.City,
            (value, next) => value with { City = next });

        Account updated = profile.Compose(city).Set(account, "Paris");
        // </snippet>
    }

    private static void HandleOutcome(Order order)
    {
        // <snippet DocumentationSamples.QuickStart.HandledOutcome>
        Option<int> quantity = Option.Some(3);
        int present = quantity.TryGetValue(out var found) ? found : 0;

        Result<Order, OrderError> saved = SaveOrder(order);
        string outcome = saved.Match(static _ => "saved", static error => error.Code);
        // </snippet>
    }

    private static void MapOrderLookup(WebApplication app)
    {
        // <snippet DocumentationSamples.QuickStart.ToHttpResult>
        app.MapGet("/orders/{id:int}", (int id, CancellationToken cancellationToken) =>
            FindOrderAsync(id, cancellationToken).ToHttpResultAsync(NotFound));
        // </snippet>
    }

    private static Result<Order, OrderError> SaveOrder(Order order) =>
        Result<Order, OrderError>.Success(order);

    private static Task<Option<Order>> FindOrderAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(Option.Some(new Order(id, AccountStatus.Draft, new Profile("London"))));

    private static ProblemDetails NotFound() => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Not found",
    };

    private sealed class Handle : IDisposable
    {
        public int Value => 42;

        public void Dispose()
        {
        }
    }

    private sealed record Account(int Id, AccountStatus Status, Profile Profile);

    private enum AccountStatus
    {
        Draft,
        Submitted,
    }

    private abstract record AuditCommand(int AccountId);

    private sealed record StoreAccount(int Id) : AuditCommand(Id);

    private sealed record Profile(string City);

    private sealed record Order(int Id, AccountStatus Status, Profile Profile);

    private sealed record OrderError(string Code);
}
