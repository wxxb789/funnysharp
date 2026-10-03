#!/usr/bin/env bash
set -uo pipefail
cd "Q:/repos/funnysharp-goal24-resolution/eng/evaluation/results/audit-resolution-v5/async-streams/funnysharp/run-2/producer/0001-context" || exit 2
export PI_RULES_DISABLED=1
"C:/Users/lhan/.omo/binary-runtime/5.1.9/omo.exe" --mode json --print --no-session --no-extensions --no-context-files --no-skills --no-prompt-templates --no-tools --offline --provider ghc --model gpt-6.1-sol --thinking high --system-prompt 'The attached request.json contains a JSON object with inputFiles, each with path and content. Those file contents are the complete task inputs. Return only a JSON object with a files array; each entry contains path, a top-level C# filename, and content, the complete file text. Return the complete solution. Do not replace supplied contract or test files.' --extension "Q:/repos/funnysharp-goal24-resolution/.omo/audit-resolution/probes/u12-native-producer-boundary.mjs" "@Q:/repos/funnysharp-goal24-resolution/eng/evaluation/results/audit-resolution-v5/async-streams/funnysharp/run-2/producer/0001-context/request.json" </dev/null >"stdout.log" 2>"stderr.log"
status=$?
printf 'V5_NATIVE_EXIT async-streams/funnysharp/run-2 %s\n' "$status"
exit "$status"
