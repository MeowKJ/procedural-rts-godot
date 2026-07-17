#!/usr/bin/env sh
set -eu

if [ "$#" -lt 1 ] || [ "$#" -gt 2 ]; then
    echo "usage: $0 /path/to/godot [--headless|--non-headless]" >&2
    exit 2
fi

root=$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)
evidence="$root/artifacts/issue-569"
mode=${2:---headless}
case "$mode" in
    --headless) display_arg=--headless ;;
    --non-headless) display_arg= ;;
    *) echo "unknown mode: $mode" >&2; exit 2 ;;
esac

mkdir -p "$evidence"
rm -f "$evidence/artifact-parity.json" "$evidence/lifecycle.json" "$evidence/editor-runtime-output-clean.txt"
if [ "$mode" = "--non-headless" ]; then
    rm -f \
        "$evidence/typed-sample.png" \
        "$evidence/path-hash.png" \
        "$evidence/invalid-last-good.png" \
        "$evidence/menu-preview.png" \
        "$evidence/authored-battle.png" \
        "$evidence/applied-command.png" \
        "$evidence/return-menu.png" \
        "$evidence/normal-no-stale.png" \
        "$evidence/post-reenable.png"
fi

dotnet run --project "$root/tools/MapAuthoringBakePlayQa/MapAuthoringBakePlayQa.csproj" \
    --no-restore -- --evidence-dir "$evidence"
MAP_AUTHORING_BAKE_PLAY_SMOKE=1 \
MAP_AUTHORING_OUTPUT_COPY="$evidence/editor-runtime-output-clean.txt" \
sh "$root/tools/map-authoring-godot-run.sh" "$1" \
    "Map Authoring Bake Play smoke PASSED" \
    ${display_arg:+$display_arg} --editor --path "$root" --quit-after 1800

test -s "$evidence/artifact-parity.json"
test -s "$evidence/lifecycle.json"
grep -F "Authored map preview runtime smoke PASSED" "$evidence/editor-runtime-output-clean.txt" >/dev/null
if [ "$mode" = "--headless" ]; then
    echo "Non-headless Bake Play evidence remains required before delivery."
else
    echo "Non-headless Map Authoring Bake Play evidence PASSED."
fi
