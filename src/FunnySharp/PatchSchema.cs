#pragma warning disable FS0017

using System.Text.Json;

namespace FunnySharp;

using DiagnosticLocation = Location;

/// <summary>
/// Describes the allowed fields of a partial update and reuses them for decoding, candidate
/// validation, explanation, and application.
/// </summary>
/// <typeparam name="T">The type of the value being changed.</typeparam>
/// <remarks>
/// <para>
/// A schema is immutable and reusable. <see cref="Decode"/> turns a JSON object into a
/// <see cref="Patch{T}"/>: an absent property is unchanged, an explicit <c>null</c> clears a
/// clearable field, a replacement value is decoded through that field's contract, and any other
/// property is rejected. <see cref="Validate"/> applies the patch to a caller-supplied current
/// value and checks the complete candidate against <see cref="CandidateContract"/>, so unchanged
/// fields and cross-field rules participate in validation.
/// </para>
/// <para>
/// The schema never reads storage, checks a version, persists a candidate, or executes rollback.
/// Decoding and validation do not catch callback exceptions. A field whose contract cannot
/// derive a decoder is reported by <see cref="Create"/> as a
/// <see cref="ContractLimitation"/> with the field name as its path.
/// </para>
/// </remarks>
public sealed class PatchSchema<T>
{
    private readonly Contract<T> candidateContract;
    private readonly PatchField<T>[] fields;
    private readonly Dictionary<string, int> fieldIndex;
    private readonly Func<Patch<T>, JsonElement, Validation<Patch<T>, DecodeError>>[] decoders;
    private readonly PatchFieldDescription[] descriptions;

    private PatchSchema(
        Contract<T> candidateContract,
        PatchField<T>[] fields,
        Dictionary<string, int> fieldIndex,
        Func<Patch<T>, JsonElement, Validation<Patch<T>, DecodeError>>[] decoders,
        PatchFieldDescription[] descriptions)
    {
        this.candidateContract = candidateContract;
        this.fields = fields;
        this.fieldIndex = fieldIndex;
        this.decoders = decoders;
        this.descriptions = descriptions;
    }

    /// <summary>Gets the contract applied to the complete candidate state.</summary>
    public Contract<T> CandidateContract => candidateContract;

    /// <summary>Gets the allowed fields in declaration order.</summary>
    public IReadOnlyList<PatchFieldDescription> Fields => Array.AsReadOnly(descriptions);

    /// <summary>
    /// Creates a schema from a complete candidate contract and the allowed field bindings.
    /// </summary>
    /// <param name="candidateContract">The non-null contract checked against every candidate state.</param>
    /// <param name="fields">The non-empty allowed field bindings, each with a unique name.</param>
    /// <returns>
    /// A reusable schema, or a <see cref="ContractLimitation"/> naming the first field whose value
    /// contract cannot derive a decoder.
    /// </returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="fields"/> is empty, a field is null, or two fields share a name.</exception>
    public static Result<PatchSchema<T>, ContractLimitation> Create(
        Contract<T> candidateContract,
        params PatchField<T>[] fields)
    {
        ArgumentNullException.ThrowIfNull(candidateContract);
        ArgumentNullException.ThrowIfNull(fields);

        if (fields.Length == 0)
        {
            throw new ArgumentException("At least one patch field is required.", nameof(fields));
        }

        var copies = new PatchField<T>[fields.Length];
        var descriptions = new PatchFieldDescription[fields.Length];
        var decoders = new Func<Patch<T>, JsonElement, Validation<Patch<T>, DecodeError>>[fields.Length];
        var fieldIndex = new Dictionary<string, int>(fields.Length, StringComparer.Ordinal);

        for (var index = 0; index < fields.Length; index++)
        {
            var field = fields[index]
                ?? throw new ArgumentNullException(nameof(fields), "A patch field must not be null.");

            if (!fieldIndex.TryAdd(field.Name, index))
            {
                throw new ArgumentException(
                    $"Duplicate patch field name '{field.Name}'.",
                    nameof(fields));
            }

            copies[index] = field;
            descriptions[index] = field.Describe();

            var created = field.CreateDecoder();
            if (created.TryGetError(out var limitation))
            {
                return Result<PatchSchema<T>, ContractLimitation>.Failure(
                    limitation.WithPathPrefix(field.Name));
            }

            created.TryGetValue(out var decoder);
            decoders[index] = decoder!;
        }

        return Result<PatchSchema<T>, ContractLimitation>.Success(
            new PatchSchema<T>(candidateContract, copies, fieldIndex, decoders, descriptions));
    }

