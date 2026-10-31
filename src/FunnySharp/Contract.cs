#pragma warning disable FS0017

using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace FunnySharp;

using BclDateTime = System.DateTime;
using BclDateTimeOffset = System.DateTimeOffset;
using BclGuid = System.Guid;
using DiagnosticLocation = Location;

/// <summary>
/// Provides factories and structural combinators for <see cref="Contract{T}"/>.
/// </summary>
/// <remarks>
/// A contract describes supported data constraints once. The same description drives validation,
/// decoding through <see cref="Decoder{T}"/>, structural explanation, schema metadata, generated
/// test inputs, and shrinking. Primitive and structural contracts support every interpretation
/// they can honestly derive. Arbitrary predicates and transformations remain available through
/// <see cref="From{T}"/> and the instance refinement methods; their missing derivations are
/// explicit <see cref="ContractLimitation"/> values and can be supplied by the caller.
/// </remarks>
public static class Contract
{
    private const string GeneratedAlphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

    /// <summary>Gets a contract for any string, including the empty string.</summary>
    public static Contract<string> String { get; } =
        LeafNode<string>.Unconstrained(
            "string",
            Decoder.String,
            static context => GenerateString(context, 0),
            static value => ShrinkString(value, 0));

    /// <summary>Gets a contract for a string that must not be empty.</summary>
    public static Contract<string> NonEmptyString { get; } =
        LeafNode<string>.Constrained(
            "non-empty string",
            Decoder.String,
            static value =>
                value.Length == 0
                    ? ContractSupport.Invalid<string>(
                        DecodeError.At(
                            DiagnosticLocation.Root,
                            DecodeErrorKind.Empty,
                            "a non-empty string"))
                    : ContractSupport.Valid(value),
            static context => GenerateString(context, 1),
            static value => ShrinkString(value, 1));

    /// <summary>Gets a contract for a Boolean value.</summary>
    public static Contract<bool> Boolean { get; } =
        LeafNode<bool>.Unconstrained(
            "boolean",
            Decoder.Boolean,
            static context => Result<bool, string>.Success(context.Random.Next(2) == 1),
            static value => value ? [false] : []);

    /// <summary>Gets a contract for any 32-bit integer.</summary>
    public static Contract<int> Int32 { get; } =
        LeafNode<int>.Unconstrained(
            "int32",
            Decoder.Int32,
            static context => Result<int, string>.Success(context.Random.Next()),
            static value => ShrinkNumber(value, 0));

    /// <summary>Gets a contract for any 64-bit integer.</summary>
    public static Contract<long> Int64 { get; } =
        LeafNode<long>.Unconstrained(
            "int64",
            Decoder.Int64,
            static context => Result<long, string>.Success(context.Random.NextInt64()),
            static value => ShrinkNumber(value, 0L));

    /// <summary>Gets a contract for a finite decimal number.</summary>
    public static Contract<decimal> Decimal { get; } =
        LeafNode<decimal>.Unconstrained(
            "decimal",
            Decoder.Decimal,
            static context => Result<decimal, string>.Success(
                decimal.Round((decimal)context.Random.NextDouble() * 2000m - 1000m, 2)),
            static value => ShrinkNumber(value, 0m));

    /// <summary>Gets a contract for a finite floating-point number.</summary>
    public static Contract<double> Double { get; } =
        LeafNode<double>.Constrained(
            "double",
            Decoder.Double,
            static value =>
                double.IsFinite(value)
                    ? ContractSupport.Valid(value)
                    : ContractSupport.Invalid<double>(
                        LeafNode<double>.InvalidValue("value must be finite")),
            static context => Result<double, string>.Success(
                (context.Random.NextDouble() * 2000d) - 1000d),
            static value => ShrinkNumber(value, 0d));

    /// <summary>Gets a contract for a GUID.</summary>
    public static Contract<BclGuid> Guid { get; } =
        LeafNode<BclGuid>.Unconstrained(
            "guid",
            Decoder.Guid,
            static context =>
            {
                var bytes = new byte[16];
                context.Random.NextBytes(bytes);
                return Result<BclGuid, string>.Success(new BclGuid(bytes));
            },
            static value => value == BclGuid.Empty ? [] : [BclGuid.Empty]);

    /// <summary>Gets a contract for an ISO 8601 date-time value.</summary>
    public static Contract<BclDateTime> DateTime { get; } =
        LeafNode<BclDateTime>.Unconstrained(
            "date-time",
            Decoder.DateTime,
            static context => Result<BclDateTime, string>.Success(
                BclDateTime.UnixEpoch.AddSeconds(context.Random.NextInt64(0, 4_102_444_800L))),
            static value => value == BclDateTime.UnixEpoch ? [] : [BclDateTime.UnixEpoch]);

    /// <summary>Gets a contract for an ISO 8601 date-time-offset value.</summary>
    public static Contract<BclDateTimeOffset> DateTimeOffset { get; } =
        LeafNode<BclDateTimeOffset>.Unconstrained(
            "date-time-offset",
            Decoder.DateTimeOffset,
            static context => Result<BclDateTimeOffset, string>.Success(
                BclDateTimeOffset.UnixEpoch.AddSeconds(context.Random.NextInt64(0, 4_102_444_800L))),
            static value =>
                value == BclDateTimeOffset.UnixEpoch ? [] : [BclDateTimeOffset.UnixEpoch]);

