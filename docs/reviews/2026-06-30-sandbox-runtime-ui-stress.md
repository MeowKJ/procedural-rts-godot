# Review Record - Sandbox Runtime UI Stress

Step:
M8 Sandbox runtime UI buttons and one-click stress tests.

Milestone:
M8 AI/campaign/sandbox developer controls.

Owner AI:
Worker C.

Reviewer AI:
ReviewGate sandboxruntimeui plus SandboxSpawnAuthoringQa.

Integrator AI:
Worker C.

Scope:
- Files/folders: `scripts/ui/HudLayer.cs`, `scripts/BattleRoot.cs`, `scripts/core/SandboxStressSpawnPlanner.cs`, `tools/SandboxSpawnAuthoringQa/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-sandbox-runtime-ui-stress.md`.
- Non-goals: no M1 UnitSpec cleanup, no combat/movement changes, no full spawn browser, no actual debug overlay drawing, no command-log/state-hash rendering.

Implementation summary:
- Added a sandbox-only HUD developer panel with context buttons for owner, faction, team, relation, time scale, atmosphere, overlay preset, and one-click stress spawn.
- Routed HUD context buttons through `SandboxDeveloperContextRequest` into `BattleRoot`, keeping stress execution guarded by `LaunchMode.Sandbox`.
- Added `SandboxStressSpawnPlanner` as a pure capped planner over context-filtered `SandboxSpawnAuthoring` entries.
- Stress spawn now creates UnitSpec runtime units and uses the existing visible building path for owner 1/2 structures.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj`
  Result: pass
  Evidence: build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SandboxSpawnAuthoringQa/SandboxSpawnAuthoringQa.csproj --no-restore`
  Result: pass
  Evidence: `SandboxSpawnAuthoringQa PASSED: entries 34, specs 34, units 26, buildings 5, turrets 3, context switches covered.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- sandboxruntimeui`
  Result: pass
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=sandbox-runtime-ui-stress`
  Result: pass
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Runtime UI is hidden outside `LaunchMode.Sandbox` and only exposed through `SetSandboxDeveloperControlsVisible`.
  Result: pass by static gate.
  Evidence: `ReviewGate sandboxruntimeui` checks the launch-mode visibility and stress handler guard.
- Check: Battle scene starts with the new HUD fields and sandbox callbacks compiled into the scene.
  Result: pass
  Evidence: `Godot_v4.7-stable_mono_win64_console.exe --headless --path . --scene res://scenes/Battle.tscn --quit-after 2` exited 0.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates pass.
- Residual risks: full spawn browser, visible debug overlays, command-log, state-hash display, and owner 3/4 visible structure rendering remain open M8 work.

TODO update:
- Items marked done: none; the broad M8 sandbox item remains open.
- Items left open: full spawn browser, actual debug overlay drawing, command-log/state-hash display.
- Reason: this is a narrow runtime UI/stress slice with concrete buttons and deterministic QA, not the complete sandbox milestone.
