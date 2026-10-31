using System.Globalization;
using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.Contracts;

internal static class ContractOrderConsumer
{
    internal const string EmailDetail = "email must contain a local part and domain";
    internal const string Last4Detail = "last4 must contain exactly four digits";
    internal const string TotalDetail = "order total must not exceed 1000";

    private static readonly ContractSet Set = Build();

    internal static Contract<Order> OrderContract => Set.Order;

    internal static Contract<OrderLine> LineContract => Set.Line;

    internal static Contract<string> EmailContract => Set.Email;

    internal static Decoder<Order> CreateOrderDecoder()
    {
        var created = OrderContract.CreateDecoder();
        if (created.TryGetError(out var limitation))
        {
            throw new InvalidOperationException(limitation.ToString());
        }

        created.TryGetValue(out var decoder);
        return decoder!;
    }

    internal static OrderDecodeResult Decode(JsonElement root)
    {
        var decoded = CreateOrderDecoder().Decode(root);
        return decoded.TryGetValue(out var order)
            ? new OrderDecodeResult(order, [])
            : new OrderDecodeResult(null, MapErrors(decoded));
    }

    internal static IReadOnlyList<OrderError> Validate(Order order)
    {
        var validated = OrderContract.Validate(order);
        return validated.IsValid ? [] : MapErrors(validated);
    }

    internal static Order Generate(int seed)
    {
        var generated = OrderContract.Generate(new ContractGenerationOptions(seed));
        if (generated.TryGetError(out var limitation))
        {
            throw new InvalidOperationException(limitation.ToString());
        }

        generated.TryGetValue(out var order);
        return order!;
    }

    internal static IReadOnlyList<Order> Shrink(Order order)
    {
        var shrunk = OrderContract.Shrink(order);
        if (shrunk.TryGetError(out var limitation))
        {
            throw new InvalidOperationException(limitation.ToString());
        }

        shrunk.TryGetValue(out var candidates);
        return candidates!;
    }

    internal static IReadOnlyList<string> DescribeFacts()
    {
        var facts = new List<string>();
        Collect(OrderContract.Description, "order", facts);
        facts.Sort(StringComparer.Ordinal);
        return facts;
    }

    internal static string Explain() => OrderContract.Explain();

    private static bool IsEmail(string value)
    {
        var at = value.IndexOf('@', StringComparison.Ordinal);
        return at > 0
            && at < value.Length - 1
            && value.IndexOf('@', at + 1) < 0;
    }

    internal static decimal Total(Order order) =>
        order.Lines.Sum(static line => line.Quantity * line.UnitPrice);

    private static ContractSet Build()
    {
        var email = Contract.From<string>(
                "email address",
                value => IsEmail(value)
                    ? Validation<string, string>.Valid(value)
                    : Validation<string, string>.Invalid(EmailDetail))
            .WithDecoder(Decoder.String)
            .WithGenerator(static random => $"user{random.Next(1000)}@example.com")
            .WithShrinker(static value => value == "a@b" ? [] : ["a@b"]);

        var last4 = Contract.NonEmptyString
            .Ensure(
                static value => value.Length == 4 && value.All(char.IsAsciiDigit),
                Last4Detail)
            .WithGenerator(
                static random => random.Next(10_000).ToString("D4", CultureInfo.InvariantCulture))
            .WithShrinker(static value => value == "0000" ? [] : ["0000"]);

        var cardFields = Contract.For<CardPayment>();
        var card = Contract.Object(
            cardFields.Field("last4", last4, static payment => payment.Last4),
            cardFields.Field("currency", Contract.NonEmptyString, static payment => payment.Currency),
            static (last4Value, currency) => new CardPayment(last4Value, currency));

        var cashFields = Contract.For<CashPayment>();
        var cash = Contract.Object(
            cashFields.Field("currency", Contract.NonEmptyString, static payment => payment.Currency),
            static currency => new CashPayment(currency));

        var cardAsPayment = card.Widen<Payment>(
            static payment => payment,
            static payment => payment is CardPayment cardPayment
                ? Option<CardPayment>.Some(cardPayment)
                : Option<CardPayment>.None,
            "payment is not a card");
        var cashAsPayment = cash.Widen<Payment>(
            static payment => payment,
            static payment => payment is CashPayment cashPayment
                ? Option<CashPayment>.Some(cashPayment)
                : Option<CashPayment>.None,
            "payment is not cash");

        var payment = Contract.DiscriminatedOneOf<Payment>(
            "method",
            static paymentValue => paymentValue switch
            {
                CardPayment => 0,
                CashPayment => 1,
                _ => -1,
            },
            Contract.Alternative("card", cardAsPayment),
            Contract.Alternative("cash", cashAsPayment));

        var lineFields = Contract.For<OrderLine>();
        var line = Contract.Object(
            lineFields.Field("sku", Contract.NonEmptyString, static orderLine => orderLine.Sku),
            lineFields.Field("quantity", Contract.Int32Range(1, 100), static orderLine => orderLine.Quantity),
            lineFields.Field(
                "unitPrice",
                Contract.DecimalRange(0.01m, 5000m),
                static orderLine => orderLine.UnitPrice),
            static (sku, quantity, unitPrice) => new OrderLine(sku, quantity, unitPrice));

        var customerFields = Contract.For<Customer>();
        var customer = Contract.Object(
            customerFields.Field("name", Contract.NonEmptyString, static value => value.Name),
            customerFields.Field("email", email, static value => value.Email),
            customerFields.OptionalField("note", Contract.String, static value => value.Note),
            static (name, emailAddress, note) => new Customer(name, emailAddress, note));

        var orderFields = Contract.For<Order>();
        var structural = Contract.Object(
            orderFields.Field("id", Contract.Guid, static order => order.Id),
            orderFields.Field("customer", customer, static order => order.Customer),
            orderFields.Field("lines", Contract.List(line, 1, 10), static order => order.Lines),
            orderFields.Field("payment", payment, static order => order.Payment),
            static (id, customerValue, lines, paymentValue) =>
                new Order(id, customerValue, lines, paymentValue));

        var order = structural
            .Ensure(orderValue => Total(orderValue) <= 1000m, TotalDetail)
            .WithGenerator(GenerateOrderValue);

        return new ContractSet(Order: order, Line: line, Email: email);
    }

