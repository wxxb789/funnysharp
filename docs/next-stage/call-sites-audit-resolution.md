# Audit Resolution Function-Workflow Comparison

Proposal provenance: U11, R8 and KTD5 of
`docs/plans/2026-10-01-1103-fix-goal-24-audit-resolution-plan.md`.
Planning checkout base: a8863473fd53eddc7cb47201508426db2e454f84,
tree 88e8a528017b0a5f68710cc1d955fda64486877f. Those identifiers describe
the supplied base, not the dirty candidate or a compiled package.
The proposed consumer explicitly references FunnySharp 0.2.0 and xunit.v3
4.0.0. Workflow source hashes, package/DLL hashes and execution receipts are
captured below. Attributable maintainer review remains open.

## Status and historical boundary

This is an additive, package-verified equivalence study, not a material-density acceptance record.
`call-sites-goal-16.md` and its source projects remain immutable.
Their 101 BCL versus 117 FunnySharp semantic-unit result remains negative
and partly nonequivalent. This successor does not relabel that result.

The complete seven-workflow consumer compiled and passed all 37 expanded
cases, with zero failures and zero skips. The retained receipt is
`artifacts/audit-resolution/u11-attempt-01/equivalence-red.json`; the attempt
name was selected before execution and does not imply a failure. Both initial
restore and the subsequent locked restore exited zero, using a fresh
attempt-local cache and a FunnySharp-only mapping to the canonical local feed.
Actual package metadata identifies that feed, not an upstream copy.
The nupkg SHA256 is
`fba1feaa30287ad94f7fcb60f87e170e1d2dd3ac6bb645e9b44a0cdc1029adb5`.
The packed and copied runtime DLLs both hash to
`37f4b3be823a254b5510c57f08e4e5127a7f7c0f567abe1144adc1b5a19d7fcc`.

Whole-class raw LOC is 122 for BCL and 119 for FunnySharp under the disclosed
physical-line rule; separately, whole-source-file LOC is 124 and 122. An
independent `deep-low` review on `gpt-6.1-sol` reproduces every member's count
and the 123 versus 144 semantic-unit totals. The parent reproduced the review's
LOC scope correction. This is not maintainer acceptance. Exact workflow
source hashes and the machine receipt are recorded in `SemanticUnits.json`.
The aggregate semantic-unit outcome remains negative.

The new project is
`eng/evaluation/comparisons/function-grammar/FunctionGrammarComparisons.csproj`.
It is outside the solution, has no ProjectReference, and consumes the packed
package rather than current source. It does not change the evaluation runner,
benchmarks, APIs, historical results, project references or shared lockfiles.

## Frozen common contracts

All input collections, service instances, callbacks and delegate arguments are
nonnull. AccountForm fields and SKU/weight/zone strings are nonnull. Invalid
null arguments outside this supplied input domain are not an equivalence claim.
Runtime-null dictionary values are explicitly included in WF-1 and WF-7.
Records use value equality; reference identity of successful form records is
not an output requirement. Error and result arrays are fully materialized.

| Pair | Inputs and observable boundary |
| --- | --- |
| WF-1 | Trim then invariant uppercase; audit exactly once before lookup; known nonnull product ID or UNKNOWN for missing/runtime-null value. Observer exceptions propagate unchanged. |
| WF-2 | One adjustment factory invocation per campaign on both sides; reuse its returned function at batch and quote sites. Multiply discount, then tax, then round away from zero to two digits. Both batches are eager decimal arrays. Include all pricing helpers and FunnySharp helper-field initialization. |
| WF-3 | Invariant decimal parsing. Only FormatException from parsing becomes an audited 4.90 default. Parse overflow propagates without audit. A missing zone audits once and uses 0.90 per unit. Arithmetic overflow and observer faults/cancellation propagate outside the parse boundary. Both public results are decimals with equal audit output. |
| WF-4 | One eager decimal array, one balance per transaction, source order, seed excluded. Empty input yields an empty array. ClosingBalance is a separate fold operation with its own source enumeration; it is not secretly a second enumeration inside RunningBalances. Both buffers and ToArray calls count. |
| WF-5 | Both styles construct reusable Task and ValueTask function values without executing stages. First stage completes before second starts. The exact supplied token reaches both stages; token cancellation alone does not preempt a stage that ignores it. Synchronous delegate throws are captured in returned awaitables. Ordinary faults retain identity; cancellation retains its token. Every ValueTask is consumed once. Batch callers materialize eager arrays sequentially. |
| WF-6 | Four independent checks accumulate machine-consumed error codes in email/password/age/country order. Successful output contains equal validated field values and an empty error array; failure contains null Value and all ordered errors. Both return FormOutput and serialize identically. All validators, successful value construction, and Validation elimination count. |
| WF-7 | Same nullable label output for missing item, runtime-null item, missing promo, null label, present label and empty present label. FunnySharp member grammar includes GetValueOrDefault elimination; no favorable query-syntax alternative replaces this pair. |

