#!/usr/bin/env sh
set -eu

if [ "$#" -ne 1 ]; then
    echo "usage: $0 /path/to/godot" >&2
    exit 2
fi

root=$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)
sh "$root/tools/map-authoring-godot-run.sh" "$1" \
    "Map Authoring sample parity PASSED" \
    --headless --path "$root" --scene res://addons/map_authoring/qa/MapAuthoringBakePlaySampleQa.tscn --quit-after 600
