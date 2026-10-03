"""Replay exact fixed suites; preserve the actual failing process exit for mutants."""

import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET


CONTROLS = Path(__file__).resolve().parent
ROOT = CONTROLS.parents[4]
AREAS = {
    "PositiveBusiness": ("business-outcomes", "OrderWorkflow.cs", 10),
    "PositiveCollections": ("collections", "FeedCleaner.cs", 9),
    "PositiveStreams": ("async-streams", "SensorStream.cs", 10),
    "PositiveConcurrency": ("concurrency", "AvailabilityCoordinator.cs", 12),
    "PositiveHttp": ("aspnetcore", "Api.cs", 9),
    "EarlySideEffects": ("business-outcomes", "OrderWorkflow.cs", 10),
    "StreamingValidation": ("async-streams", "SensorStream.cs", 10),
    "LastWinner": ("concurrency", "AvailabilityCoordinator.cs", 12),
    "UnobservedCleanupFault": ("concurrency", "AvailabilityCoordinator.cs", 12),
    "UnboundedAdmission": ("concurrency", "AvailabilityCoordinator.cs", 12),
}


def hashed(path):
    return {"path": str(path.relative_to(ROOT)).replace("\\", "/"),
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("control", choices=AREAS)
    parser.add_argument("--attempt", required=True)
    args = parser.parse_args()
    area, source, expected = AREAS[args.control]
    if not args.attempt.replace("-", "").replace("_", "").isalnum():
        parser.error("attempt must be a simple append-only directory name")
    attempt = ROOT / "artifacts/audit-resolution/u12-oracle-controls" / args.attempt
    attempt.mkdir(parents=True, exist_ok=False)
    project = CONTROLS / "projects" / args.control / (args.control + ".csproj")
    tests = ROOT / "eng/evaluation/tasks" / area / "tests"
    if area in ("business-outcomes", "async-streams", "concurrency"):
        tests = CONTROLS.parent / "tasks" / area / "tests"
    inputs = sorted(tests.glob("*.cs"))
    if area == "business-outcomes":
        inputs.append(ROOT / "eng/evaluation/tasks/business-outcomes/tests/PlaceOrderTests.cs")
    inputs += [CONTROLS / "solutions" / source, project, CONTROLS / "Directory.Build.props",
               CONTROLS / "NuGet.Config", Path(__file__).resolve(), ROOT / "global.json",
               CONTROLS.parent / "plan.json"]
    receipt = {"schema": "u12-fixed-oracle-control/v1", "control": args.control,
               "classification": "control/replay; not independent AI generation or product acceptance",
               "area": area, "expectedTests": expected, "inputs": [hashed(p) for p in inputs],
               "commands": [], "cwd": str(ROOT)}
    environment = os.environ.copy()
    environment.update({"NUGET_PACKAGES": str(attempt / "packages"),
                        "NUGET_HTTP_CACHE_PATH": str(attempt / "http-cache"),
                        "DOTNET_CLI_UI_LANGUAGE": "en", "DOTNET_NOLOGO": "1"})
    receipt["environment"] = {key: environment[key] for key in (
        "NUGET_PACKAGES", "NUGET_HTTP_CACHE_PATH", "DOTNET_CLI_UI_LANGUAGE", "DOTNET_NOLOGO")}
    artifact_property = "-p:ControlArtifacts=" + str(attempt).replace("\\", "/") + "/"

    def save():
        (attempt / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")

    def execute(label, argv):
        result = subprocess.run(argv, cwd=ROOT, env=environment, capture_output=True, text=True,
                                encoding="utf-8", errors="replace", timeout=300)
        (attempt / (label + ".stdout.log")).write_text(result.stdout, encoding="utf-8")
        (attempt / (label + ".stderr.log")).write_text(result.stderr, encoding="utf-8")
        receipt["commands"].append({"stage": label, "argv": argv, "exitCode": result.returncode})
        save()
        print("STAGE " + label + " EXIT " + str(result.returncode), flush=True)
        print(result.stdout, end="", flush=True)
        print(result.stderr, end="", file=sys.stderr, flush=True)
        return result

    for label, argv in (
        ("sdk", ["dotnet", "--info"]),
        ("git-status", ["git", "status", "--short"]),
        ("git-head", ["git", "rev-parse", "HEAD"]),
        ("restore", ["dotnet", "restore", str(project), "--configfile", str(CONTROLS / "NuGet.Config"),
                     "--packages", str(attempt / "packages"), "--locked-mode", "--no-cache", artifact_property]),
    ):
        result = execute(label, argv)
        if result.returncode != 0:
            return result.returncode
    receipt["lock"] = hashed(project.parent / "packages.lock.json")
    receipt["packages"] = [hashed(p) for p in sorted((attempt / "packages").rglob("*.nupkg"))]
    save()
    result = execute("build", ["dotnet", "build", str(project), "-c", "Release", "--no-restore", artifact_property])
    if result.returncode != 0:
        return result.returncode
    assembly = attempt / "build" / args.control / "bin/Release/net10.0" / (args.control + ".dll")
    receipt["assembly"] = hashed(assembly)
    result = execute("discover", ["dotnet", "exec", str(assembly), "-list", "full", "-nocolor"])
    if result.returncode != 0:
        return result.returncode
    xml = attempt / "tests.xml"
    result = execute("test", ["dotnet", "exec", str(assembly), "-nocolor", "-failSkips", "-result-xml", str(xml)])
    if xml.exists():
        assemblies = ET.parse(xml).getroot().findall("assembly")
        receipt["counts"] = {key: sum(int(a.get(key, "0")) for a in assemblies)
                             for key in ("total", "passed", "failed", "skipped", "errors")}
        receipt["failedCases"] = [{"name": test.get("name"),
                                    "message": test.findtext("failure/message"),
                                    "stackTrace": test.findtext("failure/stack-trace")}
                                   for a in assemblies for test in a.findall("collection/test")
                                   if test.get("result") == "Fail"]
        receipt["testXml"] = hashed(xml)
    receipt["inputsUnchanged"] = receipt["inputs"] == [hashed(p) for p in inputs]
    save()
    print("CONTROL " + args.control + " EXIT " + str(result.returncode)
          + " COUNTS " + json.dumps(receipt.get("counts", {})), flush=True)
    # A negative control remains a failing process, never an expected-failure success wrapper.
    return result.returncode


if __name__ == "__main__":
    sys.exit(main())
