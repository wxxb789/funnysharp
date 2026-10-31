using System.Text.Json;
using FunnySharp;
using FunnySharp.TestSupport.Patching;

namespace FunnySharp.Tests;

public sealed class PatchSchemaTests
{
    [Fact]
    public void DecodeKeepsAbsenceNullReplacementAndRejectionDistinct()
    {
        var absent = Decode("""{"timeZone":"Europe/Berlin"}""");
        Assert.True(absent.TryGetValue(out var absentPatch));
        Assert.Equal(1, absentPatch.Count);
        Assert.True(absentPatch.TryGet(ScheduleRules.TimeZoneField, out var timeZoneChange));
        Assert.Equal(PatchChange<string>.Set("Europe/Berlin"), timeZoneChange);
        Assert.False(absentPatch.TryGet(ScheduleRules.NoteField, out var noteChange));
        Assert.True(noteChange.IsUnchanged);

        var clearing = Decode("""{"note":null}""");
        Assert.True(clearing.TryGetValue(out var clearPatch));
        Assert.True(clearPatch.TryGet(ScheduleRules.NoteField, out var clearChange));
        Assert.True(clearChange.IsClear);

        var replacement = Decode("""{"note":"updated"}""");
        Assert.True(replacement.TryGetValue(out var setPatch));
        Assert.True(setPatch.TryGet(ScheduleRules.NoteField, out var setChange));
        Assert.Equal(PatchChange<Option<string>>.Set(Option.Some("updated")), setChange);

        var rejected = Decode("""{"ownerId":"employee-7"}""");
        Assert.True(rejected.TryGetErrors(out var rejectionErrors));
        var rejection = Assert.Single(rejectionErrors);
        Assert.Equal(DecodeErrorKind.UnsupportedField, rejection.Kind);
        Assert.Equal("ownerId", rejection.Path);
        Assert.Equal(ScheduleRules.UnsupportedFieldDetail, rejection.Detail);

        var nonClearable = Decode("""{"timeZone":null}""");
        Assert.True(nonClearable.TryGetErrors(out var nullErrors));
        var nullError = Assert.Single(nullErrors);
        Assert.Equal(DecodeErrorKind.Null, nullError.Kind);
        Assert.Equal("timeZone", nullError.Path);
        Assert.Equal(ScheduleRules.ClearNotAllowedDetail, nullError.Expected);
    }

    [Fact]
    public void DecodeRejectsDuplicateAndEmptyObjectPropertiesDeliberately()
    {
        var duplicate = Decode("""{"timeZone":"Europe/Paris","timeZone":"Europe/Berlin"}""");
        Assert.True(duplicate.TryGetErrors(out var duplicateErrors));
        var error = Assert.Single(duplicateErrors);
        Assert.Equal(DecodeErrorKind.DuplicateField, error.Kind);
        Assert.Equal("timeZone", error.Path);
        Assert.Equal(ScheduleRules.DuplicateFieldDetail, error.Detail);

        var emptyName = Decode("""{"":"value"}""");
        Assert.True(emptyName.TryGetErrors(out var emptyNameErrors));
        var emptyError = Assert.Single(emptyNameErrors);
        Assert.Equal(DecodeErrorKind.UnsupportedField, emptyError.Kind);
        Assert.Equal("[\"\"]", emptyError.Path);
    }

    [Fact]
    public void DecodeAccumulatesFieldErrorsInDocumentOrder()
    {
        var decoded = Decode("""{"timeZone":"","closesAt":"bad","ownerId":"x"}""");

        Assert.True(decoded.TryGetErrors(out var errors));
        Assert.Equal(
            new[]
            {
                ("timeZone", DecodeErrorKind.Empty),
                ("closesAt", DecodeErrorKind.Malformed),
                ("ownerId", DecodeErrorKind.UnsupportedField),
            },
            errors.Select(static error => (error.Path, error.Kind)).ToArray());
        Assert.Equal("a non-empty string", errors[0].Expected);
        Assert.Equal("bad", errors[1].Detail);
    }

