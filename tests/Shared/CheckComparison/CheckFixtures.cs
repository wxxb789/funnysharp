using FunnySharp;

namespace FunnySharp.TestSupport.Checks;

internal sealed record CheckFixture(
    string Name,
    CheckOrder Order,
    CheckError? ExistingError);

internal static class CheckFixtures
{
    internal static readonly CheckOrder ValidOrder =
        new("ORD-100", "CUS-100", "SKU-BOOK", 2, "tok-valid");

    internal static readonly CheckFixture Valid =
        new("valid", ValidOrder, null);

    internal static readonly CheckFixture OutOfStock =
        new("out-of-stock", ValidOrder with { Quantity = 9 }, null);

    internal static readonly CheckFixture PermissionDenied =
        new("permission-denied", ValidOrder with { CustomerId = "BLOCKED" }, null);

    internal static readonly CheckFixture FraudSuspected =
        new("fraud-suspected", ValidOrder with { Token = "tok-blocked" }, null);

    internal static readonly CheckFixture MultipleFailures =
        new(
            "multiple-failures",
            ValidOrder with { Quantity = 9, CustomerId = "BLOCKED", Token = "tok-blocked" },
            null);

    internal static readonly CheckFixture AlreadyFailed =
        new(
            "already-failed",
            ValidOrder,
            new CheckError("existing", "the order was already rejected"));

    internal static IReadOnlyList<CheckFixture> All { get; } =
    [
        Valid,
        OutOfStock,
        PermissionDenied,
        FraudSuspected,
        MultipleFailures,
        AlreadyFailed,
    ];
}
