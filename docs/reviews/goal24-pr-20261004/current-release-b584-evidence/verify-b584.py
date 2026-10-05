"""Read-only byte and credential-pattern verification for historical b584/r4."""
import argparse
import base64
import hashlib
import io
import json
from pathlib import Path
import re
import subprocess
import zipfile

CATALOG_SHA = "cc074f460185bc3d6d826bcf4e6117c3d806665118a7f5305228d4e3aa2b00ee"
PATTERNS = [
    ("private-key", r"-----BEGIN (?:RSA |EC |DSA |OPENSSH |ENCRYPTED )?PRIVATE KEY-----"),
    ("github-token", r"\b(?:gh[pousr]_[A-Za-z0-9]{30,255}|github_pat_[A-Za-z0-9_]{50,255})\b"),
    ("aws-access-key", r"\b(?:AKIA|ASIA)[A-Z0-9]{16}\b"),
    ("google-api-key", r"\bAIza[A-Za-z0-9_-]{35}\b"),
    ("slack-token", r"\bxox[baprs]-[A-Za-z0-9-]{20,200}\b"),
    ("stripe-live-key", r"\b(?:sk|rk)_live_[A-Za-z0-9]{20,200}\b"),
    ("jwt", r"\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{16,}\b"),
    ("authorization-token", r"\b(?:Authorization[\"']?\s*[:=]\s*[\"']?\s*(?:Bearer|Basic)|Bearer)\s+[A-Za-z0-9+/_=-]{20,}"),
    ("credential-url", r"https?://[^\s/:@]{1,120}:[^\s/@]{6,120}@"),
    ("azure-storage-key", r"AccountKey=[A-Za-z0-9+/]{40,}={0,2}"),
    ("sas-signature", r"[?&]sig=[A-Za-z0-9%+/_=-]{20,}"),
    ("literal-secret-assignment", r"(?:password|passwd|client[_-]?secret|api[_-]?key|access[_-]?token|secret[_-]?key)[\"']?\s*[:=]\s*[\"'][^\s\"'\r\n]{12,200}[\"']"),
    ("nuget-api-key", r"\boy2[A-Za-z0-9]{40,}\b"),
]
PATTERNS = [(name, re.compile(pattern, re.IGNORECASE if name in {
    "authorization-token", "credential-url", "azure-storage-key",
    "sas-signature", "literal-secret-assignment"
} else 0)) for name, pattern in PATTERNS]


