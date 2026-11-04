using System.Text.Json;

namespace FunnySharp.TestSupport.Domains;

internal static class DomainComparison
{
    internal static void Verify()
    {
        foreach (var fixture in DomainFixtures.All)
        {
            using var document = JsonDocument.Parse(fixture.Json);
            var root = document.RootElement;

            Compare(
                fixture.Name,
                FunnySharpOrderWorkflow.Run(root),
                OrdinaryOrderWorkflow.Run(root));
            Compare(
                fixture.Name + ":cancel",
                FunnySharpOrderWorkflow.RunCancel(root),
                OrdinaryOrderWorkflow.RunCancel(root));
        }

        VerifyExpected();
    }

    internal static bool OutcomesEqual(WorkflowOutcome left, WorkflowOutcome right) =>
        string.Equals(left.Status, right.Status, StringComparison.Ordinal)
        && string.Equals(left.OrderId, right.OrderId, StringComparison.Ordinal)
        && left.Quantity == right.Quantity
        && left.UnitPrice == right.UnitPrice
        && string.Equals(left.ReservationCode, right.ReservationCode, StringComparison.Ordinal)
        && string.Equals(left.AuthorizationCode, right.AuthorizationCode, StringComparison.Ordinal)
        && string.Equals(left.TrackingCode, right.TrackingCode, StringComparison.Ordinal)
        && string.Equals(left.CancellationReason, right.CancellationReason, StringComparison.Ordinal)
        && left.Failures.SequenceEqual(right.Failures);

    internal static string Format(WorkflowOutcome outcome) =>
        $"{outcome.Status}|{outcome.OrderId}|{outcome.Quantity}|{outcome.UnitPrice}|" +
        $"{outcome.ReservationCode}|{outcome.AuthorizationCode}|{outcome.TrackingCode}|" +
        $"{outcome.CancellationReason}|" +
        string.Join(
            ";",
            outcome.Failures.Select(static failure =>
                $"{failure.Source}:{failure.Code}:{failure.Path}:{failure.Detail}"));

    private static void Compare(string fixture, WorkflowOutcome funnySharp, WorkflowOutcome ordinary)
    {
        if (OutcomesEqual(funnySharp, ordinary))
        {
            return;
        }

        throw new InvalidOperationException(
            $"{fixture}: the FunnySharp and ordinary workflows disagree.\n" +
            $"funny:    {Format(funnySharp)}\n" +
            $"ordinary: {Format(ordinary)}");
    }

