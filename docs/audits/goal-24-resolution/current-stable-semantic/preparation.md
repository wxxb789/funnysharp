# Current stable-semantic preparation for PR33

Preparation is complete and independently verified. The successor binds actual current PR33 source and exact c306/r5 XML snapshot bytes while preserving all original historical claims, compiler/runtime observations, limits and thresholds. Final current release and semantic acceptance remain parent-owned. No compiler/runtime/HTTP/benchmark/model/denial/P-A-I workload was replayed.

## New artifacts

The first four paths are relative to `Q:/repos/funnysharp/.worktrees/goal24-complete-pr`.

| Path | Bytes | SHA256 |
| --- | ---: | --- |
| docs/audits/goal-24-resolution/current-stable-semantic/current-proof-index.json | 16906459 | ca4f5e9e3f442e418a514e31c64390a41f19da0fa1dbef183430bcaea474c4aa |
| docs/audits/goal-24-resolution/current-stable-semantic/input-catalog.json | 1178065 | 1f93bad25bf321d960ead8bad05fbc9eb0f21806a0dbaddba272387eea6b855a |
| docs/audits/goal-24-resolution/current-stable-semantic/inputs.zip | 6613339 | b52a61633427531ccf3caf7241d3c7208a0ca3b2efdd1c2eb5d4eb0c9f1496a1 |
| docs/audits/goal-24-resolution/current-stable-semantic/credential-scan.json | 2303 | 781b8649b0cc3b786afa005f02b38c68a18029cda80e7ddaae639ccdd1bfc113 |

Preparation reports are `Q:/repos/funnysharp/docs/reviews/goal24-pr-20261004/current-stable-semantic-preparation.json` and this Markdown file. The JSON report contains complete migrations, snapshot origins, scan rules and verification evidence.

Original index: `docs/audits/goal-24-resolution/stable-api-semantic-proofs.json`, SHA256 `ad106d7ee1ed4772e898bc17edb020a3b4e3f836f32678613e13cc95a813da18`. It is retained verbatim in the ZIP and was not modified.

## Complete preserved bindings

The index retains exactly 468 stable identities, 3,744 dimension records, 565 source units, 697 assertion units, 1,635 historical runtime cases and 2,288 actual historical compiler probes. All original compiler proof objects, required closure groups, applicability, claims, mechanism limitations, experimental exclusions and acceptance thresholds remain unchanged. Original XML, installed metadata and receipt roles are explicit historical evidence, not newly observed current metadata.

Independent Python readback reopened the ZIP, checked CRCs, the exact entry set, all 2,340 SHA256/byte-length bindings, and all 4,722 unique case-preserving aliases. Total unique uncompressed bytes are 67,900,363; ZIP size is 6,613,339 bytes. Deduplication joins only identical SHA256 and byte lengths. Safe entries have the sole form `sha256/<lowercase SHA256>`.

All 55 original external `fileHashes` inputs are present, together with additional original structured package/DLL/PDB/control bindings, eight historical runtime witness receipts, and the original index. This collection had 76 historical binding occurrences before compiler sources and current XML were added. Every original logical alias and corresponding absolute original-root alias is present. All 2,288 actual compiler Source files have both their exact absolute Source aliases (only separators changed to /) and logical relative aliases. The actual original matrix SHA256 is `0a6487965df18f19841d24a78e72515d3653845615d7d765e764da78f77bd3c4`; its raw JSON, absolute Source strings, and all raw receipts were preserved without rewriting. The 1,635 case IDs and passed statuses were independently joined to three actual raw runtime receipts.

Consumers must resolve historical bytes solely through exact catalog alias plus declared SHA256 and byte length, then independently verify archive bytes. There is no shipped old-Q filesystem fallback. No archive alias starts with `src/` or `tests/`: current source is read only from the actual checkout. Eight bound original final-format test snapshots and embedded original proof excerpts remain archival evidence under their original historical paths and cannot satisfy current-source checks.

## Exact current-source migration

All 103 original current source/test pins were checked against actual checkout bytes. Exactly six changed full-file SHA256 pins were refreshed in the new successor:

| Current file | Old SHA256 | Current SHA256 |
| --- | --- | --- |
| src/FunnySharp/AsyncSequenceExtensions.cs | 28d9549c202603dbe44c6075754f7ef1ef9edfca67bd486e0434742b66e69a17 | 5a0ea53ad1e8eedd56cc54e39f629a69e86f9998147f71259cccdc827898941f |
| src/FunnySharp/Option.cs | 2ab65969adf550465692cfa812ad2e8cc182306e1c2660eb549e1d21d873da36 | 8ee95f8d0193c71e9515f4c97616a1352ee8b4960d17628ca7b7c3cbe77cd707 |
| src/FunnySharp/PartitionExtensions.cs | 54414223c3dc4d0ac0b1b93b7abb8076c8720f1d276ad651f3dd19b38f502e7b | 57edfa4d39b0a8f6e9bdd9913d4eb0418a5c0e5fabe82c4e7e35bd7581ad5db7 |
| src/FunnySharp/SequenceExtensions.cs | 480804a00dfd2a6e0cce1d6cfc8a8a1c427c6a6c254482a1c991b050e53e6b9d | f9c6ba3c4ae07b532c02bc7cded4bc51fb47b554512b7c5c87a92c128c37eb50 |
| tests/FunnySharp.AspNetCore.Tests/HttpResultExtensionsTests.cs | 7fe9624b31b3fb4438132ed7f3fc65ff68639b5e3dc0a4db71ebed957dd9f188 | 8a9da2c461728248a102cf3449c293d475e8141969788071701feecbdd1b1a07 |
| tests/FunnySharp.AspNetCore.Tests/KestrelCancellationTests.cs | 3b892479fdf102a7d3cbc8bfe03ca037a79f92623bed55ea75c1ffa20fce8f8d | d17eb207ca0db19502754f5e536f1760fc0fe3bf2fa34fcfb636597861eb5cc0 |

