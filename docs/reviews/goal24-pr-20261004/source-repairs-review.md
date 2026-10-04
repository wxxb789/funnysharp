# Goal 24 prepublication source repairs: focused code review

## Decision

- codeQualityStatus: WATCH
- recommendation: APPROVE
- blockers: None found in the bounded source-repair review.
- Scope of approval: the repairs below, not publication readiness, actual v6 replay success, or a new Goal 24 acceptance.
- Reviewer: omo-native-code-reviewer, task st_01a10888, direct native review with no delegation.
- Checkout: Q:/repos/funnysharp/.worktrees/goal24-complete-pr
- Independently observed HEAD: e7aea9995bac6c87a69db14a8b04c6925cb45d4c, branch audit/goal24-complete-pr. Repairs were working-tree changes, including untracked files.

## Findings by severity

CRITICAL: None.

HIGH: None.

MEDIUM: None.

LOW: None.

No verified regression was found in v3 XML input handling, replay boundaries, the changed assertions, immutable Git blob resolution, or the portable verifier's identity joins. The residual limitations below are not silently converted into passing evidence.

## Scope actually inspected

The tracked repair diff against HEAD was inspected in full. All 13 scoped actual files were read, including the new untracked files that ordinary git diff omits:

1. src/FunnySharp/Option.cs
2. eng/api-baseline/xml-reviewed-inputs.json
3. eng/harness/XmlBuildBindings.fs
4. tests/FunnySharp.Harness.Tests/XmlBuildBindingsTests.fs
5. eng/evaluation/comparisons/function-grammar/packages.lock.json
6. eng/harness/FrozenReplay.fs
7. eng/evaluation/replay-frozen.fsx
8. eng/evaluation/README.md
9. eng/harness/FunnySharp.Harness.fsproj
10. tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj
11. tests/FunnySharp.Harness.Tests/FrozenReplayTests.fs
12. docs/audits/goal-24-resolution/portable-evidence/verify.fsx
13. docs/audits/goal-24-resolution/portable-evidence/README.md

Supporting reads were limited to supplied review/QA documents, the retained U11 lock, the v6 manifest header, catalog entries for the changed files, and relevant frozen Evaluation.fs contract excerpts. The original 6,664-file cohort was not reviewed again. The four deferred structural line-count findings were not reopened.

## Correctness and contract assessment

### Option and XML successor

The Option diff removes exactly the two false InvalidOperationException XML comments. Equality still compares the presence tag and, only for present values, the payload comparer; default remains legal None. No executable change or prose-pinning test was introduced.

The v3 anchor separates current xmlSha256/lfXmlSha256 from historicalXmlSha256. XmlBuildBindings accepts only v2 or v3, requires the schema-specific assembly fields, retains exact shipping-input and policy hash checks, binds the historical digest to the original policy, and checks the built XML against the explicitly reviewed current representations. Capture and later sealed validation still bind the anchor itself, canonical build receipt, candidate/attempt, DLL/XML/PDB bytes and capture ordering. The original v1 policy has no working-tree diff and its independently computed SHA256 matches the v3 reviewedPolicy pin.

The new v3 theory distinguishes reviewed current XML, unreviewed drift and a false historical hash. The existing release/legacy inventory assertions remain; the added integration assertions exercise release acceptance of a reviewed successor while the historical exact path still rejects it. These are machine-consumed contract checks, not tests for removal of a sentence.

### Frozen replay

The isolated driver validates the independently supplied manifest hash, study identity, exact snapshot inventory and plan before reserving output. It rejects existing output and lexical results/input overlap, copies rather than rewrites retained inputs, stages the frozen environment, and requests verify with --study audit-resolution-v6 --replay. The generated FSI script loads the staged support and frozen Evaluation.fs, not the live build pipeline. Post-execution checks retain the original snapshot/manifest/plan bindings and recheck staged input hashes.