    /// <summary>Creates a contract for a 32-bit integer in an inclusive range.</summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    /// <returns>A range contract.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimum"/> is greater than <paramref name="maximum"/>.</exception>
    public static Contract<int> Int32Range(int minimum, int maximum)
    {
        ValidateRange(minimum, maximum);

        return LeafNode<int>.Constrained(
            "int32",
            Decoder.Int32,
            value => ValidateInclusive(value, minimum, maximum),
            context => Result<int, string>.Success(
                minimum == maximum
                    ? minimum
                    : (int)context.Random.NextInt64(minimum, (long)maximum + 1)),
            value => ShrinkNumber(value, minimum),
            ContractDescription.Range(
                "int32",
                minimum.ToString(CultureInfo.InvariantCulture),
                maximum.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Creates a contract for a 64-bit integer in an inclusive range.</summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    /// <returns>A range contract.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimum"/> is greater than <paramref name="maximum"/>.</exception>
    public static Contract<long> Int64Range(long minimum, long maximum)
    {
        ValidateRange(minimum, maximum);

        return LeafNode<long>.Constrained(
            "int64",
            Decoder.Int64,
            value => ValidateInclusive(value, minimum, maximum),
            context => Result<long, string>.Success(GenerateInt64(context, minimum, maximum)),
            value => ShrinkNumber(value, minimum),
            ContractDescription.Range(
                "int64",
                minimum.ToString(CultureInfo.InvariantCulture),
                maximum.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Creates a contract for a decimal in an inclusive range.</summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    /// <returns>A range contract.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimum"/> is greater than <paramref name="maximum"/>.</exception>
    public static Contract<decimal> DecimalRange(decimal minimum, decimal maximum)
    {
        ValidateRange(minimum, maximum);

        return LeafNode<decimal>.Constrained(
            "decimal",
            Decoder.Decimal,
            value => ValidateInclusive(value, minimum, maximum),
            context => Result<decimal, string>.Success(GenerateDecimal(context, minimum, maximum)),
            value => ShrinkNumber(value, minimum),
            ContractDescription.Range(
                "decimal",
                minimum.ToString(CultureInfo.InvariantCulture),
                maximum.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Creates a contract for a finite double in an inclusive range.</summary>
    /// <param name="minimum">The inclusive lower bound; must be finite.</param>
    /// <param name="maximum">The inclusive upper bound; must be finite.</param>
    /// <returns>A range contract.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="minimum"/> is greater than <paramref name="maximum"/>, or a bound is not finite.
    /// </exception>
    public static Contract<double> DoubleRange(double minimum, double maximum)
    {
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum))
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimum),
                "Range bounds must be finite.");
        }

        ValidateRange(minimum, maximum);

        return LeafNode<double>.Constrained(
            "double",
            Decoder.Double,
            value => ValidateInclusive(value, minimum, maximum),
            context => Result<double, string>.Success(GenerateDouble(context, minimum, maximum)),
            value => ShrinkNumber(value, minimum),
            ContractDescription.Range(
                "double",
                minimum.ToString("R", CultureInfo.InvariantCulture),
                maximum.ToString("R", CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Creates an opaque contract from a caller-supplied description and accumulating validation.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="description">The non-empty explanation of the opaque rule.</param>
    /// <param name="validate">
    /// The caller rule. It returns the value or one or more violation messages. The value carried
    /// by a successful validation is ignored: contract validation checks an existing value.
    /// </param>
    /// <returns>
    /// A contract that supports validation. Decoding, generation, shrinking, and complete schema
    /// metadata are reported as unavailable until supplied with the matching <c>With...</c> method.
    /// </returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="description"/> is empty.</exception>
    public static Contract<T> From<T>(
        string description,
        Func<T, Validation<T, string>> validate)
    {
        ArgumentException.ThrowIfNullOrEmpty(description);
        ArgumentNullException.ThrowIfNull(validate);

        return new Contract<T>(new OpaqueNode<T>(description, validate));
    }

    /// <summary>Creates a contract for an absent-or-present value.</summary>
    /// <typeparam name="T">The present-value type.</typeparam>
    /// <param name="contract">The non-null present-value contract.</param>
    /// <returns>A contract whose absent value and present values both satisfy the contract.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="contract"/> is <see langword="null"/>.</exception>
    public static Contract<Option<T>> Optional<T>(Contract<T> contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        return new Contract<Option<T>>(new OptionalNode<T>(contract));
    }

    /// <summary>Creates a contract for a sequence with a declared cardinality.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The non-null element contract.</param>
    /// <param name="minimumLength">The non-negative minimum item count.</param>
    /// <param name="maximumLength">The optional maximum item count.</param>
    /// <returns>A collection contract.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The bounds are negative or inconsistent.</exception>
    public static Contract<IReadOnlyList<T>> List<T>(
        Contract<T> element,
        int minimumLength = 0,
        int? maximumLength = null)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
        if (maximumLength is { } maximum && maximum < minimumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumLength),
                "The maximum length must not be smaller than the minimum length.");
        }

        return new Contract<IReadOnlyList<T>>(
            new CollectionNode<T>(element, minimumLength, maximumLength));
    }

    /// <summary>Creates a contract for a sequence with at least one element.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The non-null element contract.</param>
    /// <returns>A collection contract with a minimum length of one and no declared maximum.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    public static Contract<IReadOnlyList<T>> NonEmptyList<T>(Contract<T> element) =>
        List(element, 1);

    /// <summary>Creates a parent-typed field set for concise object accessors.</summary>
    /// <typeparam name="TParent">The object type that owns the fields.</typeparam>
    /// <returns>A stateless field set whose methods fix the parent type.</returns>
    public static ContractFieldSet<TParent> For<TParent>() => new();

    /// <summary>Binds a required object field to a member contract and an accessor.</summary>
    /// <typeparam name="TParent">The object type that owns the field.</typeparam>
    /// <typeparam name="T">The member value type.</typeparam>
    /// <param name="name">The non-empty case-sensitive property name.</param>
    /// <param name="contract">The non-null member contract.</param>
    /// <param name="getter">The accessor used to validate, shrink, and describe an existing object.</param>
    /// <returns>The field binding, reused by every object interpretation.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public static ContractField<TParent, T> Field<TParent, T>(
        string name,
        Contract<T> contract,
        Func<TParent, T> getter)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(getter);

        return new ContractField<TParent, T>(
            name,
            contract,
            ContractFieldPresence.Required,
            getter,
            () => contract.CreateDecoder().Map(
                decoder => (Decoder<T>)Decoder.Field(name, decoder)));
    }

    /// <summary>
    /// Binds an optional object field where a missing property means absence.
    /// </summary>
    /// <typeparam name="TParent">The object type that owns the field.</typeparam>
    /// <typeparam name="T">The present-value type.</typeparam>
    /// <param name="name">The non-empty case-sensitive property name.</param>
    /// <param name="contract">The non-null contract for a present value.</param>
    /// <param name="getter">The accessor used to validate, shrink, and describe an existing object.</param>
    /// <returns>The field binding. An explicit JSON null is not absence and normally becomes a null error.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public static ContractField<TParent, Option<T>> OptionalField<TParent, T>(
        string name,
        Contract<T> contract,
        Func<TParent, Option<T>> getter)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(getter);

        var member = Optional(contract);
        return new ContractField<TParent, Option<T>>(
            name,
            member,
            ContractFieldPresence.MissingIsAbsence,
            getter,
            () => contract.CreateDecoder().Map(
                decoder => (Decoder<Option<T>>)Decoder.OptionalField(name, decoder)));
    }

    /// <summary>
    /// Binds an optional object field where both a missing property and an explicit null mean
    /// absence.
    /// </summary>
    /// <typeparam name="TParent">The object type that owns the field.</typeparam>
    /// <typeparam name="T">The present-value type.</typeparam>
    /// <param name="name">The non-empty case-sensitive property name.</param>
    /// <param name="contract">The non-null contract for a present non-null value.</param>
    /// <param name="getter">The accessor used to validate, shrink, and describe an existing object.</param>
    /// <returns>The field binding.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public static ContractField<TParent, Option<T>> NullableField<TParent, T>(
        string name,
        Contract<T> contract,
        Func<TParent, Option<T>> getter)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(getter);

