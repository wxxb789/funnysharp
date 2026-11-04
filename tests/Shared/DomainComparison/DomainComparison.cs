using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.Domains;

internal static class DomainComparison
{
    private static readonly IReadOnlyList<WorkflowFailureView> NoFailures = [];

    internal static void Verify()
    {
        var outcomes = new Dictionary<string, WorkflowOutcome>(StringComparer.Ordinal);

        foreach (var fixture in DomainFixtures.All)
        {
            using var document = JsonDocument.Parse(fixture.Json);
            var root = document.RootElement;

            var funnySharp = FunnySharpOrderWorkflow.Run(root);
            Compare(fixture.Name, funnySharp, OrdinaryOrderWorkflow.Run(root));
            outcomes[fixture.Name] = funnySharp;

            var funnySharpCancel = FunnySharpOrderWorkflow.RunCancel(root);
            Compare(
                fixture.Name + ":cancel",
                funnySharpCancel,
                OrdinaryOrderWorkflow.RunCancel(root));
            outcomes[fixture.Name + ":cancel"] = funnySharpCancel;
        }

        VerifyExpected(outcomes);
    }

    internal static bool OutcomesEqual(WorkflowOutcome left, WorkflowOutcome right) =>
        left.Failures.SequenceEqual(right.Failures)
        && (left with { Failures = NoFailures }) == (right with { Failures = NoFailures });

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

    private static void VerifyExpected(IReadOnlyDictionary<string, WorkflowOutcome> outcomes)
    {
        var valid = outcomes["valid"];
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

        var validPen = outcomes["valid-pen"];
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

        var unknownSku = outcomes["unknown-sku"];
        AssertOutcome(
            "unknown-sku",
            unknownSku,
            WorkflowOutcome.Rejected("ORD-300", new WorkflowFailureView("pricing", "unknown-sku", string.Empty, "SKU-UNKNOWN|2")));

        var outOfStock = outcomes["out-of-stock"];
        AssertOutcome(
            "out-of-stock",
            outOfStock,
            WorkflowOutcome.Rejected("ORD-400", new WorkflowFailureView("inventory", "insufficient-stock", string.Empty, "SKU-BOOK|9")));

        var paymentDeclined = outcomes["payment-declined"];
        AssertOutcome(
            "payment-declined",
            paymentDeclined,
            WorkflowOutcome.Rejected("ORD-500", new WorkflowFailureView("payment", "payment-declined", string.Empty, "payment-declined")));

        var invalidOrderId = outcomes["invalid-order-id"];
        AssertOutcome(
            "invalid-order-id",
            invalidOrderId,
            WorkflowOutcome.Rejected(
                null,
                new WorkflowFailureView(
                    "request",
                    nameof(DecodeErrorKind.InvalidValue),
                    "orderId",
                    "order identifier must start with ORD-")));

        var fractional = outcomes["fractional-quantity"];
        AssertOutcome(
            "fractional-quantity",
            fractional,
            WorkflowOutcome.Rejected(
                null,
                new WorkflowFailureView(
                    "request",
                    nameof(DecodeErrorKind.Malformed),
                    "line.quantity",
                    "the value cannot be parsed as a 32-bit integer")));

        var multiple = outcomes["multiple-invalid"];
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
                        nameof(DecodeErrorKind.InvalidValue),
                        "orderId",
                        "order identifier must start with ORD-"),
                    new WorkflowFailureView("request", nameof(DecodeErrorKind.Null), "customerId", string.Empty),
                    new WorkflowFailureView("request", nameof(DecodeErrorKind.Empty), "line.sku", string.Empty),
                    new WorkflowFailureView(
                        "request",
                        nameof(DecodeErrorKind.InvalidValue),
                        "line.quantity",
                        "value must be between 1 and 100"),
                    new WorkflowFailureView("request", nameof(DecodeErrorKind.Empty), "payment.token", string.Empty),
                ],
                null,
                null,
                null,
                null));

        var rootArray = outcomes["root-array"];
        AssertOutcome(
            "root-array",
            rootArray,
            WorkflowOutcome.Rejected(null, new WorkflowFailureView("request", nameof(DecodeErrorKind.WrongKind), string.Empty, "found Array")));

        var emptyObject = outcomes["empty-object"];
        AssertOutcome(
            "empty-object",
            emptyObject,
            new WorkflowOutcome(
                "rejected",
                null,
                0,
                0m,
                [
                    new WorkflowFailureView("request", nameof(DecodeErrorKind.Missing), "orderId", string.Empty),
                    new WorkflowFailureView("request", nameof(DecodeErrorKind.Missing), "customerId", string.Empty),
                    new WorkflowFailureView("request", nameof(DecodeErrorKind.Missing), "line", string.Empty),
                    new WorkflowFailureView("request", nameof(DecodeErrorKind.Missing), "payment", string.Empty),
                ],
                null,
                null,
                null,
                null));

        var duplicate = outcomes["duplicate-order-id"];
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

        var extraField = outcomes["extra-field"];
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

        AssertOutcome(
            "valid:cancel",
            outcomes["valid:cancel"],
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
