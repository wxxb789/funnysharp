using FunnySharp;

namespace FunnySharp.TestSupport.NestedDataComparison;

internal static class NestedFixtures
{
    internal static IReadOnlyList<ImportFixture> Imports { get; } =
    [
        new(
            "valid import",
            """
            [
              {"orderId":"O-1","customer":"Ada","lines":[{"sku":"BOOK-1","quantity":2},{"sku":"PEN-2","quantity":1}]},
              {"orderId":"O-2","customer":"Grace","lines":[{"sku":"PAPER-1","quantity":3}]}
            ]
            """,
            new ImportReport(
                [
                    new ImportRow("O-1", "Ada", "BOOK-1", 2),
                    new ImportRow("O-1", "Ada", "PEN-2", 1),
                    new ImportRow("O-2", "Grace", "PAPER-1", 3),
                ],
                [],
                [],
                []),
            OrdinaryImportBehavior.RowsMatch),
        new(
            "rejected item",
            """
            [
              {"orderId":"O-1","customer":"Ada","lines":[{"sku":"BOOK-1","quantity":2}]},
              {"orderId":"O-2","customer":"Grace","lines":[]},
              {"orderId":"O-3","customer":"Linus","lines":[{"sku":"BAD-1","quantity":0}]},
              {"orderId":"O-4","customer":"Margaret","lines":[{"sku":"PAPER-1","quantity":3}]}
            ]
            """,
            new ImportReport(
                [
                    new ImportRow("O-1", "Ada", "BOOK-1", 2),
                    new ImportRow("O-4", "Margaret", "PAPER-1", 3),
                ],
                [new ImportRejection(2, 0, "O-3", "non-positive quantity")],
                [],
                []),
            OrdinaryImportBehavior.RowsDiffer),
        new(
            "empty lines",
            """
            [{"orderId":"O-1","customer":"Ada","lines":[]}]
            """,
            new ImportReport([], [], [], []),
            OrdinaryImportBehavior.RowsMatch),
        new(
            "missing quantity",
            """
            [{"orderId":"O-1","customer":"Ada","lines":[{"sku":"BOOK-1"}]}]
            """,
            new ImportReport(
                [],
                [],
                [new DecodeFailure(DecodeErrorKind.Missing, "[0].lines[0].quantity")],
                []),
            OrdinaryImportBehavior.RowsDiffer),
        new(
            "null quantity",
            """
            [{"orderId":"O-1","customer":"Ada","lines":[{"sku":"BOOK-1","quantity":null}]}]
            """,
            new ImportReport(
                [],
                [],
                [new DecodeFailure(DecodeErrorKind.Null, "[0].lines[0].quantity")],
                []),
            OrdinaryImportBehavior.ThrowsJson),
        new(
            "wrong quantity kind",
            """
            [{"orderId":"O-1","customer":"Ada","lines":[{"sku":"BOOK-1","quantity":"two"}]}]
            """,
            new ImportReport(
                [],
                [],
                [new DecodeFailure(DecodeErrorKind.WrongKind, "[0].lines[0].quantity")],
                []),
            OrdinaryImportBehavior.ThrowsJson),
        new(
            "malformed quantity",
            """
            [{"orderId":"O-1","customer":"Ada","lines":[{"sku":"BOOK-1","quantity":1.5}]}]
            """,
            new ImportReport(
                [],
                [],
                [new DecodeFailure(DecodeErrorKind.Malformed, "[0].lines[0].quantity")],
                []),
            OrdinaryImportBehavior.ThrowsJson),
        new(
            "null lines",
            """
            [{"orderId":"O-1","customer":"Ada","lines":null}]
            """,
            new ImportReport(
                [],
                [],
                [new DecodeFailure(DecodeErrorKind.Null, "[0].lines")],
                []),
            OrdinaryImportBehavior.ThrowsArgumentNull),
        new(
            "missing order id",
            """
            [{"customer":"Ada","lines":[]}]
            """,
            new ImportReport(
                [],
                [],
                [new DecodeFailure(DecodeErrorKind.Missing, "[0].orderId")],
                []),
            null),
        new(
            "empty customer",
            """
            [{"orderId":"O-1","customer":"","lines":[]}]
            """,
            new ImportReport(
                [],
                [],
                [new DecodeFailure(DecodeErrorKind.Empty, "[0].customer")],
                []),
            null),
        new(
            "multiple field failures",
            """
            [{"orderId":"O-1","customer":"Ada","lines":[{"sku":"","quantity":null},{"sku":"BOOK-1"}]}]
            """,
            new ImportReport(
                [],
                [],
                [
                    new DecodeFailure(DecodeErrorKind.Empty, "[0].lines[0].sku"),
                    new DecodeFailure(DecodeErrorKind.Null, "[0].lines[0].quantity"),
                    new DecodeFailure(DecodeErrorKind.Missing, "[0].lines[1].quantity"),
                ],
                []),
            OrdinaryImportBehavior.ThrowsJson),
    ];

