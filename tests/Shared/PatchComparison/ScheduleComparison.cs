using System.Text.Json;

namespace FunnySharp.TestSupport.Patching;

internal static class ScheduleComparison
{
    internal static void Verify()
    {
        foreach (var fixture in ScheduleFixtures.All)
        {
            using var document = JsonDocument.Parse(fixture.Json);
            var current = fixture.Current ?? ScheduleFixtures.Current;
            var patch = PatchScheduleConsumer.Update(document.RootElement, current);
            var ordinary = OrdinaryScheduleConsumer.Update(document.RootElement, current);

            Compare(fixture.Name, patch, ordinary);
        }

        VerifyExpected();
    }

    private static void VerifyExpected()
    {
        var required = new[]
        {
            new ScheduleError(
                "ownerId",
                ScheduleErrorCode.UnsupportedField,
                ScheduleRules.UnsupportedFieldDetail),
        };
        Expect("rejected-field", ScheduleFixtures.Current, required);

        Expect(
            "non-clearable-null",
            ScheduleFixtures.Current,
            new ScheduleError(
                "timeZone",
                ScheduleErrorCode.Null,
                ScheduleRules.ClearNotAllowedDetail));

        Expect(
            "cross-field-invalid",
            ScheduleFixtures.Current,
            new ScheduleError(
                string.Empty,
                ScheduleErrorCode.Invalid,
                ScheduleRules.CrossFieldDetail));

        Expect(
            "duplicate-field",
            ScheduleFixtures.Current,
            new ScheduleError(
                "timeZone",
                ScheduleErrorCode.DuplicateField,
                ScheduleRules.DuplicateFieldDetail));

        Expect(
            "note-empty",
            ScheduleFixtures.Current,
            new ScheduleError("note", ScheduleErrorCode.Empty, "a non-empty string"));

        var valid = Update("cross-field-valid", ScheduleFixtures.Current);
        if (!valid.IsSuccess
            || valid.Candidate!.ClosesAt != new TimeOnly(20, 0)
            || valid.Candidate.OpensAt != ScheduleFixtures.Current.OpensAt
            || !valid.Candidate.Note.Equals(ScheduleFixtures.Current.Note))
        {
            throw new InvalidOperationException(
                "The cross-field-valid fixture did not retain unrelated fields.");
        }

        var cleared = Update("explicit-clear", ScheduleFixtures.Current);
        if (!cleared.IsSuccess || cleared.Candidate!.Note.IsSome)
        {
            throw new InvalidOperationException(
                "The explicit-clear fixture did not clear the optional note.");
        }

        var identity = Update("empty-object", ScheduleFixtures.Current);
        if (!identity.IsSuccess || !ReferenceEquals(identity.Candidate, ScheduleFixtures.Current))
        {
            throw new InvalidOperationException(
                "The empty-object fixture did not preserve source identity.");
        }
    }

    private static void Expect(
        string fixtureName,
        Schedule current,
        params ScheduleError[] expected)
    {
        var result = Update(fixtureName, current);
        if (result.IsSuccess || !result.Errors.SequenceEqual(expected))
        {
            throw new InvalidOperationException(
                $"{fixtureName}: expected [{Format(expected)}], actual [{Format(result.Errors)}].");
        }
    }

    private static ScheduleUpdateResult Update(string fixtureName, Schedule current)
    {
        var fixture = ScheduleFixtures.All.Single(item => item.Name == fixtureName);
        using var document = JsonDocument.Parse(fixture.Json);
        return PatchScheduleConsumer.Update(document.RootElement, current);
    }

    private static void Compare(
        string fixture,
        ScheduleUpdateResult left,
        ScheduleUpdateResult right)
    {
        if (left.IsSuccess != right.IsSuccess)
        {
            throw new InvalidOperationException(
                $"{fixture}: patch success={left.IsSuccess}, ordinary success={right.IsSuccess}.");
        }

        if (left.IsSuccess)
        {
            if (left.Candidate != right.Candidate)
            {
                throw new InvalidOperationException(
                    $"{fixture}: patch and ordinary produced different candidates.");
            }

            return;
        }

        if (!left.Errors.SequenceEqual(right.Errors))
        {
            throw new InvalidOperationException(
                $"{fixture}: patch and ordinary produced different errors.\n" +
                $"patch:    {Format(left.Errors)}\n" +
                $"ordinary: {Format(right.Errors)}");
        }
    }

    private static string Format(IReadOnlyList<ScheduleError> errors) =>
        string.Join(
            "; ",
            errors.Select(static error => $"{error.Path}|{error.Code}|{error.Detail}"));
}