def digest(data):
    return hashlib.sha256(data).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def run(packet, git=None):
    packet = Path(packet).resolve()
    raw_catalog = (packet / "catalog.json").read_bytes()
    require(digest(raw_catalog) == CATALOG_SHA, "Catalog SHA256 mismatch")
    require((packet / "catalog.sha256").read_text().split()[0] == CATALOG_SHA,
            "Catalog pin mismatch")
    catalog = json.loads(raw_catalog)
    graph_bytes = (packet / catalog["inputGraph"]["path"]).read_bytes()
    inventory_bytes = (packet / catalog["inventory"]["path"]).read_bytes()
    for info, data in [(catalog["inputGraph"], graph_bytes),
                       (catalog["inventory"], inventory_bytes)]:
        require(len(data) == info["length"] and digest(data) == info["sha256"],
                "Metadata pin mismatch: " + info["path"])
    graph = json.loads(graph_bytes)
    inventory = json.loads(inventory_bytes)
    payloads = {}
    physical_count = 0
    physical_bytes = 0
    physical_names = set()
    for obj in catalog["objects"]:
        decoded = []
        for part in obj["parts"]:
            p = (packet / part["path"]).resolve()
            require(p.is_relative_to(packet), "Part escapes packet")
            physical_names.add(part["path"])
            physical = p.read_bytes()
            require(len(physical) == part["physicalLength"] and
                    digest(physical) == part["physicalSha256"],
                    "Physical part mismatch: " + part["path"])
            raw = base64.b64decode(b"".join(physical.split()), validate=True)
            require(len(raw) == part["length"] and digest(raw) == part["sha256"],
                    "Decoded part mismatch: " + part["path"])
            decoded.append(raw)
            physical_count += 1
            physical_bytes += len(physical)
        data = b"".join(decoded)
        require(len(data) == obj["length"] and digest(data) == obj["sha256"],
                "Object mismatch: " + obj["sha256"])
        require(obj["sha256"] not in payloads, "Duplicate object")
        payloads[obj["sha256"]] = data
    require({str(p.relative_to(packet)).replace("\\", "/")
             for p in (packet / "objects").glob("*.base64")} == physical_names,
            "Missing or unbound object part")
    files = catalog["files"]
    require(len({f["originalLocator"] for f in files}) == len(files),
            "Duplicate locator")
    for f in files:
        require(f["sha256"] in payloads and len(payloads[f["sha256"]]) == f["length"],
                "Locator mismatch: " + f["originalLocator"])
    git_hashes = {g["sha256"] for g in graph["gitReferences"]}
    for edge in graph["edges"]:
        storage = edge.get("storage", "object")
        if storage == "object":
            require(edge["sha256"] in payloads, "Unresolved byte edge")
        elif storage == "git-blob":
            require(edge["sha256"] in git_hashes, "Unresolved Git edge")
        elif storage == "derived-representation":
            normalized = payloads[edge["fromSha256"]].replace(b"\r\n", b"\n")
            require(digest(normalized) == edge["sha256"], "LF representation mismatch")
        else:
            require(storage == "unavailable", "Unknown graph storage")
    original_root = catalog["immutableRoot"].replace("\\", "/").rstrip("/")
    r4_files = {f["originalLocator"][len(original_root) + 1:]: f
                for f in files if f["originalLocator"].startswith(original_root + "/")
                and "::" not in f["originalLocator"]}

    def r4(name):
        return payloads[r4_files[name]["sha256"]]

    outcome = json.loads(r4("release-outcome.json"))
    execution = json.loads(r4("execution-evidence.json"))
    release = json.loads(r4("release-evidence/release-evidence.json"))
    seal = json.loads(r4("xml-build-bindings.json"))
    require(outcome["succeeded"] and outcome["candidateCommit"] == catalog["candidate"]
            and outcome["attemptId"] == "r4", "Wrong historical outcome")
    for key in ["executionEvidence", "releaseEvidence"]:
        require(digest(r4(outcome[key]["path"])) == outcome[key]["sha256"],
                "Outcome pin mismatch")
    require(execution["sourceFingerprintBefore"] == execution["sourceFingerprintAfter"],
            "Source fingerprints changed")
    require(len(execution["commands"]) == 14 and
            all(c["exitCode"] == 0 for c in execution["commands"]), "Command outcome")
    for index, command in enumerate(execution["commands"], 1):
        for key in ["standardOutput", "standardError"]:
            require(digest(r4(command[key + "Log"])) == command[key + "Sha256"],
                    "Command stream pin")
        receipt = json.loads(r4("receipts/" + str(index).zfill(2) + "-" +
                                command["name"] + ".json"))
        require(receipt == command, "Receipt differs from execution command")
    require(release["succeeded"] and not release["failures"] and
            len(release["checks"]) == 10 and
            all(c["status"] == "passed" for c in release["checks"]), "Release checks")
    require(seal["candidateCommit"] == catalog["candidate"] and
            seal["attemptId"] == "r4", "Seal identity")
    require(digest(r4(seal["buildCommandReceipt"]["path"])) ==
            seal["buildCommandReceipt"]["sha256"], "Seal build receipt pin")
    for assembly in seal["assemblies"]:
        for kind in ["dll", "pdb", "xml"]:
            require(assembly[kind + "Sha256"] in payloads, "Missing sealed bytes")
    compatibility = json.loads(r4("compatibility-run/compatibility-results.json"))
    require(compatibility["Succeeded"] and
            {s["Scenario"] for s in compatibility["Scenarios"]} ==
            {"CoreTrimmed", "CoreNativeAot", "AspNetCoreTrimmed", "AspNetCoreNativeAot"}
            and all(s["Outcome"] == "Passed" for s in compatibility["Scenarios"]),
            "Compatibility scenario outcomes")
    for scenario in inventory["publishOutputs"]:
        prefix = "compatibility-run/" + scenario["scenario"] + "/publish/"
        published = [f for p, f in r4_files.items() if p.startswith(prefix)]
        require(len(published) == scenario["fileCount"] and
                sum(f["length"] for f in published) == scenario["length"],
                "Incomplete publish output: " + scenario["scenario"])
    candidates = []
    candidate_count = 0

    def scan(data, context):
        nonlocal candidate_count
        for encoding, offset, stride in [("latin1", 0, 1), ("utf-16-le", 0, 2),
                                          ("utf-16-le", 1, 2)]:
            text = data[offset:].decode(encoding, errors="ignore")
            for name, pattern in PATTERNS:
                for match in pattern.finditer(text):
                    candidate_count += 1
                    if len(candidates) < 80:
                        candidates.append({"context": context, "pattern": name,
                                           "encoding": encoding, "decodeOffset": offset,
                                           "characterOffset": match.start(),
                                           "matchSha256": digest(match.group().encode())})

    for h, data in payloads.items():
        scan(data, "object:" + h)
    archive_hashes = {e["archiveSha256"] for e in catalog["packageEntries"]}
    entry_count = 0
    entry_bytes = 0
    for archive_hash in archive_hashes:
        declared = {e["entry"]: e for e in catalog["packageEntries"]
                    if e["archiveSha256"] == archive_hash}
        with zipfile.ZipFile(io.BytesIO(payloads[archive_hash])) as archive:
            require(set(archive.namelist()) == set(declared), "Package entry inventory")
            for name, entry in declared.items():
                data = archive.read(name)
                require(len(data) == entry["length"] and digest(data) == entry["sha256"],
                        "Package entry pin: " + name)
                require(data == payloads[entry["sha256"]], "Package object mismatch")
                scan(data, "package:" + archive_hash + "::" + name)
                entry_count += 1
                entry_bytes += len(data)
    git_count = 0
    if git is not None:
        unique = {g["blob"]: g for g in graph["gitReferences"]}
        rows = list(unique.values())
        for start in range(0, len(rows), 128):
            chunk = rows[start:start + 128]
            result = subprocess.run(["git", "-C", str(git), "cat-file", "--batch"],
                                    input=("\n".join(g["blob"] for g in chunk) + "\n").encode(),
                                    capture_output=True, check=True)
            cursor = 0
            for g in chunk:
                end = result.stdout.index(b"\n", cursor)
                header = result.stdout[cursor:end].split()
                require(len(header) == 3 and header[1] == b"blob", "Git object header")
                length = int(header[2])
                data = result.stdout[end + 1:end + 1 + length]
                require(length == g["length"] and digest(data) == g["sha256"],
                        "Git blob pin: " + g["path"])
                cursor = end + 1 + length + 1
                git_count += 1
    require(len(payloads) == catalog["counts"]["objects"] and
            sum(map(len, payloads.values())) == catalog["counts"]["objectBytes"] and
            physical_count == catalog["counts"]["physicalObjectParts"] and
            physical_bytes == catalog["counts"]["physicalObjectBytes"] and
            len(r4_files) == inventory["retainedR4FileCount"], "Count totals")
    report = {"status": "PASS" if not candidate_count else "CREDENTIAL_CANDIDATES",
              "scope": "Read-only data integrity and bounded strong credential-pattern scan; historical LOCAL PASS subsequently REJECT FG-R9-001",
              "catalogSha256": CATALOG_SHA, "objects": len(payloads),
              "decodedBytes": sum(map(len, payloads.values())),
              "physicalParts": physical_count, "physicalBytes": physical_bytes,
              "locators": len(files), "requiredR4Files": len(r4_files),
              "packageArchives": len(archive_hashes), "packageEntries": entry_count,
              "packageEntryBytes": entry_bytes, "sourceGitReferences": len(graph["gitReferences"]),
              "gitBlobsRechecked": git_count, "graphEdges": len(graph["edges"]),
              "releaseCommands": 14, "releaseChecks": 10, "publishOutputs": inventory["publishOutputs"],
              "credentialPatternClasses": [name for name, _ in PATTERNS],
              "credentialCandidates": candidate_count, "candidateReportLimit": 80,
              "candidateReport": candidates, "missingByteLimits": catalog["missing"],
              "writes": 0, "networkRequests": 0, "workloadExecutions": 0}
    print(json.dumps(report, indent=2))
    require(candidate_count == 0, "Credential candidates require inspection")
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--git", type=Path, help="Optional read-only immutable Git blob check")
    args = parser.parse_args()
    run(Path(__file__).parent, args.git)
