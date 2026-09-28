using System.Diagnostics;
using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Application;

/// <summary>
/// Supplier fan-out. Every method bounds how many quote calls run at once, keeps source order where
/// the result is a decision, and delivers in completion order only where the caller streams.
/// </summary>
public sealed class SupplierQuoting(ISupplierGateway gateway, VerticalSliceOptions options)
{
    /// <summary>Cheapest unit price first, then supplier name by ordinal: the one place this rule lives.</summary>
    private static readonly IComparer<SupplierQuote> CheapestFirst = Comparer<SupplierQuote>.Create(
        static (left, right) =>
        {
            var byPrice = left.UnitPrice.Amount.CompareTo(right.UnitPrice.Amount);
            return byPrice != 0 ? byPrice : string.CompareOrdinal(left.Supplier, right.Supplier);
        });

    /// <summary>Asks every supplier in parallel and picks the cheapest deterministic quote.</summary>
    public async ValueTask<Result<QuoteBoard, OrderError>> BestAsync(
        Sku sku,
        Quantity quantity,
        CancellationToken cancellationToken)
    {
        var attempts = await gateway.FindSuppliersAsync(sku, cancellationToken)
            .SelectParallelValueAsync(
                options.QuoteConcurrency,
                (supplier, token) => gateway.QuoteAsync(supplier, sku, quantity, token))
            .ToListAsync(cancellationToken);

        var quotes = new List<SupplierQuote>(attempts.Count);
        var unavailable = new List<string>();
        foreach (var attempt in attempts)
        {
            attempt.Match(quotes.Add, error => unavailable.Add(SupplierName(error)));
        }

        if (quotes.Count == 0)
        {
            return Result<QuoteBoard, OrderError>.Failure(new OrderError.NoSupplierQuote(sku, unavailable));
        }

        var best = quotes[0];
        foreach (var quote in quotes)
        {
            if (CheapestFirst.Compare(quote, best) < 0)
            {
                best = quote;
            }
        }

        return Result<QuoteBoard, OrderError>.Success(new QuoteBoard(sku, quantity, best, unavailable));
    }

    /// <summary>Delivers each supplier answer as it arrives, for callers that stream to a client.</summary>
    public IAsyncEnumerable<Result<SupplierQuote, OrderError>> StreamAsync(
        Sku sku,
        Quantity quantity,
        CancellationToken cancellationToken) =>
        gateway.FindSuppliersAsync(sku, cancellationToken)
            .SelectParallelCompletionOrderValueAsync(
                options.QuoteConcurrency,
                (supplier, token) => gateway.QuoteAsync(supplier, sku, quantity, token));

    /// <summary>Prices every requested line with bounded parallelism; the first failure stops the rest.</summary>
    public async ValueTask<Result<NonEmpty<OrderLine>, OrderError>> SourceAsync(
        NonEmpty<PlaceOrderLine> lines,
        CancellationToken cancellationToken)
    {
        var priced = await lines.ToReadOnlyList().AsAsyncEnumerable(cancellationToken)
            .TraverseParallelValueAsync(
                options.QuoteConcurrency,
                (line, token) => PriceLineAsync(line, token),
                cancellationToken);

        return priced.Match(
            orderLines => Result<NonEmpty<OrderLine>, OrderError>.Success(ToNonEmpty(orderLines)),
            static error => Result<NonEmpty<OrderLine>, OrderError>.Failure(error));
    }

    private async ValueTask<Result<OrderLine, OrderError>> PriceLineAsync(
        PlaceOrderLine line,
        CancellationToken cancellationToken)
    {
        var quote = await BestSequentialAsync(line.Sku, line.Quantity, cancellationToken);
        return quote.Match(
            best => Result<OrderLine, OrderError>.Success(new OrderLine(line.Sku, line.Quantity, best.UnitPrice)),
            static error => Result<OrderLine, OrderError>.Failure(error));
    }

    private async ValueTask<Result<SupplierQuote, OrderError>> BestSequentialAsync(
        Sku sku,
        Quantity quantity,
        CancellationToken cancellationToken)
    {
        SupplierQuote? best = null;
        await foreach (var supplier in gateway.FindSuppliersAsync(sku, cancellationToken))
        {
            var attempt = await gateway.QuoteAsync(supplier, sku, quantity, cancellationToken);
            if (!attempt.TryGetValue(out var quote))
            {
                continue;
            }

            if (best is null || CheapestFirst.Compare(quote, best.Value) < 0)
            {
                best = quote;
            }
        }

        return best is { } chosen
            ? Result<SupplierQuote, OrderError>.Success(chosen)
            : Result<SupplierQuote, OrderError>.Failure(new OrderError.NoSupplierQuote(sku, []));
    }

    private static string SupplierName(OrderError error) => error switch
    {
        OrderError.SupplierUnavailable unavailable => unavailable.Supplier,
        OrderError.NoSupplierQuote none => string.Join(", ", none.Suppliers),
        _ => "unknown",
    };

    private static NonEmpty<OrderLine> ToNonEmpty(IReadOnlyList<OrderLine> orderLines)
    {
        if (!orderLines.ToNonEmptyOrNone().TryGetValue(out var nonEmpty))
        {
            throw new UnreachableException("A non-empty source traversal produced an empty result.");
        }

        return nonEmpty;
    }
}
