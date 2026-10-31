namespace FunnySharp.TestSupport;

internal enum OrderErrorCode
{
    Required,
    Null,
    WrongKind,
    Malformed,
    Empty,
    Invalid,
}

internal sealed record OrderError(string Path, OrderErrorCode Code, string Detail);

internal sealed record DecodeFixture(string Name, string Json);
