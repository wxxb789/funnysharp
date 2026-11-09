using Microsoft.Data.Sqlite;

namespace FunnySharp.TestSupport.OperationComparison;

internal sealed record OrderEnvironment(
    SqliteConnection Connection,
    SqliteTransaction Transaction,
    SimulatedPaymentGateway Gateway,
    FaultInjector Faults);
