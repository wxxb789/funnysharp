using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FunnySharp.Tests;

/// <summary>
/// Curates the advanced functional patterns against the Goal 14 decision record
/// (<c>docs/next-stage/api-decisions.md</c>): the effect and resource boundary (AD-7), the
/// pure state-transition and commands-as-data model (AD-8), the optics surface (AD-9), the
/// async coordination family (AD-6), and the stability boundary itself (G10). These tests
/// freeze the curated surface so a rejected or deferred capability cannot return silently
/// through a new type, member, alias, or convenience API, and so every experimental member
/// stays tracked in <c>docs/stability-inventory.md</c>.
/// </summary>
public sealed class AdvancedPatternCurationTests
{
    private static readonly HashSet<string> DelegatePlumbingNames =
    [
        ".ctor",
        "Invoke",
        "BeginInvoke",
        "EndInvoke",
    ];

    private static readonly HashSet<string> CompilerPlumbingNames =
    [
        "value__",
        "GetType",
    ];

    /// <summary>
    /// The advanced-pattern types the Goal 14 decision matrix records under AD-6, AD-7, AD-8,
    /// and AD-9, mapped to the exact public member names each decision keeps. The member-level
    /// authority is <c>docs/next-stage/decision-matrix.csv</c>; this map freezes the curated
    /// name set of every advanced type in one place.
    /// </summary>
    private static readonly Dictionary<string, string[]> CuratedAdvancedMembers = new(StringComparer.Ordinal)
    {
        // AD-6: async, streaming, concurrency (completion-order coordination delivered by Goal 18).
        ["FunnySharp.ConcurrentEffectExtensions"] = ["FirstSuccessAsync"],
        ["FunnySharp.ParallelAsyncEnumerableExtensions"] =
            ["SelectParallelCompletionOrderValueAsync", "SelectParallelValueAsync"],
        ["FunnySharp.ParallelAsyncSequenceExtensions"] = ["TraverseParallelValueAsync"],

        // AD-7: effects, resources, environment.
        ["FunnySharp.Effect"] = ["FromResult", "FromSync", "FromTask", "FromValue", "FromValueTask"],
        ["FunnySharp.EffectResourceExtensions"] = ["Using", "UsingAsync"],
        ["FunnySharp.Effect`1"] =
            ["Bind", "Map", "RunAsync", "Select", "SelectMany", "WithEnvironment"],
        ["FunnySharp.Effect`2"] =
            ["Bind", "Map", "Provide", "RunAsync", "Select", "SelectMany"],

        // AD-8: state transitions and machines (the Then redesign target is owned by Goal 20).
        ["FunnySharp.StateChange`2"] = ["Equals", "GetHashCode", "Outputs", "State", "To", "ToString"],
        ["FunnySharp.StateTransition`2"] = [],
        ["FunnySharp.StateTransitionExtensions"] = ["Then"],
        ["FunnySharp.TransitionResult`3"] =
        [
            "Applied", "Equals", "Failed", "GetHashCode", "IsApplied", "IsFailed",
            "IsRejected", "IsUndefined", "Match", "Rejected", "Status", "ToString",
            "TryGetChange", "TryGetError", "Undefined",
        ],
        ["FunnySharp.TransitionStatus"] = ["Applied", "Failed", "Rejected", "Undefined"],
        ["FunnySharp.StateMachine`4"] = [],
        ["FunnySharp.StateMachineExtensions"] = ["OrElse", "Replay"],

        // AD-9: optics.
        ["FunnySharp.Lens"] = ["Create", "Identity"],
        ["FunnySharp.Lens`2"] = ["Compose", "Get", "Set", "Update"],
        ["FunnySharp.Optional"] = ["Create"],
        ["FunnySharp.Optional`2"] = ["Compose", "GetOption", "Set", "Update"],
    };

