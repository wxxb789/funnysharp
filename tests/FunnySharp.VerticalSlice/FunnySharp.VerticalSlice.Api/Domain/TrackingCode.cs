namespace FunnySharp.VerticalSlice.Domain;

/// <summary>A refined carrier tracking code: 4 to 40 letters, digits, or dashes.</summary>
public readonly record struct TrackingCode
{
    private readonly string? value;

    private TrackingCode(string value) => this.value = value;

    public string Value => value ?? throw new InvalidOperationException("The tracking code is uninitialized.");

    public static Result<TrackingCode, InputError> Create(string? candidate)
    {
        if (candidate is not { Length: >= 4 and <= 40 }
            || !candidate.All(static character => char.IsAsciiLetterOrDigit(character) || character == '-'))
        {
            return Result<TrackingCode, InputError>.Failure(
                new InputError("trackingCode", "format", "A tracking code must be 4 to 40 letters, digits, or dashes."));
        }

        return Result<TrackingCode, InputError>.Success(new TrackingCode(candidate));
    }

    public override string ToString() => value ?? "TrackingCode(uninitialized)";
}
