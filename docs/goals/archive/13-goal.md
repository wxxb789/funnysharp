/goal
Independently audit whether FunnySharp satisfies every accepted completion contract from Goals 1-12, and issue an evidence-backed verdict without treating repository claims or a green test suite as sufficient proof. Evaluate the actual public API and behavior for the .NET 10 foundation; BCL-first and performance-first constraints; sync/async consistency; function composition, currying, and partial application; Option; Result and fail-fast railway flows; accumulating Validation and traversal; tidy and zero-copy-friendly pipelines; state/state-machine semantics; thin effects and resource safety; structured concurrency; opt-in immutability and pragmatic optics; ASP.NET Core integration; documentation, packaging, trimming and Native AOT claims; and the supported-platform compatibility contract. Confirm the agreed exclusions: no custom runtime, replacement collection ecosystem, default immutable-data tax, large typeclass or monad-transformer hierarchy, analyzer requirement, or premature general discriminated-union implementation. Do not require .NET 11 targeting or support before .NET 11 reaches GA.

Completion evidence is a goal-by-goal pass, fail, or unverified matrix. Every verdict must link to candidate-bound, reproducible build, xUnit v3, integration, documentation-compilation, API-inventory, dependency, package-consumer, supported-platform, and fair benchmark evidence as applicable. Record every material gap with a minimal reproduction or precise missing artifact, distinguish correctness defects from design and evidence gaps, and preserve the immutable contracts in `goals/archive/01-goal.md` through `goals/archive/12-goal.md`.

Report two independent final states:

- `Audit status: COMPLETE` only when every applicable contract has been evaluated and every material gap is recorded with evidence. Audit completion may coexist with failed product acceptance.
- `Product acceptance: PASS` only when every material criterion passes. Any material `FAIL` or `UNVERIFIED` result requires `Product acceptance: FAIL`.

This goal is an audit and evaluation boundary. Do not implement new scope during verification to conceal or erase a failure.
