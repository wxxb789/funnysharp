using System.Text.Json;

namespace FunnySharp.Tests;

public sealed class DecoderTests
{
    [Fact]
    public void OrderDecoderDecodesNestedValidRequestIntoDomainValues()
    {
        var order = DecoderTestData.Value(
            DecoderTestData.Decode(DecoderTestData.OrderDecoder, DecoderTestData.ValidOrderJson));

        Assert.Equal(Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"), order.Id);
        Assert.Equal("Ada", order.Customer.Name);
        Assert.Equal("ada@example.com", order.Customer.Email);
        Assert.Single(order.Customer.Addresses);
        Assert.Equal("12345", order.Customer.Addresses[0].PostalCode);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal("BOOK-1", order.Lines[0].Sku);
        Assert.Equal(2, order.Lines[0].Quantity);
        Assert.Equal("PEN-1", order.Lines[1].Sku);
        Assert.Equal(1, order.Lines[1].Quantity);
        Assert.True(order.Note.IsNone);
    }

    [Fact]
    public void OrderDecoderAccumulatesIndependentFieldAndElementErrorsInDeterministicOrder()
    {
        var validation = DecoderTestData.Decode(
            DecoderTestData.OrderDecoder,
            DecoderTestData.InvalidOrderJson);

        Assert.False(validation.IsValid);

        var errors = DecoderTestData.Errors(validation);

        Assert.Equal(
            new[]
            {
                "id",
                "customer.name",
                "customer.email",
                "customer.addresses[1].postalCode",
                "lines[1].sku",
                "lines[1].quantity",
                "note",
            },
            errors.Select(static error => error.Path).ToArray());

        Assert.Equal(
            new[]
            {
                DecodeErrorKind.Malformed,
                DecodeErrorKind.Empty,
                DecodeErrorKind.InvalidValue,
                DecodeErrorKind.Null,
                DecodeErrorKind.Empty,
                DecodeErrorKind.InvalidValue,
                DecodeErrorKind.Null,
            },
            errors.Select(static error => error.Kind).ToArray());

        Assert.Equal("the value cannot be parsed as a GUID string", errors[0].Detail);
        Assert.Equal("email must contain '@'", errors[2].Detail);
        Assert.Equal("quantity must be positive", errors[5].Detail);
        Assert.Equal(
            "lines[1].quantity: quantity must be positive",
            errors[5].ToString());
    }

    [Fact]
    public void ObjectChecksTheInputKindOnceAndSkipsFieldDecodersForANonObject()
    {
        var invocations = 0;
        var field = Decoder.Field("x", Decoder.String)
            .Map(value =>
            {
                invocations++;
                return value;
            });
        var decoder = Decoder.Object(field, field, static (left, right) => left + right);

        var errors = DecoderTestData.Errors(DecoderTestData.Decode(decoder, "[]"));

        var error = Assert.Single(errors);
        Assert.Equal(DecodeErrorKind.WrongKind, error.Kind);
        Assert.Equal(string.Empty, error.Path);
        Assert.Equal("input: expected an object (found Array)", error.ToString());
        Assert.Equal(0, invocations);
    }

    [Fact]
    public void RequiredFieldDistinguishesMissingNullWrongKindAndMalformedInput()
    {
        var decoder = Decoder.Field("id", Decoder.Guid);

        var missing = Assert.Single(
            DecoderTestData.Errors(DecoderTestData.Decode(decoder, "{}")));
        Assert.Equal(DecodeErrorKind.Missing, missing.Kind);
        Assert.Equal("id", missing.Path);
        Assert.Equal("id: required value is missing", missing.ToString());

        var explicitNull = Assert.Single(
            DecoderTestData.Errors(DecoderTestData.Decode(decoder, """{"id":null}""")));
        Assert.Equal(DecodeErrorKind.Null, explicitNull.Kind);
        Assert.Equal("id", explicitNull.Path);
        Assert.Equal("a GUID string", explicitNull.Expected);
        Assert.Equal(
            "id: value must not be null; expected a GUID string",
            explicitNull.ToString());

        var wrongKind = Assert.Single(
            DecoderTestData.Errors(DecoderTestData.Decode(decoder, """{"id":42}""")));
        Assert.Equal(DecodeErrorKind.WrongKind, wrongKind.Kind);
        Assert.Equal("id", wrongKind.Path);
        Assert.Equal("a GUID string", wrongKind.Expected);
        Assert.Equal("found Number", wrongKind.Detail);
        Assert.Equal(
            "id: expected a GUID string (found Number)",
            wrongKind.ToString());

        var malformed = Assert.Single(
            DecoderTestData.Errors(DecoderTestData.Decode(decoder, """{"id":"bad"}""")));
        Assert.Equal(DecodeErrorKind.Malformed, malformed.Kind);
        Assert.Equal("id", malformed.Path);
        Assert.Equal("a GUID string", malformed.Expected);
        Assert.Equal("the value cannot be parsed as a GUID string", malformed.Detail);
    }

    [Fact]
    public void OptionalFieldAndNullableFieldExposeTheDeliberateMissingNullMatrix()
    {
        var optional = Decoder.OptionalField("note", Decoder.String);

        var missing = DecoderTestData.Value(DecoderTestData.Decode(optional, "{}"));
        Assert.True(missing.IsNone);

        var explicitNull = Assert.Single(
            DecoderTestData.Errors(DecoderTestData.Decode(optional, """{"note":null}""")));
        Assert.Equal(DecodeErrorKind.Null, explicitNull.Kind);
        Assert.Equal("note", explicitNull.Path);

        var present = DecoderTestData.Value(
            DecoderTestData.Decode(optional, """{"note":"x"}"""));
        Assert.True(present.TryGetValue(out var text));
        Assert.Equal("x", text);

        var empty = DecoderTestData.Value(
            DecoderTestData.Decode(optional, """{"note":""}"""));
        Assert.True(empty.TryGetValue(out var emptyText));
        Assert.Equal(string.Empty, emptyText);

        var strictEmpty = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(
                    Decoder.OptionalField("note", Decoder.NonEmptyString),
                    """{"note":""}""")));
        Assert.Equal(DecodeErrorKind.Empty, strictEmpty.Kind);
        Assert.Equal("note", strictEmpty.Path);

