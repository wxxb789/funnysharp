using System.Text.Json;

namespace FunnySharp.VerticalSlice.Http;

/// <summary>Writes one JSON document per line and flushes, so a client sees rows as they are produced.</summary>
internal static class JsonLines
{
    private static readonly byte[] NewLine = "\n"u8.ToArray();

    internal static async Task WriteAsync<T>(Stream stream, T row, CancellationToken cancellationToken)
    {
        await JsonSerializer.SerializeAsync(stream, row, JsonSerializerOptions.Web, cancellationToken);
        await stream.WriteAsync(NewLine, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
