#!/usr/bin/env bash
# 构建唯一的自包含 PCL-CE-macOS.app。
# 用法：scripts/build-macos-app.sh [osx-arm64|osx-x64] [Release|Beta]
set -euo pipefail

RID="${1:-osx-arm64}"
CONFIG="${2:-Release}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TOOLCHAIN_ROOT="${PCL_TOOLCHAIN_ROOT:-$ROOT/.toolchain/dotnet}"
FINAL_OUT="$ROOT/dist"
STAGING_OUT="$ROOT/.cache/macos-staging"

case "$RID" in
  osx-arm64|osx-x64) ;;
  *) echo "unsupported RID: $RID (expected osx-arm64 or osx-x64)" >&2; exit 2 ;;
esac

if [ ! -x "$TOOLCHAIN_ROOT/dotnet" ]; then
  echo "缺少项目内 .NET SDK：$TOOLCHAIN_ROOT/dotnet" >&2
  echo "请运行 .toolchain/dotnet-install.sh --channel 11.0 --install-dir $TOOLCHAIN_ROOT --no-path" >&2
  exit 2
fi

export DOTNET_ROOT="$TOOLCHAIN_ROOT"
export DOTNET="$TOOLCHAIN_ROOT/dotnet"
export PATH="$TOOLCHAIN_ROOT:$PATH"
export DOTNET_CLI_HOME="$ROOT/.toolchain/home"
export NUGET_PACKAGES="$ROOT/.cache/nuget"
export TMPDIR="$ROOT/.cache/tmp"
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export MSBUILDDISABLENODEREUSE=1
export DOTNET_CLI_USE_MSBUILD_SERVER=0
mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES" "$TMPDIR" "$STAGING_OUT"
rm -rf "$STAGING_OUT/PCL-CE-macOS.app"

"$ROOT/scripts/package-macos.sh" "$RID" "$CONFIG" "2.15.1" "$STAGING_OUT"

APP="$STAGING_OUT/PCL-CE-macOS.app"
[ -d "$APP" ] || { echo "构建失败：未生成 $APP" >&2; exit 1; }

mkdir -p "$FINAL_OUT"
find "$FINAL_OUT" -maxdepth 1 -type d -name '*.app' -exec rm -rf {} +
mv "$APP" "$FINAL_OUT/PCL-CE-macOS.app"

echo "created: $FINAL_OUT/PCL-CE-macOS.app"
