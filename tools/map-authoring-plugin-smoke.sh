#!/usr/bin/env sh
set -eu

if [ "$#" -lt 1 ] || [ "$#" -gt 2 ]; then
    echo "usage: $0 /path/to/godot [--headless|--non-headless]" >&2
    exit 2
fi

root=$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)
mode=${2:---headless}
case "$mode" in
    --headless) display_arg=--headless ;;
    --non-headless) display_arg= ;;
    *) echo "unknown mode: $mode" >&2; exit 2 ;;
esac

MAP_AUTHORING_PLUGIN_SMOKE=1 sh "$root/tools/map-authoring-godot-run.sh" "$1" \
    "Map Authoring plugin lifecycle smoke PASSED" \
    ${display_arg:+$display_arg} --editor --path "$root" --quit-after 600

if [ "$mode" = "--headless" ]; then
    echo "Non-headless editor evidence remains required before delivery."
else
    echo "Non-headless editor plugin evidence PASSED."
fi
