using System.Text.Json;

namespace FunnySharp.TestSupport.Patching;

internal static class PatchScheduleConsumer
{
    internal static ScheduleUpdateResult Update(JsonElement input, Schedule current)
    {
        var decoded = ScheduleRules.Schema.Decode(input);
        if (!decoded.TryGetValue(out var patch))
        {
            decoded.TryGetErrors(out var decodeErrors);
            return new ScheduleUpdateResult(null, Map(decodeErrors!));
        }

        var validated = ScheduleRules.Schema.Validate(patch, current);
        if (!validated.TryGetValue(out var application))
        {
            validated.TryGetErrors(out var validationErrors);
            return new ScheduleUpdateResult(null, Map(validationErrors!));
        }

        return new ScheduleUpdateResult(application.Candidate, []);
    }

    internal static IReadOnlyList<ScheduleError> Map(IReadOnlyList<DecodeError> errors)
    {
        var mapped = new ScheduleError[errors.Count];
        for (var index = 0; index < errors.Count; index++)
        {
            var error = errors[index];
            mapped[index] = new ScheduleError(
                error.Path,
                ScheduleRules.MapCode(error.Kind),
                ScheduleRules.MapDetail(error));
        }

        return mapped;
    }
}
