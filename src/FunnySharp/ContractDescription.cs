namespace FunnySharp;

/// <summary>Identifies the structural meaning of a <see cref="ContractDescription"/>.</summary>
public enum ContractDescriptionKind
{
    /// <summary>An unconstrained scalar value with a named shape.</summary>
    Scalar,

    /// <summary>A scalar value constrained to an inclusive range.</summary>
    Range,

    /// <summary>An object with named fields.</summary>
    Object,

    /// <summary>A value selected from labelled alternatives.</summary>
    OneOf,

    /// <summary>An array-like value with a declared cardinality.</summary>
    Collection,

    /// <summary>An absent-or-present value.</summary>
    Optional,

    /// <summary>A named recursive reference whose body is not expanded.</summary>
    Recursive,

    /// <summary>A structural base with an opaque refinement.</summary>
    Refined,

    /// <summary>An arbitrary caller rule with no derived structure.</summary>
    Opaque,

    /// <summary>A structural base projected to another value type.</summary>
    Derived,
}

/// <summary>
/// The structural metadata a <see cref="Contract{T}"/> exposes for explanation and schema use.
/// </summary>
/// <remarks>
/// A description is an immutable, inspectable value. It records fields, alternatives, ranges, and
/// cardinality once, so the same facts serve validation, decoding, generation, shrinking, and
/// documentation. A description is metadata, not proof: type shape alone never establishes a
/// business invariant. Opaque, refined, and recursive nodes are marked explicitly, and
/// <see cref="ContractCapabilities.HasCompleteSchema"/> reports whether a complete schema is
/// available. Use the static factories to build metadata for <c>WithSchema</c>.
/// </remarks>
public sealed class ContractDescription
{
    private static readonly IReadOnlyList<ContractFieldDescription> NoFields =
        Array.Empty<ContractFieldDescription>();

    private static readonly IReadOnlyList<ContractAlternativeDescription> NoAlternatives =
        Array.Empty<ContractAlternativeDescription>();

    private ContractDescription(
        ContractDescriptionKind kind,
        string? name,
        string? detail,
        ContractFieldDescription[]? fields,
        ContractAlternativeDescription[]? alternatives,
        ContractDescription? element,
        string? minimum,
        string? maximum,
        int? minimumLength,
        int? maximumLength,
        string? discriminator)
    {
        Kind = kind;
        Name = name;
        Detail = detail;
        Fields = fields is null ? NoFields : Array.AsReadOnly(fields);
        Alternatives = alternatives is null ? NoAlternatives : Array.AsReadOnly(alternatives);
        Element = element;
        Minimum = minimum;
        Maximum = maximum;
        MinimumLength = minimumLength;
        MaximumLength = maximumLength;
        Discriminator = discriminator;
    }

    /// <summary>Gets the structural kind.</summary>
    public ContractDescriptionKind Kind { get; }

    /// <summary>
    /// Gets the scalar or recursive name, or <see langword="null"/> for other kinds.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the human explanation carried by a refined, opaque, or derived node, or
    /// <see langword="null"/>.
    /// </summary>
    public string? Detail { get; }

    /// <summary>Gets the fields of an object description, in declaration order.</summary>
    public IReadOnlyList<ContractFieldDescription> Fields { get; }

    /// <summary>Gets the labelled alternatives of a one-of description, in declaration order.</summary>
    public IReadOnlyList<ContractAlternativeDescription> Alternatives { get; }

    /// <summary>
    /// Gets the nested description for a collection, optional, refined, or derived node, or
    /// <see langword="null"/>.
    /// </summary>
    public ContractDescription? Element { get; }

    /// <summary>Gets the inclusive lower bound text when <see cref="Kind"/> is <see cref="ContractDescriptionKind.Range"/>.</summary>
    public string? Minimum { get; }

    /// <summary>Gets the inclusive upper bound text when <see cref="Kind"/> is <see cref="ContractDescriptionKind.Range"/>.</summary>
    public string? Maximum { get; }

    /// <summary>Gets the declared minimum item count when <see cref="Kind"/> is <see cref="ContractDescriptionKind.Collection"/>.</summary>
    public int? MinimumLength { get; }

    /// <summary>Gets the declared maximum item count, or <see langword="null"/> for no declared maximum.</summary>
    public int? MaximumLength { get; }

    /// <summary>Gets the JSON property name that selects alternatives, when one is declared.</summary>
    public string? Discriminator { get; }

