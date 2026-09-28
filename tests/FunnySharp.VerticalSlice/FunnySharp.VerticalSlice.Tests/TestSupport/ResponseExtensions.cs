using System.Net.Http.Json;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>Reads a response body with the test's cancellation token.</summary>
internal static class ResponseExtensions
{
    internal static Task<TValue?> ReadJsonAsync<TValue>(this HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<TValue>(TestContext.Current.CancellationToken);

    internal static Task<Stream> ReadStreamAsync(this HttpResponseMessage response) =>
        response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);

    internal static Task<string> ReadStringAsync(this HttpResponseMessage response) =>
        response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    internal static Task<byte[]> ReadBytesAsync(this HttpResponseMessage response) =>
        response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
}
