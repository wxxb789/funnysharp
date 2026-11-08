namespace FunnySharp.TestSupport.JoinComparison;

internal static class JoinFixtures
{
    internal static IReadOnlyList<JoinFixture> All { get; } =
    [
        Empty(),
        UniqueSuccess(),
        RepeatedSourceKeys(),
        MissingMiddle(),
        EmptyLookup(),
        DuplicateMatched(),
        DuplicateUnused(),
        CaseInsensitiveDuplicate(),
        OrdinalContrast(),
        ThreeOccurrences(),
        CombinedFailures(),
        NullSourceKey(),
        EmptyKey(),
        NoOrders(),
    ];

    private static JoinFixture Empty() =>
        new(
            "empty",
            [],
            [],
            StringComparer.Ordinal,
            [],
            [],
            []);

    private static JoinFixture UniqueSuccess() =>
        new(
            "unique success",
            [
                new("O-1", "C-1"),
                new("O-2", "C-2"),
                new("O-3", "C-1"),
            ],
            [
                new("C-1", "Ada"),
                new("C-2", "Grace"),
            ],
            StringComparer.Ordinal,
            [
                new("O-1", "Ada"),
                new("O-2", "Grace"),
                new("O-3", "Ada"),
            ],
            [],
            [
                StreamOutcome.Success(new("O-1", "Ada")),
                StreamOutcome.Success(new("O-2", "Grace")),
                StreamOutcome.Success(new("O-3", "Ada")),
            ]);

    private static JoinFixture RepeatedSourceKeys() =>
        new(
            "repeated source keys",
            [
                new("O-1", "C-1"),
                new("O-2", "C-1"),
                new("O-3", "C-1"),
            ],
            [new("C-1", "Ada")],
            StringComparer.Ordinal,
            [
                new("O-1", "Ada"),
                new("O-2", "Ada"),
                new("O-3", "Ada"),
            ],
            [],
            [
                StreamOutcome.Success(new("O-1", "Ada")),
                StreamOutcome.Success(new("O-2", "Ada")),
                StreamOutcome.Success(new("O-3", "Ada")),
            ]);

    private static JoinFixture MissingMiddle() =>
        new(
            "missing middle row",
            [
                new("O-1", "C-1"),
                new("O-2", "C-9"),
                new("O-3", "C-3"),
            ],
            [
                new("C-1", "Ada"),
                new("C-2", "Grace"),
                new("C-3", "Linus"),
            ],
            StringComparer.Ordinal,
            [],
            [Missing("C-9", 1)],
            [
                StreamOutcome.Success(new("O-1", "Ada")),
                StreamOutcome.Failed(Missing("C-9", 1)),
                StreamOutcome.Success(new("O-3", "Linus")),
            ]);

    private static JoinFixture EmptyLookup() =>
        new(
            "empty lookup",
            [new("O-1", "C-1")],
            [],
            StringComparer.Ordinal,
            [],
            [Missing("C-1", 0)],
            [StreamOutcome.Failed(Missing("C-1", 0))]);

    private static JoinFixture DuplicateMatched() =>
        new(
            "duplicate matched",
            [
                new("O-1", "C-2"),
                new("O-2", "C-1"),
            ],
            [
                new("C-1", "Ada"),
                new("C-2", "Grace"),
                new("C-2", "Duplicate"),
            ],
            StringComparer.Ordinal,
            [],
            [Duplicate("C-2", firstLookupIndex: 1, lookupIndex: 2)],
            [
                StreamOutcome.Failed(Duplicate("C-2", firstLookupIndex: 1, lookupIndex: 2, sourceIndex: 0)),
                StreamOutcome.Success(new("O-2", "Ada")),
            ]);

    private static JoinFixture DuplicateUnused() =>
        new(
            "duplicate unused",
            [
                new("O-1", "C-1"),
                new("O-2", "C-9"),
            ],
            [
                new("C-1", "Ada"),
                new("C-2", "Grace"),
                new("C-2", "Unused duplicate"),
            ],
            StringComparer.Ordinal,
            [],
            [
                Duplicate("C-2", firstLookupIndex: 1, lookupIndex: 2),
                Missing("C-9", 1),
            ],
            [
                StreamOutcome.Success(new("O-1", "Ada")),
                StreamOutcome.Failed(Missing("C-9", 1)),
            ]);

