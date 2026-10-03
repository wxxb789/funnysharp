public sealed class SideEffectsTests
{
    [Fact]
    public void InvalidOrderCannotPriceChargeOrAttemptPersistence()
    {
        var catalog = new ProductCatalog(new Dictionary<string, decimal> { ["mouse"] = 15m });
        var payment = new PaymentGateway { DeclineCard = "declined" };
        var repository = new OrderRepository { FailOnCustomer = "nobody@example.com" };
        var outcome = OrderWorkflow.PlaceOrder(new("bad-email", [new("mouse", 0)], "SECRET"), catalog, payment, repository, "card");
        Assert.False(outcome.Placed);
        Assert.Equal(["invalid-email", "invalid-quantity:mouse", "unknown-promo"], outcome.Errors);
        Assert.Empty(catalog.Lookups); Assert.Empty(payment.Attempts); Assert.Empty(repository.Attempts); Assert.Empty(repository.Saved);
    }

    [Fact]
    public void UnknownSkusAccumulateAndDeclineHasNoPersistenceAttempt()
    {
        var catalog = new ProductCatalog(new Dictionary<string, decimal> { ["mouse"] = 15m });
        var payment = new PaymentGateway { DeclineCard = "declined" };
        var repository = new OrderRepository { FailOnCustomer = "nobody@example.com" };
        var missing = OrderWorkflow.PlaceOrder(new("a@example.com", [new("missing-a", 1), new("mouse", 1), new("missing-b", 1)], null), catalog, payment, repository, "card");
        Assert.False(missing.Placed);
        Assert.Equal(["unknown-sku:missing-a", "unknown-sku:missing-b"], missing.Errors);
        Assert.Equal(["missing-a", "mouse", "missing-b"], catalog.Lookups);
        Assert.Empty(payment.Attempts); Assert.Empty(repository.Attempts);
        var declined = OrderWorkflow.PlaceOrder(new("a@example.com", [new("mouse", 2)], ""), catalog, payment, repository, "declined");
        Assert.False(declined.Placed); Assert.Equal(["payment-declined"], declined.Errors);
        Assert.Equal(("declined", 35m), Assert.Single(payment.Attempts));
        Assert.Empty(repository.Attempts); Assert.Empty(repository.Saved);
    }
}
