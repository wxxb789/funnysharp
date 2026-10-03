## Sensor stream processing

Define global SensorStream.ProcessAsync(IAsyncEnumerable<SensorReading> source,
ReferenceService reference, CancellationToken cancellationToken), returning
Task<TemperatureReport>. Use the supplied neutral records and reference fake.

Enumerate exactly once with the caller token. Keep Kind == "temperature" (ordinal,
case-sensitive). Convert with Math.Round((Value - 32) * 5 / 9, 2), preserving input order.
Fully materialize before any reference call. Temperatures contains the converted values.
RunningMaxima contains the inclusive running maximum; its first value may be negative.
Validate every converted value once in order using the unchanged caller token. When all
are plausible, ValidCelsius contains all values and Errors is empty. Otherwise ValidCelsius
is empty and Errors contains every implausible:<SensorId> in input order. Empty/filter-empty
inputs return empty lists and make no reference calls. Inclusive reference bounds apply.

Pre-cancellation and source/reference cancellation must propagate, with no partial report.
Source exceptions must preserve identity; source disposal occurs exactly once even on fault.
The fake can complete synchronously or remain pending behind a release gate. Gates express
ordering, not duration. Do not redefine the supplied types or read repository implementations.

## Style

Use idiomatic C#, .NET 10 BCL only.

Deliver C# files only in solution/. Do not read repository source, historical solutions, audits, plans or other sessions.