    private static JoinFixture CaseInsensitiveDuplicate() =>
        new(
            "case-insensitive duplicate",
            [new("O-1", "c-1")],
            [
                new("C-1", "Ada"),
                new("c-1", "Case duplicate"),
            ],
            StringComparer.OrdinalIgnoreCase,
            [],
            [Duplicate("C-1", firstLookupIndex: 0, lookupIndex: 1)],
            [StreamOutcome.Failed(Duplicate("c-1", firstLookupIndex: 0, lookupIndex: 1, sourceIndex: 0))]);

    private static JoinFixture OrdinalContrast() =>
        new(
            "ordinal contrast",
            [new("O-1", "c-1")],
            [
                new("C-1", "Ada"),
                new("c-1", "Ordinal match"),
            ],
            StringComparer.Ordinal,
            [new("O-1", "Ordinal match")],
            [],
            [StreamOutcome.Success(new("O-1", "Ordinal match"))]);

    private static JoinFixture ThreeOccurrences() =>
        new(
            "three occurrences",
            [new("O-1", "C-2")],
            [
                new("C-1", "Ada"),
                new("C-2", "Grace"),
                new("C-2", "Second duplicate"),
                new("C-2", "Third duplicate"),
            ],
            StringComparer.Ordinal,
            [],
            [
                Duplicate("C-2", firstLookupIndex: 1, lookupIndex: 2),
                Duplicate("C-2", firstLookupIndex: 1, lookupIndex: 3),
            ],
            [
                StreamOutcome.Failed(Duplicate("C-2", firstLookupIndex: 1, lookupIndex: 2, sourceIndex: 0)),
            ]);

    private static JoinFixture CombinedFailures() =>
        new(
            "combined duplicate and missing",
            [
                new("O-1", "C-9"),
                new("O-2", "C-2"),
            ],
            [
                new("C-1", "Ada"),
                new("C-2", "Grace"),
                new("C-2", "Duplicate"),
            ],
            StringComparer.Ordinal,
            [],
            [
                Duplicate("C-2", firstLookupIndex: 1, lookupIndex: 2),
                Missing("C-9", 0),
            ],
            [
                StreamOutcome.Failed(Missing("C-9", 0)),
                StreamOutcome.Failed(Duplicate("C-2", firstLookupIndex: 1, lookupIndex: 2, sourceIndex: 1)),
            ]);

    private static JoinFixture NullSourceKey() =>
        new(
            "null source key",
            [
                new("O-1", null),
                new("O-2", "C-1"),
            ],
            [new("C-1", "Ada")],
            StringComparer.Ordinal,
            [],
            [NullSource(0)],
            [
                StreamOutcome.Failed(NullSource(0)),
                StreamOutcome.Success(new("O-2", "Ada")),
            ]);

    private static JoinFixture EmptyKey() =>
        new(
            "empty string key",
            [new("O-1", "")],
            [new("", "Empty key")],
            StringComparer.Ordinal,
            [new("O-1", "Empty key")],
            [],
            [StreamOutcome.Success(new("O-1", "Empty key"))]);

    private static JoinFixture NoOrders() =>
        new(
            "no orders",
            [],
            [new("C-1", "Ada")],
            StringComparer.Ordinal,
            [],
            [],
            []);

    private static JoinFailure Duplicate(
        string key,
        int firstLookupIndex,
        int lookupIndex,
        int? sourceIndex = null) =>
        new(LookupErrorKind.DuplicateKey, key, sourceIndex, lookupIndex, firstLookupIndex);

    private static JoinFailure Missing(string key, int sourceIndex) =>
        new(LookupErrorKind.MissingKey, key, sourceIndex, null, null);

    private static JoinFailure NullSource(int sourceIndex) =>
        new(LookupErrorKind.NullKey, null, sourceIndex, null, null);
}
