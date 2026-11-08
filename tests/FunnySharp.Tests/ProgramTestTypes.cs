namespace FunnySharp.Tests;

internal sealed record CounterModel(int Value, int Version);

internal abstract record CounterMessage;

internal sealed record Add(int Amount, int Version) : CounterMessage;

internal sealed record AddMany(int First, int Second, int Version) : CounterMessage;

internal sealed record Split(int First, int Second, int Version) : CounterMessage;

internal sealed record NoOp : CounterMessage;

internal sealed record Reject(string Reason) : CounterMessage;

internal sealed record Fail(string Reason) : CounterMessage;

internal abstract record CounterCommand;

internal sealed record Emit(int Amount, int Version) : CounterCommand;

internal sealed record EmitMany(int First, int Second, int Version) : CounterCommand;

internal abstract record CounterError;

internal sealed record RejectedError(string Reason) : CounterError;

internal sealed record FailedError(string Reason) : CounterError;

internal sealed record MutableStateModel(List<string> Values, int Version);

internal abstract record MutableStateMessage;

internal sealed record AppendValue(string Value) : MutableStateMessage;

internal sealed record ParentModel(CounterModel Child, string Sibling);

internal abstract record ParentMessage;

internal sealed record ChildAdd(int Amount, int Version) : ParentMessage;

internal sealed record ChildAddMany(int First, int Second, int Version) : ParentMessage;

internal sealed record ChildSplit(int First, int Second, int Version) : ParentMessage;

internal sealed record ChildNoOp : ParentMessage;

internal sealed record ChildReject(string Reason) : ParentMessage;

internal sealed record ChildFail(string Reason) : ParentMessage;

internal sealed record Unrouted : ParentMessage;

internal abstract record ParentCommand;

internal sealed record ParentEmit(int Amount, int Version) : ParentCommand;

internal sealed record ParentEmitMany(int First, int Second, int Version) : ParentCommand;

internal sealed class CountingRandom(int seed) : Random(seed)
{
    public int Calls { get; private set; }

    public override int Next()
    {
        Calls++;
        return base.Next();
    }

    public override int Next(int maxValue)
    {
        Calls++;
        return base.Next(maxValue);
    }

    public override int Next(int minValue, int maxValue)
    {
        Calls++;
        return base.Next(minValue, maxValue);
    }

    public override double NextDouble()
    {
        Calls++;
        return base.NextDouble();
    }

    public override void NextBytes(byte[] buffer)
    {
        Calls++;
        base.NextBytes(buffer);
    }

    public override void NextBytes(Span<byte> buffer)
    {
        Calls++;
        base.NextBytes(buffer);
    }
}

internal static class ProgramTestSupport
{
    internal static TransitionResult<CounterModel, CounterCommand, CounterError> Update(
        CounterModel model,
        CounterMessage message) =>
        message switch
        {
            Add add => TransitionResult<CounterModel, CounterCommand, CounterError>.Applied(
                StateChange<CounterModel, CounterCommand>.To(
                    model with { Value = model.Value + add.Amount, Version = add.Version },
                    new Emit(add.Amount, add.Version))),
            AddMany many => TransitionResult<CounterModel, CounterCommand, CounterError>.Applied(
                StateChange<CounterModel, CounterCommand>.To(
                    model with
                    {
                        Value = model.Value + many.First + many.Second,
                        Version = many.Version,
                    },
                    new EmitMany(many.First, many.Second, many.Version))),
            Split split => TransitionResult<CounterModel, CounterCommand, CounterError>.Applied(
                StateChange<CounterModel, CounterCommand>.To(
                    model with
                    {
                        Value = model.Value + split.First + split.Second,
                        Version = split.Version,
                    },
                    new Emit(split.First, split.Version),
                    new Emit(split.Second, split.Version))),
            NoOp => TransitionResult<CounterModel, CounterCommand, CounterError>.Applied(
                StateChange<CounterModel, CounterCommand>.To(model)),
            Reject reject => TransitionResult<CounterModel, CounterCommand, CounterError>.Rejected(
                new RejectedError(reject.Reason)),
            Fail fail => TransitionResult<CounterModel, CounterCommand, CounterError>.Failed(
                new FailedError(fail.Reason)),
            _ => TransitionResult<CounterModel, CounterCommand, CounterError>.Undefined(),
        };

    internal static DomainProgram<CounterModel, CounterMessage, CounterCommand, CounterError>
        CreateProgram() =>
        DomainProgram.Create<CounterModel, CounterMessage, CounterCommand, CounterError>(Update);
}
