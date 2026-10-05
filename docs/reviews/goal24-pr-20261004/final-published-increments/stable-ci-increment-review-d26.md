# Stable semantic CI increment: independent d26 gate review

**Recommendation: APPROVE. Blockers: none.** No concrete violation of the bounded brief's success criteria was found in this 11-file increment. This is an implementation/data review, not a declaration that d26/r6, the live positive CLI, published CI, or Goal 24 has passed.

Candidate: `d26b159fdf6fe32c8afa04be92ee0e335f35dc3c`; parent: `c9acfba0091519510005558993e35d021b0ecd31`; tree: `533ea078513c1e5fe0a0c1b32bd49bcb8c60f82c`. Review date: 2026-10-05.

Actual route: **`ghc/gpt-6-astra`**, read directly from `PI_PROVIDER` and `PI_MODEL`. Reviewer: native `omo-native-gate-reviewer`, task `st_01a10ac1`; runtime session `01a10ac1-1a93-77a5-b43c-11b024c134af`. Effort was not exposed. No delegation occurred.

The companion `stable-ci-increment-review-d26.json` has SHA256 **`c46efc09dda1377489e6fa638051092f86aea29cbd058df018f759d438da692b`**. It contains the complete checked-path ledger, all 11 candidate file hashes, six independently reproduced migration hashes, eight brief-derived criteria, direct skill-perspective coverage and exact evidence gaps.

## Findings and evidence notes

There are no CRITICAL, HIGH, MEDIUM or LOW correctness findings and no blockers.

- **D26-N1, NOTE:** No separate d26 code-review report or current d26 manual-QA matrix was named in the brief or found among the named review-directory files. `source-repairs-review.md` and `postsubmit-independent-c9acfba.md` explicitly show both required skill perspectives and overfit/slop coverage, but they cover earlier work. `corrected-parent-qa-matrix.md` is for c306, not d26. This review therefore performed its own complete code-diff pass rather than treating those reports as current approval. The brief does not make a separate d26 reviewer artifact an acceptance criterion.
- **D26-N2, NOTE:** The brief reports a focused 17-test pass, complete portable proof pass, actual old-candidate CLI rejection and formatter completion using four monitor IDs. It supplies no raw completion artifact paths, and two bounded searches did not locate those outputs. These results remain attributed to the parent. `current-stable-semantic-parent-probe.fsx` was read in full; its source is not proof of its execution. No conflicting result was found, and this task expressly forbids workload/proof replay.

Neither note is evidence that the increment fails a stated criterion. Neither is silently promoted to a passing execution result.

## Original intent and desired user outcome

The valid PR33 feedback was that required CI did not execute `verify-stable-api-contracts`, while the existing real index depended on absent private/workstation artifacts and stale source pins. Adding that old command alone would install a known-failing check.

The desired result is a required Windows path that consumes a portable, byte-bound successor index, checks actual checkout source and assertions, retains historical compiler/runtime observations, and separately verifies current release DLL/PDB/XML and loaded metadata before publishing the canonical candidate artifact. Original audit accounting, evidence identities, adverse outcomes and thresholds must remain intact.

The inspected implementation establishes that path. Its current execution acceptance remains the parent's separate r6 and live-CLI obligation, as explicitly assigned by the brief. This review does not approve publication or claim that an unpushed workflow has run remotely.

## Immutable scope and preservation

Read-only Git inspection confirmed exact d26 HEAD and parent/tree. Initial `git status --porcelain=v1` was empty. The complete increment contains four modifications and seven additions, with no deletion:

- `.github/workflows/release.yml`
- `build.fsx`
- `eng/harness/StableApiContracts.fs`
- `tests/FunnySharp.Harness.Tests/StableApiContractsTests.fs`
- `docs/audits/goal-24-resolution/current-stable-semantic/{README.md,credential-scan.json,current-proof-index.json,input-catalog.json,inputs.zip,preparation.json,preparation.md}`

The reviewed working files matched immutable d26 with no diff. The complete four-file code diff was inspected. `git diff --check c9acfba0091519510005558993e35d021b0ecd31 d26b159fdf6fe32c8afa04be92ee0e335f35dc3c` returned exit 0.

The original index is outside the changed paths and independently hashes to `ad106d7ee1ed4772e898bc17edb020a3b4e3f836f32678613e13cc95a813da18`. Original accounting, policies, ledgers and proof paths have no change in this increment. This is direct preservation evidence; it is not a new classification or recount of the original 69/175 records.

All six refreshed full-file hashes were independently reproduced and agree with the successor: `AsyncSequenceExtensions.cs`, `Option.cs`, `PartitionExtensions.cs`, `SequenceExtensions.cs`, `HttpResultExtensionsTests.cs` and `KestrelCancellationTests.cs`. The five migrated Option source coordinates were checked against `src/FunnySharp/Option.cs:425-463`.

