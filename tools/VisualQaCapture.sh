#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

: "${GODOT_BIN:?Set GODOT_BIN to the Godot 4.7 Mono executable.}"
command -v xvfb-run >/dev/null

output="artifacts/visual-qa"
log="$output/visual-qa.log"
mkdir -p "$output"
rm -f "$output"/*.png "$log"

xvfb-run -a -s '-screen 0 1920x1080x24 -nolisten tcp' \
  "$GODOT_BIN" \
  --rendering-method gl_compatibility \
  --path . \
  --scene res://scenes/VisualQaCapture.tscn \
  2>&1 | tee "$log"

assert_png_dimensions() {
  local file_name="$1"
  local width="$2"
  local height="$3"
  local path="$output/$file_name"

  if [[ ! -s "$path" ]]; then
    echo "Visual QA screenshot is missing or empty: $path" >&2
    exit 1
  fi

  if ! file "$path" | grep -Fq "PNG image data, ${width} x ${height},"; then
    echo "Visual QA screenshot has unexpected dimensions: $(file "$path")" >&2
    exit 1
  fi
}

assert_png_dimensions battle_hud_1280x720.png 1280 720
assert_png_dimensions battle_hud_1600x900.png 1600 900
assert_png_dimensions battle_hud_1920x1080.png 1920 1080

for file_name in \
  main_menu.png \
  main_menu_settings.png \
  battle_hud.png \
  battle_hud_style1b_fog.png \
  battle_hud_style1c_dusk.png \
  pause_menu.png \
  outcome_victory.png
do
  assert_png_dimensions "$file_name" 1600 900
done

if grep -Eq 'Texture with GL ID|RID allocations of type .*Texture|RenderingServer::get_singleton\(\).*null|~ImageTexture' "$log"; then
  echo "Visual QA detected a managed texture teardown regression." >&2
  exit 1
fi

echo "Visual QA capture passed: true 1280x720, 1600x900, and 1920x1080 screenshots with clean managed texture teardown."