    /// <summary>Creates a named scalar description.</summary>
    /// <param name="name">The non-empty scalar shape name, such as <c>string</c> or <c>int32</c>.</param>
    /// <returns>A scalar description.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public static ContractDescription Scalar(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return new(ContractDescriptionKind.Scalar, name, null, null, null, null, null, null, null, null, null);
    }

    /// <summary>Creates an inclusive numeric range description.</summary>
    /// <param name="name">The non-empty scalar shape name.</param>
    /// <param name="minimum">The non-empty inclusive lower bound text.</param>
    /// <param name="maximum">The non-empty inclusive upper bound text.</param>
    /// <returns>A range description.</returns>
    /// <exception cref="ArgumentException">A name or bound is empty.</exception>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static ContractDescription Range(string name, string minimum, string maximum)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(minimum);
        ArgumentException.ThrowIfNullOrEmpty(maximum);
        return new(
            ContractDescriptionKind.Range,
            name,
            null,
            null,
            null,
            null,
            minimum,
            maximum,
            null,
            null,
            null);
    }

    /// <summary>Creates an object description from its fields.</summary>
    /// <param name="fields">The non-empty field list in declaration order.</param>
    /// <returns>An object description.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="fields"/> is empty.</exception>
    public static ContractDescription Object(IReadOnlyList<ContractFieldDescription> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var snapshot = new ContractFieldDescription[fields.Count];
        for (var index = 0; index < snapshot.Length; index++)
        {
            snapshot[index] = fields[index]
                ?? throw new ArgumentException("A field description must not be null.", nameof(fields));
        }

        if (snapshot.Length == 0)
        {
            throw new ArgumentException("An object description requires at least one field.", nameof(fields));
        }

        return new(
            ContractDescriptionKind.Object,
            null,
            null,
            snapshot,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    /// <summary>Creates a one-of description from labelled alternatives.</summary>
    /// <param name="discriminator">The optional JSON property name that selects an alternative.</param>
    /// <param name="alternatives">The non-empty alternatives in declaration order.</param>
    /// <returns>A one-of description.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="alternatives"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="alternatives"/> is empty or contains null.</exception>
    public static ContractDescription OneOf(
        string? discriminator,
        IReadOnlyList<ContractAlternativeDescription> alternatives)
    {
        ArgumentNullException.ThrowIfNull(alternatives);

        var snapshot = new ContractAlternativeDescription[alternatives.Count];
        for (var index = 0; index < snapshot.Length; index++)
        {
            snapshot[index] = alternatives[index]
                ?? throw new ArgumentException("An alternative description must not be null.", nameof(alternatives));
        }

        if (snapshot.Length == 0)
        {
            throw new ArgumentException("A one-of description requires at least one alternative.", nameof(alternatives));
        }

        return new(
            ContractDescriptionKind.OneOf,
            null,
            null,
            null,
            snapshot,
            null,
            null,
            null,
            null,
            null,
            discriminator);
    }

    /// <summary>Creates a collection description.</summary>
    /// <param name="element">The non-null element description.</param>
    /// <param name="minimumLength">The non-negative declared minimum item count.</param>
    /// <param name="maximumLength">The optional declared maximum item count.</param>
    /// <returns>A collection description.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The bounds are negative or inconsistent.</exception>
    public static ContractDescription Collection(
        ContractDescription element,
        int minimumLength,
        int? maximumLength)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
        if (maximumLength is { } maximum && maximum < minimumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumLength),
                "The maximum length must not be smaller than the minimum length.");
        }

        return new(
            ContractDescriptionKind.Collection,
            null,
            null,
            null,
            null,
            element,
            null,
            null,
            minimumLength,
            maximumLength,
            null);
    }

    /// <summary>Creates an optional-value description.</summary>
    /// <param name="element">The non-null present-value description.</param>
    /// <returns>An optional description.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    public static ContractDescription Optional(ContractDescription element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new(
            ContractDescriptionKind.Optional,
            null,
            null,
            null,
            null,
            element,
            null,
            null,
            null,
            null,
            null);
    }

    /// <summary>Creates a named recursive reference.</summary>
    /// <param name="name">The non-empty recursion name.</param>
    /// <returns>A recursive description whose body is not expanded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public static ContractDescription Recursive(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return new(
            ContractDescriptionKind.Recursive,
            name,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    /// <summary>Creates a refined description from a structural source and predicate detail.</summary>
    /// <param name="source">The non-null structural source description.</param>
    /// <param name="detail">The non-empty predicate explanation.</param>
    /// <returns>A refined description.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="detail"/> is empty.</exception>
    public static ContractDescription Refined(ContractDescription source, string detail)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrEmpty(detail);
        return new(
            ContractDescriptionKind.Refined,
            null,
            detail,
            null,
            null,
            source,
            null,
            null,
            null,
            null,
            null);
    }

    /// <summary>Creates an opaque description for an arbitrary caller rule.</summary>
    /// <param name="detail">The non-empty rule explanation.</param>
    /// <returns>An opaque description.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="detail"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="detail"/> is empty.</exception>
    public static ContractDescription Opaque(string detail)
    {
        ArgumentException.ThrowIfNullOrEmpty(detail);
        return new(
            ContractDescriptionKind.Opaque,
            null,
            detail,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    /// <summary>Creates a derived description from a structural source and projection detail.</summary>
    /// <param name="source">The non-null source description.</param>
    /// <param name="detail">The non-empty projection explanation.</param>
    /// <returns>A derived description.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="detail"/> is empty.</exception>
    public static ContractDescription Derived(ContractDescription source, string detail)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrEmpty(detail);
        return new(
            ContractDescriptionKind.Derived,
            null,
            detail,
            null,
            null,
            source,
            null,
            null,
            null,
            null,
            null);
    }

    /// <summary>Returns a compact deterministic one-line summary of this description.</summary>
    /// <returns>The summary text.</returns>
    public override string ToString() => RenderCompact();

    private string RenderCompact() =>
        Kind switch
        {
            ContractDescriptionKind.Scalar => Name!,
            ContractDescriptionKind.Range => $"{Name} in [{Minimum}..{Maximum}]",
            ContractDescriptionKind.Object =>
                "object { " + string.Join(", ", Fields.Select(static field => field.ToString())) + " }",
            ContractDescriptionKind.OneOf =>
                "one of " + string.Join(" | ", Alternatives.Select(static alternative => alternative.ToString())),
            ContractDescriptionKind.Collection =>
                $"collection of {Element} (min {MinimumLength}, max {MaximumLength?.ToString() ?? "unbounded"})",
            ContractDescriptionKind.Optional => $"optional {Element}",
            ContractDescriptionKind.Recursive => $"recursive {Name}",
            ContractDescriptionKind.Refined => $"refined {Element} ({Detail})",
            ContractDescriptionKind.Opaque => $"opaque rule ({Detail})",
            ContractDescriptionKind.Derived => $"derived from {Element} ({Detail})",
            _ => Kind.ToString(),
        };

    internal void AppendTree(System.Text.StringBuilder builder, int indent)
    {
        builder.Append(' ', indent);
        switch (Kind)
        {
            case ContractDescriptionKind.Scalar:
                builder.Append("scalar ").AppendLine(Name);
                break;
            case ContractDescriptionKind.Range:
                builder.Append("range ").Append(Name).Append(" [")
                    .Append(Minimum).Append("..").Append(Maximum).AppendLine("]");
                break;
            case ContractDescriptionKind.Object:
                builder.AppendLine("object");
                foreach (var field in Fields)
                {
                    builder.Append(' ', indent + 2).Append(field.Name).Append(": ")
                        .Append(field.Presence switch
                        {
                            ContractFieldPresence.Required => string.Empty,
                            ContractFieldPresence.MissingIsAbsence => "optional (missing is absence) ",
                            ContractFieldPresence.MissingOrNullIsAbsence => "optional (missing or null is absence) ",
                            _ => string.Empty,
                        });
                    builder.AppendLine(field.Contract.RenderCompact());
                }

                break;
            case ContractDescriptionKind.OneOf:
                builder.Append("one of");
                if (Discriminator is not null)
                {
                    builder.Append(" (discriminator ").Append(Discriminator).Append(')');
                }

                builder.AppendLine();
                foreach (var alternative in Alternatives)
                {
                    builder.Append(' ', indent + 2).Append(alternative.Label).Append(": ")
                        .AppendLine(alternative.Contract.RenderCompact());
                }

                break;
            case ContractDescriptionKind.Collection:
                builder.Append("collection of ")
                    .Append(Element!.RenderCompact())
                    .Append(" (min ").Append(MinimumLength)
                    .Append(", max ").Append(MaximumLength?.ToString() ?? "unbounded")
                    .AppendLine(")");
                break;
            case ContractDescriptionKind.Optional:
                builder.Append("optional ").AppendLine(Element!.RenderCompact());
                break;
            case ContractDescriptionKind.Recursive:
                builder.Append("recursive ").Append(Name).AppendLine(" (body not expanded)");
                break;
            case ContractDescriptionKind.Refined:
                builder.Append("refined ").Append(Element!.RenderCompact())
                    .Append(" (").Append(Detail).AppendLine(")");
                break;
            case ContractDescriptionKind.Opaque:
                builder.Append("opaque rule (").Append(Detail).AppendLine(")");
                break;
            case ContractDescriptionKind.Derived:
                builder.Append("derived from ").Append(Element!.RenderCompact())
                    .Append(" (").Append(Detail).AppendLine(")");
                break;
            default:
                builder.AppendLine(Kind.ToString());
                break;
        }
    }
}
