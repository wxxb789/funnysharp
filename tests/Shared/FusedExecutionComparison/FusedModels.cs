using System.Globalization;
using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.FusedExecution;

public sealed record OrderLine(string Sku, string? Note, int Quantity, decimal UnitPrice);

public sealed record OrderInput(int Id, string Customer, decimal Total, IReadOnlyList<string> Tags);

public sealed record OrderOptional(int Id, Option<string> Customer, Option<string> Note);

public sealed record OverflowLine(int Quantity, long Big);

public sealed record ScalarGroup(long Big, double Ratio, bool Flag, string Text);

public sealed record TemporalGroup(Guid Id, DateTime When, DateTimeOffset At, JsonElement Payload);

public sealed record PlainListGroup(string Label, IReadOnlyList<int> Values);

public sealed class ListOverloadProbe
{
    public ListOverloadProbe(IReadOnlyList<string> values) => Marker = "readonly";

    public ListOverloadProbe(List<string> values) => Marker = "mutable";

    public string Marker { get; }
}

public sealed class ConstructionProbe
{
    public ConstructionProbe(int id, string name)
    {
        Constructions++;
        Id = id;
        Name = name;
    }

    public static int Constructions { get; private set; }

    public static void ResetConstructions() => Constructions = 0;

    public int Id { get; }

    public string Name { get; }
}

public sealed class ExplosiveProbe
{
    public ExplosiveProbe(int id, string name)
    {
        Constructions++;
        _ = id;
        _ = name;
        throw new InvalidOperationException("construction failed");
    }

    public static int Constructions { get; private set; }

    public static void ResetConstructions() => Constructions = 0;
}

public enum SourceFault
{
    MoveNext,
    Current,
    Dispose,
}

public sealed class FaultingSequence<T> : IEnumerable<T>
{
    private readonly IReadOnlyList<T> items;
    private readonly SourceFault fault;

    public FaultingSequence(IReadOnlyList<T> items, SourceFault fault)
    {
        this.items = items;
        this.fault = fault;
    }

    public int Enumerations { get; private set; }

    public int MoveNextCalls { get; private set; }

    public int CurrentCalls { get; private set; }

    public int DisposeCalls { get; private set; }

    public IEnumerator<T> GetEnumerator()
    {
        Enumerations++;
        return new Enumerator(this);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class Enumerator(FaultingSequence<T> owner) : IEnumerator<T>
    {
        private int index = -1;
        private bool disposed;

        public T Current
        {
            get
            {
                owner.CurrentCalls++;
                if (owner.fault == SourceFault.Current)
                {
                    throw new InvalidOperationException("current failed");
                }

                return owner.items[index];
            }
        }

        object System.Collections.IEnumerator.Current => Current!;

        public bool MoveNext()
        {
            var call = owner.MoveNextCalls++;
            if (owner.fault == SourceFault.MoveNext && call == 0)
            {
                throw new InvalidOperationException("move failed");
            }

            index++;
            return index < owner.items.Count;
        }

        public void Reset() => index = -1;

        public void Dispose()
        {
            owner.DisposeCalls++;
            if (owner.fault == SourceFault.Dispose && !disposed)
            {
                disposed = true;
                throw new InvalidOperationException("dispose failed");
            }
        }
    }
}

public static class FusedFixtures
{
    public static OrderLine[] CreateLines(int count)
    {
        var lines = new OrderLine[count];
        for (var index = 0; index < lines.Length; index++)
        {
            lines[index] = new OrderLine(
                "SKU-" + (index % 17).ToString("D2"),
                index % 11 == 0 ? null : "note-" + (index % 5),
                (index % 7) + 1,
                (index % 29) * 1.25m);
        }

        return lines;
    }

    public const string ValidJson =
        "{\"id\":42,\"customer\":\"ACME\",\"total\":123.45,\"tags\":[\"priority\",\"gift\"]}";

    public const string MissingIdJson =
        "{\"customer\":\"ACME\",\"total\":123.45,\"tags\":[\"priority\"]}";

    public const string NullCustomerJson =
        "{\"id\":42,\"customer\":null,\"total\":123.45,\"tags\":[\"priority\"]}";

    public const string EmptyCustomerJson =
        "{\"id\":42,\"customer\":\"\",\"total\":123.45,\"tags\":[\"priority\"]}";

    public const string WrongKindIdJson =
        "{\"id\":\"42\",\"customer\":\"ACME\",\"total\":123.45,\"tags\":[\"priority\"]}";

    public const string MalformedDecimalJson =
        "{\"id\":42,\"customer\":\"ACME\",\"total\":1e99999,\"tags\":[\"priority\"]}";

    public const string EmptyTagsJson =
        "{\"id\":42,\"customer\":\"ACME\",\"total\":123.45,\"tags\":[]}";

