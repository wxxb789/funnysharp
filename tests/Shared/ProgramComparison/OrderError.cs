namespace FunnySharp.TestSupport.ProgramComparison;

internal abstract record OrderError;

internal sealed record InvalidTransition(string Reason) : OrderError;

internal sealed record StaleResponse(
    string Message,
    long ReceivedVersion,
    long ExpectedVersion) : OrderError;

internal sealed record DuplicateResponse(
    string Message,
    long Version) : OrderError;

internal sealed record UnexpectedResponse(string Message) : OrderError;

internal sealed record CancellationRejected(string Reason) : OrderError;

internal sealed record PaymentGatewayUnavailable(string Reason) : OrderError;

internal sealed record RefundGatewayUnavailable(string Reason) : OrderError;