        var member = Optional(contract);
        return new ContractField<TParent, Option<T>>(
            name,
            member,
            ContractFieldPresence.MissingOrNullIsAbsence,
            getter,
            () => contract.CreateDecoder().Map(
                decoder => (Decoder<Option<T>>)Decoder.NullableField(name, decoder)));
    }

    /// <summary>Creates an object contract with one independent field.</summary>
    /// <typeparam name="T1">The field value type.</typeparam>
    /// <typeparam name="TResult">The constructed object type.</typeparam>
    /// <param name="first">The non-null field binding.</param>
    /// <param name="combine">The non-null constructor invoked after every field is valid.</param>
    /// <returns>An object contract.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static Contract<TResult> Object<T1, TResult>(
        ContractField<TResult, T1> first,
        Func<T1, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(combine);

        return new Contract<TResult>(new ProductNode1<TResult, T1>(first, combine));
    }

    /// <summary>Creates an object contract with two independent fields.</summary>
    /// <typeparam name="T1">The first field value type.</typeparam>
    /// <typeparam name="T2">The second field value type.</typeparam>
    /// <typeparam name="TResult">The constructed object type.</typeparam>
    /// <param name="first">The first non-null field binding.</param>
    /// <param name="second">The second non-null field binding.</param>
    /// <param name="combine">The non-null constructor invoked after every field is valid.</param>
    /// <returns>An object contract.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Two fields share the same name.</exception>
    public static Contract<TResult> Object<T1, T2, TResult>(
        ContractField<TResult, T1> first,
        ContractField<TResult, T2> second,
        Func<T1, T2, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(combine);

        EnsureUniqueNames(first, second);
        return new Contract<TResult>(new ProductNode2<TResult, T1, T2>(first, second, combine));
    }

    /// <summary>Creates an object contract with three independent fields.</summary>
    /// <typeparam name="T1">The first field value type.</typeparam>
    /// <typeparam name="T2">The second field value type.</typeparam>
    /// <typeparam name="T3">The third field value type.</typeparam>
    /// <typeparam name="TResult">The constructed object type.</typeparam>
    /// <param name="first">The first non-null field binding.</param>
    /// <param name="second">The second non-null field binding.</param>
    /// <param name="third">The third non-null field binding.</param>
    /// <param name="combine">The non-null constructor invoked after every field is valid.</param>
    /// <returns>An object contract.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Two fields share the same name.</exception>
    public static Contract<TResult> Object<T1, T2, T3, TResult>(
        ContractField<TResult, T1> first,
        ContractField<TResult, T2> second,
        ContractField<TResult, T3> third,
        Func<T1, T2, T3, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(third);
        ArgumentNullException.ThrowIfNull(combine);

        EnsureUniqueNames(first, second, third);
        return new Contract<TResult>(
            new ProductNode3<TResult, T1, T2, T3>(first, second, third, combine));
    }

    /// <summary>Creates an object contract with four independent fields.</summary>
    /// <typeparam name="T1">The first field value type.</typeparam>
    /// <typeparam name="T2">The second field value type.</typeparam>
    /// <typeparam name="T3">The third field value type.</typeparam>
    /// <typeparam name="T4">The fourth field value type.</typeparam>
    /// <typeparam name="TResult">The constructed object type.</typeparam>
    /// <param name="first">The first non-null field binding.</param>
    /// <param name="second">The second non-null field binding.</param>
    /// <param name="third">The third non-null field binding.</param>
    /// <param name="fourth">The fourth non-null field binding.</param>
    /// <param name="combine">The non-null constructor invoked after every field is valid.</param>
    /// <returns>An object contract.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Two fields share the same name.</exception>
    public static Contract<TResult> Object<T1, T2, T3, T4, TResult>(
        ContractField<TResult, T1> first,
        ContractField<TResult, T2> second,
        ContractField<TResult, T3> third,
        ContractField<TResult, T4> fourth,
        Func<T1, T2, T3, T4, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(third);
        ArgumentNullException.ThrowIfNull(fourth);
        ArgumentNullException.ThrowIfNull(combine);

        EnsureUniqueNames(first, second, third, fourth);
        return new Contract<TResult>(
            new ProductNode4<TResult, T1, T2, T3, T4>(first, second, third, fourth, combine));
    }

    /// <summary>Creates a labelled alternative for a one-of contract.</summary>
    /// <typeparam name="T">The value type shared by the alternatives.</typeparam>
    /// <param name="label">The non-empty label used in diagnostics and schema metadata.</param>
    /// <param name="contract">The non-null alternative contract.</param>
    /// <returns>The labelled alternative.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="label"/> is empty.</exception>
    public static ContractAlternative<T> Alternative<T>(string label, Contract<T> contract)
    {
        ArgumentException.ThrowIfNullOrEmpty(label);
        ArgumentNullException.ThrowIfNull(contract);
        return new ContractAlternative<T>(label, contract);
    }

    /// <summary>Creates a one-of contract selected by caller-supplied selectors.</summary>
    /// <typeparam name="T">The value type shared by the alternatives.</typeparam>
    /// <param name="selectValue">The non-null selector that maps an existing value to an alternative index.</param>
    /// <param name="selectInput">
    /// The optional selector that maps JSON input to an alternative index; <see langword="null"/>
    /// makes decoding unavailable until a decoder is supplied.
    /// </param>
    /// <param name="discriminator">The optional JSON property name recorded in schema metadata.</param>
    /// <param name="alternatives">The non-empty, uniquely labelled alternatives.</param>
    /// <returns>A one-of contract.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An alternative label is empty or duplicated.</exception>
    public static Contract<T> OneOf<T>(
        Func<T, int> selectValue,
        Func<JsonElement, Option<int>>? selectInput,
        string? discriminator,
        params ContractAlternative<T>[] alternatives)
    {
        ArgumentNullException.ThrowIfNull(selectValue);
        var snapshot = SnapshotAlternatives(alternatives, out _);
        if (discriminator is not null)
        {
            ArgumentException.ThrowIfNullOrEmpty(discriminator);
        }

        return new Contract<T>(
            new OneOfNode<T>(snapshot, selectValue, selectInput, discriminator));
    }

    /// <summary>
    /// Creates a one-of contract whose JSON alternative is selected by a string discriminator
    /// property.
    /// </summary>
    /// <typeparam name="T">The value type shared by the alternatives.</typeparam>
    /// <param name="discriminator">The non-empty discriminator property name.</param>
    /// <param name="selectValue">The non-null selector that maps an existing value to an alternative index.</param>
    /// <param name="alternatives">The non-empty, uniquely labelled alternatives.</param>
    /// <returns>A one-of contract that decodes the first matching discriminator property.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A discriminator or label is empty, or labels are duplicated.</exception>
    public static Contract<T> DiscriminatedOneOf<T>(
        string discriminator,
        Func<T, int> selectValue,
        params ContractAlternative<T>[] alternatives)
    {
        ArgumentException.ThrowIfNullOrEmpty(discriminator);
        ArgumentNullException.ThrowIfNull(selectValue);
        var snapshot = SnapshotAlternatives(alternatives, out var labels);
        return new Contract<T>(
            new OneOfNode<T>(
                snapshot,
                selectValue,
                element => SelectByDiscriminator(element, discriminator, labels),
                discriminator));
    }

    /// <summary>Creates a named recursive contract reference.</summary>
    /// <typeparam name="T">The recursive value type.</typeparam>
    /// <param name="name">The non-empty recursion name.</param>
    /// <param name="factory">
    /// The non-null factory that builds the recursive body. It is invoked at most once per
    /// deferred contract and only when an interpretation needs the body.
    /// </param>
    /// <returns>A contract whose generation descends within the configured depth budget.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public static Contract<T> Defer<T>(string name, Func<Contract<T>> factory)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(factory);
        return new Contract<T>(new DeferredNode<T>(name, factory));
    }

    private static ContractAlternative<T>[] SnapshotAlternatives<T>(
        ContractAlternative<T>[] alternatives,
        out Dictionary<string, int> labels)
    {
        ArgumentNullException.ThrowIfNull(alternatives);
        if (alternatives.Length == 0)
        {
            throw new ArgumentException("At least one alternative is required.", nameof(alternatives));
        }

        var snapshot = new ContractAlternative<T>[alternatives.Length];
        labels = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < alternatives.Length; index++)
        {
            var alternative = alternatives[index];
            ArgumentNullException.ThrowIfNull(alternative);
            if (!labels.TryAdd(alternative.Label, index))
            {
                throw new ArgumentException(
                    "Alternative labels must be unique: " + alternative.Label + ".",
                    nameof(alternatives));
            }

            snapshot[index] = alternative;
        }

        return snapshot;
    }

    private static void EnsureUniqueNames<TParent, T1, T2>(
        ContractField<TParent, T1> first,
        ContractField<TParent, T2> second)
    {
        if (string.Equals(first.Name, second.Name, StringComparison.Ordinal))
        {
            throw new ArgumentException("Object field names must be unique: " + first.Name + ".");
        }
    }

    private static void EnsureUniqueNames<TParent, T1, T2, T3>(
        ContractField<TParent, T1> first,
        ContractField<TParent, T2> second,
        ContractField<TParent, T3> third)
    {
        EnsureUniqueNames(first, second);
        if (string.Equals(first.Name, third.Name, StringComparison.Ordinal)
            || string.Equals(second.Name, third.Name, StringComparison.Ordinal))
        {
            throw new ArgumentException("Object field names must be unique: " + third.Name + ".");
        }
    }

    private static void EnsureUniqueNames<TParent, T1, T2, T3, T4>(
        ContractField<TParent, T1> first,
        ContractField<TParent, T2> second,
        ContractField<TParent, T3> third,
        ContractField<TParent, T4> fourth)
    {
        EnsureUniqueNames(first, second, third);
        if (string.Equals(first.Name, fourth.Name, StringComparison.Ordinal)
            || string.Equals(second.Name, fourth.Name, StringComparison.Ordinal)
            || string.Equals(third.Name, fourth.Name, StringComparison.Ordinal))
        {
            throw new ArgumentException("Object field names must be unique: " + fourth.Name + ".");
        }
    }

    private static void ValidateRange<T>(T minimum, T maximum)
        where T : IComparable<T>
    {
        if (minimum.CompareTo(maximum) > 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimum),
                "The inclusive lower bound must not be greater than the upper bound.");
        }
    }

    private static Validation<T, DecodeError> ValidateInclusive<T>(T value, T minimum, T maximum)
        where T : IComparable<T> =>
        value.CompareTo(minimum) >= 0 && value.CompareTo(maximum) <= 0
            ? ContractSupport.Valid(value)
            : ContractSupport.Invalid<T>(
                LeafNode<T>.InvalidValue(
                    "value must be between "
                    + ContractSupport.FormatValue(minimum)
                    + " and "
                    + ContractSupport.FormatValue(maximum)));

    private static Result<string, string> GenerateString(GenerationContext context, int minimumLength)
    {
        var maximumLength = context.Options.MaxCollectionLength;
        if (maximumLength < minimumLength)
        {
            return Result<string, string>.Failure(
                "the generation collection length budget of "
                + maximumLength
                + " cannot satisfy the minimum string length of "
                + minimumLength
                + ".");
        }

        var length = context.Random.Next(minimumLength, maximumLength + 1);
        return Result<string, string>.Success(
            string.Create(
                length,
                context.Random,
                static (characters, random) =>
                {
                    for (var index = 0; index < characters.Length; index++)
                    {
                        characters[index] = GeneratedAlphabet[random.Next(GeneratedAlphabet.Length)];
                    }
                }));
    }

    private static IEnumerable<string> ShrinkString(string value, int minimumLength)
    {
        if (value.Length > minimumLength)
        {
            yield return value[..minimumLength];

            var halved = minimumLength + ((value.Length - minimumLength) / 2);
            if (halved > minimumLength && halved < value.Length)
            {
                yield return value[..halved];
            }
        }

        if (value.Length >= minimumLength && value.Any(static character => character != 'a'))
        {
            yield return new string('a', value.Length);
        }
    }

    private static IEnumerable<T> ShrinkNumber<T>(T value, T anchor)
        where T : INumber<T>
    {
        if (value != anchor)
        {
            yield return anchor;
        }

        // Halve each operand before adding: a wide range overflows on `value - anchor` (decimal
        // throws) or wraps (integers), while neither half can.
        var two = T.CreateChecked(2);
        var halved = (value / two) + (anchor / two);
        if (halved != value && halved != anchor)
        {
            yield return halved;
        }
    }

    private static long GenerateInt64(GenerationContext context, long minimum, long maximum)
    {
        if (minimum == maximum)
        {
            return minimum;
        }

        return maximum == long.MaxValue
            ? context.Random.NextInt64(minimum, long.MaxValue)
            : context.Random.NextInt64(minimum, maximum + 1);
    }

    private static decimal GenerateDecimal(
        GenerationContext context,
        decimal minimum,
        decimal maximum)
    {
        if (minimum == maximum)
        {
            return minimum;
        }

        var fraction = (decimal)context.Random.NextDouble();
        var generated = (minimum * (1m - fraction)) + (maximum * fraction);
        return Math.Clamp(generated, minimum, maximum);
    }

    private static double GenerateDouble(GenerationContext context, double minimum, double maximum)
    {
        if (minimum == maximum)
        {
            return minimum;
        }

        var fraction = context.Random.NextDouble();
        var generated = (minimum * (1d - fraction)) + (maximum * fraction);
        return Math.Clamp(generated, minimum, maximum);
    }

    private static Option<int> SelectByDiscriminator(
        JsonElement element,
        string discriminator,
        Dictionary<string, int> labels)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !Decoder.TryGetFirstProperty(element, discriminator, out var property))
        {
            return Option<int>.None;
        }

        return property.ValueKind == JsonValueKind.String
            ? labels.GetOption(property.GetString()!)
            : Option<int>.None;
    }
}

