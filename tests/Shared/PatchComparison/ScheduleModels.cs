using System.Globalization;
using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.Patching;

internal sealed record Schedule(
    string TimeZone,
    TimeOnly OpensAt,
    TimeOnly ClosesAt,
    Option<string> Note);

internal enum ScheduleErrorCode
{
    UnsupportedField,
    DuplicateField,
    Null,
    WrongKind,
    Malformed,
    Empty,
    Invalid,
}

internal sealed record ScheduleError(string Path, ScheduleErrorCode Code, string Detail);

internal sealed record ScheduleUpdateResult(Schedule? Candidate, IReadOnlyList<ScheduleError> Errors)
{
    internal bool IsSuccess => Candidate is not null && Errors.Count == 0;
}

internal static class ScheduleRules
{
    internal const string CrossFieldDetail = "opensAt must be before closesAt";
    internal const string UnsupportedFieldDetail = "the field is not an allowed change";
    internal const string DuplicateFieldDetail = "the field is supplied more than once";
    internal const string ClearNotAllowedDetail = "a replacement value; clearing is not allowed";

    private static readonly Decoder<TimeOnly> TimeOfDayDecoder = Decoder.From<TimeOnly>(
        static element =>
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                return Validation<TimeOnly, DecodeError>.Invalid(
                    DecodeError.Create(
                        DecodeErrorKind.WrongKind,
                        "a time string",
                        $"found {element.ValueKind}"));
            }

            var text = element.GetString()!;
            return TimeOnly.TryParseExact(
                text,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var value)
                ? Validation<TimeOnly, DecodeError>.Valid(value)
                : Validation<TimeOnly, DecodeError>.Invalid(
                    DecodeError.Create(
                        DecodeErrorKind.Malformed,
                        "a time in HH:mm format",
                        text));
        });

    internal static Contract<TimeOnly> TimeOfDay { get; } =
        Contract.From<TimeOnly>(
            "time of day",
            static value => Validation<TimeOnly, string>.Valid(value))
            .WithDecoder(TimeOfDayDecoder);

    internal static Contract<string> TimeZone { get; } = Contract.NonEmptyString;

    internal static Contract<Option<string>> Note { get; } =
        Contract.Optional(Contract.NonEmptyString);

    private static readonly Lens<Schedule, string> TimeZoneLens = Lens.Create<Schedule, string>(
        static value => value.TimeZone,
        static (value, next) => value with { TimeZone = next });

    private static readonly Lens<Schedule, TimeOnly> OpensAtLens = Lens.Create<Schedule, TimeOnly>(
        static value => value.OpensAt,
        static (value, next) => value with { OpensAt = next });

    private static readonly Lens<Schedule, TimeOnly> ClosesAtLens = Lens.Create<Schedule, TimeOnly>(
        static value => value.ClosesAt,
        static (value, next) => value with { ClosesAt = next });

    private static readonly Lens<Schedule, Option<string>> NoteLens =
        Lens.Create<Schedule, Option<string>>(
            static value => value.Note,
            static (value, next) => value with { Note = next });

    internal static PatchField<Schedule, string> TimeZoneField { get; } =
        Patch.Field("timeZone", TimeZone, TimeZoneLens);

    internal static PatchField<Schedule, TimeOnly> OpensAtField { get; } =
        Patch.Field("opensAt", TimeOfDay, OpensAtLens);

    internal static PatchField<Schedule, TimeOnly> ClosesAtField { get; } =
        Patch.Field("closesAt", TimeOfDay, ClosesAtLens);

    internal static PatchField<Schedule, Option<string>> NoteField { get; } =
        Patch.ClearableField(
            "note",
            Note,
            NoteLens,
            static value => value with { Note = Option<string>.None });

    internal static Contract<Schedule> ScheduleContract { get; } = BuildContract();

    internal static PatchSchema<Schedule> Schema { get; } = BuildSchema();

    internal static Schedule ValidSchedule { get; } = new(
        "Europe/Paris",
        new TimeOnly(9, 0),
        new TimeOnly(17, 0),
        Option.Some("front desk"));

    internal static Schedule InvalidNoteSchedule { get; } =
        ValidSchedule with { Note = Option.Some(string.Empty) };

    internal static Schedule InvalidRangeSchedule { get; } =
        ValidSchedule with { OpensAt = new TimeOnly(18, 0) };

    private static Contract<Schedule> BuildContract()
    {
        var fields = Contract.For<Schedule>();
        return Contract.Object(
                fields.Field("timeZone", TimeZone, static value => value.TimeZone),
                fields.Field("opensAt", TimeOfDay, static value => value.OpensAt),
                fields.Field("closesAt", TimeOfDay, static value => value.ClosesAt),
                fields.OptionalField("note", Contract.NonEmptyString, static value => value.Note),
                static (timeZone, opensAt, closesAt, note) =>
                    new Schedule(timeZone, opensAt, closesAt, note))
            .Ensure(
                static value => value.OpensAt < value.ClosesAt,
                CrossFieldDetail);
    }

    private static PatchSchema<Schedule> BuildSchema()
    {
        var created = PatchSchema<Schedule>.Create(
            ScheduleContract,
            TimeZoneField,
            OpensAtField,
            ClosesAtField,
            NoteField);

        if (created.TryGetError(out var limitation))
        {
            throw new InvalidOperationException(limitation.ToString());
        }

        created.TryGetValue(out var schema);
        return schema!;
    }

    internal static ScheduleErrorCode MapCode(DecodeErrorKind kind) =>
        kind switch
        {
            DecodeErrorKind.UnsupportedField => ScheduleErrorCode.UnsupportedField,
            DecodeErrorKind.DuplicateField => ScheduleErrorCode.DuplicateField,
            DecodeErrorKind.Null => ScheduleErrorCode.Null,
            DecodeErrorKind.WrongKind => ScheduleErrorCode.WrongKind,
            DecodeErrorKind.Malformed => ScheduleErrorCode.Malformed,
            DecodeErrorKind.Empty => ScheduleErrorCode.Empty,
            _ => ScheduleErrorCode.Invalid,
        };

    internal static string MapDetail(DecodeError error) =>
        error.Detail ?? error.Expected ?? error.Kind.ToString();
}
