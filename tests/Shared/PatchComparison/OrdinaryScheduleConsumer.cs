using System.Globalization;
using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.Patching;

internal static class OrdinaryScheduleConsumer
{
    private static readonly HashSet<string> AllowedFields =
        new(StringComparer.Ordinal) { "timeZone", "opensAt", "closesAt", "note" };

    internal static ScheduleUpdateResult Update(JsonElement input, Schedule current)
    {
        if (input.ValueKind == JsonValueKind.Null)
        {
            return Failure(new ScheduleError(string.Empty, ScheduleErrorCode.Null, "an object"));
        }

        if (input.ValueKind != JsonValueKind.Object)
        {
            return Failure(
                new ScheduleError(
                    string.Empty,
                    ScheduleErrorCode.WrongKind,
                    $"found {input.ValueKind}"));
        }

        var errors = new List<ScheduleError>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var timeZone = current.TimeZone;
        var opensAt = current.OpensAt;
        var closesAt = current.ClosesAt;
        var note = current.Note;

        foreach (var property in input.EnumerateObject())
        {
            if (!AllowedFields.Contains(property.Name))
            {
                errors.Add(
                    new ScheduleError(
                        property.Name,
                        ScheduleErrorCode.UnsupportedField,
                        ScheduleRules.UnsupportedFieldDetail));
                continue;
            }

            if (!seen.Add(property.Name))
            {
                errors.Add(
                    new ScheduleError(
                        property.Name,
                        ScheduleErrorCode.DuplicateField,
                        ScheduleRules.DuplicateFieldDetail));
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.Null)
            {
                if (property.Name == "note")
                {
                    note = Option<string>.None;
                }
                else
                {
                    errors.Add(
                        new ScheduleError(
                            property.Name,
                            ScheduleErrorCode.Null,
                            ScheduleRules.ClearNotAllowedDetail));
                }

                continue;
            }

            switch (property.Name)
            {
                case "timeZone":
                    if (property.Value.ValueKind != JsonValueKind.String)
                    {
                        errors.Add(
                            new ScheduleError(
                                property.Name,
                                ScheduleErrorCode.WrongKind,
                                $"found {property.Value.ValueKind}"));
                    }
                    else
                    {
                        timeZone = property.Value.GetString()!;
                        if (timeZone.Length == 0)
                        {
                            errors.Add(
                                new ScheduleError(
                                    property.Name,
                                    ScheduleErrorCode.Empty,
                                    "a non-empty string"));
                        }
                    }

                    break;

                case "opensAt":
                    if (TryReadTime(property.Name, property.Value, errors, out var newOpensAt))
                    {
                        opensAt = newOpensAt;
                    }

                    break;

                case "closesAt":
                    if (TryReadTime(property.Name, property.Value, errors, out var newClosesAt))
                    {
                        closesAt = newClosesAt;
                    }

                    break;

                case "note":
                    if (property.Value.ValueKind != JsonValueKind.String)
                    {
                        errors.Add(
                            new ScheduleError(
                                property.Name,
                                ScheduleErrorCode.WrongKind,
                                $"found {property.Value.ValueKind}"));
                    }
                    else
                    {
                        var text = property.Value.GetString()!;
                        if (text.Length == 0)
                        {
                            errors.Add(
                                new ScheduleError(
                                    property.Name,
                                    ScheduleErrorCode.Empty,
                                    "a non-empty string"));
                        }
                        else
                        {
                            note = Option.Some(text);
                        }
                    }

                    break;
            }
        }

        if (errors.Count != 0)
        {
            return new ScheduleUpdateResult(null, errors);
        }

        var candidate = new Schedule(timeZone, opensAt, closesAt, note);

        if (string.IsNullOrEmpty(candidate.TimeZone))
        {
            errors.Add(new ScheduleError("timeZone", ScheduleErrorCode.Empty, "a non-empty string"));
        }

        if (candidate.Note.TryGetValue(out var noteValue) && noteValue.Length == 0)
        {
            errors.Add(new ScheduleError("note", ScheduleErrorCode.Empty, "a non-empty string"));
        }

        if (candidate.OpensAt >= candidate.ClosesAt)
        {
            errors.Add(
                new ScheduleError(
                    string.Empty,
                    ScheduleErrorCode.Invalid,
                    ScheduleRules.CrossFieldDetail));
        }

        return errors.Count == 0
            ? new ScheduleUpdateResult(candidate, [])
            : new ScheduleUpdateResult(null, errors);
    }

    private static bool TryReadTime(
        string field,
        JsonElement value,
        List<ScheduleError> errors,
        out TimeOnly parsed)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add(
                new ScheduleError(
                    field,
                    ScheduleErrorCode.WrongKind,
                    $"found {value.ValueKind}"));
            parsed = default;
            return false;
        }

        var text = value.GetString()!;
        if (TimeOnly.TryParseExact(
                text,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsed))
        {
            return true;
        }

        errors.Add(new ScheduleError(field, ScheduleErrorCode.Malformed, text));
        return false;
    }

    private static ScheduleUpdateResult Failure(ScheduleError error) =>
        new(null, [error]);
}