/// <summary>
/// Describes supported data constraints once and derives validation, decoding, explanation,
/// schema metadata, generated values, and shrinking from that description.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <remarks>
/// A contract is immutable and reusable. It holds only its structural description and optional
/// caller-supplied derivations; no invocation state is cached. Validation is a check of an
/// existing value: a valid callback result does not replace the tested value. Decoding reuses
/// <see cref="Decoder{T}"/> and its <see cref="DecodeError"/> diagnostics. Generation and
/// shrinking are deterministic for a seed and options, and every generated or shrunk candidate is
/// checked against the contract before it is returned. Missing derivations are reported as
/// <see cref="ContractLimitation"/> values, never by silently retrying or weakening a constraint.
/// </remarks>
public sealed class Contract<T>
{
    private readonly ContractNode<T> node;
    private readonly Func<Random, T>? generator;
    private readonly Func<T, IEnumerable<T>>? shrinker;
    private readonly Decoder<T>? decoder;
    private readonly ContractDescription? schema;
    private readonly Lazy<ContractCapabilities> capabilities;
    private readonly Lazy<ContractDescription> description;
    private readonly Lazy<string> explanation;

    internal Contract(ContractNode<T> node)
        : this(node, null, null, null, null)
    {
    }

    private Contract(
        ContractNode<T> node,
        Func<Random, T>? generator,
        Func<T, IEnumerable<T>>? shrinker,
        Decoder<T>? decoder,
        ContractDescription? schema)
    {
        this.node = node;
        this.generator = generator;
        this.shrinker = shrinker;
        this.decoder = decoder;
        this.schema = schema;
        capabilities = new Lazy<ContractCapabilities>(
            BuildCapabilities,
            LazyThreadSafetyMode.ExecutionAndPublication);
        description = new Lazy<ContractDescription>(
            () => schema ?? node.Describe(),
            LazyThreadSafetyMode.ExecutionAndPublication);
        explanation = new Lazy<string>(
            BuildExplanation,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Gets the structured description used for explanation and schema metadata.</summary>
    public ContractDescription Description => description.Value;

    /// <summary>Gets the interpretations this contract can derive and the ones it cannot.</summary>
    public ContractCapabilities Capabilities => capabilities.Value;

    private ContractCapabilities BuildCapabilities()
    {
        var limitations = new List<ContractLimitation>();
        foreach (var derivation in Enum.GetValues<ContractDerivation>())
        {
            CollectLimitations(limitations, string.Empty, derivation, []);
        }

        return new ContractCapabilities(limitations);
    }

    /// <summary>Returns a deterministic, human-readable explanation of the description and capabilities.</summary>
    /// <returns>The explanation text.</returns>
    public string Explain() => explanation.Value;

    private string BuildExplanation()
    {
        var capabilities = Capabilities;
        var builder = new StringBuilder();
        Description.AppendTree(builder, 0);
        builder.AppendLine("capabilities:");
        builder.AppendLine("  validation: available");
        builder.Append("  decoding: ").AppendLine(capabilities.CanDecode ? "available" : "unavailable");
        builder.Append("  generation: ").AppendLine(capabilities.CanGenerate ? "available" : "unavailable");
        builder.Append("  shrinking: ").AppendLine(capabilities.CanShrink ? "available" : "unavailable");
        builder.Append("  schema: ").AppendLine(capabilities.HasCompleteSchema ? "complete" : "incomplete");

        if (capabilities.Limitations.Count > 0)
        {
            builder.AppendLine("limitations:");
            foreach (var limitation in capabilities.Limitations)
            {
                builder.Append("  ").AppendLine(limitation.ToString());
            }
        }

        return builder.ToString();
    }

    /// <summary>Checks an existing value against every rule in the contract.</summary>
    /// <param name="value">The value to check. A runtime null is a null error.</param>
    /// <returns>
    /// A valid validation containing the checked value, or every accumulated
    /// <see cref="DecodeError"/> in deterministic contract order.
    /// </returns>
    /// <remarks>
    /// Validation checks rather than rebuilds. A refinement callback's valid value is ignored;
    /// only its messages are used. Object fields are checked in declaration order and collection
    /// elements in source order, with the same paths that decoding reports.
    /// </remarks>
    public Validation<T, DecodeError> Validate(T value)
    {
        if (value is null)
        {
            return ContractSupport.Invalid<T>(
                DecodeError.At(DiagnosticLocation.Root, DecodeErrorKind.Null));
        }

        return node.Validate(value);
    }

    /// <summary>Creates a decoder that produces values satisfying the contract.</summary>
    /// <returns>
    /// A successful result containing the decoder, or a <see cref="ContractLimitation"/> when the
    /// contract has no decodable structure and the caller supplied no decoder.
    /// </returns>
    /// <remarks>
    /// A caller-supplied decoder through <see cref="WithDecoder"/> is treated as the structural
    /// decoder; its decoded value is then checked against the whole contract, so decoding and
    /// validation cannot disagree silently. A value produced by an overridden decoder that fails
    /// the contract returns the validation errors.
    /// </remarks>
    public Result<Decoder<T>, ContractLimitation> CreateDecoder() =>
        decoder is not null
            ? Result<Decoder<T>, ContractLimitation>.Success(
                ContractSupport.ValidatedDecoder(decoder, Validate))
            : node.CreateDecoder();

    /// <summary>Generates one value that satisfies the contract.</summary>
    /// <param name="options">The non-null seed and generation budget.</param>
    /// <returns>
    /// A successful result containing the value, or a <see cref="ContractLimitation"/> when a
    /// derivation, budget, or declared constraint cannot be satisfied.
    /// </returns>
    /// <remarks>
    /// Generation is deterministic for the options' seed. It never retries an arbitrary predicate
    /// or cross-field rule: an unavailable derivation fails explicitly, and a caller-supplied
    /// generator that produces a value the contract rejects fails with the validation messages.
    /// Every produced value is checked against the full contract before it is returned.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public Result<T, ContractLimitation> Generate(ContractGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var capabilities = Capabilities;
        if (!capabilities.CanGenerate)
        {
            return Result<T, ContractLimitation>.Failure(FirstLimitation(capabilities, ContractDerivation.Generation));
        }

        return GenerateOne(new GenerationContext(options));
    }

    /// <summary>Generates several values that satisfy the contract.</summary>
    /// <param name="count">The non-negative number of values to generate.</param>
    /// <param name="options">The non-null seed and generation budget.</param>
    /// <returns>
    /// A successful result containing the values in generation order, or the first
    /// <see cref="ContractLimitation"/> encountered.
    /// </returns>
    /// <remarks>
    /// One deterministic random sequence serves the whole call, so a seed produces the same sample
    /// list. The node budget is shared across the samples. Every produced value is checked against
    /// the full contract.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public Result<IReadOnlyList<T>, ContractLimitation> GenerateSamples(
        int count,
        ContractGenerationOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentNullException.ThrowIfNull(options);

        if (count == 0)
        {
            return Result<IReadOnlyList<T>, ContractLimitation>.Success([]);
        }

        var capabilities = Capabilities;
        if (!capabilities.CanGenerate)
        {
            return Result<IReadOnlyList<T>, ContractLimitation>.Failure(
                FirstLimitation(capabilities, ContractDerivation.Generation));
        }

        var context = new GenerationContext(options);
        var values = new List<T>(count);
        for (var index = 0; index < count; index++)
        {
            var generated = GenerateOne(context);
            if (generated.TryGetError(out var limitation))
            {
                return Result<IReadOnlyList<T>, ContractLimitation>.Failure(limitation);
            }

            generated.TryGetValue(out var value);
            values.Add(value!);
        }

        return Result<IReadOnlyList<T>, ContractLimitation>.Success(values);
    }

