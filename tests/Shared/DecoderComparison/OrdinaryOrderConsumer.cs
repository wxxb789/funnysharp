using System.Text.Json;

namespace FunnySharp.TestSupport.Decoding;

internal static class OrdinaryOrderConsumer
{
    private const string EmailDetail = "email must contain a local part and domain";

    internal static OrderDecodeResult Decode(JsonElement root)
    {
        var errors = new List<OrderError>();

        if (root.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(string.Empty, OrderErrorCode.Null, "null"));
            return new OrderDecodeResult(null, errors);
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            errors.Add(Error(string.Empty, OrderErrorCode.WrongKind, "wrong-kind"));
            return new OrderDecodeResult(null, errors);
        }

        var id = DecodeId(root, errors);
        var customer = DecodeCustomer(root, errors);
        var lines = DecodeLines(root, errors);
        var note = DecodeNote(root, errors);

        return errors.Count == 0
            ? new OrderDecodeResult(
                new Order(id!, customer!, lines!, note),
                Array.Empty<OrderError>())
            : new OrderDecodeResult(null, errors);
    }

    private static OrderId? DecodeId(JsonElement root, List<OrderError> errors)
    {
        if (!root.TryGetProperty("orderId", out var element))
        {
            errors.Add(Error("orderId", OrderErrorCode.Required, "required"));
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error("orderId", OrderErrorCode.Null, "null"));
            return null;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            errors.Add(Error("orderId", OrderErrorCode.WrongKind, "wrong-kind"));
            return null;
        }

        if (!element.TryGetGuid(out var id))
        {
            errors.Add(Error("orderId", OrderErrorCode.Malformed, "malformed"));
            return null;
        }

        return new OrderId(id);
    }

    private static Customer? DecodeCustomer(JsonElement root, List<OrderError> errors)
    {
        if (!root.TryGetProperty("customer", out var element))
        {
            errors.Add(Error("customer", OrderErrorCode.Required, "required"));
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error("customer", OrderErrorCode.Null, "null"));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add(Error("customer", OrderErrorCode.WrongKind, "wrong-kind"));
            return null;
        }

        var name = DecodeString(element, "name", "customer", nonEmpty: true, errors);
        var email = DecodeEmail(element, "customer", errors);
        var addresses = DecodeAddresses(element, "customer", errors);

        return name is null || email is null || addresses is null
            ? null
            : new Customer(name, email, addresses);
    }

    private static EmailAddress? DecodeEmail(
        JsonElement parent,
        string prefix,
        List<OrderError> errors)
    {
        var email = DecodeString(parent, "email", prefix, nonEmpty: false, errors);
        if (email is null)
        {
            return null;
        }

        if (!IsEmail(email))
        {
            errors.Add(Error(Join(prefix, "email"), OrderErrorCode.Invalid, EmailDetail));
            return null;
        }

        return new EmailAddress(email);
    }

    private static IReadOnlyList<Address>? DecodeAddresses(
        JsonElement parent,
        string prefix,
        List<OrderError> errors)
    {
        var path = Join(prefix, "addresses");
        if (!parent.TryGetProperty("addresses", out var element))
        {
            errors.Add(Error(path, OrderErrorCode.Required, "required"));
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(path, OrderErrorCode.Null, "null"));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            errors.Add(Error(path, OrderErrorCode.WrongKind, "wrong-kind"));
            return null;
        }

        var addresses = new List<Address>();
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            var address = DecodeAddress(item, path + "[" + index + "]", errors);
            if (address is not null)
            {
                addresses.Add(address);
            }

            index++;
        }

        return addresses;
    }

    private static Address? DecodeAddress(
        JsonElement element,
        string path,
        List<OrderError> errors)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(path, OrderErrorCode.Null, "null"));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add(Error(path, OrderErrorCode.WrongKind, "wrong-kind"));
            return null;
        }

        var line = DecodeString(element, "line", path, nonEmpty: true, errors);
        var city = DecodeString(element, "city", path, nonEmpty: true, errors);
        var postalCode = DecodeString(element, "postalCode", path, nonEmpty: true, errors);

        return line is null || city is null || postalCode is null
            ? null
            : new Address(line, city, postalCode);
    }

    private static IReadOnlyList<OrderLine>? DecodeLines(
        JsonElement root,
        List<OrderError> errors)
    {
        if (!root.TryGetProperty("lines", out var element))
        {
            errors.Add(Error("lines", OrderErrorCode.Required, "required"));
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error("lines", OrderErrorCode.Null, "null"));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            errors.Add(Error("lines", OrderErrorCode.WrongKind, "wrong-kind"));
            return null;
        }

        if (element.GetArrayLength() == 0)
        {
            errors.Add(Error("lines", OrderErrorCode.Empty, "empty"));
            return null;
        }

        var lines = new List<OrderLine>();
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            var line = DecodeLine(item, "lines[" + index + "]", errors);
            if (line is not null)
            {
                lines.Add(line);
            }

            index++;
        }

        return lines;
    }

    private static OrderLine? DecodeLine(
        JsonElement element,
        string path,
        List<OrderError> errors)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(path, OrderErrorCode.Null, "null"));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add(Error(path, OrderErrorCode.WrongKind, "wrong-kind"));
            return null;
        }

        var sku = DecodeString(element, "sku", path, nonEmpty: true, errors);
        var quantity = DecodeInt32(element, "quantity", path, errors);
        var unitPrice = DecodeDecimal(element, "unitPrice", path, errors);

        if (quantity is not null && quantity <= 0)
        {
            errors.Add(
                Error(Join(path, "quantity"), OrderErrorCode.Invalid, "quantity must be positive"));
            quantity = null;
        }

        if (unitPrice is not null && unitPrice < 0)
        {
            errors.Add(
                Error(
                    Join(path, "unitPrice"),
                    OrderErrorCode.Invalid,
                    "unit price must not be negative"));
            unitPrice = null;
        }

        return sku is null || quantity is null || unitPrice is null
            ? null
            : new OrderLine(sku, quantity.Value, unitPrice.Value);
    }

    private static Option<string> DecodeNote(JsonElement root, List<OrderError> errors)
    {
        if (!root.TryGetProperty("note", out var element))
        {
            return Option<string>.None;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error("note", OrderErrorCode.Null, "null"));
            return Option<string>.None;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            errors.Add(Error("note", OrderErrorCode.WrongKind, "wrong-kind"));
            return Option<string>.None;
        }

        return Option<string>.Some(element.GetString()!);
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
            errors.Add(Error(path, OrderErrorCode.Required, "required"));
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(path, OrderErrorCode.Null, "null"));
            return null;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            errors.Add(Error(path, OrderErrorCode.WrongKind, "wrong-kind"));
            return null;
        }

        var value = element.GetString()!;
        if (nonEmpty && value.Length == 0)
        {
            errors.Add(Error(path, OrderErrorCode.Empty, "empty"));
            return null;
        }

        return value;
    }

    private static int? DecodeInt32(
        JsonElement parent,
        string name,
        string prefix,
        List<OrderError> errors)
    {
        var path = Join(prefix, name);
        if (!parent.TryGetProperty(name, out var element))
        {
            errors.Add(Error(path, OrderErrorCode.Required, "required"));
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(path, OrderErrorCode.Null, "null"));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Number)
        {
            errors.Add(Error(path, OrderErrorCode.WrongKind, "wrong-kind"));
            return null;
        }

        if (!element.TryGetInt32(out var value))
        {
            errors.Add(Error(path, OrderErrorCode.Malformed, "malformed"));
            return null;
        }

        return value;
    }

    private static decimal? DecodeDecimal(
        JsonElement parent,
        string name,
        string prefix,
        List<OrderError> errors)
    {
        var path = Join(prefix, name);
        if (!parent.TryGetProperty(name, out var element))
        {
            errors.Add(Error(path, OrderErrorCode.Required, "required"));
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(Error(path, OrderErrorCode.Null, "null"));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Number)
        {
            errors.Add(Error(path, OrderErrorCode.WrongKind, "wrong-kind"));
            return null;
        }

        if (!element.TryGetDecimal(out var value))
        {
            errors.Add(Error(path, OrderErrorCode.Malformed, "malformed"));
            return null;
        }

        return value;
    }

    private static string Join(string prefix, string name) =>
        prefix.Length == 0 ? name : prefix + "." + name;

    private static OrderError Error(string path, OrderErrorCode code, string detail) =>
        new(path, code, detail);

    private static bool IsEmail(string value)
    {
        var at = value.IndexOf('@');

        return at > 0 && at < value.Length - 1;
    }
}
