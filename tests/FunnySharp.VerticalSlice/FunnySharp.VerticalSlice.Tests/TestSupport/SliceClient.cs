using System.Net.Http.Json;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// A thin test client over the slice. Every call passes <see cref="TestContext"/>'s cancellation
/// token, so a hung test is canceled by the runner instead of by a timeout of its own; the overloads
/// that take a token exist for the cancellation tests that supply their own.
/// </summary>
internal sealed class SliceClient(HttpClient client)
{
    internal Task<HttpResponseMessage> GetAsync(string path) =>
        client.GetAsync(path, TestContext.Current.CancellationToken);

    /// <summary>Used by the cancellation tests, which supply the token that must observe cancellation.</summary>
    internal Task<HttpResponseMessage> GetWithTokenAsync(string path, CancellationToken cancellationToken) =>
        client.GetAsync(path, cancellationToken);

    internal Task<HttpResponseMessage> GetStreamAsync(string path) =>
        client.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, TestContext.Current.CancellationToken);

    internal Task<HttpResponseMessage> PostJsonAsync<TBody>(string path, TBody body) =>
        client.PostAsJsonAsync(path, body, TestContext.Current.CancellationToken);

    /// <summary>Used by the cancellation tests, which supply the token that must observe cancellation.</summary>
    internal Task<HttpResponseMessage> PostJsonWithTokenAsync<TBody>(string path, TBody body, CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(path, body, cancellationToken);

    /// <summary>Posts a body the typed contract cannot express, such as a null array element.</summary>
    internal Task<HttpResponseMessage> PostRawJsonAsync(string path, string json) =>
        client.PostAsync(
            path,
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

    internal async Task<TValue?> GetJsonAsync<TValue>(string path)
    {
        using var response = await GetAsync(path);
        return await response.ReadJsonAsync<TValue>();
    }

    internal Task<string> GetStringAsync(string path) => client.GetStringAsync(path, TestContext.Current.CancellationToken);
}
