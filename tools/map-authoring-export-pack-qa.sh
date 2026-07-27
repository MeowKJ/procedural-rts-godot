#!/usr/bin/env sh
set -eu

if [ "$#" -ne 1 ]; then
    echo "usage: $0 /path/to/godot" >&2
    exit 2
fi

root=$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)
expected_hash=$(shasum -a 256 "$root/assets/maps/authored-map-preview.mapspec.json" | awk '{print $1}')
case "$(uname -s)" in
    Darwin) preset="PCK Probe macOS" ;;
    Linux) preset="PCK Probe Linux" ;;
    *) echo "unsupported PCK probe host: $(uname -s)" >&2; exit 2 ;;
esac
temporary=$(mktemp -d)
log="$temporary/runtime.log"
export_log="$temporary/export.log"
uids_before="$temporary/uids-before"
uids_after="$temporary/uids-after"
imports_before="$temporary/imports-before"
imports_after="$temporary/imports-after"
find "$root" -type f -name '*.uid' -print | LC_ALL=C sort > "$uids_before"
find "$root" -type f -name '*.import' -print | LC_ALL=C sort > "$imports_before"
cleanup() {
    find "$root" -type f -name '*.uid' -print | LC_ALL=C sort > "$uids_after"
    comm -13 "$uids_before" "$uids_after" | while IFS= read -r generated; do rm -f "$generated"; done
    find "$root" -type f -name '*.import' -print | LC_ALL=C sort > "$imports_after"
    comm -13 "$imports_before" "$imports_after" | while IFS= read -r generated; do rm -f "$generated"; done
    dotnet restore "$root/ProceduralRts.csproj" --force >/dev/null 2>&1 || true
    dotnet build "$root/ProceduralRts.csproj" --no-restore >/dev/null 2>&1 || true
    rm -rf "$temporary"
}
trap cleanup EXIT INT TERM

dotnet build "$root/ProceduralRts.csproj" -c ExportDebug --no-restore > "$temporary/build.log"
dotnet build "$root/ProceduralRts.csproj" -c ExportRelease --no-restore >> "$temporary/build.log"
if ! "$1" --headless --path "$root" --export-pack "$preset" \
    "$temporary/authored-preview.pck" > "$export_log" 2>&1; then
    tail -n 80 "$export_log" >&2
    exit 1
fi
if ! (cd "$temporary" && "$1" --headless \
    --main-pack "$temporary/authored-preview.pck" \
    --scene res://scenes/AuthoredMapPreviewExportProbe.tscn --quit-after 600) > "$log" 2>&1; then
    tail -n 80 "$log" >&2
    exit 1
fi
if grep -E '(^|[[:space:]])(ERROR:|SCRIPT ERROR:)' "$log" >/dev/null; then
    grep -E '(^|[[:space:]])(ERROR:|SCRIPT ERROR:)' "$log" >&2
    exit 1
fi
grep -F "Authored map export PCK probe PASSED: 1880 bytes sha256 $expected_hash." "$log"
