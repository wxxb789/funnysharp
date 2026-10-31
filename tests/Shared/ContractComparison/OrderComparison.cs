using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.Contracts;

internal static class OrderComparison
{
    internal static readonly (string Name, Func<JsonElement, OrderDecodeResult> Decode)[] Consumers =
    [
        ("contract", ContractOrderConsumer.Decode),
        ("ordinary", OrdinaryOrderConsumer.Decode),
    ];

    internal static void Verify()
    {
        foreach (var fixture in OrderFixtures.All)
        {
            using var document = JsonDocument.Parse(fixture.Json);
            var results = Consumers
                .Select(consumer => (consumer.Name, Result: consumer.Decode(document.RootElement)))
                .ToArray();

            for (var left = 0; left < results.Length; left++)
            {
                for (var right = left + 1; right < results.Length; right++)
                {
                    Compare(
                        fixture.Name,
                        results[left].Name,
                        results[left].Result,
                        results[right].Name,
                        results[right].Result);
                }
            }
        }

        VerifyExpected();
        VerifyValidation();
        VerifyGenerationAndShrinking();
        VerifySchemaFacts();
        VerifyCapabilities();
    }

    private static void ShrinkFromBoth(Order order)
    {
        var fromContract = ContractOrderConsumer.Shrink(order);
        var fromOrdinary = OrdinaryOrderConsumer.Shrink(order);

        foreach (var candidate in fromContract)
        {
            if (candidate == order
                || ContractOrderConsumer.Validate(candidate).Count != 0
                || OrdinaryOrderConsumer.Validate(candidate).Count != 0)
            {
                throw new InvalidOperationException("A contract shrink candidate was not valid.");
            }
        }

        foreach (var candidate in fromOrdinary)
        {
            if (candidate == order
                || ContractOrderConsumer.Validate(candidate).Count != 0
                || OrdinaryOrderConsumer.Validate(candidate).Count != 0)
            {
                throw new InvalidOperationException("An ordinary shrink candidate was not valid.");
            }
        }
    }

    private static void VerifyExpected()
    {
        var valid = DecodeFixture(OrderFixtures.Valid, ContractOrderConsumer.Decode);
        if (!valid.IsSuccess)
        {
            throw new InvalidOperationException("The valid fixture must decode.");
        }

        var expected = new Order(
            Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"),
            new Customer("Ada", "ada@example.com", Option<string>.Some("leave at door")),
            [new OrderLine("BOOK-1", 2, 12.50m)],
            new CardPayment("4242", "USD"));
        if (!OrderEquals(valid.Value!, expected))
        {
            throw new InvalidOperationException("The valid fixture produced a different order.");
        }

        var cash = DecodeFixture(OrderFixtures.ValidCash, ContractOrderConsumer.Decode);
        if (!cash.IsSuccess
            || cash.Value!.Payment is not CashPayment { Currency: "EUR" }
            || cash.Value.Customer.Note.IsSome)
        {
            throw new InvalidOperationException("The cash fixture produced a different order.");
        }

        AssertErrors(
            OrderFixtures.InvalidBoundaries,
            new[]
            {
                new OrderError("id", OrderErrorCode.Malformed, "malformed"),
                new OrderError("customer.name", OrderErrorCode.Empty, "empty"),
                new OrderError("customer.email", OrderErrorCode.Invalid, ContractOrderConsumer.EmailDetail),
                new OrderError("customer.note", OrderErrorCode.Null, "null"),
                new OrderError("lines", OrderErrorCode.Empty, "empty"),
                new OrderError("payment", OrderErrorCode.Invalid, "expected one of: card, cash"),
            });

        AssertErrors(
            OrderFixtures.InvalidValues,
            new[]
            {
                new OrderError("lines[0].sku", OrderErrorCode.Empty, "empty"),
                new OrderError(
                    "lines[0].quantity",
                    OrderErrorCode.Invalid,
                    "value must be between 1 and 100"),
                new OrderError(
                    "lines[0].unitPrice",
                    OrderErrorCode.Invalid,
                    "value must be between 0.01 and 5000"),
                new OrderError(
                    "payment.card.last4",
                    OrderErrorCode.Invalid,
                    ContractOrderConsumer.Last4Detail),
                new OrderError("payment.card.currency", OrderErrorCode.Empty, "empty"),
            });

        AssertErrors(
            OrderFixtures.TotalExceeded,
            [new OrderError(string.Empty, OrderErrorCode.Invalid, ContractOrderConsumer.TotalDetail)]);

        AssertErrors(
            OrderFixtures.MissingFields,
            new[]
            {
                new OrderError("id", OrderErrorCode.Required, "required"),
                new OrderError("customer", OrderErrorCode.Required, "required"),
                new OrderError("lines", OrderErrorCode.Required, "required"),
                new OrderError("payment", OrderErrorCode.Required, "required"),
            });

        AssertErrors(
            OrderFixtures.WrongKinds,
            new[]
            {
                new OrderError("id", OrderErrorCode.WrongKind, "wrong-kind"),
                new OrderError("customer", OrderErrorCode.WrongKind, "wrong-kind"),
                new OrderError("lines", OrderErrorCode.WrongKind, "wrong-kind"),
                new OrderError("payment", OrderErrorCode.Invalid, "expected one of: card, cash"),
            });

        AssertErrors(
            OrderFixtures.LinesTooMany,
            [
                new OrderError(
                    "lines",
                    OrderErrorCode.Invalid,
                    "at most 10 items are allowed"),
            ]);
    }