    /// <summary>
    /// Rejected or deferred capability vocabulary that must never reappear as a public type
    /// name, mapped to the decision that rejected or deferred it. Exact names only: legitimate
    /// FunnySharp names such as <c>StateChange</c> or <c>TryOperation</c> must keep compiling.
    /// </summary>
    private static readonly (string TypeName, string Decision)[] ForbiddenTypeNames =
    [
        // Product contract: no second carrier for a recorded meaning.
        ("Maybe", "product contract: canonical vocabulary"),
        ("Either", "product contract: canonical vocabulary"),
        ("Fin", "product contract: canonical vocabulary"),
        ("Unit", "decision G17: public Unit type rejected"),
        ("Prelude", "AD-4: operator/free-function model rejected"),
        ("IO", "AD-7: Eff/Aff/IO monads rejected"),
        ("Eff", "AD-7: Eff/Aff/IO monads rejected"),
        ("Aff", "AD-7: Eff/Aff/IO monads rejected"),

        // AD-7: no Reader delegates, runtime DI traits, or effect failure carriers.
        ("Reader", "AD-7: Reader delegates rejected"),
        ("ReaderT", "AD-7: monad-transformer stack rejected"),
        ("Has", "AD-7: runtime DI traits rejected"),
        ("Trait", "AD-7: runtime DI traits rejected"),

        // AD-8: no State monad family, actor runtime, STM, or workflow engine.
        ("State", "AD-8: State/StateT monads rejected"),
        ("StateT", "AD-8: State/StateT monads rejected"),
        ("Writer", "AD-8: WriterT stack rejected"),
        ("WriterT", "AD-8: WriterT stack rejected"),
        ("RWS", "AD-8: RWS stack rejected"),
        ("Mailbox", "AD-8: actor runtimes rejected"),
        ("MailboxProcessor", "AD-8: actor runtimes rejected"),
        ("Actor", "AD-8: actor runtimes rejected"),
        ("Workflow", "AD-8: workflow engines rejected"),

        // AD-9: no optics hierarchy beyond Lens and Optional.
        ("Prism", "AD-9: prism hierarchy rejected"),
        ("Iso", "AD-9: iso hierarchy rejected"),
        ("Getter", "AD-9: getter/setter hierarchy rejected"),
        ("Setter", "AD-9: getter/setter hierarchy rejected"),
        ("Traversal", "AD-9: traversal hierarchy rejected"),
        ("Fold", "AD-9: fold hierarchy rejected"),

        // AD-5: no custom sequence carriers.
        ("Seq", "AD-5: custom sequence carriers rejected"),
        ("Lst", "AD-5: custom collection carriers rejected"),
        ("Map", "AD-5: custom collection carriers rejected"),
        ("HashMap", "AD-5: custom collection carriers rejected"),

        // AD-6 deferred until a concrete consumer exists.
        ("Memoize", "AD-6: Memoize carriers deferred"),
        ("IAsyncBuffer", "AD-6: async-buffer carriers deferred"),
        ("AsyncBuffer", "AD-6: async-buffer carriers deferred"),

        // G8: a general retry, backoff, or scheduling layer is rejected for this stage.
        ("Retry", "decision G8: general retry layer rejected"),
        ("RetryPolicy", "decision G8: general retry layer rejected"),
        ("Backoff", "decision G8: general retry layer rejected"),
        ("Schedule", "decision G8: scheduling policy layer rejected"),
        ("Scheduler", "product contract: no custom scheduler"),
    ];

    /// <summary>
    /// Member-name fragments that would smuggle a rejected or deferred capability back as a
    /// convenience API. Fragments are matched case-sensitively as substrings of the public
    /// member name.
    /// </summary>
    private static readonly (string Fragment, string Decision)[] ForbiddenMemberNameFragments =
    [
        ("Retry", "decision G8: general retry layer rejected"),
        ("Backoff", "decision G8: general retry layer rejected"),
        ("Memoize", "AD-6: Memoize carriers deferred"),
        ("Prism", "AD-9: prism hierarchy rejected"),
        ("Iso", "AD-9: iso hierarchy rejected"),
        ("Getter", "AD-9: getter/setter hierarchy rejected"),
        ("Setter", "AD-9: getter/setter hierarchy rejected"),
        ("Traversal", "AD-9: traversal hierarchy rejected"),
        ("Fold", "AD-9: fold hierarchy rejected"),
        ("AsyncBuffer", "AD-6: async-buffer carriers deferred"),
        ("Mailbox", "AD-8: actor runtimes rejected"),
        ("Schedule", "decision G8: scheduling policy layer rejected"),
    ];

    private static readonly string[] ForbiddenMemberNames =
    [
        // AD-6: no naked started-Task racing API, unbounded fan-out, or Fork.
        "Fork",
        "Race",
        "RaceAsync",
        "WhenAny",

        // AD-5: rejected collection conveniences that duplicate Choose or the BCL.
        "WhereSelect",
        "BindZip",
        "Cycle",
        "Shuffle",
        "Chunk",
        "Materialize",
        "JoinToString",
    ];

    /// <summary>
    /// The carrier types that must never grow a <c>Where</c> member: a bare predicate cannot
    /// produce absence or an error (canonical vocabulary rule 2).
    /// </summary>
    private static readonly string[] CarrierTypesWithoutWhere =
    [
        "FunnySharp.Option`1",
        "FunnySharp.Result`2",
        "FunnySharp.UnitResult`1",
        "FunnySharp.Validation`2",
        "FunnySharp.Effect`1",
        "FunnySharp.Effect`2",
        "FunnySharp.TransitionResult`3",
    ];

