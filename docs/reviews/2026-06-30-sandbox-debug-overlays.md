# Review Record - Sandbox Debug Overlays

Step: Add pure state model for sandbox debug overlay toggles and presets.
Milestone: M8 Sandbox developer controls.
Owner AI: Worker Pascal.
Reviewer AI: ReviewGate sandboxdebugoverlays plus SimulationSmoke assertions.
Integrator AI: Main thread.

Scope:
- Files/folders: `scripts/core/SandboxDebugOverlayState.cs`, `tools/SimulationSmoke/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-sandbox-debug-overlays.md`.
- Non-goals: no Godot drawing, no BattleRoot hotkeys, no HUD panel, no command-log capture, no state-hash visualization.

Implementation summary:
- Added `SandboxDebugOverlayFlag` for paths, slots, avoidance, rings, anchors, components, command-log, and state-hash.
- Added `SandboxDebugOverlayState` with toggle, set, preset application, enabled checks, labels, and status formatting.
- Added movement, diagnostics, all, and off presets for later sandbox UI wiring.
- Added `SimulationSmoke` assertions for empty state, toggles, explicit set, movement preset membership, and all-overlay status.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors during worker validation.
- Command: `dotnet run --project tools/SimulationSmoke/SimulationSmoke.csproj --no-restore`
  Result: pass.
  Evidence: `Simulation smoke passed: 300s, orders 10, completions 10, waves 3, outcome Defeat`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- sandboxdebugoverlays`
  Result: pass.
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.

Manual/visual gates:
- Later overlay rendering needs in-engine visual QA for readability and culling once these flags drive actual layers.

Reviewer result:
- Status: pass.
- Required fixes: none known.
- Residual risks: state model is ready, but hotkey/HUD/drawing integration remains open.

TODO update:
- Items marked done: none.
- Items left open: the broad sandbox parent remains open for actual overlay rendering, command-log/state-hash content, and one-click stress tests.
