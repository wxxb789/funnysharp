using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public static class Api
{
    public static void MapApi(WebApplication app)
    {
        app.MapPost("/orders", (OrderRequest request) =>
        {
            var errors = new List<string>();
            var at = request.CustomerEmail.IndexOf('@');
            if (at < 0 || request.CustomerEmail.IndexOf('.', at + 1) < 0) errors.Add("invalid-email");
            if (request.Lines.Count == 0) errors.Add("empty-order");
            foreach (var line in request.Lines)
                if (line.Quantity is < 1 or > 99) errors.Add("invalid-quantity:" + line.Sku);
            if (request.PromoCode is not (null or "" or "SAVE10" or "FREESHIP")) errors.Add("unknown-promo");
            if (errors.Count != 0) return InvalidOrder(errors);
            decimal subtotal = 0;
            foreach (var line in request.Lines)
            {
                var product = ShopData.FindProduct(line.Sku);
                if (product is null) errors.Add("unknown-sku:" + line.Sku);
                else subtotal += product.Price * line.Quantity;
            }
            if (errors.Count != 0) return InvalidOrder(errors);
            var total = Math.Round(subtotal * (request.PromoCode == "SAVE10" ? 0.9m : 1m)
                + (request.PromoCode == "FREESHIP" ? 0m : 5m), 2);
            var id = ShopData.AddOrder(request.CustomerEmail, total);
            return Results.Created("/orders/" + id, new OrderCreated(id));
        });
        app.MapGet("/products/{id}", (string id) =>
        {
            var product = ShopData.FindProduct(id);
            return product is null ? Results.Problem(statusCode: 404, detail: "product-not-found:" + id) : Results.Ok(product);
        });
        app.MapDelete("/orders/{id}", (int id) =>
        {
            var order = ShopData.FindOrder(id);
            if (order is null) return Results.Problem(statusCode: 409, detail: "order-not-found:" + id);
            if (order.Paid) return Results.Problem(statusCode: 409, detail: "order-paid");
            ShopData.RemoveOrder(id);
            return Results.NoContent();
        });
        app.MapPost("/orders/{id}/pay", (int id, PaymentRequest request) =>
        {
            var order = ShopData.FindOrder(id);
            if (order is null) return Results.Problem(statusCode: 404, detail: "order-not-found:" + id);
            if (!ShopData.Charge(request.CardToken, order.Total)) return Results.Problem(statusCode: 402, detail: "payment-declined");
            ShopData.MarkOrderPaid(id);
            return Results.Ok(new PaymentAccepted(true));
        });
    }

    private static IResult InvalidOrder(List<string> errors) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = errors.ToArray() });
}