    /// <summary>
    /// The complete experimental surface, tracked in <c>docs/stability-inventory.md</c> as the
    /// FS0017 location-context family delivered by Goal 17 (decision G6: adopt experimental
    /// first). Every entry must carry <c>[Experimental("FS0017")]</c>, and no other member may
    /// be experimental, so experimental status stays unmistakable and tracked.
    /// </summary>
    private static readonly (string TypeName, string MemberName, int OverloadCount)[] TrackedExperimentalMembers =
    [
        ("FunnySharp.Location", "*", 1),
        ("FunnySharp.SequenceExtensions", "Traverse", 8),
        ("FunnySharp.AsyncSequenceExtensions", "TraverseAsync", 4),
        ("FunnySharp.AsyncSequenceExtensions", "TraverseValueAsync", 4),
    ];

    [Fact]
    public void AdvancedPatternSurfaceMatchesTheCuratedDecisionMatrix()
    {
        var assembly = typeof(Effect).Assembly;
        var publicTypes = assembly.GetTypes()
            .Where(type => type.IsPublic)
            .ToDictionary(type => type.FullName!, StringComparer.Ordinal);

        var missingTypes = CuratedAdvancedMembers.Keys
            .Where(name => !publicTypes.ContainsKey(name))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(missingTypes);

        var mismatches = new List<string>();
        foreach (var (typeName, expectedMembers) in CuratedAdvancedMembers)
        {
            var actualMembers = PublicDeclaredMemberNames(publicTypes[typeName])
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
            var expected = expectedMembers.Order(StringComparer.Ordinal).ToArray();
            if (!actualMembers.SequenceEqual(expected, StringComparer.Ordinal))
            {
                var added = actualMembers.Except(expected, StringComparer.Ordinal).ToArray();
                var removed = expected.Except(actualMembers, StringComparer.Ordinal).ToArray();
                mismatches.Add(
                    typeName + ": added [" + string.Join(", ", added) + "], removed [" +
                    string.Join(", ", removed) + "]");
            }
        }

        Assert.True(
            mismatches.Count == 0,
            "The advanced-pattern surface drifted from the Goal 14 decision matrix:\n" +
            string.Join('\n', mismatches));
    }

