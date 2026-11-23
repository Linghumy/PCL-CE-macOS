#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP="${1:-$ROOT/dist/PCL-CE-macOS.app}"
OUT="${2:-$ROOT/dist/PCL-CE-macOS.dmg}"
VOLUME_NAME="${PCL_DMG_VOLUME_NAME:-PCL-CE-macOS}"
STAGING="$ROOT/.cache/dmg-staging"
MOUNT_POINT="/Volumes/$VOLUME_NAME"
RW_DMG="$ROOT/.cache/tmp/PCL-CE-macOS-rw.dmg"
BACKGROUND_DIR="$STAGING/.background"

fail() {
  echo "[dmg] $*" >&2
  exit 1
}

cleanup() {
  if mount | grep -F "on $MOUNT_POINT " >/dev/null 2>&1; then
    hdiutil detach "$MOUNT_POINT" -force >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

[ -d "$APP" ] || fail "app bundle not found: $APP"
command -v hdiutil >/dev/null 2>&1 || fail "hdiutil is required"
command -v osascript >/dev/null 2>&1 || fail "osascript is required"
command -v swift >/dev/null 2>&1 || fail "swift is required to render the background"

mkdir -p "$ROOT/.cache/tmp" "$ROOT/dist"
rm -rf "$STAGING"
rm -f "$RW_DMG" "$OUT"
mkdir -p "$BACKGROUND_DIR"

cp -a "$APP" "$STAGING/PCL-CE-macOS.app"
ln -s /Applications "$STAGING/Applications"

"$ROOT/scripts/macos/render-dmg-background.swift" \
  "$BACKGROUND_DIR/background.png" 1
"$ROOT/scripts/macos/render-dmg-background.swift" \
  "$BACKGROUND_DIR/background@2x.png" 2
chflags hidden "$BACKGROUND_DIR"

hdiutil create \
  -volname "$VOLUME_NAME" \
  -srcfolder "$STAGING" \
  -fs HFS+ \
  -format UDRW \
  -ov \
  "$RW_DMG" >/dev/null

hdiutil attach "$RW_DMG" \
  -readwrite \
  -noverify \
  -noautoopen \
  -mountpoint "$MOUNT_POINT" >/dev/null

osascript <<APPLESCRIPT
tell application "Finder"
    tell disk "$VOLUME_NAME"
        open
        set current view of container window to icon view
        set toolbar visible of container window to false
        set statusbar visible of container window to false
        set pathbar visible of container window to false
        set the bounds of container window to {140, 140, 820, 560}
        set viewOptions to the icon view options of container window
        set arrangement of viewOptions to not arranged
        set icon size of viewOptions to 112
        set text size of viewOptions to 13
        set background picture of viewOptions to file ".background:background.png"
        set position of item "PCL-CE-macOS.app" of container window to {170, 225}
        set position of item "Applications" of container window to {510, 225}
        update without registering applications
        delay 1
        close
    end tell
end tell
APPLESCRIPT

sync
hdiutil detach "$MOUNT_POINT" >/dev/null

hdiutil convert "$RW_DMG" \
  -format UDZO \
  -imagekey zlib-level=9 \
  -o "$OUT" >/dev/null

if command -v codesign >/dev/null 2>&1; then
  codesign --force --sign - "$OUT" >/dev/null
fi

hdiutil verify "$OUT" >/dev/null
rm -rf "$STAGING"
rm -f "$RW_DMG"
echo "created: $OUT"
