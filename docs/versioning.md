# Versioning

This page states how a FunnySharp release earns its version number and how the shipped surface may
change. It is the document the [product contract](product-contract.md) names from the Stability
Boundary section; the release history and the supported-runtime statement are in
[release notes](release-notes.md).

## Version Numbering

- **Semantic versioning with preview semantics.** `0.x` releases are preview candidates: the
  surface is deliberately small and every stable member is expected to survive, but a `0.x` minor
  bump may still break a caller. `1.0.0` is the first release that promises stability across a
  whole major version.
- **Patch releases** fix defects only. They may not add, remove, or change a public member, and
  they may not change a documented behavior contract.
- **Minor releases** may add stable members under an accepted goal, and may change or remove an
  experimental member. They may not remove a stable member.
- **Major releases** may remove stable members, and must carry a migration note for each.

## The Committed API Baseline

[`eng/api-baseline/`](../eng/api-baseline) records the exported types and declared members of
`FunnySharp` and `FunnySharp.AspNetCore`, rendered from the built shipping assemblies.

- `dotnet fsi build.fsx -- -p verify-api-baseline` compares the built surface with the committed
  files and exits `1`, naming every added, removed, or changed member, when they differ. The release
  audit applies the same comparison to a candidate, so a drifted candidate cannot pass.
- `dotnet fsi build.fsx -- -p verify-api-baseline --write` regenerates the committed files. This is
  a deliberate, reviewable change: **a baseline change requires an accepted goal** that defines the
  new behavior and its evidence, and the regeneration lands with that goal. Regenerating the
  baseline to silence the gate is exactly the invalid change the gate exists to catch.
- The baseline is the machine-checkable half of the stability boundary. The [stability
  inventory](stability-inventory.md) is the human-readable half.

## Compatibility Commitments

- **Stable members** are public, XML-documented, covered by semantic tests, listed in the
  committed baseline, and benchmark-characterized or explicitly excluded with a rationale where
  performance-relevant. They change only through a later accepted goal.
- **Experimental members** carry `System.Diagnostics.CodeAnalysis.ExperimentalAttribute` with a
  documented diagnostic ID, appear in the [stability inventory](stability-inventory.md), make their
  status visible at every use site, and carry **no** compatibility promise: they may change or be
  removed without a breaking-change process. Callers opt in by suppressing that diagnostic ID.
- **A `0.x` preview is not a support promise.** The packages declare `EnablePackageValidation` and
  ship reproducible inputs, so any published artifact can be compared against the tree that
  produced it, but no external feed is served by the repository itself.
- **No competitor compatibility surface exists or is promised.** There is no alias, carrier
  conversion, or naming concession for another library, and no migration guarantee from one.
