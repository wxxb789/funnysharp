namespace FunnySharp.Tests;

public sealed class StableObjectEqualityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void InitializedCarrierObjectEqualityRejectsMatchingBoxedDefaultsButNotUnrelatedObjects(int carrier)
    {
        object initialized = carrier switch
        {
            0 => Result<int, string>.Success(42),
            1 => UnitResult<string>.Success(),
            2 => Validation<int, string>.Valid(42),
            _ => throw new ArgumentOutOfRangeException(nameof(carrier)),
        };
        var boxedDefault = Activator.CreateInstance(initialized.GetType());
        Assert.NotNull(boxedDefault);

        Assert.Throws<InvalidOperationException>(() => initialized.Equals(boxedDefault));
        Assert.False(initialized.Equals(new object()));
        Assert.False(initialized.Equals(null));
    }
}