    private static Order GenerateOrderValue(Random random)
    {
        var idBytes = new byte[16];
        random.NextBytes(idBytes);

        var lineCount = random.Next(1, 4);
        var lines = new OrderLine[lineCount];
        for (var index = 0; index < lines.Length; index++)
        {
            lines[index] = new OrderLine(
                "SKU-" + random.Next(1000).ToString("D3", CultureInfo.InvariantCulture),
                random.Next(1, 6),
                decimal.Round(((decimal)random.NextDouble() * 49m) + 1m, 2));
        }

        var customer = new Customer(
            random.Next(2) == 0 ? "Ada" : "Grace",
            $"user{random.Next(1000)}@example.com",
            random.Next(3) == 0
                ? Option<string>.None
                : Option<string>.Some("leave at door"));

        Payment payment = random.Next(2) == 0
            ? new CardPayment(
                random.Next(10_000).ToString("D4", CultureInfo.InvariantCulture),
                random.Next(2) == 0 ? "USD" : "EUR")
            : new CashPayment(random.Next(2) == 0 ? "USD" : "EUR");

        return new Order(new Guid(idBytes), customer, lines, payment);
    }

    private static IReadOnlyList<OrderError> MapErrors(Validation<Order, DecodeError> validation)
    {
        validation.TryGetErrors(out var errors);
        var mapped = new OrderError[errors!.Count];
        for (var index = 0; index < mapped.Length; index++)
        {
            mapped[index] = Map(errors[index]);
        }

        return mapped;
    }

    private static OrderError Map(DecodeError error) =>
        new(
            error.Path,
            error.Kind switch
            {
                DecodeErrorKind.Missing => OrderErrorCode.Required,
                DecodeErrorKind.Null => OrderErrorCode.Null,
                DecodeErrorKind.WrongKind => OrderErrorCode.WrongKind,
                DecodeErrorKind.Malformed => OrderErrorCode.Malformed,
                DecodeErrorKind.Empty => OrderErrorCode.Empty,
                _ => OrderErrorCode.Invalid,
            },
            error.Kind switch
            {
                DecodeErrorKind.Missing => "required",
                DecodeErrorKind.Null => "null",
                DecodeErrorKind.WrongKind => "wrong-kind",
                DecodeErrorKind.Malformed => "malformed",
                DecodeErrorKind.Empty => "empty",
                _ => error.Detail ?? error.Expected ?? "invalid",
            });

    private static void Collect(
        ContractDescription description,
        string path,
        List<string> facts)
    {
        switch (description.Kind)
        {
            case ContractDescriptionKind.Scalar:
                facts.Add($"{path}=scalar:{description.Name}");
                break;
            case ContractDescriptionKind.Range:
                facts.Add(
                    $"{path}=range:{description.Name}:{description.Minimum}..{description.Maximum}");
                break;
            case ContractDescriptionKind.Object:
                facts.Add($"{path}=object");
                foreach (var field in description.Fields)
                {
                    if (field.Presence != ContractFieldPresence.Required)
                    {
                        facts.Add($"{path}.{field.Name}=presence:{field.Presence}");
                    }

                    Collect(field.Contract, $"{path}.{field.Name}", facts);
                }

                break;
            case ContractDescriptionKind.OneOf:
                facts.Add(
                    $"{path}=oneof:{description.Discriminator}:"
                    + string.Join(",", description.Alternatives.Select(static item => item.Label)));
                foreach (var alternative in description.Alternatives)
                {
                    Collect(alternative.Contract, $"{path}.{alternative.Label}", facts);
                }

                break;
            case ContractDescriptionKind.Collection:
                facts.Add(
                    $"{path}=collection:{description.MinimumLength}.."
                    + (description.MaximumLength?.ToString(CultureInfo.InvariantCulture) ?? "*"));
                Collect(description.Element!, $"{path}[]", facts);
                break;
            case ContractDescriptionKind.Optional:
                facts.Add($"{path}=optional");
                Collect(description.Element!, path, facts);
                break;
            case ContractDescriptionKind.Recursive:
                facts.Add($"{path}=recursive:{description.Name}");
                break;
            case ContractDescriptionKind.Refined:
                facts.Add($"{path}=refined:{description.Detail}");
                Collect(description.Element!, path, facts);
                break;
            case ContractDescriptionKind.Opaque:
                facts.Add($"{path}=opaque:{description.Detail}");
                break;
            case ContractDescriptionKind.Derived:
                facts.Add($"{path}=derived:{description.Detail}");
                Collect(description.Element!, path, facts);
                break;
        }
    }

    private sealed record ContractSet(
        Contract<Order> Order,
        Contract<OrderLine> Line,
        Contract<string> Email);
}
