#!/usr/bin/env python3
"""Portable regression tests for packaging checks; no Mach-O execution or hardware access."""
from contextlib import redirect_stdout, redirect_stderr
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
from unittest import mock
import hashlib
import io
import json
import subprocess
import tempfile
import unittest


def load_script(name):
    path = Path(__file__).resolve().parent / name
    spec = spec_from_file_location(name.replace("-", "_"), path)
    module = module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


binary_check = load_script("verify-binary.py")
source_manifest = load_script("source-manifest.py")


def library_report(*libraries):
    return "/tmp/ScreenKeeper.app/Contents/MacOS/ScreenKeeper:\n" + "".join(
        f"\t{library} (compatibility version 1.0.0, current version 1.0.0)\n" for library in libraries)


def rpath_report(*paths):
    return "".join(f"Load command {index}\n          cmd LC_RPATH\n      cmdsize 48\n"
                   f"         path {path} (offset 12)\n" for index, path in enumerate(paths))


def version_report(minimum="13.0", platform="MACOS"):
    return f"Load command 1\n      cmd LC_BUILD_VERSION\n platform {platform}\n    minos {minimum}\n      sdk 15.0\n"


class BinaryCheckTests(unittest.TestCase):
    def evaluate(self, architecture="arm64\n", libraries=None, rpaths="", version=None):
        return binary_check.evaluate_macho(architecture,
            libraries if libraries is not None else library_report("/usr/lib/libSystem.B.dylib",
                "/System/Library/Frameworks/AppKit.framework/Versions/C/AppKit"),
            rpaths, version if version is not None else version_report())

    def test_arm64_macos_13_system_dependencies_pass_without_claiming_hardware_validation(self):
        report = self.evaluate()
        self.assertEqual(report["errors"], [])
        self.assertEqual(report["architecture"], "arm64")
        self.assertEqual(report["minimumOS"], ["13.0"])
        self.assertFalse(report["hardwareValidated"])

    def test_version_components_are_normalized(self):
        for version in ("13", "13.0", "13.0.0", "12.7.5"):
            with self.subTest(version=version):
                self.assertEqual(self.evaluate(version=version_report(version))["errors"], [])

    def test_wrong_or_universal_architecture_fails(self):
        for architecture in ("x86_64", "arm64 x86_64", "arm64e", ""):
            with self.subTest(architecture=architecture):
                self.assertIn("Expected arm64 only", self.evaluate(architecture=architecture)["errors"])

    def test_unknown_malformed_and_too_new_minimums_fail(self):
        for version in ("", "unknown", "13.0beta", "13.0.0.1", "13.0.1", "13.1", "14.0"):
            with self.subTest(version=version):
                self.assertIn("Minimum macOS version exceeds 13.0 or is unknown",
                    self.evaluate(version=version_report(version))["errors"])

    def test_all_build_commands_must_be_macos_with_supported_minimum(self):
        for extra in (version_report("14.0"), version_report(platform="IOS")):
            with self.subTest(extra=extra):
                self.assertTrue(self.evaluate(version=version_report() + extra)["errors"])
        self.assertIn("Expected macOS build platform", self.evaluate(version="minos 13.0\n")["errors"])

    def test_non_system_libraries_and_path_escape_fail(self):
        for library in ("/opt/homebrew/lib/libExample.dylib", "/Users/builder/.build/libExample.dylib",
                        "@rpath/libExample.dylib", "@loader_path/libExample.dylib",
                        "/usr/lib/../../Applications/libExample.dylib"):
            with self.subTest(library=library):
                self.assertIn("Unbundled or unknown dependency: " + library,
                    self.evaluate(libraries=library_report(library))["errors"])

    def test_system_swift_rpath_is_explicit_and_filename_is_not_a_path(self):
        swift = library_report("@rpath/libswiftCore.dylib")
        self.assertEqual(self.evaluate(libraries=swift, rpaths=rpath_report("/usr/lib/swift"))["errors"], [])
        self.assertTrue(self.evaluate(libraries=swift)["errors"])
        self.assertTrue(self.evaluate(libraries=library_report("@rpath/libswift/../../Other.dylib"),
                                      rpaths=rpath_report("/usr/lib/swift"))["errors"])

    def test_developer_rpaths_and_bundle_escape_fail(self):
        for path in ("/Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift/macosx",
                     "/Users/builder/project/.build/arm64-apple-macosx/release",
                     "/opt/homebrew/lib", "@executable_path_suffix/lib", "@loader_path_evil",
                     "@executable_path/../../../outside", "/usr/lib/../../Users/builder"):
            with self.subTest(path=path):
                self.assertIn("Nonportable runtime search path: " + path,
                    self.evaluate(rpaths=rpath_report(path))["errors"])

    def test_system_and_bundle_contained_rpaths_pass(self):
        self.assertEqual(self.evaluate(rpaths=rpath_report("/usr/lib/swift", "@executable_path",
            "@loader_path/../Frameworks", "@executable_path/../Frameworks"))["errors"], [])

    def test_empty_or_malformed_tool_output_is_not_a_success(self):
        self.assertIn("Missing or unrecognized dependency report", self.evaluate(libraries="")["errors"])
        self.assertIn("Missing or unrecognized dependency report",
                      self.evaluate(libraries="unexpected output\n/usr/lib/libSystem.B.dylib\n")["errors"])
        self.assertIn("Unrecognized runtime search path command",
                      self.evaluate(rpaths="cmd LC_RPATH\ncmdsize 48\npath ???\n")["errors"])

    def test_cli_writes_reports_and_returns_failure_for_invalid_binary_output(self):
        responses = {("lipo", "-archs"): "x86_64\n", ("otool", "-L"): library_report("/usr/lib/libSystem.B.dylib"),
                     ("otool", "-l"): "", ("xcrun", "vtool"): version_report()}
        commands = []

        def runner(*command):
            commands.append(command)
            return responses[command[:2]]

        with tempfile.TemporaryDirectory(prefix="screenkeeper-build-check-") as directory:
            output = Path(directory) / "binary.json"
            with redirect_stdout(io.StringIO()), redirect_stderr(io.StringIO()):
                result = binary_check.main([str(Path(directory) / "ScreenKeeper"), str(output)], runner)
            self.assertEqual(result, 1)
            self.assertIn("Expected arm64 only", json.loads(output.read_text(encoding="utf-8"))["errors"])
            self.assertTrue(output.with_suffix(".txt").exists())
        self.assertEqual(len(commands), 4)

    def test_cli_preserves_tool_failure_instead_of_claiming_pass(self):
        def runner(*command):
            raise subprocess.CalledProcessError(1, command)

        with tempfile.TemporaryDirectory(prefix="screenkeeper-build-check-") as directory:
            output = Path(directory) / "binary.json"
            with redirect_stdout(io.StringIO()), redirect_stderr(io.StringIO()):
                result = binary_check.main([str(Path(directory) / "ScreenKeeper"), str(output)], runner)
            self.assertEqual(result, 2)
            self.assertFalse(output.exists())


class SourceManifestTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="screenkeeper-manifest-check-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name) / "macos"
        for name in ("Package.swift", "README.md", "TESTING.md", "DISTRIBUTION.txt",
                     "Sources/ScreenKeeperCore/Example.swift", "Tests/ScreenKeeperCoreTests/ExampleTests.swift",
                     "Sources/ScreenKeeperMac/Resources/LICENSE", "Sources/ScreenKeeperMac/Resources/icon.svg",
                     "Sources/ScreenKeeperMac/Resources/forest-mist.png", "scripts/build-macos.sh",
                     "scripts/source-manifest.py", "scripts/GenerateIcon.swift"):
            self.write(name, name.encode("utf-8"))

    def write(self, relative, contents=b"fixture"):
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(contents)
        return path

    def entries(self):
        return {line.split("  ", 1)[1]: line.split("  ", 1)[0]
                for line in source_manifest.manifest(self.root).splitlines()}

    def test_manifest_hashes_relative_source_paths_deterministically(self):
        manifest = source_manifest.manifest(self.root)
        self.assertEqual(manifest, source_manifest.manifest(self.root))
        entries = self.entries()
        self.assertIn("Sources/ScreenKeeperCore/Example.swift", entries)
        self.assertIn("Tests/ScreenKeeperCoreTests/ExampleTests.swift", entries)
        self.assertIn("scripts/GenerateIcon.swift", entries)
        for name, digest in entries.items():
            self.assertFalse(Path(name).is_absolute())
            self.assertEqual(hashlib.sha256((self.root / name).read_bytes()).hexdigest(), digest)
        self.assertEqual(list(entries), sorted(entries))

    def test_outputs_runtime_data_and_previous_manifests_are_excluded(self):
        before = source_manifest.manifest(self.root)
        for name in ("dist/build/ScreenKeeper.app/Contents/MacOS/ScreenKeeper", ".build/debug/Generated.swift",
                     "Data/mac-settings.json", "Data/Runtime.swift", "PrivacyImages/private.png",
                     "mac-settings.json", "SOURCE-SHA256SUMS.txt", "scripts/test.log", "scripts/__pycache__/cache.pyc"):
            self.write(name, b"private or generated fixture")
        self.assertEqual(source_manifest.manifest(self.root), before)

    def test_missing_required_input_fails_instead_of_silently_omitting_it(self):
        for name in ("Package.swift", "DISTRIBUTION.txt", "Sources/ScreenKeeperMac/Resources/forest-mist.png"):
            with self.subTest(name=name):
                path = self.root / name
                content = path.read_bytes()
                path.unlink()
                try:
                    with self.assertRaisesRegex(ValueError, "Missing or unsafe build input"):
                        source_manifest.manifest(self.root)
                finally:
                    path.write_bytes(content)

    def test_symlink_input_is_rejected_even_when_link_cannot_be_created_on_host(self):
        # Exercise this guard on Windows hosts that cannot create real symlinks as well.
        original = Path.is_symlink
        target = self.root / "Package.swift"
        with mock.patch.object(Path, "is_symlink", lambda path: path == target or original(path)):
            with self.assertRaisesRegex(ValueError, "Missing or unsafe build input"):
                source_manifest.manifest(self.root)

    def test_resolved_input_outside_root_is_rejected(self):
        # A symlinked parent directory is not caught by is_symlink on the leaf file.
        original = Path.resolve
        target = self.root / "Package.swift"
        outside = Path(self.temporary.name) / "outside.swift"
        with mock.patch.object(Path, "resolve", lambda path, *args, **kwargs:
                outside if path == target else original(path, *args, **kwargs)):
            with self.assertRaisesRegex(ValueError, "Missing or unsafe build input"):
                source_manifest.manifest(self.root)

    def test_actual_external_symlink_is_rejected_when_host_supports_it(self):
        outside = Path(self.temporary.name) / "external.swift"
        outside.write_text("external fixture", encoding="utf-8")
        target = self.root / "Sources/ScreenKeeperCore/External.swift"
        try:
            target.symlink_to(outside)
        except OSError as error:
            self.skipTest(f"Host cannot create symlinks: {error.__class__.__name__}")
        with self.assertRaisesRegex(ValueError, "Missing or unsafe build input"):
            source_manifest.manifest(self.root)


if __name__ == "__main__":
    unittest.main()