The successor's inspected header, receipt roles, full `fileHashes` region, a complete sampled member row and historical/current XML metadata sections preserve the distinction between source continuity and execution. For example, `current-proof-index.json:44105-44389` uses `current-xml/FunnySharp.xml` while retaining the original installed assembly hash, token and receipt under `historicalXmlBinding`/`historicalMetadata`, not as a current DLL identity.

The original and successor header counts and limitations agree. The exhaustive 565-source-unit, 697-assertion-unit, 393-helper-occurrence, 1,635-case, 2,288-probe and all-claim preservation assertions remain attributed to preparation. This reviewer did not perform an exhaustive parsed comparison of the 298,884-line successor.

## Required Windows enforcement and live release boundary

`.github/workflows/release.yml:40-91` runs the new command after mandatory canonical release and before canonical artifact upload. There is no `continue-on-error` or optional step condition. The supplied path contains the same candidate SHA and run/attempt identity used by the canonical output layout. Linux/macOS consumers still depend on `win-x64`.

`build.fsx:131-142` forwards both `--proof-index` and `--release-evidence`; its existing `gate` function at lines 36-43 terminates the process with the verifier's nonzero code. The workflow therefore cannot report successful canonical upload after a failed semantic step through this path. Existing job names and triggers are unchanged. Remote branch rules and check outcomes were not queried.

`eng/harness/StableApiContracts.fs:267-307` requires:

1. The immutable original index hash.
2. A passing release report whose `environment.commit` equals actual `git rev-parse HEAD`.
3. An empty current `git status --porcelain --untracked-files=all`.
4. Actual current Release XML bytes equal to the successor's reviewed XML snapshots.
5. The existing `getReleaseXmlDocumentationInventory` post-build binding path.

That last call is substantive, not just a count check. `eng/harness/ReleaseVerifyArtifacts.fs:766-775` invokes `XmlBuildBindings.validate`, whose complete implementation was read. It binds the external execution manifest, candidate/attempt, canonical successful build receipt, shipping inputs, reviewed policy, capture ordering and both assemblies' DLL/PDB/XML hashes. `ReleaseVerifyArtifacts.fs:564-615` then checks loaded assembly bytes, and `XmlBuildBindings.fs:75-82` compares loaded MVID with the sealed PE before the established XML alias/inheritance policy is applied.

The standalone semantic command consumes a successful release report; it is not a second implementation of the entire release verifier. In the required workflow, that report is produced by the immediately preceding canonical release. This review withholds the actual d26/r6 execution result rather than inferring it from static control flow.

## Portable inputs and retained semantic checks

`eng/harness/StableApiContracts.fs:194-248` checks catalog/archive paths are contained beneath the repository and verifies both outer hashes. It validates catalog schema, declared entry uniqueness, byte length, inner SHA256, alias uniqueness and exact non-directory ZIP membership.

ZIP entry names are never used as extraction destinations. Each output filename is the validated 64-hex digest beneath a unique temporary directory. Per-entry streams close before hashing, the archive/catalog are scoped resources, and `finally` removes the temporary directory after success or failure. The callback consumes only these bound bytes and actual checkout inputs.

Aliases normalize only slash direction and retain case. Canonical `src/` and `tests/` aliases are rejected. An unknown absolute/drive path, `artifacts/` path or `.omo/` path fails instead of falling back to the original workstation. Remaining checkout fallback must be root-contained. The complete catalog text search found no traversal or canonical current-source aliases. `.gitattributes` preserves source line endings and frozen audit bytes on Windows checkout.

The portable adapter invokes the same semantic validator as the old route. The diff changes that validator only by injecting its path resolver. Its checks at `StableApiContracts.fs:30-188` still include exact full-file hashes, source excerpts and excerpt hashes, assertion bodies/lines/helpers, actual receipt case-ID/name/type/method/status joins, compiler source hashes, expected/actual diagnostics, suppression and location checks, legal exact binding, exact member identities, all eight dimension names, required references/runtime applicability and XML/policy bindings. It does not replace these checks with declared totals.

The old `validateProofIndex` and `verify root proofPath` entry points remain. Zero/one-argument `main` retains the old path; a second argument selects the new live-release portable mode. This preserves old callers without creating an automatic historical fallback in the new CI path.

Independently reproduced outer identities:

