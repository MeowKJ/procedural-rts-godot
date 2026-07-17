#!/usr/bin/env sh
set -eu

if [ "$#" -lt 1 ] || [ "$#" -gt 2 ]; then
    echo "usage: $0 /path/to/godot [--headless|--non-headless]" >&2
    exit 2
fi

root=$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)
evidence_dir="$root/artifacts/issue-568"
mode=${2:---headless}
case "$mode" in
    --headless) display_arg=--headless ;;
    --non-headless) display_arg= ;;
    *) echo "unknown mode: $mode" >&2; exit 2 ;;
esac

mkdir -p "$evidence_dir"
rm -f "$evidence_dir/diagnostics.json" "$evidence_dir/editor-output-clean.txt"
if [ "$mode" = "--non-headless" ]; then
    rm -f \
        "$evidence_dir/diagnostic-dock.png" \
        "$evidence_dir/source-selection.png" \
        "$evidence_dir/conflict-selection.png" \
        "$evidence_dir/rotated-footprint-clearance-reservations.png" \
        "$evidence_dir/environment-markers.png" \
        "$evidence_dir/post-reenable-clean.png"
fi
dotnet run --project "$root/tools/MapAuthoringValidationQa/MapAuthoringValidationQa.csproj" \
    --no-restore -- --diagnostics-json "$evidence_dir/diagnostics.json"
MAP_AUTHORING_VALIDATION_SMOKE=1 \
MAP_AUTHORING_OUTPUT_COPY="$evidence_dir/editor-output-clean.txt" \
sh "$root/tools/map-authoring-godot-run.sh" "$1" \
    "Map Authoring validation smoke PASSED" \
    ${display_arg:+$display_arg} --editor --path "$root" --quit-after 600

if [ "$mode" = "--headless" ]; then
    echo "Non-headless validation evidence remains required before delivery."
else
    echo "Non-headless Map Authoring validation evidence PASSED."
fi
