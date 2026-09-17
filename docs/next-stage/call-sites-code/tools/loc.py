#!/usr/bin/env python3
"""Extract a C# method body by name and report raw LOC (non-blank, non-comment)."""
import re
import sys


def extract(path, name):
    text = open(path, encoding="utf-8").read()
    lines = text.split("\n")
    # Find method declaration line (allow modifiers and return types).
    pat = re.compile(rf"\b{re.escape(name)}\s*[<(]")
    start = None
    depth = 0
    for i, line in enumerate(lines):
        if start is None:
            if pat.search(line) and ("static" in line or "public" in line or "private" in line):
                # Skip references inside expressions: require it to be declaration-ish.
                start = i
                depth = 0
            continue
    if start is None:
        return None
    # Scan from start for balanced braces or terminating semicolon for expr-bodied.
    j = start
    seen_brace = False
    while j < len(lines):
        line = lines[j]
        depth += line.count("{") - line.count("}")
        if "{" in line:
            seen_brace = True
        if seen_brace and depth == 0:
            break
        if not seen_brace and ";" in line:
            break
        j += 1
    body = lines[start : j + 1]
    raw = [l for l in body if l.strip() and not l.strip().startswith("//")]
    return body, raw


def main():
    path, name = sys.argv[1], sys.argv[2]
    result = extract(path, name)
    if result is None:
        print(f"NOT FOUND: {name}")
        return
    body, raw = result
    for i, line in enumerate(body):
        print(f"{i:3d}| {line}")
    print(f"--- raw LOC (non-blank, non-comment): {len(raw)}")


if __name__ == "__main__":
    main()
