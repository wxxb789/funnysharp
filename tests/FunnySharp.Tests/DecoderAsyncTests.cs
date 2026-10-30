#pragma warning disable xUnit1051

using System.Text.Json;

namespace FunnySharp.Tests;

public sealed class DecoderAsyncTests
{
    [Fact]
    public async Task DecodeAsyncInvokesValidationOnceAndReturnsItsValue()
    {
        using var document = JsonDocument.Parse(DecoderTestData.ValidOrderJson);
        var calls = 0;

        var validation = await DecoderTestData.OrderDecoder.DecodeAsync(
            document.RootElement,
            (order, _) =>
            {
                calls++;
                return Task.FromResult(Validation<DecoderTestData.Order, string>.Valid(order));
            });

        Assert.True(validation.IsValid);
        Assert.Equal(1, calls);
        Assert.Equal(
            Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"),
            DecoderTestData.Value(validation).Id);
    }

    [Fact]
    public async Task DecodeAsyncMapsMultipleMessagesToRootErrors()
    {
        using var document = JsonDocument.Parse(DecoderTestData.ValidOrderJson);

        var validation = await DecoderTestData.OrderDecoder.DecodeAsync(
            document.RootElement,
            (_, _) => Task.FromResult(
                Validation<DecoderTestData.Order, string>.InvalidMany(
                    new[] { "one", "two" })));

        var errors = DecoderTestData.Errors(validation);
        Assert.Equal(
            new[] { string.Empty, string.Empty },
            errors.Select(static error => error.Path).ToArray());
        Assert.Equal(
            new[] { DecodeErrorKind.InvalidValue, DecodeErrorKind.InvalidValue },
            errors.Select(static error => error.Kind).ToArray());
        Assert.Equal(
            new[] { "one", "two" },
            errors.Select(static error => error.Detail).ToArray());
    }

    [Fact]
    public async Task DecodeAsyncSkipsValidationAndTokenInspectionAfterDecodeFailure()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        using var document = JsonDocument.Parse("{}");
        var calls = 0;

        var validation = await DecoderTestData.OrderDecoder.DecodeAsync(
            document.RootElement,
            (order, _) =>
            {
                calls++;
                return Task.FromResult(
                    Validation<DecoderTestData.Order, string>.Valid(order));
            },
            cancellationSource.Token);

        Assert.True(validation.IsInvalid);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task DecodeAsyncForwardsTheExactToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        using var document = JsonDocument.Parse(DecoderTestData.ValidOrderJson);
        var observed = default(CancellationToken);

        var validation = await DecoderTestData.OrderDecoder.DecodeAsync(
            document.RootElement,
            (order, token) =>
            {
                observed = token;
                return Task.FromResult(
                    Validation<DecoderTestData.Order, string>.Valid(order));
            },
            cancellationSource.Token);

        Assert.True(validation.IsValid);
        Assert.Equal(cancellationSource.Token, observed);
    }

    [Fact]
    public async Task DecodeAsyncPropagatesCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        using var document = JsonDocument.Parse(DecoderTestData.ValidOrderJson);

        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => DecoderTestData.OrderDecoder.DecodeAsync(
                document.RootElement,
                (_, token) => Task.FromCanceled<Validation<DecoderTestData.Order, string>>(token),
                cancellationSource.Token));

        Assert.Equal(cancellationSource.Token, actual.CancellationToken);
    }

    [Fact]
    public async Task DecodeAsyncPropagatesFaultsUnchanged()
    {
        using var document = JsonDocument.Parse(DecoderTestData.ValidOrderJson);
        var expected = new InvalidOperationException("boom");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DecoderTestData.OrderDecoder.DecodeAsync(
                document.RootElement,
                (_, _) => Task.FromException<Validation<DecoderTestData.Order, string>>(expected)));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task DecodeAsyncPropagatesUninitializedValidationAsAProgrammingError()
    {
        using var document = JsonDocument.Parse(DecoderTestData.ValidOrderJson);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DecoderTestData.OrderDecoder.DecodeAsync(
                document.RootElement,
                (_, _) => Task.FromResult(
                    default(Validation<DecoderTestData.Order, string>))));

        Assert.Equal("The validation has not been initialized.", actual.Message);
    }

    [Fact]
    public async Task DecodeAsyncSkipsValidationAfterSynchronousDomainFailure()
    {
        using var document = JsonDocument.Parse(DecoderTestData.ValidOrderJson);
        var calls = 0;
        var decoder = DecoderTestData.OrderDecoder.Ensure(
            static _ => false,
            "order rejected");

        var validation = await decoder.DecodeAsync(
            document.RootElement,
            (order, _) =>
            {
                calls++;
                return Task.FromResult(
                    Validation<DecoderTestData.Order, string>.Valid(order));
            });

        var error = Assert.Single(DecoderTestData.Errors(validation));
        Assert.Equal(DecodeErrorKind.InvalidValue, error.Kind);
        Assert.Equal("order rejected", error.Detail);
        Assert.Equal(string.Empty, error.Path);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task DecodeAsyncPropagatesSynchronousCallbackThrows()
    {
        using var document = JsonDocument.Parse(DecoderTestData.ValidOrderJson);
        var expected = new InvalidOperationException("boom");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DecoderTestData.OrderDecoder.DecodeAsync(
                document.RootElement,
                (_, _) => throw expected));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task DecodeAsyncRejectsANullTaskFromTheCallback()
    {
        using var document = JsonDocument.Parse(DecoderTestData.ValidOrderJson);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DecoderTestData.OrderDecoder.DecodeAsync(
                document.RootElement,
                (_, _) => null!));

        Assert.Equal("The asynchronous validator returned a null task.", actual.Message);
    }

    [Fact]
    public async Task DecodeValueAsyncValidatesAndPropagatesCancellation()
    {
        using var document = JsonDocument.Parse(DecoderTestData.ValidOrderJson);
        var calls = 0;

        var validation = await DecoderTestData.OrderDecoder.DecodeValueAsync(
            document.RootElement,
            (order, _) =>
            {
                calls++;
                return new ValueTask<Validation<DecoderTestData.Order, string>>(
                    Validation<DecoderTestData.Order, string>.Valid(order));
            });

        Assert.True(validation.IsValid);
        Assert.Equal(1, calls);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await DecoderTestData.OrderDecoder.DecodeValueAsync(
                document.RootElement,
                (_, token) => new ValueTask<Validation<DecoderTestData.Order, string>>(
                    Task.FromCanceled<Validation<DecoderTestData.Order, string>>(token)),
                cancellationSource.Token));

        Assert.Equal(cancellationSource.Token, actual.CancellationToken);
    }

    [Fact]
    public async Task DecodeAsyncNullCallbackThrowsEagerly()
    {
        using var document = JsonDocument.Parse(DecoderTestData.ValidOrderJson);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => DecoderTestData.OrderDecoder.DecodeAsync(
                document.RootElement,
                null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await DecoderTestData.OrderDecoder.DecodeValueAsync(
                document.RootElement,
                null!));
    }
}