    /// <summary>Produces one-step shrink candidates that still satisfy the contract.</summary>
    /// <param name="value">The non-null value to shrink.</param>
    /// <returns>
    /// A successful result containing candidates that pass the full contract and differ from
    /// <paramref name="value"/>, or a <see cref="ContractLimitation"/> when shrinking is
    /// unavailable.
    /// </returns>
    /// <remarks>
    /// Shrinking never invents an inverse. Derived shrinkers move numbers toward a lower anchor,
    /// shorten collections and strings, remove present values, and rebuild objects one field at a
    /// time. Every candidate is checked against the whole contract, including cross-field rules,
    /// so a refinement cannot leak an invalid shrunk input. A caller-supplied shrinker has its
    /// candidates checked the same way. At most 256 candidates are materialized. The input must
    /// satisfy the contract; an invalid input returns a shrinking limitation instead of being
    /// shrunk.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public Result<IReadOnlyList<T>, ContractLimitation> Shrink(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var capabilities = Capabilities;
        if (!capabilities.CanShrink)
        {
            return Result<IReadOnlyList<T>, ContractLimitation>.Failure(
                FirstLimitation(capabilities, ContractDerivation.Shrinking));
        }

        var inputValidation = Validate(value);
        if (!inputValidation.IsValid)
        {
            inputValidation.TryGetErrors(out var inputErrors);
            return Result<IReadOnlyList<T>, ContractLimitation>.Failure(
                new ContractLimitation(
                    ContractDerivation.Shrinking,
                    string.Empty,
                    "the value does not satisfy the contract: "
                    + ContractSupport.FormatErrors(inputErrors!)));
        }