    private static void VerifyValidation()
    {
        var valid = DecodeFixture(OrderFixtures.Valid, ContractOrderConsumer.Decode).Value!;
        CompareValidation("valid", valid);

        CompareValidation("invalid-values", OrderFixtures.InvalidOrder());

        var total = valid with
        {
            Lines = [new OrderLine("BULK", 100, 5000m)],
        };
        CompareValidation("total", total);

        var unknownPayment = valid with { Payment = new OtherPayment() };
        CompareValidation("unknown-payment", unknownPayment);
    }

    private static void VerifyGenerationAndShrinking()
    {
        for (var seed = 1; seed <= 10; seed++)
        {
            var generated = ContractOrderConsumer.Generate(seed);
            if (ContractOrderConsumer.Validate(generated).Count != 0
                || OrdinaryOrderConsumer.Validate(generated).Count != 0)
            {
                throw new InvalidOperationException(
                    "A generated order failed contract or ordinary validation.");
            }

            if (ContractOrderConsumer.Total(generated) > 1000m)
            {
                throw new InvalidOperationException("A generated order exceeded the cross-field rule.");
            }

            var ordinaryGenerated = OrdinaryOrderConsumer.Generate(seed);
            if (ContractOrderConsumer.Validate(ordinaryGenerated).Count != 0
                || OrdinaryOrderConsumer.Validate(ordinaryGenerated).Count != 0)
            {
                throw new InvalidOperationException(
                    "An ordinary generated order failed contract or ordinary validation.");
            }

            ShrinkFromBoth(generated);
        }
    }

    private static void VerifySchemaFacts()
    {
        var fromContract = ContractOrderConsumer.DescribeFacts();
        var fromOrdinary = OrdinaryOrderConsumer.DescribeFacts()
            .OrderBy(static fact => fact, StringComparer.Ordinal)
            .ToArray();
        if (!fromContract.SequenceEqual(fromOrdinary))
        {
            throw new InvalidOperationException(
                "The contract and ordinary schema facts differ.\n"
                + "contract: " + string.Join(" | ", fromContract) + "\n"
                + "ordinary: " + string.Join(" | ", fromOrdinary));
        }
    }

