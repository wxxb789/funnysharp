using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using static Microsoft.AspNetCore.Http.Results;

// Minimal API surface over the shop domain. Every endpoint keeps its failure,
// absence, and accumulation rules inline: which check failed, which status it
// maps to, and why.

public static class Api
{
    private const string OrderErrorKey = "order";
    private const decimal Shipping = 5.00m;
    private const decimal Save10Rate = 0.10m;

    public static void MapApi(WebApplication app)
    {
        // POST /orders - accumulate rule failures, then price and store.
        app.MapPost("/orders", (OrderRequest request) =>
        {
            var ruleErrors = ValidateOrder(request);
            if (ruleErrors.Count > 0)
            {
                // Rules 1-4: 400 with every reported code, in rule order.
                return InvalidOrder(ruleErrors);
            }

            var skuErrors = UnknownSkuErrors(request);
            if (skuErrors.Count > 0)
            {
                // Pricing precondition: 400 with one code per missing line.
                return InvalidOrder(skuErrors);
            }

            var orderId = ShopData.AddOrder(request.CustomerEmail, PriceOrder(request));
            return Created($"/orders/{orderId}", new OrderCreated(orderId));
        });

        // GET /products/{id} - known product or 404.
        app.MapGet("/products/{id}", (string id) =>
            ShopData.FindProduct(id) is { } product
                ? Ok(product)
                : Problem(detail: $"product-not-found:{id}", statusCode: StatusCodes.Status404NotFound));

        // DELETE /orders/{id} - absent orders and paid orders both conflict.
        app.MapDelete("/orders/{id}", (int id) =>
        {
            if (ShopData.FindOrder(id) is not { } order)
            {
                return Problem(detail: $"order-not-found:{id}", statusCode: StatusCodes.Status409Conflict);
            }

            if (order.Paid)
            {
                return Problem(detail: "order-paid", statusCode: StatusCodes.Status409Conflict);
            }

            ShopData.RemoveOrder(id);
            return NoContent();
        });

        // POST /orders/{id}/pay - charge the stored total, then mark paid.
        app.MapPost("/orders/{id}/pay", (int id, PaymentRequest payment) =>
        {
            if (ShopData.FindOrder(id) is not { } order)
            {
                return Problem(detail: $"order-not-found:{id}", statusCode: StatusCodes.Status404NotFound);
            }

            if (!ShopData.Charge(payment.CardToken, order.Total))
            {
                return Problem(detail: "payment-declined", statusCode: StatusCodes.Status402PaymentRequired);
            }

            ShopData.MarkOrderPaid(order.Id);
            return Ok(new PaymentAccepted(Paid: true));
        });
    }

    // Rules 1-4 are independent: run them all and accumulate every failure in rule order.
    private static List<string> ValidateOrder(OrderRequest request)
    {
        List<string> errors = [];

        // 1. invalid-email: no '@', or no '.' after the '@'.
        var atIndex = request.CustomerEmail.IndexOf('@');
        if (atIndex < 0 || request.CustomerEmail.IndexOf('.', atIndex + 1) < 0)
        {
            errors.Add("invalid-email");
        }

        // 2. empty-order.
        if (request.Lines.Count == 0)
        {
            errors.Add("empty-order");
        }

        // 3. invalid-quantity:<sku>: one code per offending line, in line order.
        foreach (var line in request.Lines)
        {
            if (line.Quantity is < 1 or > 99)
            {
                errors.Add($"invalid-quantity:{line.Sku}");
            }
        }

        // 4. unknown-promo.
        if (request.PromoCode is not (null or "" or "SAVE10" or "FREESHIP"))
        {
            errors.Add("unknown-promo");
        }

        return errors;
    }

    // unknown-sku:<sku>: one code per line whose sku is not in the catalog.
    private static List<string> UnknownSkuErrors(OrderRequest request) =>
        request.Lines
            .Where(line => ShopData.FindProduct(line.Sku) is null)
            .Select(line => $"unknown-sku:{line.Sku}")
            .ToList();

    // Goods subtotal plus shipping; SAVE10 discounts the goods only, FREESHIP drops the shipping.
    private static decimal PriceOrder(OrderRequest request)
    {
        var goods = request.Lines.Sum(line => ShopData.FindProduct(line.Sku)!.Price * line.Quantity);

        var total = request.PromoCode switch
        {
            "SAVE10" => goods - goods * Save10Rate + Shipping,
            "FREESHIP" => goods,
            _ => goods + Shipping,
        };

        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    // 400 validation problem: every reported code under the single "order" key.
    private static IResult InvalidOrder(IReadOnlyList<string> errorCodes) =>
        ValidationProblem(new Dictionary<string, string[]> { [OrderErrorKey] = [.. errorCodes] });
}
