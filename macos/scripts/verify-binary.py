#!/usr/bin/env python3
"""Inspect a built Mach-O; this does not test display hardware or Gatekeeper."""
from pathlib import Path
import argparse
import json
import posixpath
import re
import subprocess
import sys

def run(*command):
    return subprocess.check_output(command, text=True, stderr=subprocess.STDOUT)


def system_path(path):
    """Only absolute paths that remain under Apple's system library locations."""
    normalized = posixpath.normpath(path)
    return path.startswith("/") and normalized.startswith(("/usr/lib/", "/System/Library/"))


def portable_rpath(path):
    if system_path(path):
        return True
    for token in ("@executable_path", "@loader_path"):
        if path == token or path.startswith(token + "/"):
            # The checked executable lives at App.app/Contents/MacOS/ScreenKeeper.
            # An app-relative search path must not escape that bundle.
            relative = path[len(token):].lstrip("/")
            resolved = posixpath.normpath(posixpath.join("/App.app/Contents/MacOS", relative))
            return resolved.startswith("/App.app/")
    return False


def evaluate_macho(architecture, libraries, load_commands, build_version):
    """Evaluate captured tool output without executing tools or touching the filesystem."""
    architecture = architecture.strip()
    errors = []
    if architecture != "arm64":
        errors.append("Expected arm64 only")

    minimums = re.findall(r"^\s*minos\s+(\S+)", build_version, flags=re.MULTILINE)
    versions = []
    for value in minimums:
        if not re.fullmatch(r"\d+(?:\.\d+){0,2}", value):
            versions.append(None)
        else:
            parts = tuple(map(int, value.split(".")))
            versions.append(parts + (0,) * (3 - len(parts)))
    if not versions or any(version is None or version > (13, 0, 0) for version in versions):
        errors.append("Minimum macOS version exceeds 13.0 or is unknown")
    platforms = re.findall(r"^\s*platform\s+(\S+)", build_version, flags=re.MULTILINE)
    if not platforms or any(platform != "MACOS" for platform in platforms):
        errors.append("Expected macOS build platform")

    library_lines = libraries.splitlines()
    dependencies = [line.strip().split(" (", 1)[0] for line in library_lines[1:] if line.strip()]
    if not library_lines or not library_lines[0].rstrip().endswith(":") or not dependencies:
        errors.append("Missing or unrecognized dependency report")
    rpaths = re.findall(r"cmd LC_RPATH\s+cmdsize \d+\s+path (.*?) \(offset", load_commands)
    if len(rpaths) != len(re.findall(r"^\s*cmd LC_RPATH\s*$", load_commands, flags=re.MULTILINE)):
        errors.append("Unrecognized runtime search path command")
    for dependency in dependencies:
        if system_path(dependency):
            continue
        if re.fullmatch(r"@rpath/libswift[A-Za-z0-9_]+\.dylib", dependency) and "/usr/lib/swift" in rpaths:
            continue
        errors.append("Unbundled or unknown dependency: " + dependency)
    for path in rpaths:
        if not portable_rpath(path):
            errors.append("Nonportable runtime search path: " + path)
    return {"architecture": architecture, "minimumOS": minimums, "dependencies": dependencies,
            "rpaths": rpaths, "errors": errors, "hardwareValidated": False}


def main(argv=None, run_command=run):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("binary", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args(argv)
    binary = args.binary.resolve()
    output = args.output.resolve()
    if output == binary or output.with_suffix(".txt") == binary:
        parser.error("Report paths must not overwrite the binary")
    try:
        architecture = run_command("lipo", "-archs", str(binary))
        libraries = run_command("otool", "-L", str(binary))
        load_commands = run_command("otool", "-l", str(binary))
        build_version = run_command("xcrun", "vtool", "-show-build", str(binary))
    except (OSError, subprocess.CalledProcessError) as error:
        print(f"Mach-O inspection command failed: {error}", file=sys.stderr)
        return 2
    report = evaluate_macho(architecture, libraries, load_commands, build_version)
    output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    output.with_suffix(".txt").write_text(libraries + "\n" + build_version + "\n" + load_commands,
                                          encoding="utf-8")
    if report["errors"]:
        print("\n".join(report["errors"]), file=sys.stderr)
        return 1
    print("Mach-O architecture, minimum system and dependency checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
