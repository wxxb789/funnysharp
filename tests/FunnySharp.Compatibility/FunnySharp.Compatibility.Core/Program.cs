using FunnySharp;

var mapped = Option.Some("release").Map(static value => value.Length);
Require(mapped.TryGetValue(out var length) && length == 7, "Option mapping failed.");

var result = Result<int, string>.Success(20).Map(static value => value + 1);
Require(result.TryGetValue(out var resultValue) && resultValue == 21, "Result mapping failed.");

var validation = Validation<int, string>.InvalidMany(["first", "second"]);
Require(
    validation.TryGetErrors(out var errors) && errors.SequenceEqual(["first", "second"]),
    "Validation error accumulation failed.");

var effect = Effect
    .FromSync(() => Option.Some(6))
    .Map(static option => option.Map(static value => value * 7));
var effectValue = await effect.RunAsync();
Require(effectValue.TryGetValue(out var answer) && answer == 42, "Effect execution failed.");

Console.WriteLine("FunnySharp core compatibility smoke passed.");

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
