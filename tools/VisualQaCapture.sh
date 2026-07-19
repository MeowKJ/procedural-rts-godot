#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

: "${GODOT_BIN:?Set GODOT_BIN to the Godot 4.7 Mono executable.}"
command -v xvfb-run >/dev/null
command -v timeout >/dev/null

output="artifacts/visual-qa"
log="$output/visual-qa.log"
exit_log="$output/normal-exit-qa.log"
mkdir -p "$output"
rm -f "$output"/*.png "$log" "$exit_log"
rm -f "$output"/normal-exit-qa-attempt-*.log

capture_started_seconds=$SECONDS
capture_start_line="Visual QA capture starting: timeout 180s, signal TERM, kill-after 5s; elapsed=0s capture_status=pending tee_status=pending."
printf '%s\n' "$capture_start_line"
printf '%s\n' "$capture_start_line" > "$log"
set +e
timeout --signal=TERM --kill-after=5s 180s \
  xvfb-run -a -s '-screen 0 1920x1080x24 -nolisten tcp' \
  "$GODOT_BIN" \
  --rendering-method gl_compatibility \
  --path . \
  --scene res://scenes/VisualQaCapture.tscn \
  2>&1 | tee -a "$log"
capture_pipeline_status=("${PIPESTATUS[@]}")
set -e
capture_status="${capture_pipeline_status[0]}"
tee_status="${capture_pipeline_status[1]}"
capture_elapsed_seconds=$((SECONDS - capture_started_seconds))

if ((capture_status != 0 || tee_status != 0)); then
  if ((capture_status != 0)); then
    final_status="$capture_status"
    case "$capture_status" in
      124)
        failure_reason="timed out after 180s; TERM"
        ;;
      137)
        failure_reason="forced KILL after 5s grace"
        ;;
      *)
        failure_reason="capture child or timeout invocation failed"
        ;;
    esac
    if ((tee_status != 0)); then
      failure_reason="${failure_reason}; tee also failed"
    fi
  else
    final_status="$tee_status"
    failure_reason="capture succeeded but tee failed"
  fi

  failure_line="Visual QA capture failed: ${failure_reason}; elapsed=${capture_elapsed_seconds}s capture_status=${capture_status} tee_status=${tee_status}."
  printf '%s\n' "$failure_line" >&2
  set +e
  printf '%s\n' "$failure_line" >> "$log"
  failure_log_status=$?
  set -e
  if ((failure_log_status != 0)); then
    printf 'Visual QA capture diagnostic append failed with status %s; preserving exit %s.\n' \
      "$failure_log_status" "$final_status" >&2
  fi
  exit "$final_status"
fi

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

for state in \
  empty \
  unit_selected \
  production_building_selected \
  unavailable_low_resources \
  queue_progress \
  alert
do
  for dimensions in 1280x720 1600x900 1920x1080
  do
    width="${dimensions%x*}"
    height="${dimensions#*x}"
    assert_png_dimensions "battle_hud_runtime_${state}_${width}x${height}.png" "$width" "$height"
  done
done

for file_name in \
  main_menu.png \
  main_menu_settings.png \
  battle_hud.png \
  battle_hud_command_ribbon.png \
  battle_hud_command_deck.png \
  battle_hud_command_deck_queue.png \
  battle_hud_command_deck_dense.png \
  battle_hud_selection_detail.png \
  battle_hud_style1b_fog.png \
  battle_hud_style1c_dusk.png \
  battle_hud_style1d_night.png \
  battle_hud_theme_transition.png \
  battle_hud_foundation_states.png \
  battle_projectile_direct.png \
  battle_projectile_ballistic.png \
  battle_projectile_tracking.png \
  pause_menu.png \
  outcome_victory.png
do
  assert_png_dimensions "$file_name" 1600 900
done

run_normal_exit_attempt() {
  local attempt="$1"
  local attempt_log="$output/normal-exit-qa-attempt-${attempt}.log"
  local status=0

  echo "Normal exit QA attempt ${attempt}/2 (timeout 45s)."
  rm -f "$attempt_log"
  if timeout --signal=TERM --kill-after=5s 45s \
    xvfb-run -a -s '-screen 0 1920x1080x24 -nolisten tcp' \
    "$GODOT_BIN" \
    --rendering-method gl_compatibility \
    --path . \
    --scene res://scenes/NormalExitQa.tscn \
    2>&1 | tee "$attempt_log"
  then
    status=0
  else
    status=$?
  fi

  {
    echo "===== NormalExitQa attempt ${attempt}/2 (exit ${status}) ====="
    cat "$attempt_log"
  } >> "$exit_log"
  return "$status"
}

normal_exit_passed=false
for attempt in 1 2
do
  if run_normal_exit_attempt "$attempt"; then
    if grep -Fq 'Normal exit QA passed:' "$output/normal-exit-qa-attempt-${attempt}.log"; then
      normal_exit_passed=true
      break
    fi
    echo "Normal exit QA attempt ${attempt}/2 exited without the success marker." | tee -a "$exit_log" >&2
  else
    status=$?
    echo "Normal exit QA attempt ${attempt}/2 failed or timed out with exit ${status}." | tee -a "$exit_log" >&2
  fi

  if [[ "$attempt" -eq 1 ]]; then
    echo "Normal exit QA retrying once with a clean Godot/Xvfb process." | tee -a "$exit_log"
  fi
done

if [[ "$normal_exit_passed" != true ]]; then
  echo "Normal exit QA failed after 2 attempts." >&2
  exit 1
fi

if ! grep -Fq 'Normal exit QA passed:' "$exit_log"; then
  echo "Normal exit QA did not reach the real PauseQuitButton path." >&2
  exit 1
fi

for teardown_log in "$log" "$exit_log"
do
  if grep -Eq 'Texture with GL ID|RID allocations of type .*Texture|RenderingServer::get_singleton\(\).*null|~ImageTexture' "$teardown_log"; then
    echo "Visual QA detected a managed texture teardown regression in $teardown_log." >&2
    exit 1
  fi
done

echo "Visual QA capture passed: six normal-skirmish HUD states at true 1280x720, 1600x900, and 1920x1080 plus real PauseQuitButton exit with clean managed texture teardown."
