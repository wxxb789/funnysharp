namespace FunnySharp.VerticalSlice.Domain;

/// <summary>A refined customer identifier: printable, 3 to 40 characters.</summary>
public readonly record struct CustomerId
{
    private readonly string? value;

    private CustomerId(string value) => this.value = value;

    public string Value => value ?? throw new InvalidOperationException("The customer id is uninitialized.");

    public static Result<CustomerId, InputError> Create(string? candidate)
    {
        if (candidate is not { Length: >= 3 and <= 40 } || !candidate.All(static character => !char.IsControl(character)))
        {
            return Result<CustomerId, InputError>.Failure(
                new InputError("customerId", "format", "A customer id must be 3 to 40 printable characters."));
        }

        return Result<CustomerId, InputError>.Success(new CustomerId(candidate));
    }

    public override string ToString() => value ?? "CustomerId(uninitialized)";
}
