# TODO

## Performance Infrastructure

- [ ] Provision a fixed-hardware self-hosted performance runner after the initial GitHub-hosted CI
  matrix is stable. Pin the OS, CPU configuration, .NET SDK/runtime, and power settings; establish a
  measured noise floor and source-bound historical baselines before making throughput regressions a
  blocking release gate. Until then, GitHub-hosted timing is informational and allocation budgets
  remain blocking.

## Runtime Support

- [ ] After .NET 11 reaches GA, add `net11.0` targeting and the full supported-runtime validation
  matrix: restore, build, tests, package assets, package-consumer smoke, trimming, Native AOT, and
  documented support evidence. Do not introduce .NET 11 Preview support into the current release.
