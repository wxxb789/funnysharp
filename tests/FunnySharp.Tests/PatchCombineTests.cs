using FunnySharp;
using FunnySharp.TestSupport.Patching;

namespace FunnySharp.Tests;

public sealed class PatchCombineTests
{
    [Fact]
    public void DisjointChangesCombineIntoOneOrderedPatch()
    {
        var left = Patch<Schedule>.Empty
            .With(ScheduleRules.TimeZoneField, PatchChange<string>.Set("Europe/Berlin"));
        var right = Patch<Schedule>.Empty
            .With(ScheduleRules.ClosesAtField, PatchChange<TimeOnly>.Set(new TimeOnly(18, 0)));

        var combined = left.Combine(right);

        Assert.True(combined.TryGetValue(out var merged));
        Assert.Equal(2, merged.Count);
        Assert.Equal("timeZone", merged.Entries[0].Name);
        Assert.Equal("closesAt", merged.Entries[1].Name);

        var application = merged.ApplyTo(ScheduleRules.ValidSchedule);
        Assert.Equal("Europe/Berlin", application.Candidate.TimeZone);
        Assert.Equal(new TimeOnly(18, 0), application.Candidate.ClosesAt);
        Assert.Equal(ScheduleRules.ValidSchedule.OpensAt, application.Candidate.OpensAt);
        Assert.Equal(ScheduleRules.ValidSchedule.Note, application.Candidate.Note);
    }

    [Fact]
    public void AnyRepeatedFieldIsAConflictEvenWhenTheWritesAreEqual()
    {
        var left = Patch<Schedule>.Empty
            .With(ScheduleRules.TimeZoneField, PatchChange<string>.Set("Europe/Paris"));
        var right = Patch<Schedule>.Empty
            .With(ScheduleRules.TimeZoneField, PatchChange<string>.Set("Europe/Paris"));

        var combined = left.Combine(right);

        Assert.True(combined.TryGetErrors(out var conflicts));
        var conflict = Assert.Single(conflicts);
        Assert.Equal("timeZone", conflict.Field);
        Assert.Equal(PatchChangeKind.Set, conflict.First.Kind);
        Assert.Equal("Europe/Paris", conflict.First.Value);
        Assert.Equal(PatchChangeKind.Set, conflict.Second.Kind);
        Assert.Equal("Europe/Paris", conflict.Second.Value);
        Assert.False(combined.TryGetValue(out _));

        var repeated = left.Combine(right);
        Assert.True(repeated.TryGetErrors(out var repeatedConflicts));
        Assert.Equal(conflict, repeatedConflicts![0]);
        Assert.Equal(conflict.GetHashCode(), repeatedConflicts[0].GetHashCode());
    }

    [Fact]
    public void CombineReportsEveryConflictingFieldWithoutMergingTheRest()
    {
        var left = Patch<Schedule>.Empty
            .With(ScheduleRules.TimeZoneField, PatchChange<string>.Set("Europe/Berlin"))
            .With(ScheduleRules.OpensAtField, PatchChange<TimeOnly>.Set(new TimeOnly(10, 0)));
        var right = Patch<Schedule>.Empty
            .With(ScheduleRules.OpensAtField, PatchChange<TimeOnly>.Set(new TimeOnly(11, 0)))
            .With(ScheduleRules.NoteField, PatchChange<Option<string>>.Clear)
            .With(ScheduleRules.TimeZoneField, PatchChange<string>.Set("Europe/London"));

        var combined = left.Combine(right);

        Assert.True(combined.TryGetErrors(out var conflicts));
        Assert.Equal(
            new[] { "timeZone", "opensAt" },
            conflicts.Select(static conflict => conflict.Field).ToArray());
        Assert.False(combined.TryGetValue(out _));
    }

    [Fact]
    public void CombiningWithAnEmptyPatchPreservesTheOtherPatch()
    {
        var patch = Patch<Schedule>.Empty
            .With(ScheduleRules.NoteField, PatchChange<Option<string>>.Clear);

        var leftEmpty = Patch<Schedule>.Empty.Combine(patch);
        var rightEmpty = patch.Combine(Patch<Schedule>.Empty);

        Assert.True(leftEmpty.TryGetValue(out var fromLeft));
        Assert.True(rightEmpty.TryGetValue(out var fromRight));
        Assert.Equal(patch, fromLeft);
        Assert.Equal(patch, fromRight);
    }

    [Fact]
    public void ConflictingClearAndSetWritesArePresentedAsAConflict()
    {
        var clearing = Patch<Schedule>.Empty
            .With(ScheduleRules.NoteField, PatchChange<Option<string>>.Clear);
        var setting = Patch<Schedule>.Empty
            .With(
                ScheduleRules.NoteField,
                PatchChange<Option<string>>.Set(Option.Some("updated")));

        var combined = clearing.Combine(setting);

        Assert.True(combined.TryGetErrors(out var conflicts));
        var conflict = Assert.Single(conflicts);
        Assert.Equal("note", conflict.Field);
        Assert.Equal(PatchChangeKind.Clear, conflict.First.Kind);
        Assert.Equal(PatchChangeKind.Set, conflict.Second.Kind);
    }
}