    private static void VerifyCapabilities()
    {
        var order = ContractOrderConsumer.OrderContract.Capabilities;
        if (!order.CanDecode || !order.CanGenerate || !order.CanShrink || order.HasCompleteSchema)
        {
            throw new InvalidOperationException("The order contract capabilities are unexpected.");
        }

        if (!order.Limitations.Any(
                static item => item.Derivation == ContractDerivation.Schema
                    && item.Reason.Contains("arbitrary predicate", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("The order schema limitation was not reported.");
        }

        var line = ContractOrderConsumer.LineContract.Capabilities;
        if (!line.CanDecode
            || !line.CanGenerate
            || !line.CanShrink
            || !line.HasCompleteSchema
            || line.Limitations.Count != 0)
        {
            throw new InvalidOperationException("The line contract capabilities are unexpected.");
        }

        var email = ContractOrderConsumer.EmailContract.Capabilities;
        if (!email.CanDecode || !email.CanGenerate || !email.CanShrink || email.HasCompleteSchema)
        {
            throw new InvalidOperationException("The email contract capabilities are unexpected.");
        }
    }

    private static void CompareValidation(string name, Order order)
    {
        var fromContract = ContractOrderConsumer.Validate(order);
        var fromOrdinary = OrdinaryOrderConsumer.Validate(order);
        if (!fromContract.SequenceEqual(fromOrdinary))
        {
            throw new InvalidOperationException(
                $"{name}: validation diagnostics differ.\n"
                + "contract: " + FormatErrors(fromContract) + "\n"
                + "ordinary: " + FormatErrors(fromOrdinary));
        }
    }

    private static void AssertErrors(DecodeFixture fixture, IReadOnlyList<OrderError> expected)
    {
        foreach (var (name, decode) in Consumers)
        {
            var result = DecodeFixture(fixture, decode);
            if (result.IsSuccess || !result.Errors.SequenceEqual(expected))
            {
                throw new InvalidOperationException(
                    $"{fixture.Name}/{name}: expected errors differ.\n"
                    + "expected: " + FormatErrors(expected) + "\n"
                    + "actual:   " + FormatErrors(result.Errors));
            }
        }
    }

    private static OrderDecodeResult DecodeFixture(
        DecodeFixture fixture,
        Func<JsonElement, OrderDecodeResult> decode)
    {
        using var document = JsonDocument.Parse(fixture.Json);
        return decode(document.RootElement);
    }

    private static void Compare(
        string fixture,
        string leftName,
        OrderDecodeResult left,
        string rightName,
        OrderDecodeResult right)
    {
        if (left.IsSuccess != right.IsSuccess)
        {
            throw new InvalidOperationException(
                $"{fixture}: {leftName} success={left.IsSuccess}, {rightName} success={right.IsSuccess}.");
        }

        if (left.IsSuccess)
        {
            if (!OrderEquals(left.Value!, right.Value!))
            {
                throw new InvalidOperationException(
                    $"{fixture}: {leftName} and {rightName} produced different orders.");
            }

            return;
        }

        if (!left.Errors.SequenceEqual(right.Errors))
        {
            throw new InvalidOperationException(
                $"{fixture}: {leftName} and {rightName} produced different errors.\n"
                + $"{leftName}: {FormatErrors(left.Errors)}\n"
                + $"{rightName}: {FormatErrors(right.Errors)}");
        }
    }

    internal static bool OrderEquals(Order left, Order right) =>
        left.Id == right.Id
        && string.Equals(left.Customer.Name, right.Customer.Name, StringComparison.Ordinal)
        && string.Equals(left.Customer.Email, right.Customer.Email, StringComparison.Ordinal)
        && left.Customer.Note.Equals(right.Customer.Note)
        && left.Lines.SequenceEqual(right.Lines)
        && left.Payment.Equals(right.Payment);

    private static string FormatErrors(IReadOnlyList<OrderError> errors) =>
        string.Join(
            "; ",
            errors.Select(static error => $"{error.Path}|{error.Code}|{error.Detail}"));

    private sealed record OtherPayment : Payment;
}
