using System.Collections.Immutable;
using System.Globalization;

using FunnySharp;

namespace CallSites.FunnySharp;

// The same ten workflows expressed with FunnySharp 0.1.0.
public static class Workflows
{
    // W1: dictionary lookup with fallback.
    public static int TimeoutSeconds(IReadOnlyDictionary<string, string> config) =>
        config.GetOption("request.timeoutSeconds")
            .Bind(raw => Option.FromTry<int>(
                (out int seconds) => int.TryParse(
                    raw,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out seconds)))
            .GetValueOr(30);

    // W2: parse-then-validate fail-fast pipeline with typed errors.
    // Note: BindAsync returns Task<Result<...>> and FunnySharp 0.1.0 has no Map on task
    // carriers, so the chain must be awaited before it can continue.
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

    // W3: independent form-field validation accumulating all errors.
    public static Validation<SignupForm, string> ValidateSignup(SignupForm form) =>
        ValidateEmail(form.Email)
            .Zip(ValidatePassword(form.Password))
            .Zip(ValidateAge(form.Age))
            .Map(parts => new SignupForm(parts.First.First, parts.First.Second, parts.Second));

    // W4: delete-or-notify workflow. FunnySharp 0.1.0 has no UnitResult<TError>;
    // this is the closest current Result-based workaround with a dummy payload.
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

    // W5: composed data transforms with observation/tap.
    public static decimal TotalWithAudit(OrderLine line, Action<string> audit) =>
        (line.Quantity * line.UnitPrice)
            .Tap(total => audit($"line={line.Sku} total={total}"))
            .Pipe(total => total * Discount(line.Sku))
            .Tap(discounted => audit($"discounted={discounted}"));

    // W6: traverse a sequence carrying index context (workaround for the missing
    // first-class path context; see Goal 17).
    public static Result<IReadOnlyList<OrderLine>, OrderError> ParseAll(IReadOnlyList<string> rows) =>
        rows.Select((row, index) => (row, index))
            .Traverse(item => ParseLine(item.row)
                .MapError(error => (OrderError)new LocatedError($"rows[{item.index}]", error)));

    // W7: bounded parallel ordered fetch over IAsyncEnumerable with cancellation.
    // The operator is library-provided; this is the consumer call site.
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

    // W8: first-success selection across cold operations with timeout.
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

    // W9: environment + resource scoped workflow with cancellation.
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

    // W10: nested record update with immutable collections.
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

    // Shared helpers for the workflows above.
    public static decimal Discount(string sku) =>
        sku.StartsWith("SAVE", StringComparison.Ordinal) ? 0.9m : 1m;

    public static Result<OrderLine, OrderError> ParseLine(string row)
    {
        var parts = row.Split(':');
        if (parts.Length != 3 ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) ||
            !decimal.TryParse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            return Result<OrderLine, OrderError>.Failure(
                new InvalidPayload($"cannot parse row '{row}'"));
        }

        return Result<OrderLine, OrderError>.Success(new OrderLine(parts[0], quantity, price));
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

    private static Validation<string, string> ValidateEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@')
            ? Validation<string, string>.Valid(email)
            : Validation<string, string>.Invalid("email must contain '@'");

    private static Validation<string, string> ValidatePassword(string password) =>
        password.Length is >= 12 and <= 128
            ? Validation<string, string>.Valid(password)
            : Validation<string, string>.Invalid("password must have 12..128 characters");

    private static Validation<int, string> ValidateAge(int age) =>
        age is >= 18 and <= 130
            ? Validation<int, string>.Valid(age)
            : Validation<int, string>.Invalid("age must be between 18 and 130");
}
