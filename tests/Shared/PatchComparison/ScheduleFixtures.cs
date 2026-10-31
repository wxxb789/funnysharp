namespace FunnySharp.TestSupport.Patching;

internal sealed record ScheduleFixture(
    string Name,
    string Json,
    Schedule? Current = null);

internal static class ScheduleFixtures
{
    internal static Schedule Current => ScheduleRules.ValidSchedule;

    internal static readonly ScheduleFixture[] All =
    [
        new("unchanged-and-replacement", """{"timeZone":"Europe/Berlin","closesAt":"18:30"}"""),
        new("explicit-clear", """{"note":null}"""),
        new("empty-object", "{}"),
        new("rejected-field", """{"ownerId":"employee-7"}"""),
        new("non-clearable-null", """{"timeZone":null}"""),
        new("cross-field-invalid", """{"opensAt":"18:00"}"""),
        new("cross-field-valid", """{"closesAt":"20:00"}"""),
        new("accumulated-errors", """{"timeZone":"","closesAt":"bad","ownerId":"x"}"""),
        new("unknown-and-null", """{"ownerId":"x","timeZone":null}"""),
        new("root-null", "null"),
        new("root-array", "[]"),
        new("duplicate-field", """{"timeZone":"Europe/Paris","timeZone":"Europe/Berlin"}"""),
        new("same-value", """{"timeZone":"Europe/Paris"}"""),
        new("unchanged-invalid-note", """{"timeZone":"Europe/London"}""", ScheduleRules.InvalidNoteSchedule),
        new("unchanged-invalid-range", """{"timeZone":"Europe/London"}""", ScheduleRules.InvalidRangeSchedule),
        new("note-empty", """{"note":""}"""),
        new("time-zone-wrong-kind", """{"timeZone":42}"""),
        new("note-wrong-kind", """{"note":42}"""),
        new("note-replacement", """{"note":"updated"}"""),
    ];
}
