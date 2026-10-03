# Fixed-oracle controls

These are BCL/ASP.NET Core positive implementations and single-defect mutation
controls for the `audit-resolution-v1` fixed suites. They are replay evidence,
not independent AI generation, the 20-session cohort, or product acceptance.
Projects compile the original oracle files by link; no oracle copies or
replacement contracts are authored here.

| Project | Suite | Expected cases | Deliberate defect |
| --- | --- | ---: | --- |
| PositiveBusiness | business-outcomes | 10 | None |
| PositiveCollections | collections | 9 | None |
| PositiveStreams | async-streams | 10 | None |
| PositiveConcurrency | concurrency | 12 | None |
| PositiveHttp | aspnetcore | 9 | None |
| EarlySideEffects | business-outcomes | 10 | Pricing lookup before validation |
| StreamingValidation | async-streams | 10 | Reference validation before complete source materialization |
| LastWinner | concurrency | 12 | Late accepting replies overwrite the observed winner |
| UnobservedCleanupFault | concurrency | 12 | Success publication before observing loser cleanup |
| UnboundedAdmission | concurrency | 12 | Warehouse admission ignores maxConcurrency |

Each mutation is an explicit compile symbol in its own project. Its remaining
implementation and exact oracle inputs are shared with the positive project.
The reservation implementation uses actual gateway tasks, not fake signal
properties; the availability implementation owns bounded workers and drains
them via `Task.WhenAll`. No control introduces sleeps, polling, or synthetic
responses in place of domain work.

From the repository root:

```bash
python eng/evaluation/studies/audit-resolution-v1/controls/replay.py PositiveBusiness --attempt positive-business-new
python eng/evaluation/studies/audit-resolution-v1/controls/replay.py LastWinner --attempt last-winner-new
```

Every attempt must have a new name. The script creates isolated package and
HTTP caches, uses `NuGet.Config` with the reachable upstream proxy (including
the audit source), and retains stdout, stderr, exact commands, process exits,
source/package/lock/assembly hashes, discovered tests, and xUnit XML under
`artifacts/audit-resolution/u12-oracle-controls/<attempt>/`.
It returns the actual test exit code: mutant failures are not converted to a
successful expected-failure result. All projects use xUnit v3 4.0.0 and net10.0;
the HTTP suite additionally uses the existing TestHost 10.0.11 package and
drives the actual mapped API through the fixed TestServer suite.

The aggregate findings and observed limitations are in
`.omo/audit-resolution/research/u12-oracle-controls.md`.
