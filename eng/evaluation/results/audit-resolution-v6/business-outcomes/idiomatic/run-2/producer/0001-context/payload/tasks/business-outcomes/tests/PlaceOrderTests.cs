public sealed class PlaceOrderTests
{
    private static readonly IReadOnlyDictionary<string, decimal> Prices =
        new Dictionary<string, decimal>
        {
            ["keyboard"] = 40m,
            ["mouse"] = 15m,
            ["monitor"] = 220m,
        };

    private static ProductCatalog Catalog() => new(Prices);

    [Fact]
    public void ValidOrderWithoutPromoPlacesTheOrder()
    {
        var gateway = new PaymentGateway { DeclineCard = "decline" };
        var repository = new OrderRepository { FailOnCustomer = "nobody@example.com" };
        var outcome = OrderWorkflow.PlaceOrder(
            new OrderRequest("ada@example.com", [new OrderLine("keyboard", 1), new OrderLine("mouse", 2)], null),
            Catalog(),
            gateway,
            repository,
            "card-1");
        Assert.True(outcome.Placed);
        Assert.Equal(1001, outcome.OrderId);
        // 40 + 2 x 15 + 5.00 shipping.
        Assert.Equal(75.00m, outcome.Total);
        Assert.Empty(outcome.Errors);
        Assert.Equal(1, gateway.Charges);
        Assert.Single(repository.Saved);
    }

    [Fact]
    public void Save10PromoDiscountsTenPercent()
    {
        var outcome = OrderWorkflow.PlaceOrder(
            new OrderRequest("ada@example.com", [new OrderLine("monitor", 1)], "SAVE10"),
            Catalog(),
            new PaymentGateway { DeclineCard = "decline" },
            new OrderRepository { FailOnCustomer = "nobody@example.com" },
            "card-1");
        Assert.True(outcome.Placed);
        // 220 + 5 shipping, then 10% off the goods, shipping stays.
        Assert.Equal(203.00m, outcome.Total);
    }

    [Fact]
    public void FreeshipPromoRemovesShipping()
    {
        var outcome = OrderWorkflow.PlaceOrder(
            new OrderRequest("ada@example.com", [new OrderLine("mouse", 1)], "FREESHIP"),
            Catalog(),
            new PaymentGateway { DeclineCard = "decline" },
            new OrderRepository { FailOnCustomer = "nobody@example.com" },
            "card-1");
        Assert.True(outcome.Placed);
        Assert.Equal(15.00m, outcome.Total);
    }

    [Fact]
    public void ValidationErrorsAccumulateInOrder()
    {
        var outcome = OrderWorkflow.PlaceOrder(
            new OrderRequest("not-an-email", [new OrderLine("keyboard", 0), new OrderLine("mouse", 100)], "SECRET"),
            Catalog(),
            new PaymentGateway { DeclineCard = "decline" },
            new OrderRepository { FailOnCustomer = "nobody@example.com" },
            "card-1");
        Assert.False(outcome.Placed);
        Assert.Equal(0, outcome.OrderId);
        Assert.Equal(0m, outcome.Total);
        Assert.Equal(
            ["invalid-email", "invalid-quantity:keyboard", "invalid-quantity:mouse", "unknown-promo"],
            outcome.Errors);
    }

    [Fact]
    public void EmptyOrderReportsEmptyOrder()
    {
        var outcome = OrderWorkflow.PlaceOrder(
            new OrderRequest("ada@example.com", [], null),
            Catalog(),
            new PaymentGateway { DeclineCard = "decline" },
            new OrderRepository { FailOnCustomer = "nobody@example.com" },
            "card-1");
        Assert.False(outcome.Placed);
        Assert.Equal(["empty-order"], outcome.Errors);
    }

    [Fact]
    public void UnknownSkuIsReported()
    {
        var outcome = OrderWorkflow.PlaceOrder(
            new OrderRequest("ada@example.com", [new OrderLine("trackball", 1)], null),
            Catalog(),
            new PaymentGateway { DeclineCard = "decline" },
            new OrderRepository { FailOnCustomer = "nobody@example.com" },
            "card-1");
        Assert.False(outcome.Placed);
        Assert.Equal(["unknown-sku:trackball"], outcome.Errors);
    }

    [Fact]
    public void DeclinedCardStopsBeforePersistence()
    {
        var gateway = new PaymentGateway { DeclineCard = "card-1" };
        var repository = new OrderRepository { FailOnCustomer = "nobody@example.com" };
        var outcome = OrderWorkflow.PlaceOrder(
            new OrderRequest("ada@example.com", [new OrderLine("mouse", 1)], null),
            Catalog(),
            gateway,
            repository,
            "card-1");
        Assert.False(outcome.Placed);
        Assert.Equal(["payment-declined"], outcome.Errors);
        Assert.Equal(0, gateway.Charges);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public void SaveFailureIsReportedAfterCharging()
    {
        var gateway = new PaymentGateway { DeclineCard = "decline" };
        var repository = new OrderRepository { FailOnCustomer = "ada@example.com" };
        var outcome = OrderWorkflow.PlaceOrder(
            new OrderRequest("ada@example.com", [new OrderLine("mouse", 1)], null),
            Catalog(),
            gateway,
            repository,
            "card-1");
        Assert.False(outcome.Placed);
        Assert.Equal(["save-failed"], outcome.Errors);
        Assert.Equal(1, gateway.Charges);
        Assert.Empty(repository.Saved);
    }
}