All 565 exact source excerpts, all 697 methodBody values, and all 393 helperSpans.code occurrences matched uniquely in current source. No declaration, code excerpt, assertion text, method body or helper body was rewritten. The five source span relocations are:

- src/FunnySharp/Option.cs:449-450 -> src/FunnySharp/Option.cs:447-448
- src/FunnySharp/Option.cs:453-464 -> src/FunnySharp/Option.cs:451-462
- src/FunnySharp/Option.cs:437-437 -> src/FunnySharp/Option.cs:436-436
- src/FunnySharp/Option.cs:443-443 -> src/FunnySharp/Option.cs:441-441
- src/FunnySharp/Option.cs:447-447 -> src/FunnySharp/Option.cs:445-445

The four assertion spans moved by 14 lines in `HttpResultExtensionsTests.cs`:

- AsyncOverloadsValidateEveryRequiredArgument: 541-605 -> 555-619
- EnvironmentEffectReceivesTheExplicitEnvironment: 392-412 -> 406-426
- RequiredMappersAreValidatedAndProblemsWithoutStatusAreRejected: 523-538 -> 537-552
- ResultAndValidationEffectsMapWithAndWithoutEnvironments: 415-491 -> 429-505

The same migration updates four case source lines and 19 helper-span coordinate occurrences. Every assertionLines entry matches actual source at its new line. Source-unit keys, row source spans, file pins and all current source references agree; unchanged claims do not become fresh runtime observations.

## Source-bound XML snapshots

The exact actual c306/r5 nupkg XML bytes were extracted without normalization and are virtual catalog aliases inside the ZIP:

- `current-xml/FunnySharp.xml`: 474,331 bytes; SHA256 `35dd9f2354a0dc45799e32adf90e7649594aa62988f423a42fa12185ae63f5b3`.
- `current-xml/FunnySharp.AspNetCore.xml`: 32,514 bytes; SHA256 `27af8c3d2b65daa59a081f0b24674be0c41fe9cd652a19455cce01f2637e66d0`.

All 431 current xmlNodes, directNode identities/hashes/lines and configured alias type-node hashes/lines match the snapshots exactly. Of the original used nodes, 31 changed text (29 R9 clauses and two Option equality nodes); 264 changed text or coordinates. Original XML and installed metadata are retained in `historicalXmlNodes`, `historicalFinalXmlMetadata`, per-row `historicalMetadata`, and `xmlAndAliases.historicalXmlBinding`. They were not rebound to new DLL, token or MVID identities.

The snapshot origins are historical sealed candidate `c306991992b04299a3300235ce3285d4537fe6c2`, attempt `r5`. Their current source file pins are included in the successor. Actual post-build live release metadata/XML must still be joined by the parent against its real current seal; this preparation is not that gate.

## Bounded credential findings

No concrete credential candidates were found. The scan covers every one of the 2,340 decompressed selected ZIP entries, all 16 direct members of the two archived nupkgs, and the final successor index/catalog: 2,358 payloads totaling 86,852,754 scanned bytes. UTF-8 and UTF-16LE views were examined where applicable. The scan report binds the exact final index and catalog SHA256 values.

Rules cover private-key headers, GitHub/AWS/Slack/Google/Stripe/OpenAI token formats, Azure storage/SAS secrets, long literal bearer tokens, credential-bearing URLs and named secret literals. Matched values would be withheld; no external credential validation was performed. This bounded pattern scan does not establish absence of arbitrary formats, encrypted/obfuscated values or credentials in unselected private-history files, full artifact roots, Git history, remote assets or unrelated user-owned inputs.

## Integration boundary

The parent owns enforced `eng/harness/StableApiContracts.fs` and required-CI integration, live post-build DLL/PDB/XML/metadata seal checks, and full gates. Use actual checkout bytes for current source; use only catalog aliases and hashes for historical byte inputs. Preserve the original historic receipts and exact limitations as evidence, not current execution. Preparation has no missing bytes, changed-body findings, alias gaps, migration errors or credential blockers. Original private-history and artifact roots were read-only and remain untouched; full-directory retention is a separate lane.
