# Review Record - Active Battle Performance

Step:
Prove the "Runs at 60 FPS / 1080p with both bases active and a 40+ unit battle"
vertical-slice TODO item with a Godot runtime scene, render-path optimizations,
sim performance gates, and deterministic replay gates.

Milestone:
Playable 1v1 skirmish vertical slice - active-battle performance.

Owner AI:
Workers A/B/C plus integrator implementation.

Reviewer AI:
Integrator review plus `ReviewGate activebattleperf`.

Integrator AI:
Codex main thread.

Scope:
- Files/folders:
  - `project.godot`
  - `scripts/ActiveBattlePerfQaRoot.cs`
  - `scripts/BattleRoot.cs`
  - `scripts/core/UnitVisualRenderer.cs`
  - `scripts/world/BuildingView.cs`
  - `scripts/world/CombatEffectsLayer.cs`
  - `scripts/world/CommandAcknowledgementLayer.cs`
  - `scripts/world/GridLayer.cs`
  - `scripts/world/ResourceFieldView.cs`
  - `scripts/world/SignalNetworkLayer.cs`
  - `scripts/world/UnitInstanceView.cs`
  - `scripts/world/UnitView.cs`
  - `scenes/ActiveBattlePerfQa.tscn`
  - `tools/ReviewGate/Program.cs`
  - `tools/VerifyAll/Program.cs`
  - `docs/reviews/2026-06-30-active-battle-perf.md`
- Non-goals:
  - No 200-unit batching/atlas implementation in this slice.
  - No gameplay balance retuning except active-battle QA scale.
  - No campaign or UI redesign work.

Implementation summary:
- Added `ActiveBattlePerfQa`, a Godot runtime scene that opens a 1920x1080 battle,
  seeds both active bases plus a 40+ unit fight, samples `PresentationMetrics`,
  checks frame/process/sim/fog budgets, and writes a screenshot or headless note.
- Wired the QA into `VerifyAll` and added the `activebattleperf` ReviewGate mode.
- Removed position from `UnitInstanceView` redraw signatures so ordinary movement
  uses the node transform without rebuilding draw commands.
- Added a dirty `BuildingRedrawSignature` so static buildings no longer redraw on
  a bare 20Hz timer.
- Reduced hot-path vector draw cost: lower arc segment counts, hard-edged unit
  strokes, lower-cost command acknowledgements, resources, signal rings, building
  rings, and combat feedback arcs.
- Simplified `GridLayer` to a soft tactical field instead of dense grid/floor
  survey marks, matching the no-grid visual direction and reducing cached Canvas
  draw command count.
- Changed project defaults for this 2D line-art RTS: VSync disabled by default,
  2D MSAA disabled, and 2D HDR disabled. Runtime settings can still expose frame
  rate modes.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `Godot_v4.7-stable_mono_win64_console.exe --path . --scene res://scenes/ActiveBattlePerfQa.tscn`
  Result:
  Pass.
  Evidence:
  ActiveBattlePerfQa passed with 53 live / 53 visible units, buildings P/E 4/5,
  commanded P/E 28/31, frame avg 13.53ms, 1% low 32.11ms, process avg 2.89ms,
  sim avg 0.02ms, fog 2.23ms / 16 uploads, screenshot
  `artifacts/active-battle-perf/active_battle_perf_1920x1080.png`.
- Command:
  `Godot_v4.7-stable_mono_win64_console.exe --headless --path . --scene res://scenes/ActiveBattlePerfQa.tscn`
  Result:
  Pass.
  Evidence:
  ActiveBattlePerfQa passed headless with 58 live / 58 visible units, frame avg
  6.94ms, process avg 2.44ms, sim avg 0.01ms, fog 2.29ms / 9 uploads.
- Command:
  `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj -c Release --no-restore`
  Result:
  Pass.
  Evidence:
  PerfSmoke passed; worst average was 1.611ms at 400 units, under the 16.667ms
  budget.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  SimReplay passed all deterministic checks, including movement, combat, group
  attack, firing-anchor, and outcome scenarios.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- activebattleperf`
  Result:
  Pass.
  Evidence:
  ReviewGate activebattleperf passed with 0 errors and 0 warnings, validating the
  scene, QA root, BattleRoot debug hooks, VerifyAll wiring, and review record.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=active-battle-perf`
  Result:
  Pass.
  Evidence:
  ReviewGate review passed with 0 errors and 0 warnings for this durable record.
- Command:
  `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  VerifyAll passed 22/22 steps, including build, SimReplay, CombatBehavior,
  PerfSmoke, ReviewGate, BalanceReport, CounterReadabilityQa, and
  `godot-active-battle-perf-qa`.

Manual/visual gates:
- Check:
  1920x1080 runtime screenshot.
  Result:
  Pass.
  Evidence:
  Screenshot written to `artifacts/active-battle-perf/active_battle_perf_1920x1080.png`.
- Check:
  Visual risk review.
  Result:
  Pass with follow-up.
  Evidence:
  The no-grid soft-field background matches the user's stated direction, but a
  future art pass should replace the flat field with a low-cost shader or cached
  texture treatment.

Reviewer result:
- Status: pass
- Required fixes:
  - None after active-battle runtime QA passed.
- Residual risks:
  - 1% low still spikes during the real-window run; the TODO item only gates
    average 60 FPS, but future batching/atlas work should target smoother lows.
  - This proves a 40+ unit active battle, not the later 200+ unit performance
    target.
  - Dense vector art still needs a batching or atlas strategy for large armies.

Status:
Pass.

TODO update:
- Items marked done:
  - `Runs at 60 FPS / 1080p with both bases active and a 40+ unit battle; sim under budget; deterministic.`