The relevant frozen runner excerpts confirm that bindReplay retains loadStudy's frozen-environment equality guard, selects frozen templates/oracles/feed, uses locked restore and marks records as replay. The added route does not supply a generation producer or consume a cohort slot. Compile order includes the new implementation after Evaluation.fs and includes its tests after the existing evaluation tests.

The historical manifest omitted Output.fs, Proc.fs, Repo.fs and Loc.fs. The driver binds contemporary copies separately and the README names this limitation explicitly. It does not represent those copies as recovered historical dependencies.

The tests use real Evaluation.mainWith behind injected dotnet outcomes to exercise staging, immutable history, failure retention and replay classification. They deliberately do not execute staged FSI source or real consumer compilation; the fixture explicitly labels that boundary. Their GREEN results therefore cannot establish successful execution of the actual retained v6 route.

### Comparison lock and portable verifier

The new comparison lock was read alongside Q:/repos/funnysharp-goal24-resolution/artifacts/audit-resolution/u11-attempt-01/packages.lock.json. A read-only git diff --no-index --ignore-space-at-eol --exit-code produced no content differences; only the retained file's CRLF versus the new file's LF representation differs. The FunnySharp package content hash and entire dependency graph are retained.

The portable verifier keeps its fixed locator SHA256 and byte length, keys frozen object identities by path plus hash, permits only consistent exact aliases, and does not allow directory anchors to satisfy proof pins. Its Git fallback uses local git cat-file blob with an exact 40-character object ID and checks the returned bytes' length/SHA256. It never fetches history or interprets original absolute paths as local fallbacks. The changed current Option and anchor cannot replace their historical proof bytes merely by matching a filename.

The verifier checks raw archive digests, exact provenance/index inventories, P/A/I candidate/hash joins, all 307 P-bound attachments, original criterion identities, retained native acceptance, all row evidence pins and all final-index pin triples before printing PASS. The full locator file was not reread as a historical cohort; its fixed hash and relevant changed-file mappings were inspected. Its README correctly limits the result to frozen byte/identity joins, not a rerun of experiments or a new acceptance decision.

## Independent evidence receipt

Read-only SHA256/length probes returned:

| Input | SHA256 | Bytes |
| --- | --- | ---: |
| current Option.cs | 8ee95f8d0193c71e9515f4c97616a1352ee8b4960d17628ca7b7c3cbe77cd707 | 20060 |
| current v3 xml-reviewed-inputs.json | d5319f334d5220b38861da411f5025cf28e4c187ef5b5258677751690ccc6140 | 10718 |
| unchanged xml-contract-bindings.json | 696fe5154ee970778ce7b415ea9bc964cedbe6158d34e3f6e545be3843d08601 | 27620 |
| retained v6 manifest.json | 83d7d3522ccad4fa04cc56bbf8ed540521147d178d1940cc656d9667e49ac76d | 33627 |
| portable locations.json | b5e8754c668b6e3640acca0da12ff7af1baf8a69aea9c7830b95e99281889657 | 426742 |
| new comparison packages.lock.json | 0c2c02e5d0b37f16839f2f08a6614473971d2fdab30daae94eb1c558bf6f1b38 | 5461 |

Local immutable blob probes independently confirmed:

- e01be8e7c84172a3771f8471a48acf4785dccc59: SHA256 2ab65969adf550465692cfa812ad2e8cc182306e1c2660eb549e1d21d873da36, 20254 bytes. This is catalogued historical Option.cs, distinct from the current repair.
- b2d4fe54f94495127e8b7b6a476d0455d483ac48: SHA256 b0411918c0b1ec71605614786453d84afcbdcb8d7c16ccca662585efa5fd47dc, 9331 bytes. This is the catalogued historical v2 anchor and matches v3's previousReviewedInputs pin.

The manifest and original XML policy have no working-tree diff against the observed HEAD. These probes authenticate the named local byte identities; they do not rerun the portable gate or prove remote artifact availability.

Evidence documents read under Q:/repos/funnysharp/docs/reviews/goal24-pr-20261004/:

