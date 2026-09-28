using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>Builds refined domain values in tests through the same factories production uses.</summary>
internal static class Refined
{
    internal static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    internal static Sku SkuOf(string value)
    {
        Assert.True(Sku.Create(value).TryGetValue(out var sku));
        return sku;
    }

    internal static Quantity QuantityOf(int value)
    {
        Assert.True(Quantity.Create(value).TryGetValue(out var quantity));
        return quantity;
    }

    internal static CustomerId CustomerOf(string value)
    {
        Assert.True(CustomerId.Create(value).TryGetValue(out var customer));
        return customer;
    }

    internal static TrackingCode TrackingOf(string value)
    {
        Assert.True(TrackingCode.Create(value).TryGetValue(out var tracking));
        return tracking;
    }

    internal static Money MoneyOf(decimal amount)
    {
        Assert.True(Money.Create(amount, "EUR").TryGetValue(out var money));
        return money;
    }

    internal static NonEmpty<OrderLine> LinesOf(decimal unitPrice, params (string Sku, int Quantity)[] lines)
    {
        var priced = lines
            .Select(line => new OrderLine(SkuOf(line.Sku), QuantityOf(line.Quantity), MoneyOf(unitPrice)))
            .ToArray();
        Assert.True(priced.ToNonEmptyOrNone().TryGetValue(out var nonEmpty));
        return nonEmpty;
    }
}
