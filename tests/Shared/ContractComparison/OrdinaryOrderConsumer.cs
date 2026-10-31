using System.Globalization;
using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.Contracts;

internal static class OrdinaryOrderConsumer
{
    internal const string EmailDetail = "email must contain a local part and domain";
    internal const string Last4Detail = "last4 must contain exactly four digits";
    internal const string TotalDetail = "order total must not exceed 1000";
    private const string QuantityDetail = "value must be between 1 and 100";
    private const string UnitPriceDetail = "value must be between 0.01 and 5000";
    private const string MaxLinesDetail = "at most 10 items are allowed";
    private const string PaymentDetail = "expected one of: card, cash";

    internal static OrderDecodeResult Decode(JsonElement root)
    {
        var errors = new List<OrderError>();

        if (root.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(string.Empty, OrderErrorCode.Null));
            return new OrderDecodeResult(null, errors);
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            errors.Add(Error(string.Empty, OrderErrorCode.WrongKind));
            return new OrderDecodeResult(null, errors);
        }

        var id = DecodeGuid(root, "id", string.Empty, errors);
        var customer = DecodeCustomer(root, errors);
        var lines = DecodeLines(root, errors);
        var payment = DecodePayment(root, errors);

        if (errors.Count > 0)
        {
            return new OrderDecodeResult(null, errors);
        }

        var order = new Order(id!.Value, customer!, lines!, payment!);
        if (Total(order) > 1000m)
        {
            errors.Add(Error(string.Empty, OrderErrorCode.Invalid, TotalDetail));
            return new OrderDecodeResult(null, errors);
        }

