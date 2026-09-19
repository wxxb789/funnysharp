using FunnySharp;

namespace FunnySharp.DocumentationSamples;

internal static class FunctionCompositionSamples
{
    private static async Task PipeAsync()
    {
        // <snippet DocumentationSamples.FunctionComposition.PipeAsync>
        var result = await 4.Pipe(async value =>
        {
            await Task.Yield();
            return value * 3;
        });
        // </snippet>
    }

    private static Result<decimal, ParseError> FallibleCompose(QuantityRequest request)
    {
        // <snippet DocumentationSamples.FunctionComposition.FallibleCompose>
        Func<string, Result<int, ParseError>> parseQuantity = ParseQuantity;
        Func<int, Result<decimal, ParseError>> lookupUnitPrice = LookupUnitPrice;

        Result<decimal, ParseError> lineTotal = parseQuantity(request.QuantityText)
            .Bind(lookupUnitPrice)
            .Map(unitPrice => unitPrice * request.Units);
        // </snippet>
        return lineTotal;
    }

    private static Result<int, ParseError> ParseQuantity(string text) =>
        int.TryParse(text, out var quantity) && quantity > 0
            ? Result<int, ParseError>.Success(quantity)
            : Result<int, ParseError>.Failure(new ParseError("quantity"));

    private static Result<decimal, ParseError> LookupUnitPrice(int quantity) =>
        Result<decimal, ParseError>.Success(quantity * 12.50m);

    private sealed record QuantityRequest(string QuantityText, int Units);

    private sealed record ParseError(string Field);
}