WF-3 uses a counted FormatException-only parsing helper. The existing
Result.Try API deliberately converts a broader non-cancellation exception set;
using it here without additional policy would recreate the historical defect.
No convenience API is added to avoid this cost.

## Whole-study semantic-unit rules

This is a disclosed successor counting rule, not a recalculation of historical
counts. Semantic units are S + O, counted syntactically once per source
occurrence, not multiplied by loop iterations or function invocations.

S counts initialized local or field declarations, assignments, invocation
statements, return/throw statements, control headers (if, else, loop, try,
catch, finally), and expression-bodied members. Expression-bodied lambda
bodies also count one S; block lambda bodies count their ordinary statements.
A declaration without an initializer does not count. An expression-bodied
factory and its expression-bodied lambda therefore count two S, not one.
This prevents compression into a lambda from making a consumer-owned body
disappear. Declaration headers, braces, comments and blank lines are excluded.

O counts method and delegate calls, explicit object/delegate construction,
collection expressions, indexing, each arithmetic/comparison/Boolean operator,
conditional/coalescing/null-conditional operators, and await. Compound +=
counts once, not additionally as +. Each interpolated-string hole counts
once. No counted workflow has more than one hole per interpolation.
Property/field access, method-group conversions and ConfigureAwait(false)
are excluded equally on both sides. Lambda creation is not a separate O;
its initialized declaration/member and body remain counted as S.

Count the complete two workflow classes, including private helpers, all four
validators, delegate initializers, factories, wrappers, materialization and
boundary conversions. Their shared-looking method bodies are intentionally
present and counted on both sides. Contract.cs contains supplied neutral DTO
and interface declarations only; tests and their causal probes are oracle
infrastructure and excluded from consumer counts. Neither file hides workflow
algorithms or style-specific validators.

`SemanticUnits.json` enumerates every consumer-owned member or initializer,
its S/O subtotal and operation inventory. Raw LOC is a separate physical
metric: nonblank, non-comment source lines including signatures and braces,
over each whole workflow class, including its helpers and initializers.
Raw LOC and source hashes are captured separately, not inferred from semantic totals.

## Provisional hand-count result

These counts are reviewable source-reading results, not executed or
maintainer-accepted measurements. The independent AI source recount is at
`.omo/audit-resolution/research/u11-independent-count-review.md`.

| Pair | BCL S | BCL O | BCL units | FunnySharp S | FunnySharp O | FunnySharp units | Direction |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| WF-1 | 3 | 7 | 10 | 4 | 9 | 13 | Loss |
| WF-2 | 8 | 12 | 20 | 9 | 15 | 24 | Loss |
| WF-3 | 9 | 9 | 18 | 13 | 18 | 31 | Loss |
| WF-4 | 8 | 6 | 14 | 4 | 5 | 9 | Win |
| WF-5 | 11 | 14 | 25 | 7 | 10 | 17 | Win |
| WF-6 | 10 | 22 | 32 | 8 | 34 | 42 | Loss |
| WF-7 | 1 | 3 | 4 | 3 | 5 | 8 | Loss |
| Whole study | 50 | 73 | 123 | 48 | 96 | 144 | Negative |

There is no provisional tie under this rule. WF-1's historical tie remains a
historical tie under its own rule; the new lambda/body-inclusive rule differs.
WF-5 now compares reusable function values on both sides, so the library owns
the sequential async wrapper on equal terms. That likely local win cannot
erase the other five losses. The whole-study total is larger for FunnySharp.

Construction bodies are included once for a one-shot consumer implementation.
Reusable execution uses the same source definitions and the same external
factory lifetime on both sides. Repeated use does not remove construction
source units, multiply source LOC, or justify an unmeasured runtime-allocation
claim. Static helper delegate initialization on the FunnySharp side is
explicitly counted. This is not a runtime performance benchmark.

## Equivalence oracle and execution

The executed xUnit v3 suite has 37 test cases after InlineData expansion:
WF-1 five; WF-2 one; WF-3 six; WF-4 five; WF-5 thirteen; WF-6 one
containing all sixteen masks; WF-7 six. All seven pairs are mandatory.
No test is skipped or timed by sleep/polling.

Pending-source tests install continuation-subscription signals before
invocation, await those exact signals, release each stage causally, and drain
the operation in finally. Ten-second waits are bounded failure detection
only. Both direct ValueTask stages and the ordinary Task composition surface
are exercised. The Task fake adapts the same controlled stage sources with
AsTask; single-consumption accounting includes that adapter. The test-only
adapter does not remove either native public factory or counted caller.

Run commands and source/package binding requirements are in
`.omo/audit-resolution/research/u11-resume.md`. Package equivalence passed as
recorded above; material-density and maintainer acceptance remain open.

## Acceptance boundary

Reproducibility and equivalence do not establish a material density reduction.
The user has not approved qualifying the original ambition. An honest negative
whole-study result therefore remains negative and does not close G16-02 or
G16-E4. Exact-source attributable maintainer acceptance, product-claim
disposition, and final package proof remain owner/main-session obligations.
An AI review or this proposal is not maintainer acceptance.
