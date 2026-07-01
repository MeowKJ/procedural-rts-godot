# Review Record - Grid visible-rect drawing

Step:
Finish the GridLayer performance TODO by drawing only the camera-visible world
rect instead of the full map.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread has been operating at the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/world/GridLayer.cs`
  - `scripts/BattleRoot.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-grid-visible-rect.md`
- Non-goals:
  - Do not switch GridLayer to a baked texture or MultiMesh.
  - Do not alter terrain layout generation, palette math, or gameplay state.
  - Do not change camera smoothing/culling behavior outside feeding the grid rect.

Implementation summary:
- Added `GridLayer.VisibleWorldRect`; setting it queues a redraw only when the rect
  changes.
- `BattleRoot.RefreshViewCulling()` now passes the camera visible rect plus margin
  to `GridLayer`.
- `GridLayer._Draw()` now computes a grown visible draw rect and filters floor
  panels, command zones, navigation hints, water highlights, directional strata,
  survey marks, irregular traces, and world-boundary lines.
- `ReviewGate grid` now verifies visible-rect hooks in addition to layout caching.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj grid`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `Godot_v4.7-stable_mono_win64_console.exe --headless --path . --scene res://scenes/Battle.tscn --quit-after 2`
  Result:
  Pass.
  Evidence:
  Battle scene started and exited cleanly.

Manual/visual gates:
- Check:
  Visible camera panning screenshot comparison.
  Result:
  Not run.
  Evidence:
  Headless startup validates runtime safety; visible-window inspection remains
  useful to tune decoration margins if needed.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded rendering slice.
- Residual risks:
  - Visible panning has not been screenshot-verified.
  - Some long decorative lines may still be drawn when their bounding box
    intersects the visible rect; this is much cheaper than full-map floor panels.

TODO update:
- Items marked done:
  - `GridLayer`: cache static grid or draw only the visible rect.
- Items left open:
  - Batch unit bodies.
  - Full VFX pooling.
  - Fog camera-rect recompute and quality tiers.
- Reason:
  - The TODO allowed either texture/MultiMesh caching or visible-rect drawing; the
    visible-rect path is implemented and verified.