    /// <summary>
    /// Decodes an allowed partial change from a JSON object.
    /// </summary>
    /// <param name="input">The parsed JSON object.</param>
    /// <returns>
    /// A patch carrying only the supplied changes, or every structural and field error in document
    /// order. An empty object is a valid empty patch.
    /// </returns>
    /// <remarks>
    /// Object property names are case-sensitive. A property absent from the input is unchanged; an
    /// explicit null is a clearing instruction only for a clearable field; a non-null value is
    /// decoded through the field's value contract. Unknown fields and repeated property names are
    /// errors. The input element is read during this call and is not retained.
    /// </remarks>
    public Validation<Patch<T>, DecodeError> Decode(JsonElement input)
    {
        if (input.ValueKind == JsonValueKind.Null)
        {
            return Validation<Patch<T>, DecodeError>.Invalid(
                DecodeError.At(
                    DiagnosticLocation.Root,
                    DecodeErrorKind.Null,
                    "an object"));
        }

        if (input.ValueKind != JsonValueKind.Object)
        {
            return Validation<Patch<T>, DecodeError>.Invalid(
                DecodeError.At(
                    DiagnosticLocation.Root,
                    DecodeErrorKind.WrongKind,
                    "an object",
                    $"found {input.ValueKind}"));
        }

        var patch = Patch<T>.Empty;
        List<DecodeError>? errors = null;
        ulong seen = 0;
        HashSet<string>? seenNames = null;

        foreach (var property in input.EnumerateObject())
        {
            if (!fieldIndex.TryGetValue(property.Name, out var index))
            {
                AddError(
                    ref errors,
                    DecodeError.At(
                        FieldLocation(property.Name),
                        DecodeErrorKind.UnsupportedField,
                        detail: "the field is not an allowed change"));
                continue;
            }

            if (index < 64)
            {
                var bit = 1UL << index;
                if ((seen & bit) != 0)
                {
                    AddError(
                        ref errors,
                        DecodeError.At(
                            FieldLocation(property.Name),
                            DecodeErrorKind.DuplicateField,
                            detail: "the field is supplied more than once"));
                    continue;
                }

                seen |= bit;
            }
            else
            {
                seenNames ??= new HashSet<string>(StringComparer.Ordinal);
                if (!seenNames.Add(property.Name))
                {
                    AddError(
                        ref errors,
                        DecodeError.At(
                            FieldLocation(property.Name),
                            DecodeErrorKind.DuplicateField,
                            detail: "the field is supplied more than once"));
                    continue;
                }
            }

            var field = fields[index];
            if (property.Value.ValueKind == JsonValueKind.Null)
            {
                if (field.AllowsClear)
                {
                    patch = field.WithClear(patch);
                }
                else
                {
                    AddError(
                        ref errors,
                        DecodeError.At(
                            DiagnosticLocation.Root.Property(property.Name),
                            DecodeErrorKind.Null,
                            "a replacement value; clearing is not allowed"));
                }

                continue;
            }

            var decoded = decoders[index](patch, property.Value);
            if (decoded.TryGetValue(out var updated))
            {
                patch = updated;
            }
            else
            {
                decoded.TryGetErrors(out var fieldErrors);
                (errors ??= []).AddRange(fieldErrors!);
            }
        }

        return errors is null
            ? Validation<Patch<T>, DecodeError>.Valid(patch)
            : Validation<Patch<T>, DecodeError>.InvalidMany(errors);
    }

    /// <summary>
    /// Applies a patch to <paramref name="source"/> and validates the complete candidate state.
    /// </summary>
    /// <param name="patch">The patch to apply.</param>
    /// <param name="source">The exact current value; no storage read or version check occurs.</param>
    /// <returns>
    /// A valid application carrying the source and candidate, or every candidate validation error.
    /// </returns>
    /// <remarks>
    /// The candidate is built by <see cref="Patch{T}.ApplyTo"/> before validation. A setter,
    /// clear callback, or contract predicate that throws propagates unchanged. Validation applies
    /// to the exact <paramref name="source"/> supplied; a concurrent writer or stale version is
    /// the caller's concern and must be checked in its own concurrency policy.
    /// </remarks>
    public Validation<PatchApplication<T>, DecodeError> Validate(Patch<T> patch, T source)
    {
        var unbound = patch.FindUnbound(fields);
        if (unbound is not null)
        {
            return Validation<PatchApplication<T>, DecodeError>.Invalid(
                DecodeError.At(
                    FieldLocation(unbound),
                    DecodeErrorKind.UnsupportedField,
                    detail: "the field is not part of this schema"));
        }

        var application = patch.ApplyTo(source);
        var validated = candidateContract.Validate(application.Candidate);

        return validated.TryGetErrors(out var errors)
            ? Validation<PatchApplication<T>, DecodeError>.InvalidMany(errors!)
            : Validation<PatchApplication<T>, DecodeError>.Valid(application);
    }

    /// <summary>Returns a deterministic explanation of the allowed fields and candidate rules.</summary>
    /// <returns>The field names, value descriptions, clearing policy, and candidate contract explanation.</returns>
    public string Explain() =>
        "patch fields: "
        + string.Join(", ", descriptions)
        + Environment.NewLine
        + "candidate: "
        + candidateContract.Explain();

    private static void AddError(ref List<DecodeError>? errors, DecodeError error) =>
        (errors ??= []).Add(error);

    private static DiagnosticLocation FieldLocation(string name) =>
        name.Length == 0
            ? DiagnosticLocation.Root.Key(name)
            : DiagnosticLocation.Root.Property(name);
}
