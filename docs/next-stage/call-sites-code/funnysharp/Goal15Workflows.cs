using FunnySharp;

namespace CallSites.FunnySharp;

// Goal 15 versions of the call sites that the Goal 14 evidence flagged as gaps:
// W4 (UnitResult<TError> carrier) and W3 (bounded Zip combiner), plus a
// value-producing fail-fast workflow that ends at a no-value command boundary.
public static class Goal15Workflows
{
    // W4: delete-or-notify, success carries no value.
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

    // W3: independent form-field validation accumulating all errors, with the
    // arity-3 Zip combiner. The combiner arguments are the accumulation order.
    public static Validation<SignupForm, string> ValidateSignup(SignupForm form) =>
        ValidateEmail(form.Email).Zip(
            ValidatePassword(form.Password),
            ValidateAge(form.Age),
            (email, password, age) => new SignupForm(email, password, age));

    // Goal 15 extra: submit a request through a fail-fast value pipeline, then
    // drop the produced value at the command boundary with ToUnitResultAsync.
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
}