| Artifact | SHA256 |
| --- | --- |
| `current-proof-index.json` | `ca4f5e9e3f442e418a514e31c64390a41f19da0fa1dbef183430bcaea474c4aa` |
| `input-catalog.json` | `1f93bad25bf321d960ead8bad05fbc9eb0f21806a0dbaddba272387eea6b855a` |
| `inputs.zip` | `b52a61633427531ccf3caf7241d3c7208a0ca3b2efdd1c2eb5d4eb0c9f1496a1` |
| `credential-scan.json` | `781b8649b0cc3b786afa005f02b38c68a18029cda80e7ddaae639ccdd1bfc113` |
| `preparation.json` | `046f495fae5324453a7157045b6e5125c7c33bde24634dba25f565ef1c00ac58` |
| `preparation.md` | `b940861a7a1917cbb9706dab6d171cea3f4747d2a7bdf691060ffe52116330f2` |

The catalog records for both current XML snapshots and the original index were read directly. ZIP inner bytes, all 2,340 entry bindings and all 4,722 aliases were not independently replay-verified here. Their exhaustive checks remain attributed to the preparation worker, not converted into reviewer evidence.

## Direct programming and overfit/slop pass

Both available skill files were loaded and applied directly:

- `C:/Users/lhan/.omo/binary-runtime/5.1.15/plugin/skills/remove-ai-slops/SKILL.md`
- `C:/Users/lhan/.omo/binary-runtime/5.1.15/plugin/skills/programming/SKILL.md`

The pass covered the full changed production/test code, dispatch/workflow diff and supporting seal call chain. Programming has no F# language-specific reference set, so its shared correctness, type, boundary, resource and test criteria were used. Cleanup/delegation/refactoring instructions did not expand this explicit read-only task. No module-size or architectural preference was made a blocker.

One positive and five negative cases were added at `tests/FunnySharp.Harness.Tests/StableApiContractsTests.fs:73-136`. They use actual ZIP, JSON and filesystem behavior. The positive deletes the original receipt/XML inputs, forcing archive consumption. The absolute-fallback negative deliberately leaves a real local file available and expects rejection, distinguishing correct behavior from a permissive fallback. Missing entry, wrong inner hash, unindexed entry and current-source alias cases each corrupt a distinct boundary condition.

No excessive/useless test, deletion-only test, requested-removal test, prose pin, tautological expectation or internal-call mirror was found. The fixture deletions are preconditions, not the assertions being tested. Hash expectations come from fixture input bytes. There are no new mocks, sleeps, polls, retries, skips, weaker assertions or warning suppressions. Existing negative tests remain intact.

The resolver extraction is required to reuse the existing validator and preserve its public legacy seam. ZIP/JSON parsing and separator normalization serve the requested evidence boundary; no unrelated production parser, compatibility layer or normalization was introduced. Explicit null matches and `JsonElement` use do not suppress type errors. Resource cleanup was traced directly.

The earlier `source-repairs-review.md#Skill-perspective-check` explicitly covers both skills and the same removal/prose-pin, tautology, implementation-mirror, abstraction and parsing criteria. Its scope is earlier repairs. That report was consulted, not substituted for this direct d26 pass.

## Credential scope, verification limits and withheld conclusions

The complete credential report, regex rules and limitations were read and its outer hash matches preparation. It binds the actual final index/catalog hashes and describes selected ZIP entries, direct nested package members and UTF-8/UTF-16LE views. No concrete credential was identified in inspected text. The scan was not rerun; no universal clearance of opaque, encrypted, arbitrary-format or unselected material is asserted.

The parent's reported 17-test pass, portable proof pass, old-c306/r5 exit-1 rejection and formatter result remain attributed to monitor IDs `mon_J07J5VT89KWR02SV`, `mon_Z0R9KXBJ99P58M76`, `mon_JDTSZ8RMY0YVZ30T` and `mon_EN90QJ3NSQV5C14X0`. Their raw completion paths were not supplied or located. No tests, build, release, semantic CLI, workload, proof replay, scan replay, wait or installation ran in this child.

F# LSP was unavailable (`fsautocomplete` not installed). JSON report diagnostics were also unavailable (`biome` not installed); no package was installed. The written JSON was read back directly, but no machine parser result is claimed. An initial read-only shell metadata probe used a PowerShell command in Bash and exited 127 after successful Git output; a later labeled `printf` probe returned the exact route. No candidate file was affected.

There is no exposed JS eval, todo, `agentToolkit.status` or `apply_patch` tool. The explicit two report destinations govern, and the required available `write` tool was used. Only these report files were written; no source, Git ref, remote state, original evidence or cleanup target was changed.

**Withheld:** d26/r6 release/full-solution PASS; its 14-command/full-count/current-seal acceptance; actual current positive stable CLI PASS; exact-head remote CI PASS; feedback/babysitting completion; merge or publication readiness; whole-goal approval; fresh execution of historical runtime/compiler evidence; universal credential clearance; durable retention or owned cleanup completion.

**Final decision: APPROVE for the immutable d26 11-file increment, with zero blockers and the evidence boundaries above.**
