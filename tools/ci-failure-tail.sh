#!/usr/bin/env sh
set -eu

if [ "$#" -lt 1 ]; then
  echo "Usage: sh tools/ci-failure-tail.sh <run-id-or-url> [lines]" >&2
  exit 2
fi

run="$1"
lines="${2:-160}"

case "$run" in
  http*)
    run="${run%/}"
    run="${run##*/}"
    ;;
esac

echo "Failed log tail for run $run (last $lines lines):"
gh run view "$run" --log-failed | tail -n "$lines"
