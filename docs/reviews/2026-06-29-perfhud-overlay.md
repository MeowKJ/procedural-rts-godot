# Review Record - PerfHud overlay

Step:
Add an in-engine performance HUD overlay for profiling frame feel and live counts.

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
  - `scripts/ui/PerfHudLayer.cs`
  - `scripts/core/PerfHudCounts.cs`
  - `scripts/core/PresentationMetrics.cs`
  - `scripts/core/GameState.cs`
  - `scripts/BattleRoot.cs`
  - `scripts/world/CombatEffectsLayer.cs`
  - `scripts/world/FootprintLayer.cs`
  - `scripts/core/UnitVisualRenderer.cs`
  - `tools/ReviewGate/Program.cs`
  - `tools/SimReplay/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-perfhud-overlay.md`
- Non-goals:
  - Do not build a player-facing polished diagnostics screen.
  - Do not claim true GPU render time; the HUD displays a render/wait estimate
    from frame time minus `_Process` time.
  - Do not change gameplay simulation authority.

Implementation summary:
- Added `PerfHudLayer`, hidden by default and toggleable with F3; it can default
  on through `PROCEDURAL_RTS_PERF_HUD=1`.
- Added `PerfHudCounts` and `BattleRoot.PerfHudCounts()` to feed live entity,
  unit, visible-unit, projectile, effect, fog-upload, and fog-update metrics.
- Extended `PresentationMetrics` with render/wait estimate fields consumed by the
  HUD.
- Timed fog updates in `GameState` using a reused Stopwatch.
- Exposed active effect/footprint counts for diagnostics.
- Fixed an unrelated `UnitVisualRenderer.DrawCircle` warning found during Godot
  headless scene startup.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj perfhud`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  SimReplay reported the presentation metrics check and all deterministic
  scenarios passed.
- Command:
  `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj -c Release --no-restore`
  Result:
  Pass.
  Evidence:
  Worst average was 1.172ms at 400 units under the 16.667ms budget; allocation was
  188125 bytes/tick at 400 units.
- Command:
  `Godot_v4.7-stable_mono_win64_console.exe --headless --path . --scene res://scenes/Battle.tscn --quit-after 2`
  Result:
  Pass.
  Evidence:
  Battle scene started and exited cleanly; the previous `DrawCircle` warnings were
  removed.

Manual/visual gates:
- Check:
  Interactive F3 toggle in a visible Godot window.
  Result:
  Not run.
  Evidence:
  The headless scene startup verifies construction/runtime safety, but visual
  placement and text legibility still need a visible-window pass.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for source/runtime startup.
- Residual risks:
  - Visible placement has not been screenshot-verified.
  - Render timing is an estimate, not GPU profiler data.

TODO update:
- Items marked done:
  - In-engine `PerfHud` overlay.
- Items left open:
  - Broader renderer batching, fog camera-rect recompute, and grid cache work.
- Reason:
  - The overlay exists, toggles in-engine, consumes real metrics/counters, and
    passes build, review, replay, perf smoke, and Godot scene-start gates.