    [Fact]
    public void RejectedAndDeferredCapabilitiesAreAbsentFromThePublicSurface()
    {
        var assembly = typeof(Effect).Assembly;
        var violations = new List<string>();

        foreach (var type in assembly.GetTypes().Where(type => type.IsPublic))
        {
            var typeLeafName = type.Name.Split('`')[0];
            foreach (var (forbiddenName, decision) in ForbiddenTypeNames)
            {
                if (string.Equals(typeLeafName, forbiddenName, StringComparison.Ordinal))
                {
                    violations.Add(type.FullName + " reintroduces " + forbiddenName + " (" + decision + ").");
                }
            }

            foreach (var memberName in PublicDeclaredMemberNames(type))
            {
                foreach (var (fragment, decision) in ForbiddenMemberNameFragments)
                {
                    if (memberName.Contains(fragment, StringComparison.Ordinal))
                    {
                        violations.Add(type.FullName + "." + memberName + " matches '" + fragment + "' (" + decision + ").");
                    }
                }

                foreach (var forbidden in ForbiddenMemberNames)
                {
                    if (string.Equals(memberName, forbidden, StringComparison.Ordinal))
                    {
                        violations.Add(type.FullName + "." + memberName + " is a rejected member name.");
                    }
                }
            }

            // AD-1/AD-2: no implicit or explicit conversion operators on the semantic
            // carriers. Conversion operators are compiler-shaped (specialName), so the
            // member scan above never sees them; scan them directly instead.
            foreach (var method in type.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName && method.Name is "op_Implicit" or "op_Explicit")
                {
                    violations.Add(type.FullName + "." + method.Name + " is a rejected member name.");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Rejected or deferred capabilities must not be smuggled back through aliases or convenience APIs:\n" +
            string.Join('\n', violations));
    }

    [Fact]
    public void CarrierTypesExposeNoWhereMember()
    {
        var assembly = typeof(Effect).Assembly;
        var offenders = new List<string>();

        foreach (var typeName in CarrierTypesWithoutWhere)
        {
            var type = assembly.GetType(typeName);
            Assert.NotNull(type);
            if (PublicDeclaredMemberNames(type!).Contains("Where"))
            {
                offenders.Add(typeName);
            }
        }

        Assert.True(
            offenders.Count == 0,
            "A bare Where predicate cannot produce absence or an error (canonical vocabulary rule 2): " +
            string.Join(", ", offenders));
    }

    [Fact]
    public void EveryExperimentalMemberCarriesTheTrackedStabilityDiagnostic()
    {
        var actual = ExperimentalMembers()
            .GroupBy(entry => (entry.TypeName, entry.MemberName, entry.DiagnosticId))
            .Select(group => (group.Key.TypeName, group.Key.MemberName, group.Key.DiagnosticId, Count: group.Count()))
            .OrderBy(entry => entry.TypeName, StringComparer.Ordinal)
            .ThenBy(entry => entry.MemberName, StringComparer.Ordinal)
            .ToArray();

        var expected = TrackedExperimentalMembers
            .Select(entry => (entry.TypeName, entry.MemberName, "FS0017", entry.OverloadCount))
            .OrderBy(entry => entry.TypeName, StringComparer.Ordinal)
            .ThenBy(entry => entry.MemberName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StabilityInventoryTracksExactlyTheExperimentalSurface()
    {
        var inventoryPath = FindRepositoryFile(Path.Combine("docs", "stability-inventory.md"));
        Assert.True(inventoryPath is not null, "docs/stability-inventory.md must exist in the repository.");

        var experimentalRows = File.ReadAllLines(inventoryPath!)
            .Select(line => line.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Where(cells => cells.Length > 1 &&
                cells[0].Length == 6 &&
                cells[0].StartsWith("FS", StringComparison.Ordinal) &&
                char.IsAsciiDigit(cells[0][2]))
            .ToArray();

        var trackedDiagnostics = experimentalRows
            .Select(cells => cells[0])
            .ToHashSet(StringComparer.Ordinal);

        var experimentalDiagnostics = ExperimentalMembers()
            .Select(entry => entry.DiagnosticId)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(experimentalDiagnostics, trackedDiagnostics);
        Assert.Contains("FS0017", trackedDiagnostics);

        // The inventory header promises every experimental member "an entry in this
        // inventory": every tracked member needs a row that mentions its type and, for
        // member-level entries, its member name. Mentions rather than exact row counts,
        // because the located list and dictionary overloads occupy separate rows while
        // the async overloads share one.
        var missingEntries = TrackedExperimentalMembers
            .Where(entry => !experimentalRows.Any(cells =>
                cells[1].Contains("`" + LeafName(entry.TypeName), StringComparison.Ordinal) &&
                (entry.MemberName == "*" ||
                    cells[1].Contains(entry.MemberName, StringComparison.Ordinal))))
            .Select(entry => entry.TypeName + "." + entry.MemberName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missingEntries.Length == 0,
            "docs/stability-inventory.md must track every experimental member:\n" +
            string.Join('\n', missingEntries));
    }

    private static IEnumerable<string> PublicDeclaredMemberNames(Type type)
    {
        var members = type.GetMembers(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var isDelegate = typeof(MulticastDelegate).IsAssignableFrom(type);
        foreach (var member in members)
        {
            // Property accessors and operators are compiler-shaped (specialName); the property
            // itself is already represented by its PropertyInfo name.
            if (member is MethodInfo method && method.IsSpecialName)
            {
                continue;
            }

            if (CompilerPlumbingNames.Contains(member.Name) ||
                member.Name.StartsWith("<", StringComparison.Ordinal) ||
                (isDelegate && DelegatePlumbingNames.Contains(member.Name)))
            {
                continue;
            }

            yield return member.Name;
        }
    }

    /// <summary>
    /// Every public experimental member in the library assembly, as a
    /// (type name, member name, diagnostic id) triple: the type itself is reported with the
    /// member name "*", a marked member with its declared name.
    /// </summary>
    private static IEnumerable<(string TypeName, string MemberName, string DiagnosticId)> ExperimentalMembers()
    {
        foreach (var type in typeof(Effect).Assembly.GetTypes().Where(type => type.IsPublic))
        {
            var typeAttribute = type.GetCustomAttribute<ExperimentalAttribute>();
            if (typeAttribute is not null)
            {
                yield return (type.FullName!, "*", typeAttribute.DiagnosticId);
            }

            foreach (var member in type.GetMembers(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var memberAttribute = member.GetCustomAttribute<ExperimentalAttribute>();
                if (memberAttribute is not null)
                {
                    yield return (type.FullName!, member.Name, memberAttribute.DiagnosticId);
                }
            }
        }
    }

    /// <summary>
    /// The leaf name of a full type name without the generic arity marker: the name the
    /// stability inventory's member column mentions, as in
    /// <c>FunnySharp.AsyncSequenceExtensions</c> -> <c>AsyncSequenceExtensions</c>.
    /// </summary>
    private static string LeafName(string typeName) => typeName.Split('`')[0].Split('.')[^1];

    private static string? FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FunnySharp.slnx")))
            {
                var candidate = Path.Combine(directory.FullName, relativePath);
                return File.Exists(candidate) ? candidate : null;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
