using FunnySharp;

namespace FunnySharp.Tests;

public sealed class PatchTests
{
    private static readonly Lens<Profile, string> NameLens = Lens.Create<Profile, string>(
        static value => value.Name,
        static (value, next) => value with { Name = next });

    private static readonly Lens<Profile, int> AgeLens = Lens.Create<Profile, int>(
        static value => value.Age,
        static (value, next) => value with { Age = next });

    private static readonly Lens<Profile, Option<string>> NoteLens =
        Lens.Create<Profile, Option<string>>(
            static value => value.Note,
            static (value, next) => value with { Note = next });

    private static readonly PatchField<Profile, string> NameField =
        Patch.Field("name", Contract.NonEmptyString, NameLens);

    private static readonly PatchField<Profile, int> AgeField =
        Patch.Field("age", Contract.Int32Range(1, 200), AgeLens);

    private static readonly PatchField<Profile, Option<string>> NoteField =
        Patch.ClearableField(
            "note",
            Contract.Optional(Contract.NonEmptyString),
            NoteLens,
            static value => value with { Note = Option<string>.None });

    [Fact]
    public void DefaultChangeIsUnchangedAndSetRejectsNull()
    {
        PatchChange<string> absent = default;
        Assert.Equal(PatchChangeKind.Unchanged, absent.Kind);
        Assert.True(absent.IsUnchanged);
        Assert.True(absent.Value.IsNone);
        Assert.False(absent.TryGetValue(out _));

        var clear = PatchChange<string>.Clear;
        Assert.True(clear.IsClear);
        Assert.True(clear.Value.IsNone);
        Assert.False(clear.TryGetValue(out _));

        var set = PatchChange<string>.Set("Ada");
        Assert.True(set.IsSet);
        Assert.True(set.TryGetValue(out var replacement));
        Assert.Equal("Ada", replacement);
        Assert.Equal(Option.Some("Ada"), set.Value);

        Assert.Throws<ArgumentNullException>(() => PatchChange<string?>.Set(null!));
        Assert.Equal(PatchChange<string>.Set("Ada"), PatchChange<string>.Set("Ada"));
        Assert.NotEqual(PatchChange<string>.Set("Ada"), PatchChange<string>.Set("Grace"));
        Assert.Equal(PatchChange<string>.Clear, PatchChange<string>.Clear);
        Assert.NotEqual(PatchChange<string>.Clear, PatchChange<string>.Unchanged);
        Assert.Equal("Clear", PatchChange<string>.Clear.ToString());
        Assert.Equal("Set(Ada)", PatchChange<string>.Set("Ada").ToString());
    }

    [Fact]
    public void OptionValueFieldsCanBeReplacedWithNoneWithoutUsingNull()
    {
        var replacement = PatchChange<Option<string>>.Set(Option<string>.None);

        Assert.True(replacement.Value.IsSome);
        Assert.True(replacement.Value.GetValueOrDefault().IsNone);
    }

    [Fact]
    public void DefaultPatchIsEmptyAndRecordsChangesInOrder()
    {
        Patch<Profile> empty = default;
        Assert.True(empty.IsEmpty);
        Assert.Equal(0, empty.Count);
        Assert.Empty(empty.Entries);
        Assert.Equal(Patch<Profile>.Empty, empty);

        var patch = empty
            .With(NameField, PatchChange<string>.Set("Ada"))
            .With(AgeField, PatchChange<int>.Set(37));

        Assert.False(patch.IsEmpty);
        Assert.Equal(2, patch.Count);
        Assert.Equal(2, patch.Entries.Count);
        Assert.Equal("name", patch.Entries[0].Name);
        Assert.Equal(PatchChangeKind.Set, patch.Entries[0].Kind);
        Assert.Equal("Ada", patch.Entries[0].Value);
        Assert.Equal("age", patch.Entries[1].Name);
        Assert.Equal(PatchChangeKind.Set, patch.Entries[1].Kind);
        Assert.Equal(37, patch.Entries[1].Value);

        Assert.True(patch.TryGet(NameField, out var nameChange));
        Assert.Equal(PatchChange<string>.Set("Ada"), nameChange);
        Assert.False(patch.TryGet(NoteField, out var noteChange));
        Assert.True(noteChange.IsUnchanged);
    }

    [Fact]
    public void AddingAnUnchangedDuplicateOrUnsupportedClearIsRejected()
    {
        var patch = Patch<Profile>.Empty.With(NameField, PatchChange<string>.Set("Ada"));

        Assert.Throws<ArgumentException>(
            () => patch.With(AgeField, PatchChange<int>.Unchanged));
        Assert.Throws<ArgumentException>(
            () => patch.With(NameField, PatchChange<string>.Set("Grace")));
        Assert.Throws<ArgumentException>(
            () => Patch<Profile>.Empty.With(NameField, PatchChange<string>.Clear));
        Assert.Throws<ArgumentNullException>(
            () => patch.With<string>(null!, PatchChange<string>.Set("Grace")));
    }

