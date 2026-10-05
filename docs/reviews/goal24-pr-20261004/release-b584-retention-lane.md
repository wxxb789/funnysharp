# b584 local r4 release retention lane

Task `st_01a109ee`, native category `deep-low`, completed data-only retention on 2026-10-05. No delegated child. The packet is in the integration checkout at:

`Q:/repos/funnysharp/.worktrees/goal24-complete-pr/docs/reviews/goal24-pr-20261004/current-release-b584-evidence`

It preserves the already completed `b584e748699a4f19d8a901695634be5a85ce8b07` / `r4` release as historical **LOCAL PASS subsequently REJECT FG-R9-001**. It is not the forthcoming Traverse XML successor or a new release, remote CI, publication or whole-Goal acceptance. Original d8744 approval remains frozen. Parent owns integration and final verification.

## Delivered bytes and file list

Catalog SHA256: `cc074f460185bc3d6d826bcf4e6117c3d806665118a7f5305228d4e3aa2b00ee`.

288 distinct reversible objects retain **113,282,860 decoded bytes**, represented by 408 SHA256-named base64 parts (**152,303,142 encoded bytes**) and 380 original locators. All parts have physical and decoded hashes/lengths; no ZIP is rewritten. `packet-files.json` is the complete physical file list. Packet metadata: `README.md`, `catalog.json`, `catalog.sha256`, `inventory.json`, `input-graph.json`, `verify-b584.py`, `verification-report.json`, `packet-files.json`, and 408 `objects/*.base64` files. This main-tree report is the only write outside the allowed integration packet.

All 219 required r4 files are retained: outcome, execution, release checks, 14 receipts, 28 command logs, verifier logs, XML seals, inventories, version receipts, four release archives, compatibility configs/results and four **complete** publish outputs: CoreTrimmed 36 files/20,696,326 bytes; CoreNativeAot 2/11,147,264; AspNetCoreTrimmed 114/24,309,615; AspNetCoreNativeAot 3/44,338,741. Actual operational attempt 01/02 RED outcomes, execution/receipt/log/config/results plus parent usage/fixed-output RED records remain exact. The six current sealed DLL/PDB/XML aliases come from exact r4 package entries, never moving integration bin/obj. Six retained package archives contain 42 checked entries, including bound historical policy packages.

The complete input graph cites 7,568 b584 source fingerprint paths as immutable Git blobs, plus two previous e7 references. All 2,840 distinct b584 blobs (449,433,193 bytes) and both added historical references matched their recorded hashes/lengths. Exact external System.Runtime.xml and the original policy DLL/XML bytes are retained. The two historical moving DLL candidates mismatched and were not captured; subsequent explicitly hash-bound original package entries supplied the actual policy DLL bytes. Source fingerprint before/after r4 is equal.

## Checks actually executed

A read-only Git status/ls-tree/cat-file sweep, in bounded byte batches, checked the immutable source bindings without compiling or using current working source. Bun byte reads/hash comparisons and ZIP-entry decoding collected exact retained bytes. The landed Python standard-library verifier ran inside the persistent Python eval kernel via `exec(compile(read(verify-b584.py), path, 'exec'), namespace)` followed by `namespace['run'](packet)`. It returned **PASS** for physical and decoded object integrity, catalog/graph/inventory pins, locator and 161 graph-edge joins, all package entries, all 14 command/receipt/log joins, ten recorded release checks, outcome/XML seal identities and all four publish directory inventories. Its Git recheck option was not run again; Git hashes were already independently verified by the preceding Bun/read-only Git sweep.

Strong credential-pattern scanning covered all 288 decoded objects and all 42 package entries, using 13 classes across Latin-1, UTF-16LE and one-byte-shifted UTF-16LE. **Zero candidates**, bounded candidate report limit 80. No credential content is included in scan findings. This reports bounded pattern coverage, not universal secret absence in arbitrary encodings, unretained caches or all cited source files. `verification-report.json` retains exact check names and scope.

## Exact limits and exclusions

The preflight/final 404 response body has only recorded SHA256 `e64db0fde59206a0cf85c783e644f5e2ffd6a519dbf7d720cc9e5b0ace3d46a4`, no captured raw bytes or known length. No response was synthesized or fetched. Usage exit 2 and custom-output precondition exit 1 have exact parent JSON records, but no separately named raw stream file is claimed. Two observed external runner/config files are byte-exact retention snapshots without a contemporaneous r4 hash pin; they are explicitly labeled `operational-input-observed-unpinned`.

Restore cache and compatibility bin/obj intermediates are excluded; published output bytes are included. The original v6 experiment's 65 unavailable obj files remain an unrelated historical gap. No build, test, benchmark, release, workload/protocol verifier, experiment rerun, source/lock/protocol/observation edit, Git mutation, commit, push, remote message, merge, NuGet operation, cleanup, polling or waiting was executed. Parent-owned source dirt was preserved.
