using FunnySharp;

namespace FunnySharp.TestSupport.IncrementalComparison;

internal static class PricingKernel
{
    internal static ProductEconomics ComputeProduct(Product product, PricingWork work)
    {
        work.ProductEvaluations++;
        var unitPrice = decimal.Round(
            product.UnitCost * 1.35m,
            2,
            MidpointRounding.AwayFromZero);

        return new ProductEconomics(
            product.Id,
            unitPrice,
            product.TaxRate,
            product.Category);
    }

    internal static Result<Quote, PricingError> ComputeQuote(
        Order order,
        IReadOnlyList<ProductEconomics> products,
        PricingWork work)
    {
        work.QuoteEvaluations++;
        var subtotal = 0m;
        var tax = 0m;

        foreach (var line in order.Lines)
        {
            if (line.Quantity <= 0)
            {
                return Result<Quote, PricingError>.Failure(new PricingError(
                    order.Id,
                    "quantity",
                    $"Order {order.Id} has a non-positive quantity."));
            }

            if ((uint)line.ProductIndex >= (uint)products.Count)
            {
                return Result<Quote, PricingError>.Failure(new PricingError(
                    order.Id,
                    "product",
                    $"Order {order.Id} references product index {line.ProductIndex}."));
            }

            var product = products[line.ProductIndex];
            work.LineEvaluations++;
            var extended = product.UnitPrice * line.Quantity;
            subtotal += extended;
            tax += extended * product.TaxRate;
        }

        var discount = Discount(order.Region, subtotal);
        var total = decimal.Round(
            subtotal - discount + tax,
            2,
            MidpointRounding.AwayFromZero);

        return Result<Quote, PricingError>.Success(new Quote(order.Id, subtotal, discount, tax, total));
    }

    internal static Result<Quote, PricingError> ComputeQuoteFromLineProducts(
        Order order,
        IReadOnlyList<ProductEconomics> lineProducts,
        PricingWork work)
    {
        work.QuoteEvaluations++;

        if (lineProducts.Count != order.Lines.Count)
        {
            return Result<Quote, PricingError>.Failure(new PricingError(
                order.Id,
                "product-slice",
                $"Order {order.Id} has {order.Lines.Count} lines but {lineProducts.Count} product values."));
        }

        var subtotal = 0m;
        var tax = 0m;

        for (var lineIndex = 0; lineIndex < order.Lines.Count; lineIndex++)
        {
            var line = order.Lines[lineIndex];

            if (line.Quantity <= 0)
            {
                return Result<Quote, PricingError>.Failure(new PricingError(
                    order.Id,
                    "quantity",
                    $"Order {order.Id} has a non-positive quantity."));
            }

            var product = lineProducts[lineIndex];
            work.LineEvaluations++;
            var extended = product.UnitPrice * line.Quantity;
            subtotal += extended;
            tax += extended * product.TaxRate;
        }

        var discount = Discount(order.Region, subtotal);
        var total = decimal.Round(
            subtotal - discount + tax,
            2,
            MidpointRounding.AwayFromZero);

        return Result<Quote, PricingError>.Success(
            new Quote(order.Id, subtotal, discount, tax, total));
    }

    internal static Validation<IReadOnlyList<int>, PricingError> ValidateQuantities(
        Order order,
        IReadOnlyList<int> quantities)
    {
        if (quantities.Count != order.Lines.Count)
        {
            return Validation<IReadOnlyList<int>, PricingError>.Invalid(new PricingError(
                order.Id,
                "line-count",
                $"Order {order.Id} has {order.Lines.Count} lines, not {quantities.Count}."));
        }

        var errors = new List<PricingError>();
        var values = new int[quantities.Count];

        for (var index = 0; index < quantities.Count; index++)
        {
            var quantity = quantities[index];
            values[index] = quantity;

            if (quantity <= 0)
            {
                errors.Add(new PricingError(
                    order.Id,
                    "quantity",
                    $"Order {order.Id} line {index} must be positive."));
            }
        }

        return errors.Count == 0
            ? Validation<IReadOnlyList<int>, PricingError>.Valid(values)
            : Validation<IReadOnlyList<int>, PricingError>.InvalidMany(errors);
    }

    private static decimal Discount(string region, decimal subtotal) =>
        region switch
        {
            "EU" when subtotal >= 1_000m =>
                decimal.Round(subtotal * 0.05m, 2, MidpointRounding.AwayFromZero),
            "US" when subtotal >= 2_000m =>
                decimal.Round(subtotal * 0.03m, 2, MidpointRounding.AwayFromZero),
            "APAC" when subtotal >= 1_500m =>
                decimal.Round(subtotal * 0.04m, 2, MidpointRounding.AwayFromZero),
            _ => 0m,
        };
}