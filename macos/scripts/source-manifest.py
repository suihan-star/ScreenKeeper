#!/usr/bin/env python3
"""Create a manifest of build inputs, not local runtime data or prior outputs."""
from pathlib import Path
import argparse
import hashlib


def build_inputs(root):
    exact = ["Package.swift", "README.md", "TESTING.md", "DISTRIBUTION.txt"]
    paths = [root / name for name in exact]
    paths += list((root / "Sources").rglob("*.swift"))
    paths += list((root / "Tests").rglob("*.swift"))
    paths += [root / "Sources/ScreenKeeperMac/Resources" / name
              for name in ("LICENSE", "icon.svg", "forest-mist.png")]
    paths += [p for p in (root / "scripts").iterdir() if p.suffix in (".py", ".sh", ".swift")]
    for path in sorted(set(paths), key=lambda p: p.relative_to(root).as_posix()):
        if path.is_symlink() or not path.is_file() or not path.resolve().is_relative_to(root.resolve()):
            raise ValueError(f"Missing or unsafe build input: {path.name}")
        yield path


def manifest(root):
    return "".join(f"{hashlib.sha256(path.read_bytes()).hexdigest()}  {path.relative_to(root).as_posix()}\n"
                   for path in build_inputs(root))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    args.output.write_text(manifest(root), encoding="utf-8", newline="\n")