    [Fact]
    public void DecodeRejectsNonObjectRootsWithDistinctErrors()
    {
        var rootNull = Decode("null");
        Assert.True(rootNull.TryGetErrors(out var nullErrors));
        Assert.Equal(DecodeErrorKind.Null, Assert.Single(nullErrors).Kind);
        Assert.Equal(string.Empty, nullErrors[0].Path);

        var rootArray = Decode("[]");
        Assert.True(rootArray.TryGetErrors(out var arrayErrors));
        var arrayError = Assert.Single(arrayErrors);
        Assert.Equal(DecodeErrorKind.WrongKind, arrayError.Kind);
        Assert.Equal("an object", arrayError.Expected);
        Assert.Equal("found Array", arrayError.Detail);
    }

    [Fact]
    public void EmptyObjectIsAValidEmptyPatch()
    {
        var decoded = Decode("{}");

        Assert.True(decoded.TryGetValue(out var patch));
        Assert.True(patch.IsEmpty);
    }

    [Fact]
    public void ValidateChecksTheCandidateStateIncludingUnchangedFields()
    {
        var current = ScheduleRules.ValidSchedule;
        var patch = Patch<Schedule>.Empty
            .With(ScheduleRules.ClosesAtField, PatchChange<TimeOnly>.Set(new TimeOnly(18, 30)));

        var validated = ScheduleRules.Schema.Validate(patch, current);

        Assert.True(validated.TryGetValue(out var application));
        Assert.Same(current, application.Source);
        Assert.Equal(new TimeOnly(18, 30), application.Candidate.ClosesAt);
        Assert.Equal(current.OpensAt, application.Candidate.OpensAt);
        Assert.Equal(current.Note, application.Candidate.Note);
        Assert.False(application.IsIdentity);
        Assert.Single(application.Changes);
        Assert.Equal("closesAt", application.Changes[0].Name);
        Assert.Equal(PatchChangeKind.Set, application.Changes[0].Kind);
    }

    [Fact]
    public void CrossFieldValidationUsesExistingAndSubmittedValues()
    {
        var current = ScheduleRules.ValidSchedule;
        var patch = Patch<Schedule>.Empty
            .With(ScheduleRules.OpensAtField, PatchChange<TimeOnly>.Set(new TimeOnly(18, 0)));

        var validated = ScheduleRules.Schema.Validate(patch, current);

        Assert.True(validated.TryGetErrors(out var errors));
        var error = Assert.Single(errors);
        Assert.Equal(string.Empty, error.Path);
        Assert.Equal(DecodeErrorKind.InvalidValue, error.Kind);
        Assert.Equal(ScheduleRules.CrossFieldDetail, error.Detail);
    }

    [Fact]
    public void CandidateValidationReportsUnchangedInvalidFields()
    {
        var noteCurrent = ScheduleRules.InvalidNoteSchedule;
        var notePatch = Patch<Schedule>.Empty
            .With(ScheduleRules.TimeZoneField, PatchChange<string>.Set("Europe/London"));

        var noteValidated = ScheduleRules.Schema.Validate(notePatch, noteCurrent);
        Assert.True(noteValidated.TryGetErrors(out var noteErrors));
        var noteError = Assert.Single(noteErrors);
        Assert.Equal("note", noteError.Path);
        Assert.Equal(DecodeErrorKind.Empty, noteError.Kind);

        var rangeCurrent = ScheduleRules.InvalidRangeSchedule;
        var rangePatch = Patch<Schedule>.Empty
            .With(ScheduleRules.TimeZoneField, PatchChange<string>.Set("Europe/London"));

        var rangeValidated = ScheduleRules.Schema.Validate(rangePatch, rangeCurrent);
        Assert.True(rangeValidated.TryGetErrors(out var rangeErrors));
        var rangeError = Assert.Single(rangeErrors);
        Assert.Equal(string.Empty, rangeError.Path);
        Assert.Equal(ScheduleRules.CrossFieldDetail, rangeError.Detail);
    }