        return new OrderDecodeResult(order, []);
    }

    internal static IReadOnlyList<OrderError> Validate(Order order)
    {
        var errors = new List<OrderError>();

        if (order is null)
        {
            errors.Add(Error(string.Empty, OrderErrorCode.Null));
            return errors;
        }

        ValidateCustomer(order.Customer, "customer", errors);
        ValidateLines(order.Lines, errors);
        ValidatePayment(order.Payment, "payment", errors);

        if (errors.Count == 0 && Total(order) > 1000m)
        {
            errors.Add(Error(string.Empty, OrderErrorCode.Invalid, TotalDetail));
        }

        return errors;
    }

    internal static Order Generate(int seed)
    {
        var random = new Random(seed);
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

    internal static IReadOnlyList<Order> Shrink(Order order)
    {
        var candidates = new List<Order>();
        foreach (var line in order.Lines)
        {
            if (line.Quantity > 1)
            {
                candidates.Add(ReplaceLine(order, line, line with { Quantity = 1 }));
            }

            if (line.UnitPrice > 0.01m)
            {
                candidates.Add(ReplaceLine(order, line, line with { UnitPrice = 0.01m }));
            }

            if (line.Sku != "a")
            {
                candidates.Add(ReplaceLine(order, line, line with { Sku = "a" }));
            }
        }

        if (order.Lines.Count > 1)
        {
            candidates.Add(order with { Lines = order.Lines.Take(order.Lines.Count - 1).ToArray() });
        }

        if (order.Customer.Note.IsSome)
        {
            candidates.Add(order with { Customer = order.Customer with { Note = Option<string>.None } });
        }

        if (order.Customer.Name != "a")
        {
            candidates.Add(order with { Customer = order.Customer with { Name = "a" } });
        }

        if (order.Customer.Email != "a@b")
        {
            candidates.Add(order with { Customer = order.Customer with { Email = "a@b" } });
        }

        candidates.Add(order with { Id = Guid.Empty });

        if (order.Payment is CardPayment card)
        {
            if (card.Last4 != "0000")
            {
                candidates.Add(order with { Payment = card with { Last4 = "0000" } });
            }

            if (card.Currency != "USD")
            {
                candidates.Add(order with { Payment = card with { Currency = "USD" } });
            }
        }
        else if (order.Payment is CashPayment cash && cash.Currency != "USD")
        {
            candidates.Add(order with { Payment = cash with { Currency = "USD" } });
        }

        var seen = new HashSet<Order> { order };
        var valid = new List<Order>();
        foreach (var candidate in candidates)
        {
            if (seen.Add(candidate) && Validate(candidate).Count == 0)
            {
                valid.Add(candidate);
            }
        }

        return valid;
    }

    internal static IReadOnlyList<string> DescribeFacts() =>
    [
        "order=refined:" + TotalDetail,
        "order=object",
        "order.id=scalar:guid",
        "order.customer=object",
        "order.customer.name=scalar:non-empty string",
        "order.customer.email=opaque:email address",
        "order.customer.note=presence:MissingIsAbsence",
        "order.customer.note=optional",
        "order.customer.note=scalar:string",
        "order.lines=collection:1..10",
        "order.lines[]=object",
        "order.lines[].sku=scalar:non-empty string",
        "order.lines[].quantity=range:int32:1..100",
        "order.lines[].unitPrice=range:decimal:0.01..5000",
        "order.payment=oneof:method:card,cash",
        "order.payment.card=derived:payment is not a card",
        "order.payment.card=object",
        "order.payment.card.last4=refined:" + Last4Detail,
        "order.payment.card.last4=scalar:non-empty string",
        "order.payment.card.currency=scalar:non-empty string",
        "order.payment.cash=derived:payment is not cash",
        "order.payment.cash=object",
        "order.payment.cash.currency=scalar:non-empty string",
    ];

    private static bool IsEmail(string value)
    {
        var at = value.IndexOf('@', StringComparison.Ordinal);
        return at > 0
            && at < value.Length - 1
            && value.IndexOf('@', at + 1) < 0;
    }

    private static Order ReplaceLine(Order order, OrderLine original, OrderLine replacement)
    {
        var lines = order.Lines.ToArray();
        var index = Array.IndexOf(lines, original);
        lines[index] = replacement;
        return order with { Lines = lines };
    }

    private static void ValidateCustomer(Customer? customer, string path, List<OrderError> errors)
    {
        if (customer is null)
        {
            errors.Add(Error(path, OrderErrorCode.Null));
            return;
        }

        if (customer.Name is null)
        {
            errors.Add(Error(path + ".name", OrderErrorCode.Null));
        }
        else if (customer.Name.Length == 0)
        {
            errors.Add(Error(path + ".name", OrderErrorCode.Empty));
        }

        if (customer.Email is null)
        {
            errors.Add(Error(path + ".email", OrderErrorCode.Null));
        }
        else if (!IsEmail(customer.Email))
        {
            errors.Add(Error(path + ".email", OrderErrorCode.Invalid, EmailDetail));
        }

        if (customer.Note.TryGetValue(out var note) && note is null)
        {
            errors.Add(Error(path + ".note", OrderErrorCode.Null));
        }
    }

    private static void ValidateLines(IReadOnlyList<OrderLine>? lines, List<OrderError> errors)
    {
        if (lines is null)
        {
            errors.Add(Error("lines", OrderErrorCode.Null));
            return;
        }

        if (lines.Count == 0)
        {
            errors.Add(Error("lines", OrderErrorCode.Empty));
        }
        else if (lines.Count > 10)
        {
            errors.Add(Error("lines", OrderErrorCode.Invalid, MaxLinesDetail));
        }

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var path = "lines[" + index.ToString(CultureInfo.InvariantCulture) + "]";
            if (line is null)
            {
                errors.Add(Error(path, OrderErrorCode.Null));
                continue;
            }

            if (line.Sku.Length == 0)
            {
                errors.Add(Error(path + ".sku", OrderErrorCode.Empty));
            }

            if (line.Quantity < 1 || line.Quantity > 100)
            {
                errors.Add(Error(path + ".quantity", OrderErrorCode.Invalid, QuantityDetail));
            }

            if (line.UnitPrice < 0.01m || line.UnitPrice > 5000m)
            {
                errors.Add(Error(path + ".unitPrice", OrderErrorCode.Invalid, UnitPriceDetail));
            }
        }
    }

    private static void ValidatePayment(Payment? payment, string path, List<OrderError> errors)
    {
        switch (payment)
        {
            case null:
                errors.Add(Error(path, OrderErrorCode.Null));
                return;
            case CardPayment card:
                if (card.Last4.Length == 0)
                {
                    errors.Add(Error(path + ".card.last4", OrderErrorCode.Empty));
                }
                else if (card.Last4.Length != 4 || !card.Last4.All(char.IsAsciiDigit))
                {
                    errors.Add(Error(path + ".card.last4", OrderErrorCode.Invalid, Last4Detail));
                }

                if (card.Currency.Length == 0)
                {
                    errors.Add(Error(path + ".card.currency", OrderErrorCode.Empty));
                }

                return;
            case CashPayment cash:
                if (cash.Currency.Length == 0)
                {
                    errors.Add(Error(path + ".cash.currency", OrderErrorCode.Empty));
                }

                return;
            default:
                errors.Add(
                    Error(
                        path,
                        OrderErrorCode.Invalid,
                        PaymentDetail));
                return;
        }
    }

    private static Guid? DecodeGuid(
        JsonElement parent,
        string name,
        string prefix,
        List<OrderError> errors)
    {
        var path = Join(prefix, name);
        if (!parent.TryGetProperty(name, out var element))
        {
            errors.Add(Error(path, OrderErrorCode.Required));
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(path, OrderErrorCode.Null));
            return null;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            errors.Add(Error(path, OrderErrorCode.WrongKind));
            return null;
        }

        if (!element.TryGetGuid(out var value))
        {
            errors.Add(Error(path, OrderErrorCode.Malformed));
            return null;
        }

        return value;
    }

    private static Customer? DecodeCustomer(JsonElement root, List<OrderError> errors)
    {
        if (!root.TryGetProperty("customer", out var element))
        {
            errors.Add(Error("customer", OrderErrorCode.Required));
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error("customer", OrderErrorCode.Null));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add(Error("customer", OrderErrorCode.WrongKind));
            return null;
        }

        var name = DecodeString(element, "name", "customer", nonEmpty: true, errors);
        var email = DecodeString(element, "email", "customer", nonEmpty: false, errors);
        if (email is not null && !IsEmail(email))
        {
            errors.Add(Error("customer.email", OrderErrorCode.Invalid, EmailDetail));
            email = null;
        }

        var note = DecodeOptionalString(element, "note", "customer", errors);
        if (name is null || email is null || !note.TryGetValue(out var noteValue))
        {
            return null;
        }

        return new Customer(name, email, noteValue);
    }

    private static string? DecodeString(
        JsonElement parent,
        string name,
        string prefix,
        bool nonEmpty,
        List<OrderError> errors)
    {
        var path = Join(prefix, name);
        if (!parent.TryGetProperty(name, out var element))
        {
            errors.Add(Error(path, OrderErrorCode.Required));
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(path, OrderErrorCode.Null));
            return null;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            errors.Add(Error(path, OrderErrorCode.WrongKind));
            return null;
        }

        var value = element.GetString()!;
        if (nonEmpty && value.Length == 0)
        {
            errors.Add(Error(path, OrderErrorCode.Empty));
            return null;
        }

        return value;
    }

    private static Option<Option<string>> DecodeOptionalString(
        JsonElement parent,
        string name,
        string prefix,
        List<OrderError> errors)
    {
        var path = Join(prefix, name);
        if (!parent.TryGetProperty(name, out var element))
        {
            return Option<Option<string>>.Some(Option<string>.None);
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(path, OrderErrorCode.Null));
            return Option<Option<string>>.None;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            errors.Add(Error(path, OrderErrorCode.WrongKind));
            return Option<Option<string>>.None;
        }

        return Option<Option<string>>.Some(Option<string>.Some(element.GetString()!));
    }

    private static IReadOnlyList<OrderLine>? DecodeLines(JsonElement root, List<OrderError> errors)
    {
        if (!root.TryGetProperty("lines", out var element))
        {
            errors.Add(Error("lines", OrderErrorCode.Required));
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error("lines", OrderErrorCode.Null));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            errors.Add(Error("lines", OrderErrorCode.WrongKind));
            return null;
        }

        var count = element.GetArrayLength();
        if (count == 0)
        {
            errors.Add(Error("lines", OrderErrorCode.Empty));
        }
        else if (count > 10)
        {
            errors.Add(Error("lines", OrderErrorCode.Invalid, MaxLinesDetail));
        }

        var lines = new List<OrderLine>(count);
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            var line = DecodeLine(item, "lines[" + index.ToString(CultureInfo.InvariantCulture) + "]", errors);
            if (line is not null)
            {
                lines.Add(line);
            }

            index++;
        }

        return lines;
    }

    private static OrderLine? DecodeLine(JsonElement element, string path, List<OrderError> errors)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(path, OrderErrorCode.Null));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add(Error(path, OrderErrorCode.WrongKind));
            return null;
        }

        var sku = DecodeString(element, "sku", path, nonEmpty: true, errors);
        var quantity = DecodeInt32(element, "quantity", path, 1, 100, QuantityDetail, errors);
        var unitPrice = DecodeDecimal(
            element,
            "unitPrice",
            path,
            0.01m,
            5000m,
            UnitPriceDetail,
            errors);

        return sku is null || quantity is null || unitPrice is null
            ? null
            : new OrderLine(sku, quantity.Value, unitPrice.Value);
    }

    private static Payment? DecodePayment(JsonElement root, List<OrderError> errors)
    {
        if (!root.TryGetProperty("payment", out var element))
        {
            errors.Add(Error("payment", OrderErrorCode.Required));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty("method", out var method)
            || method.ValueKind != JsonValueKind.String)
        {
            errors.Add(Error("payment", OrderErrorCode.Invalid, PaymentDetail));
            return null;
        }

        return method.GetString() switch
        {
            "card" => DecodeCard(element, errors),
            "cash" => DecodeCash(element, errors),
            _ => UnknownMethod(errors),
        };
    }

    private static CardPayment? DecodeCard(JsonElement element, List<OrderError> errors)
    {
        var last4 = DecodeString(element, "last4", "payment.card", nonEmpty: true, errors);
        if (last4 is not null
            && (last4.Length != 4 || !last4.All(char.IsAsciiDigit)))
        {
            errors.Add(Error("payment.card.last4", OrderErrorCode.Invalid, Last4Detail));
            last4 = null;
        }

        var currency = DecodeString(element, "currency", "payment.card", nonEmpty: true, errors);
        return last4 is null || currency is null
            ? null
            : new CardPayment(last4, currency);
    }

    private static CashPayment? DecodeCash(JsonElement element, List<OrderError> errors)
    {
        var currency = DecodeString(element, "currency", "payment.cash", nonEmpty: true, errors);
        return currency is null ? null : new CashPayment(currency);
    }

    private static Payment? UnknownMethod(List<OrderError> errors)
    {
        errors.Add(Error("payment", OrderErrorCode.Invalid, PaymentDetail));
        return null;
    }

    private static int? DecodeInt32(
        JsonElement parent,
        string name,
        string prefix,
        int minimum,
        int maximum,
        string rangeDetail,
        List<OrderError> errors)
    {
        var path = Join(prefix, name);
        if (!parent.TryGetProperty(name, out var element))
        {
            errors.Add(Error(path, OrderErrorCode.Required));
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(path, OrderErrorCode.Null));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
        {
            errors.Add(
                Error(
                    path,
                    element.ValueKind == JsonValueKind.Number
                        ? OrderErrorCode.Malformed
                        : OrderErrorCode.WrongKind));
            return null;
        }

        if (value < minimum || value > maximum)
        {
            errors.Add(Error(path, OrderErrorCode.Invalid, rangeDetail));
            return null;
        }

        return value;
    }

    private static decimal? DecodeDecimal(
        JsonElement parent,
        string name,
        string prefix,
        decimal minimum,
        decimal maximum,
        string rangeDetail,
        List<OrderError> errors)
    {
        var path = Join(prefix, name);
        if (!parent.TryGetProperty(name, out var element))
        {
            errors.Add(Error(path, OrderErrorCode.Required));
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(path, OrderErrorCode.Null));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDecimal(out var value))
        {
            errors.Add(
                Error(
                    path,
                    element.ValueKind == JsonValueKind.Number
                        ? OrderErrorCode.Malformed
                        : OrderErrorCode.WrongKind));
            return null;
        }

        if (value < minimum || value > maximum)
        {
            errors.Add(Error(path, OrderErrorCode.Invalid, rangeDetail));
            return null;
        }

        return value;
    }

    private static string Join(string prefix, string name) =>
        prefix.Length == 0 ? name : prefix + "." + name;

    private static OrderError Error(string path, OrderErrorCode code, string detail) =>
        new(path, code, detail);

    private static OrderError Error(string path, OrderErrorCode code) =>
        code switch
        {
            OrderErrorCode.Required => new(path, code, "required"),
            OrderErrorCode.Null => new(path, code, "null"),
            OrderErrorCode.WrongKind => new(path, code, "wrong-kind"),
            OrderErrorCode.Malformed => new(path, code, "malformed"),
            OrderErrorCode.Empty => new(path, code, "empty"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(code),
                code,
                "This error code requires an explicit detail."),
        };

    private static decimal Total(Order order) =>
        order.Lines.Sum(static line => line.Quantity * line.UnitPrice);
}
