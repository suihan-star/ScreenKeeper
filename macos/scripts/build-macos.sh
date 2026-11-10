#!/bin/bash
set -euo pipefail

TASK_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$TASK_ROOT"
if [[ "$(uname -s)" != Darwin || "$(uname -m)" != arm64 ]]; then
    echo '需要在 Apple Silicon Mac 上原生运行。 / Run natively on an Apple Silicon Mac.' >&2
    exit 1
fi
if ! xcrun --find swift >/dev/null 2>&1; then
    echo '请先安装 Xcode 或 Command Line Tools：xcode-select --install' >&2
    exit 1
fi
command -v python3 >/dev/null
python3 -c 'import sys; sys.exit(0 if sys.version_info >= (3, 10) else "Packaging requires Python 3.10 or newer.")'
export MACOSX_DEPLOYMENT_TARGET=13.0
TASK_BUILD_ID="${SCREENKEEPER_BUILD_ID:-$(date -u +%Y%m%dT%H%M%SZ)}"
if [[ ! "$TASK_BUILD_ID" =~ ^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$ ]]; then
    echo 'Invalid build identifier.' >&2
    exit 1
fi
TASK_OUT="$TASK_ROOT/dist/$TASK_BUILD_ID"
mkdir -p "$TASK_ROOT/dist"
mkdir "$TASK_OUT"

# A supplied snapshot must be checked before its code executes in the build.
if [[ -f "$TASK_ROOT/SOURCE-SHA256SUMS.txt" ]]; then
    shasum -a 256 -c "$TASK_ROOT/SOURCE-SHA256SUMS.txt" > "$TASK_OUT/input-verification.txt"
fi
python3 "$TASK_ROOT/scripts/source-manifest.py" --output "$TASK_OUT/SOURCE-SHA256SUMS.txt"
TASK_SOURCE_HASH="$(shasum -a 256 "$TASK_OUT/SOURCE-SHA256SUMS.txt" | cut -d ' ' -f 1)"
{
    printf 'Build: %s\nSource manifest: %s\n' "$TASK_BUILD_ID" "$TASK_SOURCE_HASH"
    printf 'Commit: '
    git rev-parse HEAD 2>/dev/null || printf 'uncommitted-source-snapshot\n'
    uname -m
    sw_vers
    xcrun swift --version
    python3 --version
    xcodebuild -version 2>/dev/null || true
    xcrun --show-sdk-version
    xcrun --show-sdk-path
} > "$TASK_OUT/environment.txt"

echo '运行测试 / Running tests'
swift test 2>&1 | tee "$TASK_OUT/tests.txt"
echo '构建 arm64 应用 / Building arm64 app'
swift build -c release --arch arm64 --product ScreenKeeper 2>&1 | tee "$TASK_OUT/build.txt"
TASK_BIN="$(swift build -c release --arch arm64 --show-bin-path)"
TASK_APP="$TASK_OUT/ScreenKeeper.app"
mkdir -p "$TASK_APP/Contents/MacOS" "$TASK_APP/Contents/Resources"
cp "$TASK_BIN/ScreenKeeper" "$TASK_APP/Contents/MacOS/ScreenKeeper"
cp -R "$TASK_ROOT/Sources/ScreenKeeperMac/Resources/." "$TASK_APP/Contents/Resources/"
cp "$TASK_ROOT/DISTRIBUTION.txt" "$TASK_APP/Contents/Resources/DISTRIBUTION.txt"
cp "$TASK_OUT/SOURCE-SHA256SUMS.txt" "$TASK_APP/Contents/Resources/"
cat > "$TASK_APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleIdentifier</key><string>com.skyrrats.screenkeeper.mac</string>
<key>CFBundleName</key><string>ScreenKeeper</string>
<key>CFBundleDisplayName</key><string>ScreenKeeper</string>
<key>CFBundleExecutable</key><string>ScreenKeeper</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>0.1.0</string>
<key>CFBundleVersion</key><string>$(date -u +%y%m).$(date -u +%d).$(date -u +%H)</string>
<key>ScreenKeeperBuildID</key><string>$TASK_BUILD_ID</string>
<key>ScreenKeeperSourceManifestSHA256</key><string>$TASK_SOURCE_HASH</string>
<key>ScreenKeeperChannel</key><string>development-preview</string>
<key>CFBundleIconFile</key><string>AppIcon</string>
<key>LSMinimumSystemVersion</key><string>13.0</string>
<key>LSArchitecturePriority</key><array><string>arm64</string></array>
<key>NSHighResolutionCapable</key><true/>
<key>NSSupportsAutomaticTermination</key><false/>
<key>NSSupportsSuddenTermination</key><false/>
</dict></plist>
PLIST

swift "$TASK_ROOT/scripts/GenerateIcon.swift" "$TASK_OUT/AppIcon.iconset"
iconutil -c icns "$TASK_OUT/AppIcon.iconset" -o "$TASK_APP/Contents/Resources/AppIcon.icns"
plutil -lint "$TASK_APP/Contents/Info.plist"
test "$(lipo -archs "$TASK_APP/Contents/MacOS/ScreenKeeper")" = arm64
python3 "$TASK_ROOT/scripts/verify-binary.py" "$TASK_APP/Contents/MacOS/ScreenKeeper" "$TASK_OUT/binary-check.json"
# Local testing signature only. This is not Developer ID signing or notarization.
codesign --force --sign - "$TASK_APP"
codesign --verify --strict --verbose=2 "$TASK_APP"
codesign -dv --verbose=4 "$TASK_APP" > "$TASK_OUT/signature.txt" 2>&1
"$TASK_APP/Contents/MacOS/ScreenKeeper" --package-check > "$TASK_OUT/package-check.json"
shasum -a 256 -c "$TASK_OUT/SOURCE-SHA256SUMS.txt" > "$TASK_OUT/source-verification.txt"
ditto -c -k --sequesterRsrc --keepParent "$TASK_APP" "$TASK_OUT/ScreenKeeper-Mac-0.1.0-Dev-$TASK_BUILD_ID-arm64.zip"
mkdir "$TASK_OUT/dmg-content"
ditto "$TASK_APP" "$TASK_OUT/dmg-content/ScreenKeeper.app"
ln -s /Applications "$TASK_OUT/dmg-content/Applications"
cp "$TASK_ROOT/DISTRIBUTION.txt" "$TASK_OUT/dmg-content/Read me - 使用说明.txt"
hdiutil create -quiet -volname ScreenKeeper -srcfolder "$TASK_OUT/dmg-content" -format UDZO \
    "$TASK_OUT/ScreenKeeper-Mac-0.1.0-Dev-$TASK_BUILD_ID-arm64.dmg"
hdiutil verify "$TASK_OUT/ScreenKeeper-Mac-0.1.0-Dev-$TASK_BUILD_ID-arm64.dmg"
cp "$TASK_ROOT/DISTRIBUTION.txt" "$TASK_OUT/Read me - 使用说明.txt"
(
    cd "$TASK_OUT"
    shasum -a 256 ./*.zip ./*.dmg > SHA256SUMS.txt
)
printf '\n本地开发包已生成 / Local development build: %s\n' "$TASK_APP"
echo '此构建未经 Developer ID 签名和公证，不是正式发行版。'
echo 'This build is not Developer ID signed or notarized and is not a public release.'
if [[ "${CI:-false}" != true ]]; then open -R "$TASK_APP"; fi