    private static void VerifyExpected()
    {
        var valid = Run(DomainFixtures.Valid);
        AssertOutcome(
            "valid",
            valid,
            new WorkflowOutcome(
                "shipped",
                "ORD-100",
                2,
                12.50m,
                [],
                "RSV-100",
                "AUTH-100",
                "TRK-ORD-100",
                null));

        var validPen = Run(DomainFixtures.ValidPen);
        AssertOutcome(
            "valid-pen",
            validPen,
            new WorkflowOutcome(
                "shipped",
                "ORD-200",
                3,
                1.25m,
                [],
                "RSV-100",
                "AUTH-100",
                "TRK-ORD-200",
                null));

        var unknownSku = Run(DomainFixtures.UnknownSku);
        AssertOutcome(
            "unknown-sku",
            unknownSku,
            Rejected("ORD-300", "pricing", "unknown-sku", string.Empty, "SKU-UNKNOWN|2"));

        var outOfStock = Run(DomainFixtures.OutOfStock);
        AssertOutcome(
            "out-of-stock",
            outOfStock,
            Rejected("ORD-400", "inventory", "insufficient-stock", string.Empty, "SKU-BOOK|9"));

        var paymentDeclined = Run(DomainFixtures.PaymentDeclined);
        AssertOutcome(
            "payment-declined",
            paymentDeclined,
            Rejected("ORD-500", "payment", "payment-declined", string.Empty, "payment-declined"));

        var invalidOrderId = Run(DomainFixtures.InvalidOrderId);
        AssertOutcome(
            "invalid-order-id",
            invalidOrderId,
            Rejected(
                null,
                "request",
                "InvalidValue",
                "orderId",
                "order identifier must start with ORD-"));

        var fractional = Run(DomainFixtures.FractionalQuantity);
        AssertOutcome(
            "fractional-quantity",
            fractional,
            Rejected(
                null,
                "request",
                "Malformed",
                "line.quantity",
                "the value cannot be parsed as a 32-bit integer"));

        var multiple = Run(DomainFixtures.MultipleInvalid);
        AssertOutcome(
            "multiple-invalid",
            multiple,
            new WorkflowOutcome(
                "rejected",
                null,
                0,
                0m,
                [
                    new WorkflowFailureView(
                        "request",
                        "InvalidValue",
                        "orderId",
                        "order identifier must start with ORD-"),
                    new WorkflowFailureView("request", "Null", "customerId", string.Empty),
                    new WorkflowFailureView("request", "Empty", "line.sku", string.Empty),
                    new WorkflowFailureView(
                        "request",
                        "InvalidValue",
                        "line.quantity",
                        "value must be between 1 and 100"),
                    new WorkflowFailureView("request", "Empty", "payment.token", string.Empty),
                ],
                null,
                null,
                null,
                null));

        var rootArray = Run(DomainFixtures.RootArray);
        AssertOutcome(
            "root-array",
            rootArray,
            Rejected(null, "request", "WrongKind", string.Empty, "found Array"));

        var emptyObject = Run(DomainFixtures.EmptyObject);
        AssertOutcome(
            "empty-object",
            emptyObject,
            new WorkflowOutcome(
                "rejected",
                null,
                0,
                0m,
                [
                    new WorkflowFailureView("request", "Missing", "orderId", string.Empty),
                    new WorkflowFailureView("request", "Missing", "customerId", string.Empty),
                    new WorkflowFailureView("request", "Missing", "line", string.Empty),
                    new WorkflowFailureView("request", "Missing", "payment", string.Empty),
                ],
                null,
                null,
                null,
                null));

        var duplicate = Run(DomainFixtures.DuplicateOrderId);
        AssertOutcome(
            "duplicate-order-id",
            duplicate,
            new WorkflowOutcome(
                "shipped",
                "ORD-700",
                2,
                12.50m,
                [],
                "RSV-100",
                "AUTH-100",
                "TRK-ORD-700",
                null));

        var extraField = Run(DomainFixtures.ExtraField);
        AssertOutcome(
            "extra-field",
            extraField,
            new WorkflowOutcome(
                "shipped",
                "ORD-600",
                2,
                12.50m,
                [],
                "RSV-100",
                "AUTH-100",
                "TRK-ORD-600",
                null));

        using var cancelDocument = JsonDocument.Parse(DomainFixtures.Valid.Json);
        AssertOutcome(
            "valid:cancel",
            FunnySharpOrderWorkflow.RunCancel(cancelDocument.RootElement),
            new WorkflowOutcome(
                "cancelled",
                "ORD-100",
                0,
                0m,
                [],
                null,
                null,
                null,
                "customer-request"));
    }

    private static WorkflowOutcome Run(DomainFixture fixture)
    {
        using var document = JsonDocument.Parse(fixture.Json);
        return FunnySharpOrderWorkflow.Run(document.RootElement);
    }

    private static WorkflowOutcome Rejected(
        string? orderId,
        string source,
        string code,
        string path,
        string detail) =>
        new(
            "rejected",
            orderId,
            0,
            0m,
            [new WorkflowFailureView(source, code, path, detail)],
            null,
            null,
            null,
            null);

    private static void AssertOutcome(string name, WorkflowOutcome actual, WorkflowOutcome expected)
    {
        if (!OutcomesEqual(actual, expected))
        {
            throw new InvalidOperationException(
                $"{name}: unexpected outcome.\n" +
                $"expected: {Format(expected)}\n" +
                $"actual:   {Format(actual)}");
        }
    }
}