    public const string EmptyTagElementJson =
        "{\"id\":42,\"customer\":\"ACME\",\"total\":123.45,\"tags\":[\"ok\",\"\"]}";

    public const string TagsWrongKindJson =
        "{\"id\":42,\"customer\":\"ACME\",\"total\":123.45,\"tags\":\"priority\"}";

    public const string DuplicateFirstValidJson =
        "{\"id\":42,\"id\":\"bad\",\"customer\":\"ACME\",\"total\":123.45,\"tags\":[\"priority\"]}";

    public const string DuplicateFirstInvalidJson =
        "{\"id\":\"bad\",\"id\":42,\"customer\":\"ACME\",\"total\":123.45,\"tags\":[\"priority\"]}";

    public const string RootArrayJson = "[1,2,3]";

    public const string RootNullJson = "null";

    public const string ExtraPropertiesJson =
        "{\"id\":42,\"customer\":\"ACME\",\"total\":123.45,\"tags\":[\"priority\"],\"unused\":true}";

    public const string OptionalMissingJson = "{\"id\":7}";

    public const string OptionalPresentJson =
        "{\"id\":7,\"customer\":\"ACME\",\"note\":\"hello\"}";

    public const string OptionalNullNoteJson =
        "{\"id\":7,\"note\":null}";

    public const string OptionalNullCustomerJson =
        "{\"id\":7,\"customer\":null,\"note\":null}";

    public const string OptionalEmptyNoteJson =
        "{\"id\":7,\"note\":\"\"}";

    public static IEnumerable<(string Name, string Json)> DecoderFixtures()
    {
        yield return ("valid", ValidJson);
        yield return ("missing-id", MissingIdJson);
        yield return ("null-customer", NullCustomerJson);
        yield return ("empty-customer", EmptyCustomerJson);
        yield return ("wrong-kind-id", WrongKindIdJson);
        yield return ("malformed-decimal", MalformedDecimalJson);
        yield return ("empty-tags", EmptyTagsJson);
        yield return ("empty-tag-element", EmptyTagElementJson);
        yield return ("tags-wrong-kind", TagsWrongKindJson);
        yield return ("duplicate-first-valid", DuplicateFirstValidJson);
        yield return ("duplicate-first-invalid", DuplicateFirstInvalidJson);
        yield return ("root-array", RootArrayJson);
        yield return ("root-null", RootNullJson);
        yield return ("extra-properties", ExtraPropertiesJson);
    }

    public static IEnumerable<(string Name, string Json)> OptionalFixtures()
    {
        yield return ("missing", OptionalMissingJson);
        yield return ("present", OptionalPresentJson);
        yield return ("null-note", OptionalNullNoteJson);
        yield return ("null-customer", OptionalNullCustomerJson);
        yield return ("empty-note", OptionalEmptyNoteJson);
    }

    public static IEnumerable<(string Name, string Json)> ScalarFixtures()
    {
        yield return ("valid", "{\"big\":9223372036854775807,\"ratio\":1.5,\"flag\":true,\"text\":\"hello\"}");
        yield return ("big-wrong-kind", "{\"big\":\"9223372036854775807\",\"ratio\":1.5,\"flag\":true,\"text\":\"hello\"}");
        yield return ("ratio-non-finite", "{\"big\":1,\"ratio\":1e400,\"flag\":true,\"text\":\"hello\"}");
        yield return ("flag-wrong-kind", "{\"big\":1,\"ratio\":1.5,\"flag\":\"true\",\"text\":\"hello\"}");
        yield return ("text-wrong-kind", "{\"big\":1,\"ratio\":1.5,\"flag\":true,\"text\":5}");
        yield return ("missing-flag", "{\"big\":1,\"ratio\":1.5,\"text\":\"hello\"}");
    }

    public static IEnumerable<(string Name, string Json)> TemporalFixtures()
    {
        yield return ("valid", "{\"id\":\"6f9619ff-8b86-d011-b42d-00c04fc964ff\",\"when\":\"2024-01-02T03:04:05Z\",\"at\":\"2024-01-02T03:04:05+02:00\",\"payload\":{\"a\":1}}");
        yield return ("id-wrong-kind", "{\"id\":42,\"when\":\"2024-01-02T03:04:05Z\",\"at\":\"2024-01-02T03:04:05+02:00\",\"payload\":1}");
        yield return ("when-invalid", "{\"id\":\"6f9619ff-8b86-d011-b42d-00c04fc964ff\",\"when\":\"not-a-date\",\"at\":\"2024-01-02T03:04:05+02:00\",\"payload\":1}");
        yield return ("at-wrong-kind", "{\"id\":\"6f9619ff-8b86-d011-b42d-00c04fc964ff\",\"when\":\"2024-01-02T03:04:05Z\",\"at\":5,\"payload\":1}");
        yield return ("missing-payload", "{\"id\":\"6f9619ff-8b86-d011-b42d-00c04fc964ff\",\"when\":\"2024-01-02T03:04:05Z\",\"at\":\"2024-01-02T03:04:05+02:00\"}");
    }