- continuation-pr.md: current repair outcomes and pending publication/gate state.
- parent-focused-verification.json: parent portable/Option/restore evidence summary. Its currentBindingSuccessorPending field is an earlier checkpoint, not a current v3 failure or proof of completion.
- actual-replay-snapshot-diagnosis.json: 188 declared files, 123 retained files, 65 missing historical obj files, zero additions and zero hash mismatches.
- portable-negative-controls.json: four retained controls each report exit 1 and no PASS marker, with actual failure output for missing bundle, tampered retained bytes, tampered catalog and tampered ledger.
- prepublication-review.json: original defect context only; not reused as approval of these repairs.

The supplied task/continuation reports XmlBuildBindings 37/37, Option 2/2, FrozenReplay 18/18 plus 71 existing focused tests, exact U11 locked restore, full Release solution build, portable 69/120-pin/P-A-I/307-attachment verification and four actual negative controls. This reviewer inspected the named summaries and negative-control outputs, but did not independently execute these commands or observe their live completion. Raw command receipts for every reported test/build count were not supplied in the reviewed QA JSON; those counts remain attributed to the parent, not relabeled as reviewer-run evidence.

## Skill-perspective check

Loaded and applied both available skill files before judging test relevance and maintainability:

- C:/Users/lhan/.omo/binary-runtime/5.1.15/plugin/skills/remove-ai-slops/SKILL.md
- C:/Users/lhan/.omo/binary-runtime/5.1.15/plugin/skills/programming/SKILL.md

The applicable review perspective found no deletion-only tests, removal/prose pins, tautological expected values, pointless implementation-constant mirrors, untyped escape hatches, needless abstraction, or unnecessary production extraction/parsing/normalization introduced by these repairs. Hashing and parsing in these gates are required evidence-boundary behavior. Test fixtures hash independently supplied input bytes and include drift/failure cases. Contemporary-support and synthetic-process seams are explicit, not disguised as historical or real-process coverage.

Programming has no C#/F# language-specific reference set; its shared boundary, type, mock and test-discipline criteria were applied. The user explicitly excluded refactoring and reopening deferred line-count findings, so skill cleanup/refactor instructions did not expand this read-only review. No violation of either applicable correctness/test/slop perspective was established in the repair diff.

## Residual risks and publication state

1. Actual retained v6 replay remains RED. The inspected diagnosis reports 65 unavailable historical obj files out of 188; all 123 existing files hash-match, with no additions or mismatches. The driver correctly rejects this before output reservation/consumer build. Successful synthetic plumbing tests do not remove this limitation. Restore only independently recovered original bytes; do not weaken, omit or regenerate entries in the original manifest.
2. The exact-checkout formatter is pending in the supplied continuation (mon_PA5CWEWRG8AX3E7N / bash75). The earlier main-CWD formatter result is not current-candidate evidence. No later formatter outcome was observed in this review.
3. The clean final-candidate full test gate, current-candidate release/source API/docs gates and post-update CI remain incomplete or unverified here. The reported full Release build is not a replacement for those gates. This report is not merge/publication authorization.
4. Real staged-FSI/consumer execution after recovery of all historical bytes remains unverified. Contemporary support is explicitly bound but is not a recovered historical support closure. POSIX relocation was not exercised by this reviewer; the local Git lookup probes were on Windows with retained history. Raw published archives and required immutable blobs must remain available to any later complete portable verification.
5. No tests, builds, formatter, model, benchmark, HTTP, negative-control action, install, commit, push or remote communication was performed by this reviewer. F# LSP inspection was unavailable because fsautocomplete is not installed; no installation was attempted. Static reads, Git status/diff and local hash/blob probes are the review's own verification evidence.

Original product d8744c934d86833f9817245ecc7c78b1177b8b76 and its 69 accepted dispositions remain frozen. The v3 current-documentation successor is separate. WATCH reflects the explicit outstanding evidence boundaries, not a hidden source-repair blocker.
