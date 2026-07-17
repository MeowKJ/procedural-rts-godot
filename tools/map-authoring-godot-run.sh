#!/usr/bin/env sh
set -eu

if [ "$#" -lt 3 ]; then
    echo "usage: $0 /path/to/godot pass-marker godot-args..." >&2
    exit 2
fi

godot=$1
marker=$2
shift 2
root=$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)
before=$(mktemp)
after=$(mktemp)
uids_before=$(mktemp)
uids_after=$(mktemp)
imports_before=$(mktemp)
imports_after=$(mktemp)
log=$(mktemp)
project_before=$(mktemp)

cleanup() {
    if [ -s "$project_before" ] && ! cmp -s "$project_before" "$root/project.godot"; then
        cp "$project_before" "$root/project.godot"
    fi
    if [ -s "$uids_before" ]; then
        find "$root" -name '*.uid' -type f -print | LC_ALL=C sort > "$uids_after"
        comm -13 "$uids_before" "$uids_after" | while IFS= read -r generated; do
            rm -f "$generated"
        done
    fi
    if [ -s "$imports_before" ]; then
        find "$root" -name '*.import' -type f -print | LC_ALL=C sort > "$imports_after"
        comm -13 "$imports_before" "$imports_after" | while IFS= read -r generated; do
            rm -f "$generated"
        done
    fi
    rm -f "$before" "$after" "$uids_before" "$uids_after" "$imports_before" "$imports_after" "$log" "$project_before"
}
trap cleanup EXIT INT TERM

for script in $(find "$root/addons/map_authoring" -type f -name '*.cs'); do
    if [ ! -f "$script.uid" ]; then
        echo "Godot authoring script is missing stable .uid sidecar: $script" >&2
        exit 1
    fi
done
for sidecar in $(find "$root/addons/map_authoring" -type f -name '*.cs.uid'); do
    if [ ! -f "${sidecar%.uid}" ]; then
        echo "Godot authoring .uid sidecar is orphaned: $sidecar" >&2
        exit 1
    fi
done

snapshot() {
    git -C "$root" status --porcelain=v1 --untracked-files=all
    git -C "$root" diff --binary HEAD
    git -C "$root" ls-files --others --exclude-standard | LC_ALL=C sort | while IFS= read -r path; do
        shasum -a 256 "$root/$path"
    done
}

snapshot > "$before"
cp "$root/project.godot" "$project_before"
find "$root" -name '*.uid' -type f -print | LC_ALL=C sort > "$uids_before"
find "$root" -name '*.import' -type f -print | LC_ALL=C sort > "$imports_before"
if ! "$godot" "$@" > "$log" 2>&1; then
    grep -E '(ERROR:|SCRIPT ERROR:|InvalidOperationException)' "$log" >&2 || true
    tail -n 30 "$log" >&2
    exit 1
fi

if grep -E '(^|[[:space:]])(ERROR:|SCRIPT ERROR:)' "$log" > /dev/null; then
    grep -E '(^|[[:space:]])(ERROR:|SCRIPT ERROR:)' "$log" >&2
    exit 1
fi
if ! grep -F "$marker" "$log"; then
    tail -n 120 "$log" >&2
    exit 1
fi

find "$root" -name '*.uid' -type f -print | LC_ALL=C sort > "$uids_after"
generated_uid_count=$(comm -13 "$uids_before" "$uids_after" | wc -l | tr -d ' ')
comm -13 "$uids_before" "$uids_after" | while IFS= read -r generated; do
    rm -f "$generated"
done
if [ "$generated_uid_count" -gt 0 ]; then
    echo "Cleaned $generated_uid_count cache-derived UID sidecars outside the tracked authoring set."
fi
find "$root" -name '*.import' -type f -print | LC_ALL=C sort > "$imports_after"
generated_import_count=$(comm -13 "$imports_before" "$imports_after" | wc -l | tr -d ' ')
comm -13 "$imports_before" "$imports_after" | while IFS= read -r generated; do
    rm -f "$generated"
done
if [ "$generated_import_count" -gt 0 ]; then
    echo "Cleaned $generated_import_count cache-derived import sidecars."
fi
if ! cmp -s "$project_before" "$root/project.godot"; then
    cp "$project_before" "$root/project.godot"
    echo "Restored explicit project.godot runtime settings after editor normalization."
fi

snapshot > "$after"
if ! cmp -s "$before" "$after"; then
    echo "Godot authoring QA worktree changed during execution." >&2
    git -C "$root" status --short >&2
    exit 1
fi
