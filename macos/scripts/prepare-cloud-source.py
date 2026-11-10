#!/usr/bin/env python3
"""Prepare a reviewable private-build snapshot locally. Never uploads or commits."""
from pathlib import Path
import argparse
import hashlib
import json
import stat
import subprocess
import sys
import zipfile
from importlib.util import module_from_spec, spec_from_file_location

parser = argparse.ArgumentParser()
sys.dont_write_bytecode = True
parser.add_argument("--output", type=Path, required=True, help="New empty directory; must not already exist")
args = parser.parse_args()
mac = Path(__file__).resolve().parents[1]
repo = mac.parent
output = args.output.resolve()
if output == repo or repo.is_relative_to(output) or output.is_relative_to(mac):
    raise SystemExit("Snapshot output must not replace source or be nested inside the Mac module")
if output.exists():
    raise SystemExit("Snapshot output already exists; choose a new build directory")
spec = spec_from_file_location("source_manifest", mac / "scripts/source-manifest.py")
source_manifest = module_from_spec(spec)
spec.loader.exec_module(source_manifest)
inputs = list(source_manifest.build_inputs(mac))
inputs += [mac / name for name in (".gitignore", "AGENTS.md", "Build on Mac.command")]
inputs += [repo / "LICENSE", repo / ".github/workflows/mac-preview.yml"]
payload = {}
for path in inputs:
    if path.is_symlink() or not path.resolve().is_relative_to(repo):
        raise SystemExit("Unsafe source path")
    payload[path.relative_to(repo).as_posix()] = path.read_bytes()
baseline = subprocess.check_output(["git", "-c", f"safe.directory={repo.as_posix()}", "rev-parse", "HEAD"],
                                  cwd=repo, text=True).strip()
payload["README.md"] = (
    "# ScreenKeeper private Mac build snapshot\n\n"
    "Only the Mac development source and its manual build workflow are included.\n"
    "No Windows implementation, user settings, recovery records, or credentials are included.\n"
    "Run the manual workflow only after checking the account's available allowance and spending limits.\n"
    "The output is an ad-hoc signed preview, not a notarized release.\n"
    "See [Mac development notes](macos/README.md) and [tester instructions](macos/DISTRIBUTION.txt).\n"
).encode()
# Preserve frozen bytes across Windows staging and the Mac checkout so the
# manifest identifies the actual uploaded inputs, regardless of core.autocrlf.
payload[".gitattributes"] = b"* -text\n"
payload["SOURCE-STATE.json"] = (json.dumps({"sourceBaselineCommit": baseline,
    "sourceState": "uncommitted working-tree snapshot", "scope": "Mac source and manual workflow only",
    "uploaded": False, "nativeBuildExecuted": False}, indent=2) + "\n").encode()
payload["macos/SOURCE-SHA256SUMS.txt"] = source_manifest.manifest(mac).encode()
manifest = "".join(f"{hashlib.sha256(data).hexdigest()}  {name}\n" for name, data in sorted(payload.items()))
payload["BUILD-SOURCE-SHA256SUMS.txt"] = manifest.encode()
output.mkdir(parents=True)
snapshot = output / "repository"
snapshot.mkdir()
for name, data in payload.items():
    target = snapshot / name
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(data)
archive = output / "ScreenKeeper-private-mac-build-source.zip"
with zipfile.ZipFile(archive, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as package:
    for name, data in sorted(payload.items()):
        info = zipfile.ZipInfo(name)
        info.create_system = 3
        info.external_attr = (stat.S_IFREG | (0o755 if name.endswith((".sh", ".command")) else 0o644)) << 16
        info.compress_type = zipfile.ZIP_DEFLATED
        package.writestr(info, data)
with zipfile.ZipFile(archive) as package:
    assert package.testzip() is None
    for name, data in payload.items():
        assert package.read(name) == data
digest = hashlib.sha256(archive.read_bytes()).hexdigest()
(output / "SHA256SUMS.txt").write_text(f"{digest}  {archive.name}\n", encoding="utf-8", newline="\n")
print(json.dumps({"directory": str(snapshot), "files": len(payload),
                  "zip": str(archive), "sha256": digest, "uploaded": False}, indent=2))
