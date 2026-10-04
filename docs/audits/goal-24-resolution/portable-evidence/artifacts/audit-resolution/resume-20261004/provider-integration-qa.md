# Parent real-surface adapter QA

Observed2026-10-04. Full P not yet executed.

## Preserved failed attempt

provider-surface-qa-01 failed on actual /get with ERR_INVALID_STATE: native imported-module Bun.WebView chrome backend is main-thread-only. Its metadata/failure JSON remain retained; its loopback bridge was closed and independently refused a subsequent request. No P was produced.

## Narrow runtime correction

feed-bridge.mjs now requires the owning eval kernel WebView constructor. Parent imports the updated module with a query-key and passes WebView:Bun.WebView. README and two input code hashes updated. No runtime/TLS trust setting, feed URL, product source, harness source or strict predicate changed.

## Actual successful QA

provider-surface-qa-02 used current input SHA d488ddc6bec8802c612ec267c3ad4c463c96f8006f4e6d2b75f202f8cf24e9a7. Undeclared HTTPS origin returned400; correct live original NuGet service returned200 with9272 exact response bytes/SHA cea4d74624be767e0569c891597c0e6f5329f19f6c9b7fcf8b8c9e9f4933a4bb at 2026-10-04T09:03:11.412Z through 2026-10-04T09:03:11.952Z; wrong attempt returned409. All owned views/server closed; subsequent request refused. No cached response will be used in full P.

Current258 evidence IDs and226localProofIds preserve all251 original evidence entries and219original proofs byte-for-byte; all other input fields are unchanged except actual current producer identity. Seven runtime source/assembly/deps pins match the actual QA startup inventory. JSON syntax/reference checks passed. Repository formatter exit0; it reports F# unsupported as before. F# adapter warnaserror+ compilation passed; JS import passed. JSONLSP unavailable (biome absent), JSdaemon timedout; no new tooling dependency installed.

Next: atomic data-only input commit, then a NEW actual complete owner freeze using the verified current adapter.

