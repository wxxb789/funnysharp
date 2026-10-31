using System.Text.Json;
using FunnySharp;

internal static class PatchExamples
{
    internal static void Verify()
    {
        var current = new Schedule(
            "Europe/Paris",
            OpensAt: 9 * 60,
            ClosesAt: 17 * 60,
            Option.Some("front desk"));
        var schema = CreateSchema();

        using var validDocument = JsonDocument.Parse(
            """{"closesAt":1080,"note":null}""");
        var decoded = schema.Decode(validDocument.RootElement);
        ExampleAssertions.True(decoded.TryGetValue(out var change), "The change must decode.");

        var validated = schema.Validate(change, current);
        ExampleAssertions.True(
            validated.TryGetValue(out var application),
            "The candidate must validate.");
        ExampleAssertions.Equal(1080, application.Candidate.ClosesAt);
        ExampleAssertions.Equal(9 * 60, application.Candidate.OpensAt);
        ExampleAssertions.True(
            application.Candidate.Note.IsNone,
            "An explicit null must clear the optional note.");
        ExampleAssertions.True(
            current.Note.IsSome,
            "Applying a change must not mutate the caller-owned source.");

        using var rejectedDocument = JsonDocument.Parse("""{"ownerId":"employee-7"}""");
        var rejected = schema.Decode(rejectedDocument.RootElement);
        ExampleAssertions.True(
            rejected.TryGetErrors(out var errors),
            "A field outside the allowed change must be rejected.");
        ExampleAssertions.Equal("ownerId", errors![0].Path);
        ExampleAssertions.Equal(DecodeErrorKind.UnsupportedField, errors[0].Kind);

        var left = Patch<Schedule>.Empty
            .With(Fields.ClosesAt, PatchChange<int>.Set(1080));
        var right = Patch<Schedule>.Empty
            .With(Fields.ClosesAt, PatchChange<int>.Set(1200));
        var combined = left.Combine(right);
        ExampleAssertions.True(
            combined.TryGetErrors(out var conflicts),
            "Two writes to the same field must be a conflict.");
        ExampleAssertions.Equal("closesAt", conflicts![0].Field);

        Patch<Schedule> empty = default;
        ExampleAssertions.True(empty.IsEmpty, "The default patch must be empty.");
        var identity = empty.ApplyTo(current);
        ExampleAssertions.True(
            ReferenceEquals(identity.Candidate, current),
            "An empty patch must preserve source identity.");
    }

    private static PatchSchema<Schedule> CreateSchema()
    {
        var created = PatchSchema<Schedule>.Create(ScheduleContract, Fields.ClosesAt, Fields.Note);
        if (created.TryGetError(out var limitation))
        {
            throw new InvalidOperationException(limitation.ToString());
        }

        _ = created.TryGetValue(out var schema);
        return schema!;
    }

    private static readonly Contract<Schedule> ScheduleContract = BuildContract();

    private static readonly PatchFields Fields = BuildFields();

    private static Contract<Schedule> BuildContract()
    {
        var fields = Contract.For<Schedule>();
        return Contract.Object(
                fields.Field("timeZone", Contract.NonEmptyString, static value => value.TimeZone),
                fields.Field("opensAt", Contract.Int32Range(0, 1440), static value => value.OpensAt),
                fields.Field("closesAt", Contract.Int32Range(0, 1440), static value => value.ClosesAt),
                fields.OptionalField("note", Contract.NonEmptyString, static value => value.Note),
                static (timeZone, opensAt, closesAt, note) =>
                    new Schedule(timeZone, opensAt, closesAt, note))
            .Ensure(
                static value => value.OpensAt < value.ClosesAt,
                "opensAt must be before closesAt");
    }

    private static PatchFields BuildFields()
    {
        var closesAtLens = Lens.Create<Schedule, int>(
            static value => value.ClosesAt,
            static (value, next) => value with { ClosesAt = next });
        var noteLens = Lens.Create<Schedule, Option<string>>(
            static value => value.Note,
            static (value, next) => value with { Note = next });

        return new PatchFields(
            Patch.Field("closesAt", Contract.Int32Range(0, 1440), closesAtLens),
            Patch.ClearableField(
                "note",
                Contract.Optional(Contract.NonEmptyString),
                noteLens,
                static value => value with { Note = Option<string>.None }));
    }

    private sealed record PatchFields(
        PatchField<Schedule, int> ClosesAt,
        PatchField<Schedule, Option<string>> Note);

    private sealed record Schedule(
        string TimeZone,
        int OpensAt,
        int ClosesAt,
        Option<string> Note);
}