    [Fact]
    public void ApplyingAChangeCreatesACandidateAndLeavesTheSourceUnchanged()
    {
        var source = new Profile("Ada", 36, Option.Some("front desk"));
        var patch = Patch<Profile>.Empty
            .With(NameField, PatchChange<string>.Set("Grace"))
            .With(NoteField, PatchChange<Option<string>>.Clear);

        var application = patch.ApplyTo(source);

        Assert.Same(source, application.Source);
        Assert.Equal(new Profile("Grace", 36, Option<string>.None), application.Candidate);
        Assert.Equal("Ada", source.Name);
        Assert.True(source.Note.IsSome);
        Assert.False(application.IsIdentity);
        Assert.Equal(2, application.Changes.Count);
        Assert.Equal("note", application.Changes[1].Name);
        Assert.Equal(PatchChangeKind.Clear, application.Changes[1].Kind);
        Assert.Null(application.Changes[1].Value);
    }

    [Fact]
    public void EmptyAndSameValueApplicationsDiscloseIdentity()
    {
        var source = new Profile("Ada", 36, Option<string>.None);

        var empty = Patch<Profile>.Empty.ApplyTo(source);
        Assert.Same(source, empty.Source);
        Assert.Same(source, empty.Candidate);
        Assert.True(empty.IsIdentity);
        Assert.Empty(empty.Changes);

        var same = Patch<Profile>.Empty
            .With(NameField, PatchChange<string>.Set("Ada"))
            .ApplyTo(source);
        Assert.Same(source, same.Source);
        Assert.NotSame(source, same.Candidate);
        Assert.Equal(source, same.Candidate);
        Assert.True(same.IsIdentity);
        Assert.Single(same.Changes);
    }

    [Fact]
    public void CallbackFailurePropagatesAndLaterCallbacksDoNotRun()
    {
        var calls = 0;
        var throwingLens = Lens.Create<Profile, string>(
            static value => value.Name,
            (_, _) =>
            {
                calls++;
                throw new InvalidOperationException("setter failed");
            });
        var throwingField = Patch.Field("name", Contract.NonEmptyString, throwingLens);
        var laterCalls = 0;
        var laterLens = Lens.Create<Profile, int>(
            static value => value.Age,
            (value, next) =>
            {
                laterCalls++;
                return value with { Age = next };
            });
        var laterField = Patch.Field("age", Contract.Int32, laterLens);

        var patch = Patch<Profile>.Empty
            .With(throwingField, PatchChange<string>.Set("Grace"))
            .With(laterField, PatchChange<int>.Set(37));

        var exception = Assert.Throws<InvalidOperationException>(
            () => patch.ApplyTo(new Profile("Ada", 36, Option<string>.None)));

        Assert.Equal("setter failed", exception.Message);
        Assert.Equal(1, calls);
        Assert.Equal(0, laterCalls);
    }

    [Fact]
    public void ClearCallbackFailurePropagatesUnchanged()
    {
        var clearCalls = 0;
        var field = Patch.ClearableField(
            "note",
            Contract.Optional(Contract.NonEmptyString),
            NoteLens,
            _ =>
            {
                clearCalls++;
                throw new InvalidOperationException("clear failed");
            });
        var patch = Patch<Profile>.Empty
            .With(field, PatchChange<Option<string>>.Clear);
        var source = new Profile("Ada", 36, Option.Some("front desk"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => patch.ApplyTo(source));

        Assert.Equal("clear failed", exception.Message);
        Assert.Equal(1, clearCalls);
        Assert.True(source.Note.IsSome);
    }

    [Fact]
    public void PatchEqualityIsOrderSensitiveAndUsesFieldBindings()
    {
        var first = Patch<Profile>.Empty
            .With(NameField, PatchChange<string>.Set("Ada"))
            .With(AgeField, PatchChange<int>.Set(37));
        var same = Patch<Profile>.Empty
            .With(NameField, PatchChange<string>.Set("Ada"))
            .With(AgeField, PatchChange<int>.Set(37));
        var reversed = Patch<Profile>.Empty
            .With(AgeField, PatchChange<int>.Set(37))
            .With(NameField, PatchChange<string>.Set("Ada"));
        var foreignField = Patch.Field(
            "name",
            Contract.NonEmptyString,
            Lens.Create<Profile, string>(
                static value => value.Name,
                static (value, next) => value with { Name = next }));

        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.Equal(first.Entries[0], same.Entries[0]);
        Assert.Equal(first.Entries[0].GetHashCode(), same.Entries[0].GetHashCode());
        Assert.NotEqual(first, reversed);
        Assert.False(first.TryGet(foreignField, out _));
        Assert.NotEqual(first, Patch<Profile>.Empty);
    }

    private sealed record Profile(string Name, int Age, Option<string> Note);
}
