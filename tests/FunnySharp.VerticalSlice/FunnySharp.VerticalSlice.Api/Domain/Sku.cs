namespace FunnySharp.VerticalSlice.Domain;

/// <summary>A refined stock-keeping unit: 2 to 32 upper-case letters, digits, or dashes.</summary>
public readonly record struct Sku
{
    private readonly string? value;

    private Sku(string value) => this.value = value;

    public string Value => value ?? throw new InvalidOperationException("The SKU is uninitialized.");

    public static Result<Sku, InputError> Create(string? candidate, string field = "sku")
    {
        if (candidate is not { Length: >= 2 and <= 32 }
            || !candidate.All(static character => char.IsAsciiLetterUpper(character) || char.IsAsciiDigit(character) || character == '-'))
        {
            return Result<Sku, InputError>.Failure(
                new InputError(field, "format", "A SKU must be 2 to 32 upper-case letters, digits, or dashes."));
        }

        return Result<Sku, InputError>.Success(new Sku(candidate));
    }

    public override string ToString() => value ?? "Sku(uninitialized)";
}
