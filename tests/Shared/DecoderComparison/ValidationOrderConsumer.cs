using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.Decoding;

internal static class ValidationOrderConsumer
{
    private const string EmailDetail = "email must contain a local part and domain";

    internal static OrderDecodeResult Decode(JsonElement root)
    {
        var validation = DecodeOrder(root);
        if (validation.TryGetValue(out var order))
        {
            return new OrderDecodeResult(order!, Array.Empty<OrderError>());
        }

        validation.TryGetErrors(out var errors);
        return new OrderDecodeResult(null, errors!);
    }

    private static Validation<Order, OrderError> DecodeOrder(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Null)
        {
            return Invalid<Order>(string.Empty, OrderErrorCode.Null, "null");
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return Invalid<Order>(string.Empty, OrderErrorCode.WrongKind, "wrong-kind");
        }

        return DecodeOrderId(root)
            .Zip(
                DecodeCustomer(root),
                DecodeLines(root),
                DecodeNote(root),
                static (id, customer, lines, note) => new Order(id, customer, lines, note));
    }

    private static Validation<OrderId, OrderError> DecodeOrderId(JsonElement root) =>
        RequiredField(root, "orderId", string.Empty)
            .Then(element => DecodeGuid(element, "orderId"));

    private static Validation<Customer, OrderError> DecodeCustomer(JsonElement root) =>
        RequiredField(root, "customer", string.Empty)
            .Then(element => DecodeCustomerObject(element, "customer"));

    private static Validation<Customer, OrderError> DecodeCustomerObject(
        JsonElement element,
        string path)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return Invalid<Customer>(path, OrderErrorCode.Null, "null");
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return Invalid<Customer>(path, OrderErrorCode.WrongKind, "wrong-kind");
        }

        return DecodeNonEmptyStringField(element, "name", path)
            .Zip(
                DecodeEmailField(element, path),
                DecodeAddresses(element, path),
                static (name, email, addresses) => new Customer(name, email, addresses));
    }

    private static Validation<EmailAddress, OrderError> DecodeEmailField(
        JsonElement parent,
        string prefix) =>
        DecodeStringField(parent, "email", prefix, nonEmpty: false)
            .Then(email => IsEmail(email)
                ? Valid(new EmailAddress(email))
                : Invalid<EmailAddress>(
                    Join(prefix, "email"),
                    OrderErrorCode.Invalid,
                    EmailDetail));

    private static Validation<IReadOnlyList<Address>, OrderError> DecodeAddresses(
        JsonElement parent,
        string prefix) =>
        RequiredField(parent, "addresses", prefix)
            .Then(element => DecodeArray(
                element,
                Join(prefix, "addresses"),
                DecodeAddressObject));

    private static Validation<Address, OrderError> DecodeAddressObject(
        JsonElement element,
        string path)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return Invalid<Address>(path, OrderErrorCode.Null, "null");
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return Invalid<Address>(path, OrderErrorCode.WrongKind, "wrong-kind");
        }

        return DecodeNonEmptyStringField(element, "line", path)
            .Zip(
                DecodeNonEmptyStringField(element, "city", path),
                DecodeNonEmptyStringField(element, "postalCode", path),
                static (line, city, postalCode) => new Address(line, city, postalCode));
    }

    private static Validation<IReadOnlyList<OrderLine>, OrderError> DecodeLines(JsonElement root) =>
        RequiredField(root, "lines", string.Empty)
            .Then(element => DecodeLinesArray(element, "lines"));

    private static Validation<IReadOnlyList<OrderLine>, OrderError> DecodeLinesArray(
        JsonElement element,
        string path)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return Invalid<IReadOnlyList<OrderLine>>(path, OrderErrorCode.Null, "null");
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return Invalid<IReadOnlyList<OrderLine>>(
                path,
                OrderErrorCode.WrongKind,
                "wrong-kind");
        }

        if (element.GetArrayLength() == 0)
        {
            return Invalid<IReadOnlyList<OrderLine>>(path, OrderErrorCode.Empty, "empty");
        }

        return DecodeArray(element, path, DecodeLineObject);
    }

    private static Validation<OrderLine, OrderError> DecodeLineObject(
        JsonElement element,
        string path)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return Invalid<OrderLine>(path, OrderErrorCode.Null, "null");
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return Invalid<OrderLine>(path, OrderErrorCode.WrongKind, "wrong-kind");
        }

        return DecodeNonEmptyStringField(element, "sku", path)
            .Zip(
                DecodePositiveInt32Field(element, "quantity", path),
                DecodeNonNegativeDecimalField(element, "unitPrice", path),
                static (sku, quantity, unitPrice) => new OrderLine(sku, quantity, unitPrice));
    }

    private static Validation<Option<string>, OrderError> DecodeNote(JsonElement root)
    {
        if (!root.TryGetProperty("note", out var element))
        {
            return Valid(Option<string>.None);
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            return Invalid<Option<string>>("note", OrderErrorCode.Null, "null");
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            return Invalid<Option<string>>("note", OrderErrorCode.WrongKind, "wrong-kind");
        }

        return Valid(Option<string>.Some(element.GetString()!));
    }

    private static Validation<string, OrderError> DecodeNonEmptyStringField(
        JsonElement parent,
        string name,
        string prefix) =>
        DecodeStringField(parent, name, prefix, nonEmpty: true);

    private static Validation<string, OrderError> DecodeStringField(
        JsonElement parent,
        string name,
        string prefix,
        bool nonEmpty)
    {
        var path = Join(prefix, name);
        if (!parent.TryGetProperty(name, out var element))
        {
            return Invalid<string>(path, OrderErrorCode.Required, "required");
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            return Invalid<string>(path, OrderErrorCode.Null, "null");
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            return Invalid<string>(path, OrderErrorCode.WrongKind, "wrong-kind");
        }

        var value = element.GetString()!;
        return nonEmpty && value.Length == 0
            ? Invalid<string>(path, OrderErrorCode.Empty, "empty")
            : Valid(value);
    }

    private static Validation<int, OrderError> DecodePositiveInt32Field(
        JsonElement parent,
        string name,
        string prefix) =>
        DecodeInt32Field(parent, name, prefix)
            .Then(value => value > 0
                ? Valid(value)
                : Invalid<int>(
                    Join(prefix, name),
                    OrderErrorCode.Invalid,
                    "quantity must be positive"));

    private static Validation<decimal, OrderError> DecodeNonNegativeDecimalField(
        JsonElement parent,
        string name,
        string prefix) =>
        DecodeDecimalField(parent, name, prefix)
            .Then(value => value >= 0
                ? Valid(value)
                : Invalid<decimal>(
                    Join(prefix, name),
                    OrderErrorCode.Invalid,
                    "unit price must not be negative"));

    private static Validation<int, OrderError> DecodeInt32Field(
        JsonElement parent,
        string name,
        string prefix)
    {
        var path = Join(prefix, name);
        if (!parent.TryGetProperty(name, out var element))
        {
            return Invalid<int>(path, OrderErrorCode.Required, "required");
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            return Invalid<int>(path, OrderErrorCode.Null, "null");
        }

        if (element.ValueKind != JsonValueKind.Number)
        {
            return Invalid<int>(path, OrderErrorCode.WrongKind, "wrong-kind");
        }

        return element.TryGetInt32(out var value)
            ? Valid(value)
            : Invalid<int>(path, OrderErrorCode.Malformed, "malformed");
    }

    private static Validation<decimal, OrderError> DecodeDecimalField(
        JsonElement parent,
        string name,
        string prefix)
    {
        var path = Join(prefix, name);
        if (!parent.TryGetProperty(name, out var element))
        {
            return Invalid<decimal>(path, OrderErrorCode.Required, "required");
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            return Invalid<decimal>(path, OrderErrorCode.Null, "null");
        }

        if (element.ValueKind != JsonValueKind.Number)
        {
            return Invalid<decimal>(path, OrderErrorCode.WrongKind, "wrong-kind");
        }

        return element.TryGetDecimal(out var value)
            ? Valid(value)
            : Invalid<decimal>(path, OrderErrorCode.Malformed, "malformed");
    }

    private static Validation<OrderId, OrderError> DecodeGuid(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return Invalid<OrderId>(path, OrderErrorCode.Null, "null");
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            return Invalid<OrderId>(path, OrderErrorCode.WrongKind, "wrong-kind");
        }

        return element.TryGetGuid(out var id)
            ? Valid(new OrderId(id))
            : Invalid<OrderId>(path, OrderErrorCode.Malformed, "malformed");
    }

    private static Validation<JsonElement, OrderError> RequiredField(
        JsonElement parent,
        string name,
        string prefix)
    {
        if (!parent.TryGetProperty(name, out var element))
        {
            return Invalid<JsonElement>(
                Join(prefix, name),
                OrderErrorCode.Required,
                "required");
        }

        return Valid(element);
    }

    private static Validation<IReadOnlyList<T>, OrderError> DecodeArray<T>(
        JsonElement element,
        string path,
        Func<JsonElement, string, Validation<T, OrderError>> decodeItem)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return Invalid<IReadOnlyList<T>>(path, OrderErrorCode.Null, "null");
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return Invalid<IReadOnlyList<T>>(path, OrderErrorCode.WrongKind, "wrong-kind");
        }

        return element.EnumerateArray()
            .Traverse((index, item) => decodeItem(item, path + "[" + index + "]"));
    }

    private static Validation<TResult, OrderError> Then<T, TResult>(
        this Validation<T, OrderError> validation,
        Func<T, Validation<TResult, OrderError>> next) =>
        validation.Match(
            next,
            static errors => Validation<TResult, OrderError>.InvalidMany(errors));

    private static Validation<T, OrderError> Valid<T>(T value) =>
        Validation<T, OrderError>.Valid(value);

    private static Validation<T, OrderError> Invalid<T>(
        string path,
        OrderErrorCode code,
        string detail) =>
        Validation<T, OrderError>.Invalid(new OrderError(path, code, detail));

    private static string Join(string prefix, string name) =>
        prefix.Length == 0 ? name : prefix + "." + name;

    private static bool IsEmail(string value)
    {
        var at = value.IndexOf('@');

        return at > 0 && at < value.Length - 1;
    }
}