        var nullable = Decoder.NullableField("note", Decoder.String);

        Assert.True(DecoderTestData.Value(DecoderTestData.Decode(nullable, "{}")).IsNone);
        Assert.True(
            DecoderTestData.Value(
                DecoderTestData.Decode(nullable, """{"note":null}""")).IsNone);

        var nullablePresent = DecoderTestData.Value(
            DecoderTestData.Decode(nullable, """{"note":"x"}"""));
        Assert.True(nullablePresent.TryGetValue(out var nullableText));
        Assert.Equal("x", nullableText);
    }

    [Fact]
    public void FieldCombinatorsSelectTheFirstDuplicateProperty()
    {
        const string json = """{"note":1,"note":null}""";

        var required = DecoderTestData.Value(
            DecoderTestData.Decode(Decoder.Field("note", Decoder.Int32), json));
        Assert.Equal(1, required);

        var optional = DecoderTestData.Value(
            DecoderTestData.Decode(Decoder.OptionalField("note", Decoder.Int32), json));
        Assert.True(optional.TryGetValue(out var optionalValue));
        Assert.Equal(1, optionalValue);

        var nullable = DecoderTestData.Value(
            DecoderTestData.Decode(Decoder.NullableField("note", Decoder.Int32), json));
        Assert.True(nullable.TryGetValue(out var nullableValue));
        Assert.Equal(1, nullableValue);
    }

    [Fact]
    public void OptionalValueMapsOnlyExplicitNullToAbsence()
    {
        var invocations = 0;
        var counting = Decoder.Int32.Map(value =>
        {
            invocations++;
            return value;
        });
        var decoder = Decoder.OptionalValue(counting);

        var absent = DecoderTestData.Value(DecoderTestData.Decode(decoder, "null"));
        Assert.True(absent.IsNone);
        Assert.Equal(0, invocations);

        var present = DecoderTestData.Value(DecoderTestData.Decode(decoder, "7"));
        Assert.True(present.TryGetValue(out var value));
        Assert.Equal(7, value);
        Assert.Equal(1, invocations);

        var wrongKind = Assert.Single(
            DecoderTestData.Errors(DecoderTestData.Decode(decoder, "\"x\"")));
        Assert.Equal(DecodeErrorKind.WrongKind, wrongKind.Kind);
        Assert.Equal(string.Empty, wrongKind.Path);
    }

    [Fact]
    public void NonEmptyStringAndNonEmptyListReportEmptyWhileTheirPermissiveFormsAcceptIt()
    {
        var emptyString = Assert.Single(
            DecoderTestData.Errors(DecoderTestData.Decode(Decoder.NonEmptyString, "\"\"")));
        Assert.Equal(DecodeErrorKind.Empty, emptyString.Kind);
        Assert.Equal(string.Empty, emptyString.Path);
        Assert.Equal("input: expected a non-empty string", emptyString.ToString());

        var nullString = Assert.Single(
            DecoderTestData.Errors(DecoderTestData.Decode(Decoder.NonEmptyString, "null")));
        Assert.Equal(DecodeErrorKind.Null, nullString.Kind);

        var emptyList = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(Decoder.NonEmptyList(Decoder.Int32), "[]")));
        Assert.Equal(DecodeErrorKind.Empty, emptyList.Kind);
        Assert.Equal(string.Empty, emptyList.Path);

        var stringValue = DecoderTestData.Value(
            DecoderTestData.Decode(Decoder.String, "\"\""));
        Assert.Equal(string.Empty, stringValue);

        var listValue = DecoderTestData.Value(
            DecoderTestData.Decode(Decoder.List(Decoder.Int32), "[]"));
        Assert.Empty(listValue);

        var fieldEmpty = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(
                    Decoder.Field("lines", Decoder.NonEmptyList(Decoder.Int32)),
                    """{"lines":[]}""")));
        Assert.Equal(DecodeErrorKind.Empty, fieldEmpty.Kind);
        Assert.Equal("lines", fieldEmpty.Path);
        Assert.Equal("lines: expected a non-empty array", fieldEmpty.ToString());
    }

    [Fact]
    public void ListAccumulatesElementErrorsInSourceOrderWithIndexPaths()
    {
        var errors = DecoderTestData.Errors(
            DecoderTestData.Decode(Decoder.List(Decoder.Int32), "[1,\"x\",3,\"y\"]"));

        Assert.Equal(
            new[] { "[1]", "[3]" },
            errors.Select(static error => error.Path).ToArray());
        Assert.Equal(
            new[] { DecodeErrorKind.WrongKind, DecodeErrorKind.WrongKind },
            errors.Select(static error => error.Kind).ToArray());

        var nested = DecoderTestData.Errors(
            DecoderTestData.Decode(
                Decoder.List(DecoderTestData.LineDecoder),
                """[{"sku":"ok","quantity":1},{"sku":"","quantity":0}]"""));

        Assert.Equal(
            new[] { "[1].sku", "[1].quantity" },
            nested.Select(static error => error.Path).ToArray());
        Assert.Equal(
            new[] { DecodeErrorKind.Empty, DecodeErrorKind.InvalidValue },
            nested.Select(static error => error.Kind).ToArray());
    }

    [Fact]
    public void ObjectArityTwoThreeAndFourConstructValuesAndSkipCombineOnFailure()
    {
        var two = Decoder.Object(
            Decoder.Field("a", Decoder.Int32),
            Decoder.Field("b", Decoder.Int32),
            static (a, b) => a + b);
        Assert.Equal(3, DecoderTestData.Value(DecoderTestData.Decode(two, """{"a":1,"b":2}""")));

        var three = Decoder.Object(
            Decoder.Field("a", Decoder.Int32),
            Decoder.Field("b", Decoder.Int32),
            Decoder.Field("c", Decoder.Int32),
            static (a, b, c) => a + b + c);
        Assert.Equal(
            6,
            DecoderTestData.Value(DecoderTestData.Decode(three, """{"a":1,"b":2,"c":3}""")));

        var four = Decoder.Object(
            Decoder.Field("a", Decoder.Int32),
            Decoder.Field("b", Decoder.Int32),
            Decoder.Field("c", Decoder.Int32),
            Decoder.Field("d", Decoder.Int32),
            static (a, b, c, d) => a + b + c + d);
        Assert.Equal(
            10,
            DecoderTestData.Value(
                DecoderTestData.Decode(four, """{"a":1,"b":2,"c":3,"d":4}""")));

        var combineCalls = 0;
        var failing = Decoder.Object(
            Decoder.Field("a", Decoder.Int32),
            Decoder.Field("b", Decoder.Int32),
            (int a, int b) =>
            {
                combineCalls++;
                return a + b;
            });

        var errors = DecoderTestData.Errors(
            DecoderTestData.Decode(failing, """{"a":"x","b":2}"""));

        Assert.Single(errors);
        Assert.Equal("a", errors[0].Path);
        Assert.Equal(0, combineCalls);
    }

    [Fact]
    public void MapTransformsOnlySuccessfulValues()
    {
        var invocations = 0;
        var decoder = Decoder.Int32.Map(value =>
        {
            invocations++;
            return value * 2;
        });

        Assert.Equal(4, DecoderTestData.Value(DecoderTestData.Decode(decoder, "2")));
        Assert.Equal(1, invocations);

        var errors = DecoderTestData.Errors(DecoderTestData.Decode(decoder, "\"x\""));
        Assert.Single(errors);
        Assert.Equal(DecodeErrorKind.WrongKind, errors[0].Kind);
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void BindRunsOnlyAfterSuccessAndDecodesFromTheSameInput()
    {
        var binderCalls = 0;
        var decoder = Decoder.Field("kind", Decoder.String).Bind(kind =>
        {
            binderCalls++;
            return Decoder.Field("value", Decoder.Int32)
                .Map(value => kind + ":" + value);
        });

        Assert.Equal(
            "count:7",
            DecoderTestData.Value(
                DecoderTestData.Decode(decoder, """{"kind":"count","value":7}""")));
        Assert.Equal(1, binderCalls);

        var invalidKind = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(decoder, """{"kind":7,"value":7}""")));
        Assert.Equal(DecodeErrorKind.WrongKind, invalidKind.Kind);
        Assert.Equal("kind", invalidKind.Path);
        Assert.Equal(1, binderCalls);

        var missingValue = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(decoder, """{"kind":"count"}""")));
        Assert.Equal(DecodeErrorKind.Missing, missingValue.Kind);
        Assert.Equal("value", missingValue.Path);
        Assert.Equal(2, binderCalls);
    }

    [Fact]
    public void EnsureAndValidateSkipAfterFailureAndAccumulateDomainMessages()
    {
        var predicateCalls = 0;
        var ensure = Decoder.Int32.Ensure(
            value =>
            {
                predicateCalls++;
                return value > 0;
            },
            "must be positive");

        Assert.Equal(1, DecoderTestData.Value(DecoderTestData.Decode(ensure, "1")));
        Assert.Equal(1, predicateCalls);

        var ensureField = Decoder.Field("quantity", ensure);
        var rejected = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(ensureField, """{"quantity":0}""")));
        Assert.Equal(DecodeErrorKind.InvalidValue, rejected.Kind);
        Assert.Equal("quantity", rejected.Path);
        Assert.Equal("must be positive", rejected.Detail);
        Assert.Equal(2, predicateCalls);

        var wrongKind = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(ensureField, """{"quantity":"x"}""")));
        Assert.Equal(DecodeErrorKind.WrongKind, wrongKind.Kind);
        Assert.Equal(2, predicateCalls);

        var validateCalls = 0;
        var validate = Decoder.Int32.Validate(value =>
        {
            validateCalls++;
            return value >= 0
                ? Validation<int, string>.Valid(value)
                : Validation<int, string>.InvalidMany(
                    new[] { "must be non-negative", "must be even" });
        });
        var validateField = Decoder.Field("quantity", validate);

        var domainErrors = DecoderTestData.Errors(
            DecoderTestData.Decode(validateField, """{"quantity":-1}"""));
        Assert.Equal(
            new[] { "quantity", "quantity" },
            domainErrors.Select(static error => error.Path).ToArray());
        Assert.Equal(
            new[] { DecodeErrorKind.InvalidValue, DecodeErrorKind.InvalidValue },
            domainErrors.Select(static error => error.Kind).ToArray());
        Assert.Equal(
            new[] { "must be non-negative", "must be even" },
            domainErrors.Select(static error => error.Detail).ToArray());
        Assert.Equal(1, validateCalls);

        var skipped = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(validateField, """{"quantity":"x"}""")));
        Assert.Equal(DecodeErrorKind.WrongKind, skipped.Kind);
        Assert.Equal(1, validateCalls);

        var normalized = DecoderTestData.Value(
            DecoderTestData.Decode(
                Decoder.Int32.Validate(
                    value => Validation<int, string>.Valid(value * 2)),
                "3"));
        Assert.Equal(6, normalized);
    }

    [Fact]
    public void DecoderFromComposesRelativeCustomErrorsUnderFieldPaths()
    {
        var custom = Decoder.From<int>(element =>
            element.ValueKind == JsonValueKind.Number
                ? Validation<int, DecodeError>.Valid(element.GetInt32())
                : Validation<int, DecodeError>.Invalid(
                    DecodeError.Create(
                        DecodeErrorKind.WrongKind,
                        "a number",
                        "custom decoder")));

        var rootError = Assert.Single(
            DecoderTestData.Errors(DecoderTestData.Decode(custom, "\"x\"")));
        Assert.Equal(DecodeErrorKind.WrongKind, rootError.Kind);
        Assert.Equal(string.Empty, rootError.Path);
        Assert.Equal("a number", rootError.Expected);
        Assert.Equal("custom decoder", rootError.Detail);
        Assert.Equal("input: expected a number (custom decoder)", rootError.ToString());

        var fieldError = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(
                    Decoder.Field("value", custom),
                    """{"value":"x"}""")));
        Assert.Equal("value", fieldError.Path);
        Assert.Equal(DecodeErrorKind.WrongKind, fieldError.Kind);
    }

    [Fact]
    public void SameDecoderInstanceServesTwoFieldLocationsWithoutLeakingInvocationState()
    {
        var pair = Decoder.Object(
            Decoder.Field("billing", DecoderTestData.CustomerDecoder),
            Decoder.Field("shipping", DecoderTestData.CustomerDecoder),
            static (billing, shipping) => (billing, shipping));

        var errors = DecoderTestData.Errors(
            DecoderTestData.Decode(
                pair,
                """
                {
                  "billing": { "name": "", "email": "a@b", "addresses": [] },
                  "shipping": { "name": "", "email": "a@b", "addresses": [] }
                }
                """));

        Assert.Equal(
            new[] { "billing.name", "shipping.name" },
            errors.Select(static error => error.Path).ToArray());
        Assert.All(errors, static error => Assert.Equal(DecodeErrorKind.Empty, error.Kind));
    }

    [Fact]
    public async Task DecoderInstanceDecodesValidAndInvalidInputsConcurrently()
    {
        var outcomes = await Task.WhenAll(
            Enumerable.Range(0, 64)
                .Select(index => Task.Run(() =>
                {
                    var valid = index % 2 == 0;
                    var validation = DecoderTestData.Decode(
                        DecoderTestData.OrderDecoder,
                        valid
                            ? DecoderTestData.ValidOrderJson
                            : DecoderTestData.InvalidOrderJson);

                    return (
                        IsValid: validation.IsValid,
                        Paths: validation.IsValid
                            ? Array.Empty<string>()
                            : DecoderTestData.Errors(validation)
                                .Select(static error => error.Path)
                                .ToArray());
                })));

        for (var index = 0; index < outcomes.Length; index++)
        {
            if (index % 2 == 0)
            {
                Assert.True(outcomes[index].IsValid);
                Assert.Empty(outcomes[index].Paths);
            }
            else
            {
                Assert.False(outcomes[index].IsValid);
                Assert.Equal(7, outcomes[index].Paths.Length);
                Assert.Equal("id", outcomes[index].Paths[0]);
                Assert.Equal("note", outcomes[index].Paths[^1]);
            }
        }
    }
}