    public static IEnumerable<(string Name, string Json)> PlainListFixtures()
    {
        yield return ("valid", "{\"label\":\"x\",\"values\":[1,2,3]}");
        yield return ("empty", "{\"label\":\"x\",\"values\":[]}");
        yield return ("element-wrong-kind", "{\"label\":\"x\",\"values\":[1,\"two\"]}");
        yield return ("values-wrong-kind", "{\"label\":\"x\",\"values\":\"1,2,3\"}");
        yield return ("label-empty", "{\"label\":\"\",\"values\":[1]}");
    }

    public static JsonElement Parse(string json, out JsonDocument document)
    {
        document = JsonDocument.Parse(json);
        return document.RootElement;
    }

    public static string Summarize(Validation<OrderInput, DecodeError> validation)
    {
        if (validation.TryGetValue(out var value))
        {
            return "valid:" + value.Id + ":" + value.Customer + ":" + value.Total.ToString("G29")
                + ":" + string.Join("|", value.Tags);
        }

        validation.TryGetErrors(out var errors);
        return "invalid:" + string.Join(
            ";",
            errors!.Select(error => error.Path + "/" + error.Kind + "/" + error.Expected + "/" + error.Detail));
    }

    public static string Summarize(Validation<OrderOptional, DecodeError> validation)
    {
        if (validation.TryGetValue(out var value))
        {
            return "valid:" + value.Id + ":" + OptionText(value.Customer) + ":" + OptionText(value.Note);
        }

        validation.TryGetErrors(out var errors);
        return "invalid:" + string.Join(
            ";",
            errors!.Select(error => error.Path + "/" + error.Kind + "/" + error.Expected + "/" + error.Detail));
    }

    public static string Summarize(Validation<ScalarGroup, DecodeError> validation) =>
        Summarize(
            validation,
            static value => value.Big + ":" + value.Ratio.ToString("R", CultureInfo.InvariantCulture)
                + ":" + value.Flag + ":" + value.Text);

    public static string Summarize(Validation<TemporalGroup, DecodeError> validation) =>
        Summarize(
            validation,
            static value => value.Id.ToString("D") + ":" + value.When.ToString("O", CultureInfo.InvariantCulture)
                + ":" + value.At.ToString("O", CultureInfo.InvariantCulture) + ":" + value.Payload.GetRawText());

    public static string Summarize(Validation<PlainListGroup, DecodeError> validation) =>
        Summarize(validation, static value => value.Label + ":" + string.Join(",", value.Values));

    public static string Summarize(Validation<ListOverloadProbe, DecodeError> validation) =>
        Summarize(validation, static value => value.Marker);

    public static string Summarize<T>(Validation<T, DecodeError> validation, Func<T, string> describe)
    {
        if (validation.TryGetValue(out var value))
        {
            return "valid:" + describe(value);
        }

        validation.TryGetErrors(out var errors);
        return "invalid:" + string.Join(
            ";",
            errors!.Select(error => error.Path + "/" + error.Kind + "/" + error.Expected + "/" + error.Detail));
    }

    private static string OptionText(Option<string> option) =>
        option.TryGetValue(out var value) ? "Some(" + value + ")" : "None";

    public static int Digest((int Count, decimal Total, Option<decimal> Min, Option<decimal> Max) result) =>
        HashCode.Combine(
            result.Count,
            result.Total,
            result.Min.TryGetValue(out var min) ? min : decimal.MinValue,
            result.Max.TryGetValue(out var max) ? max : decimal.MaxValue);
}

public sealed class CountingSequence<T> : IEnumerable<T>
{
    private readonly IReadOnlyList<T> items;

    public CountingSequence(IReadOnlyList<T> items) => this.items = items;

    public int Enumerations { get; private set; }

    public int MoveNextCalls { get; private set; }

    public int CurrentCalls { get; private set; }

    public int DisposeCalls { get; private set; }

    public IEnumerator<T> GetEnumerator()
    {
        Enumerations++;
        return new Enumerator(this);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class Enumerator(CountingSequence<T> owner) : IEnumerator<T>
    {
        private int index = -1;

        public T Current
        {
            get
            {
                owner.CurrentCalls++;
                return owner.items[index];
            }
        }

        object System.Collections.IEnumerator.Current => Current!;

        public bool MoveNext()
        {
            owner.MoveNextCalls++;
            index++;
            return index < owner.items.Count;
        }

        public void Reset() => index = -1;

        public void Dispose() => owner.DisposeCalls++;
    }
}
