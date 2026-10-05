# Current performance evidence

This packet retains the actual single fresh serial main/competitor benchmark pair for source commit `1fbb50df35e6b1a4cd423460eb6d2d149a64517f`, after the two incorrect Option equality XML exception comments were removed. Main executed 190 rows; competitor executed 40 rows. The original Performance verifier accepted all 230 rows under unchanged allocation budgets and unchanged policy/protocol bytes. Timings remain directional. The original d8744 three-repetition evidence and 69-finding approval are separate, unchanged history.

## Read-only portable check

From a complete checkout with .NET 10:

```powershell
dotnet fsi --warnaserror+ docs/reviews/goal24-pr-20261004/current-performance-evidence/verify-current.fsx -- .
```

`catalog.json` SHA256 is `093523fa54b126b9f65dd34718371f7f2d11d4afc729cc85cf4b3e4f6d98d187`. The packet contains 387 logical locators and 365 SHA256-named reversible base64 byte objects, 41,710,931 distinct source bytes. The verifier reads only current checkout files and these objects. Original Q-drive paths are locator labels resolved through the catalog; they are never old-filesystem fallbacks. It writes nothing, makes no network request and does not execute benchmark workloads or tests.

The check covers source/protocol fingerprints, original raw policy hashes, exact verifier-generated observations, all 230 actual launch bindings, raw reports, preflight/source snapshots, loaded/workload PE bytes and MVIDs, allocation ceilings, the 499/468/31 current metadata census, and 24 passing CTRF/TRX witness cases. The actual original strict verifier exits are recorded in the catalog; this portable check does not claim that unchanged absolute-path producers rerun on every checkout.

## Actual boundaries

The independent current-census/runtime lane used exact benchmark-bound DLL direct references. It is a shipping-assembly metadata and runtime proof, not a new nupkg-layout, feed or public NuGet publication proof. Its source, locked dependency graph, real command records and failed verifier attempts are retained as byte objects. Parent compared every one of the 499 projected metadata fields against the current census and ran both original strict performance verifiers. Seven resource methods account for 23 cases; one existing delegate Fact covers all six generated BeginInvoke/EndInvoke calls, which correctly throw PlatformNotSupportedException without invoking targets or callbacks on modern .NET.

Only current observation bodies and generated performance regions were replaced. Original raw policy bytes, allocation thresholds, exclusions and protocol files are untouched. The former two RealManifest failures now pass with 2 total / 0 errors / 0 failed / 0 skipped / 0 not run. The full clean-candidate suite and release/remote CI remain separately reported gates, not implied by this packet.

Four historical source-review JSON bodies are retained byte-identically in the sibling historical-source-reviews directory. They remain historical review basis; no source-only record was relabeled as a new runtime observation. Original replay limitations, adverse density/model/timing results and historical proof whitespace diagnostics remain visible.
