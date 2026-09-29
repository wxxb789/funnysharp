## Business brief: sensor stream processing

You are implementing the temperature-processing pipeline of a small monitoring service as a
library consumed by a fixed xUnit test suite. The test suite and the neutral domain contract
are already provided in the build directory; you implement the pipeline behind the seam below
and the tests must pass.

Seam (you must define this exactly):

public static class SensorStream
{
    public static Task<TemperatureReport> ProcessAsync(
        IAsyncEnumerable<SensorReading> source,
        ReferenceService reference,
        CancellationToken cancellationToken)
}

Declare your types in the global namespace (no namespace declaration) in the files you write.
The provided contract defines SensorReading, TemperatureValue, TemperatureReport, and
ReferenceService; do not redefine them. The provided fake is deterministic:

- ReferenceService.IsPlausibleAsync(celsius, cancellationToken) records every call (the celsius
  value and the exact token) in call order, delays with Task.Delay(1, cancellationToken), and
  returns true exactly when MinCelsius <= celsius <= MaxCelsius (both bounds inclusive).

Pipeline stages, in this exact order:

1. Filter, convert, materialize - enumerate the source exactly once, forwarding the caller's
   CancellationToken to it (WithCancellation or GetAsyncEnumerator). Keep only readings whose
   Kind equals "temperature" (ordinal, case-sensitive); every other reading is skipped without
   a reference call. Convert each kept reading to Celsius with
   Math.Round((reading.Value - 32) * 5 / 9, 2), and materialize the stage as the report's
   Temperatures (SensorId plus the converted Celsius), in source order.
2. Running maximum - RunningMaxima[i] is the maximum Celsius value of the first i + 1
   materialized readings, so a negative first reading makes RunningMaxima[0] negative and a
   falling later reading keeps the previous maximum; an empty stage-1 list yields an empty
   RunningMaxima. This stage never calls the reference service.
3. Validation - for every materialized reading, in source order, call
   reference.IsPlausibleAsync(reading.Celsius, cancellationToken) exactly once, forwarding the
   caller's token unchanged; the first failure does not stop the remaining checks. Then exactly
   one of:
   - every reading plausible: ValidCelsius lists every Celsius value in source order and Errors
     is empty;
   - any reading implausible: Errors lists "implausible:<SensorId>" for every rejected reading
     in source order and ValidCelsius is empty.

Cancellation and faults:

- Never catch, wrap, or swallow cancellation or source failures: a source that throws
  OperationCanceledException surfaces from ProcessAsync as an OperationCanceledException (not a
  derived type, not translated), and a faulting run throws instead of returning a partial
  report. A canceled token must surface as an OperationCanceledException before any reference
  call.

Requirements:

- Correctness under the provided tests is the acceptance bar; the tests are visible to you.
- Keep the filter, running-aggregate, and validation semantics explicit and readable: a
  maintainer should see at a glance which stage produced which part of the report.
- Do not read or copy the FunnySharp repository source code.
## Style: idiomatic C# (BCL only)

Use .NET 10 base-class-library C# only: no third-party libraries or packages (the project file
references none). Express the workflow with idiomatic C#: nullable annotations, ordinary
collections and LINQ, exceptions or early returns where natural, and records. There is no
requirement to avoid exceptions or try patterns; choose what a careful .NET maintainer would
write today.
## Delivery

Write every source file you need into the solution/ directory next to this prompt. Only files
under solution/ are copied into the build; the tests and contract are fixed. Compile with:

    dotnet fsi build.fsx -- -p eval-verify --task async-streams --style `STYLE` --run-dir <run-directory>

where <run-directory> contains your solution/ folder. A verifier reply with compiler errors,
test failures, or analyzer diagnostics is feedback: fix your code and verify again.