    internal static IReadOnlyList<AlignedFixture> Aligned { get; } =
    [
        new(
            "aligned success",
            """
            [
              {"meterId":"M-1","timestamps":["t0","t1"],"values":[1.5,2.5]},
              {"meterId":"M-2","timestamps":["t2"],"values":[3.5]}
            ]
            """,
            new AlignedReport(
                [
                    new ReadingRow("M-1", "t0", 1.5),
                    new ReadingRow("M-1", "t1", 2.5),
                    new ReadingRow("M-2", "t2", 3.5),
                ],
                []),
            [
                new ReadingRow("M-1", "t0", 1.5),
                new ReadingRow("M-1", "t1", 2.5),
                new ReadingRow("M-2", "t2", 3.5),
            ]),
        new(
            "aligned mismatch",
            """
            [{"meterId":"M-1","timestamps":["t0"],"values":[1.5,2.5]}]
            """,
            new AlignedReport(
                [],
                [new ExpansionFailure(NestedRowErrorKind.ChildCountMismatch, 0, 1, 2)]),
            [new ReadingRow("M-1", "t0", 1.5)]),
        new(
            "aligned mismatch after a valid record",
            """
            [
              {"meterId":"M-1","timestamps":["t0"],"values":[1.5]},
              {"meterId":"M-2","timestamps":["t1","t2"],"values":[2.5]}
            ]
            """,
            new AlignedReport(
                [],
                [new ExpansionFailure(NestedRowErrorKind.ChildCountMismatch, 1, 2, 1)]),
            [
                new ReadingRow("M-1", "t0", 1.5),
                new ReadingRow("M-2", "t1", 2.5),
            ]),
        new(
            "aligned empty pair",
            """
            [{"meterId":"M-1","timestamps":[],"values":[]}]
            """,
            new AlignedReport([], []),
            []),
        new(
            "aligned empty versus nonempty",
            """
            [{"meterId":"M-1","timestamps":[],"values":[1.5]}]
            """,
            new AlignedReport(
                [],
                [new ExpansionFailure(NestedRowErrorKind.ChildCountMismatch, 0, 0, 1)]),
            []),
    ];

    internal static IReadOnlyList<CartesianFixture> Cartesian { get; } =
    [
        new(
            "cartesian product",
            """
            [{"productId":"P-1","sizes":["S","M"],"colors":["red","blue"]}]
            """,
            new CartesianReport(
                [
                    new CartesianRow("P-1", "S", "red"),
                    new CartesianRow("P-1", "S", "blue"),
                    new CartesianRow("P-1", "M", "red"),
                    new CartesianRow("P-1", "M", "blue"),
                ],
                [])),
        new(
            "cartesian unequal sides",
            """
            [{"productId":"P-1","sizes":["S","M"],"colors":["red"]}]
            """,
            new CartesianReport(
                [
                    new CartesianRow("P-1", "S", "red"),
                    new CartesianRow("P-1", "M", "red"),
                ],
                [])),
        new(
            "cartesian empty side",
            """
            [{"productId":"P-1","sizes":[],"colors":["red"]}]
            """,
            new CartesianReport([], [])),
    ];
}
