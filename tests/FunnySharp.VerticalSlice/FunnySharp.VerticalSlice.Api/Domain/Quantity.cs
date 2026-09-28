namespace FunnySharp.VerticalSlice.Domain;

/// <summary>A refined positive quantity, capped per order line.</summary>
public readonly record struct Quantity
{
    public const int MaximumPerLine = 20;

    private readonly int value;

    private Quantity(int value) => this.value = value;

    public int Value => value > 0 ? value : throw new InvalidOperationException("The quantity is uninitialized.");

    public static Result<Quantity, InputError> Create(int candidate, string field = "quantity")
    {
        if (candidate < 1)
        {
            return Result<Quantity, InputError>.Failure(
                new InputError(field, "positive", "A quantity must be at least 1."));
        }

        if (candidate > MaximumPerLine)
        {
            return Result<Quantity, InputError>.Failure(
                new InputError(field, "maximum", $"A quantity cannot exceed {MaximumPerLine}."));
        }

        return Result<Quantity, InputError>.Success(new Quantity(candidate));
    }

    public override string ToString() => value > 0 ? value.ToString() : "Quantity(uninitialized)";
}