    [Fact]
    public void EmptyPatchValidationStillChecksCurrentStateAndPreservesIdentity()
    {
        var valid = ScheduleRules.Schema.Validate(Patch<Schedule>.Empty, ScheduleRules.ValidSchedule);
        Assert.True(valid.TryGetValue(out var validApplication));
        Assert.Same(validApplication.Source, validApplication.Candidate);
        Assert.True(validApplication.IsIdentity);

        var invalid = ScheduleRules.Schema.Validate(Patch<Schedule>.Empty, ScheduleRules.InvalidRangeSchedule);
        Assert.True(invalid.TryGetErrors(out var errors));
        Assert.Equal(ScheduleRules.CrossFieldDetail, Assert.Single(errors).Detail);
    }

    [Fact]
    public void SchemaExposesAllowedFieldsAndDeterministicExplanation()
    {
        var schema = ScheduleRules.Schema;

        Assert.Same(ScheduleRules.ScheduleContract, schema.CandidateContract);
        Assert.Equal(
            new[] { "timeZone", "opensAt", "closesAt", "note" },
            schema.Fields.Select(static field => field.Name).ToArray());
        Assert.False(schema.Fields[0].AllowsClear);
        Assert.True(schema.Fields[3].AllowsClear);
        Assert.Contains("patch fields: timeZone", schema.Explain(), StringComparison.Ordinal);
        Assert.Contains("candidate:", schema.Explain(), StringComparison.Ordinal);
    }

    [Fact]
    public void SchemaCreationRejectsInvalidFieldSetsAndReportsMissingDecoders()
    {
        Assert.Throws<ArgumentException>(
            () => PatchSchema<Schedule>.Create(ScheduleRules.ScheduleContract));
        Assert.Throws<ArgumentNullException>(
            () => PatchSchema<Schedule>.Create(ScheduleRules.ScheduleContract, null!));
        Assert.Throws<ArgumentException>(
            () => PatchSchema<Schedule>.Create(
                ScheduleRules.ScheduleContract,
                ScheduleRules.TimeZoneField,
                ScheduleRules.TimeZoneField));

        var opaque = Contract.From<int>(
            "opaque integer",
            static value => Validation<int, string>.Valid(value));
        var field = Patch.Field<Schedule, int>(
            "capacity",
            opaque,
            Lens.Create<Schedule, int>(
                static _ => 0,
                static (value, _) => value));

        var created = PatchSchema<Schedule>.Create(ScheduleRules.ScheduleContract, field);

        Assert.True(created.TryGetError(out var limitation));
        Assert.Equal(ContractDerivation.Decoding, limitation.Derivation);
        Assert.Equal("capacity", limitation.Path);
    }

    [Fact]
    public void ValidateRejectsPatchFieldsFromAnotherSchema()
    {
        var foreignField = Patch.Field<Schedule, string>(
            "timeZone",
            Contract.NonEmptyString,
            Lens.Create<Schedule, string>(
                static value => value.TimeZone,
                static (value, next) => value with { TimeZone = next.ToUpperInvariant() }));
        var patch = Patch<Schedule>.Empty
            .With(foreignField, PatchChange<string>.Set("europe/london"));

        var validated = ScheduleRules.Schema.Validate(patch, ScheduleRules.ValidSchedule);

        Assert.True(validated.TryGetErrors(out var errors));
        var error = Assert.Single(errors);
        Assert.Equal(DecodeErrorKind.UnsupportedField, error.Kind);
        Assert.Equal("timeZone", error.Path);
        Assert.Equal("the field is not part of this schema", error.Detail);
    }

    [Fact]
    public void UninitializedLensesAreRejectedWhenFieldsAreCreated()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => Patch.Field<Schedule, int>("capacity", Contract.Int32, default));

        Assert.Contains("uninitialized", exception.Message, StringComparison.Ordinal);
    }

    private static Validation<Patch<Schedule>, DecodeError> Decode(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ScheduleRules.Schema.Decode(document.RootElement);
    }
}
