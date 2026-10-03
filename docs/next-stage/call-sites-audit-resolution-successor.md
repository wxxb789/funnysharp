# Audit Resolution Function-Workflow Successor

This append-only successor retains the original negative seven-workflow comparison in [call-sites-audit-resolution.md](call-sites-audit-resolution.md). Base commit `a8863473fd53eddc7cb47201508426db2e454f84` describes the checkout base, not a clean or accepted candidate. The exact package-only successor is `artifacts/audit-resolution/u11-successor-indexed/attempt-01`.

## Observable equivalence and regression

All 37 original expanded cases remain unchanged. Adding `Wf4IndexedSourceRetainsOneEnumerationAndDisposal` exposed a real fault in the proposed BCL `Select(...).ToArray()` baseline: the indexed List source was never enumerated through its generic interface. Actual execution was 37 passed and 1 failed, Expected 1 / Actual 0. Restoring only BCL `RunningBalances` to foreach preserved the frozen one-enumeration/one-disposal contract. The complete 38-case native consumer then passed, with zero errors, failures, skips and not-run cases and process exit 0.

The actual runtime and package-cache DLLs both hash to `6a903f737356fdf67845c9cd39385e4a5e2b39bcae0ee92ff2882480ecb7e49d`; the canonical 0.2.0 nupkg hashes to `49886c3c6e505d03b1e3565bfe71d43f2476eae3edfd35c4d6afbe761203fc18`. Initial and locked restores both exited 0. See [the full source/package red-green proof](../audits/goal-24-resolution/evidence/u11-indexed-source-red-green.json).

## Complete equal-rule source accounting

The unchanged S+O rule counts both complete consumer classes, all construction, lambda bodies, materialization and output conversions. The independent native `deep-low` review `st_01a0fb93` used `ghc/gpt-6.1-sol` with high reasoning effort. The parent checked all 10 exact file hashes, all 26 member inventories, subtotal arithmetic and both LOC scopes. This is AI source accounting, not attributable maintainer acceptance.

| Pair | BCL units | FunnySharp units | Direction |
| --- | ---: | ---: | --- |
| WF-1 | 9 | 9 | Tie |
| WF-2 | 14 | 14 | Tie |
| WF-3 | 18 | 17 | Win |
| WF-4 | 14 | 9 | Win |
| WF-5 | 23 | 17 | Win |
| WF-6 | 24 | 24 | Tie |
| WF-7 | 4 | 6 | Loss |
| Whole study | 106 | 96 | 3 wins, 3 ties, 1 loss |

BCL totals are S=38/O=68; FunnySharp totals are S=32/O=64. Ten fewer source-occurrence units is an arithmetic reduction of 9.43 percent for this exact successor. Whole-class raw LOC is 86/76 and whole-file raw LOC is 88/79. LOC and S+O are distinct metrics; neither establishes runtime allocation or performance.

Exact complete source hashes: BCL `10d982b0a3eed246ff2aba6fb83d582a9370f4e017881bc2d0a089bc3949b044`; FunnySharp `8b0728945c3e8ecfc371e54975f5ad8b0fbc8e6294f022656aa5eb666931da8c`. All source occurrences and input hashes are retained in `.omo/audit-resolution/research/u11-successor-source-accounting.{md,json}`.

## Acceptance boundary

The original 123/144 negative result, its 37 cases and the earlier 101/117 partly nonequivalent historical comparison remain unchanged. This successor uses ordinary ordered error arrays for WF-6; it provides no evidence that Validation grammar became shorter. No favorable subset, new convenience API, excluded conversion, hidden helper or construction-lifetime adjustment is used to manufacture the total.

Materiality has no supplied numeric threshold. The exact-source arithmetic and complete equivalence proof are ready for owner review, but neither material-density acceptance nor attributable maintainer readability acceptance has been supplied. U11 and the original associated final findings remain open until that required acceptance exists.