        var context = new ShrinkContext();
        Result<IReadOnlyList<T>, ContractLimitation> raw;
        if (shrinker is not null)
        {
            raw = ContractSupport.TakeShrinkCandidates(shrinker(value));
        }
        else
        {
            raw = node.Shrink(value, context, string.Empty);
        }

        if (raw.TryGetError(out var limitation))
        {
            return Result<IReadOnlyList<T>, ContractLimitation>.Failure(limitation);
        }

        raw.TryGetValue(out var candidates);
        var seen = new HashSet<T>(EqualityComparer<T>.Default)
        {
            value,
        };
        var results = new List<T>();
        foreach (var candidate in candidates!)
        {
            if (candidate is null || !Validate(candidate).IsValid)
            {
                continue;
            }

            if (seen.Add(candidate))
            {
                results.Add(candidate);
                if (results.Count == ContractSupport.MaxShrinkCandidates)
                {
                    break;
                }
            }
        }

        return Result<IReadOnlyList<T>, ContractLimitation>.Success(results);
    }

    /// <summary>Adds one independent predicate rule to the contract.</summary>
    /// <param name="predicate">The non-null predicate applied to values that pass the existing rules.</param>
    /// <param name="detail">The non-empty message reported when the predicate rejects a value.</param>
    /// <returns>A contract whose validation and decoding apply the predicate after the existing rules.</returns>
    /// <remarks>
    /// The predicate is opaque: generation is unavailable and schema metadata becomes incomplete
    /// until the caller supplies <see cref="WithGenerator"/> or <see cref="WithSchema"/>. Shrinking
    /// remains available because derived candidates are checked against the predicate. Generation
    /// never rejects-and-retries this rule. Predicate exceptions, including cancellation, propagate
    /// unchanged.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="detail"/> is empty.</exception>
    public Contract<T> Ensure(Func<T, bool> predicate, string detail)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentException.ThrowIfNullOrEmpty(detail);

        return new Contract<T>(
            new RefinedNode<T>(
                this,
                value => predicate(value)
                    ? Validation<T, string>.Valid(value)
                    : Validation<T, string>.Invalid(detail),
                detail));
    }

    /// <summary>Adds an accumulating domain validation to the contract.</summary>
    /// <param name="validate">The non-null validation applied to values that pass the existing rules.</param>
    /// <param name="description">
    /// The optional non-empty explanation recorded in the structure; the default states that a
    /// caller-supplied domain validation is present.
    /// </param>
    /// <returns>A contract whose validation and decoding collect every returned message.</returns>
    /// <remarks>
    /// The callback is opaque: generation is unavailable and schema metadata becomes incomplete
    /// until supplied explicitly. The value carried by a successful validation is ignored because
    /// contract validation checks an existing value. Shrinking checks candidates against the
    /// callback. Callback exceptions, including cancellation, propagate unchanged.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="validate"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="description"/> is empty.</exception>
    public Contract<T> ValidateWith(
        Func<T, Validation<T, string>> validate,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(validate);

        var detail = description ?? "caller-supplied domain validation";
        ArgumentException.ThrowIfNullOrEmpty(detail);
        return new Contract<T>(new RefinedNode<T>(this, validate, detail));
    }

    /// <summary>Supplies an explicit generator for this contract.</summary>
    /// <param name="generator">The non-null function that produces values from a seeded random source.</param>
    /// <returns>A contract that uses the supplied generator and checks every result against all rules.</returns>
    /// <remarks>
    /// The generator is the honest extension path for an opaque predicate, transformation, or
    /// cross-field rule. A generated value that fails the contract is an explicit
    /// <see cref="ContractLimitation"/>; it is never silently retried.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    public Contract<T> WithGenerator(Func<Random, T> generator)
    {
        ArgumentNullException.ThrowIfNull(generator);
        return new Contract<T>(node, generator, shrinker, decoder, schema);
    }

    /// <summary>Supplies an explicit shrinker for this contract.</summary>
    /// <param name="shrinker">The non-null function that proposes smaller candidates.</param>
    /// <returns>A contract whose candidates are filtered to values that satisfy every rule.</returns>
    /// <remarks>
    /// Candidates equal to the input, invalid under the contract, or duplicate are omitted. The
    /// shrinker is the honest extension path when no inverse or derived shrinker exists.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="shrinker"/> is <see langword="null"/>.</exception>
    public Contract<T> WithShrinker(Func<T, IEnumerable<T>> shrinker)
    {
        ArgumentNullException.ThrowIfNull(shrinker);
        return new Contract<T>(node, generator, shrinker, decoder, schema);
    }

    /// <summary>Supplies an explicit structural decoder for this contract.</summary>
    /// <param name="decoder">The non-null decoder that reads the contract's input shape.</param>
    /// <returns>
    /// A contract that decodes with the supplied decoder and then checks the decoded value against
    /// every rule.
    /// </returns>
    /// <remarks>
    /// The decoder is the honest extension path for a value shape the contract cannot derive. It
    /// does not bypass validation: a decoded value that fails the contract reports the contract's
    /// errors instead of succeeding.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="decoder"/> is <see langword="null"/>.</exception>
    public Contract<T> WithDecoder(Decoder<T> decoder)
    {
        ArgumentNullException.ThrowIfNull(decoder);
        return new Contract<T>(node, generator, shrinker, decoder, schema);
    }

    /// <summary>Supplies explicit schema metadata for this contract.</summary>
    /// <param name="schema">The non-null description used for explanation and schema inspection.</param>
    /// <returns>A contract that reports complete schema metadata at this level.</returns>
    /// <remarks>
    /// This is the honest extension path for an opaque rule whose shape the caller knows. It does
    /// not change validation, decoding, generation, or shrinking.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null"/>.</exception>
    public Contract<T> WithSchema(ContractDescription schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return new Contract<T>(node, generator, shrinker, decoder, schema);
    }

    /// <summary>Projects this contract to a base value type with a caller-supplied narrowing.</summary>
    /// <typeparam name="TBase">The projected value type.</typeparam>
    /// <param name="widen">The non-null projection from the derived value to the base value.</param>
    /// <param name="narrow">The non-null partial inverse used for validation and shrinking.</param>
    /// <param name="mismatchDetail">The non-empty message when a base value is not the derived type.</param>
    /// <returns>A contract for the base type whose structural facts come from this contract.</returns>
    /// <remarks>
    /// This is useful for a base type and its derived cases without a general discriminated-union
    /// API. Because the caller supplies the partial inverse, every derivation except the base
    /// type's own unknown rules remains available. A projected value is checked through
    /// <paramref name="narrow"/> and the source contract, so an inconsistent projection fails
    /// explicitly instead of producing an unchecked value.
    /// </remarks>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="mismatchDetail"/> is empty.</exception>
    public Contract<TBase> Widen<TBase>(
        Func<T, TBase> widen,
        Func<TBase, Option<T>> narrow,
        string mismatchDetail)
    {
        ArgumentNullException.ThrowIfNull(widen);
        ArgumentNullException.ThrowIfNull(narrow);
        ArgumentException.ThrowIfNullOrEmpty(mismatchDetail);

        return new Contract<TBase>(
            new WidenNode<T, TBase>(this, widen, narrow, mismatchDetail));
    }

    internal void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited)
    {
        if (HasOverride(derivation))
        {
            return;
        }

        if (!visited.Add(this))
        {
            return;
        }

        node.CollectLimitations(sink, path, derivation, visited);
        visited.Remove(this);
    }

    private bool HasOverride(ContractDerivation derivation) =>
        derivation switch
        {
            ContractDerivation.Decoding => decoder is not null,
            ContractDerivation.Generation => generator is not null,
            ContractDerivation.Shrinking => shrinker is not null,
            ContractDerivation.Schema => schema is not null,
            _ => false,
        };

    private Result<T, ContractLimitation> GenerateOne(GenerationContext context)
    {
        var generated = GenerateCore(context, 0, string.Empty);
        if (generated.TryGetError(out var limitation))
        {
            return Result<T, ContractLimitation>.Failure(limitation);
        }

        generated.TryGetValue(out var value);
        if (value is null)
        {
            return ContractSupport.GenerationFailure<T>(
                string.Empty,
                "the generated value was null; use Option to represent absence.");
        }

        var validated = Validate(value);
        if (validated.IsValid)
        {
            return Result<T, ContractLimitation>.Success(value);
        }

        validated.TryGetErrors(out var errors);
        return ContractSupport.GenerationFailure<T>(
            string.Empty,
            "the generated value does not satisfy the contract: "
            + ContractSupport.FormatErrors(errors!));
    }

    internal Result<T, ContractLimitation> GenerateCore(
        GenerationContext context,
        int depth,
        string path)
    {
        if (generator is not null)
        {
            if (!context.TryTakeNode())
            {
                return ContractSupport.GenerationBudgetFailure<T>(context, path);
            }

            var value = generator(context.Random);
            return value is null
                ? ContractSupport.GenerationFailure<T>(
                    path,
                    "the caller-supplied generator produced null; use Option to represent absence.")
                : Result<T, ContractLimitation>.Success(value);
        }

        return node.Generate(context, depth, path);
    }

    internal Result<IReadOnlyList<T>, ContractLimitation> ShrinkCore(
        T value,
        ShrinkContext context,
        string path) =>
        shrinker is not null
            ? ContractSupport.TakeShrinkCandidates(shrinker(value))
            : node.Shrink(value, context, path);

    private static ContractLimitation FirstLimitation(
        ContractCapabilities capabilities,
        ContractDerivation derivation) =>
        capabilities.Limitations.First(
            item => item.Derivation == derivation);
}
