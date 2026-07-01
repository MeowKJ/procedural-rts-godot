# Review Record - Minimap drag camera

Step: Complete camera control feel with minimap drag-to-jump.
Milestone: Controls and command feel.
Owner AI: Codex.
Reviewer AI: Codex self-review with ReviewGate and SelectionStress coverage.
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/ui/HudLayer.cs`, `scripts/controllers/CameraController.cs`, `scripts/core/CameraInputMath.cs`, `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `tools/SelectionStress/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-minimap-drag-camera.md`.
- Non-goals: no remappable camera hotkeys, no follow-selection mode, no UI visual redesign.

Implementation summary:
- Added minimap drag-to-jump state to `HudLayer.MinimapSurface`.
- Left-click still jumps immediately; holding left mouse and moving over the minimap continuously submits world focus requests.
- Existing `BattleRoot.OnMinimapJumpRequested` continues to route minimap requests to `CameraController.FocusOnWorldPoint`, preserving damped camera motion.
- Extended `ReviewGate camera` to require minimap drag-to-jump hooks in HUD and the BattleRoot wiring.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj camera --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for camera/minimap controls.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass
  Evidence: SelectionStress passed 80 cases, including camera smoothing and drag-related math checks.

Manual/visual gates:
- Check: hands-on minimap drag feel
  Result: not run
  Evidence: Godot headless scene startup is covered by VerifyAll; interactive feel tuning remains available for later playtest.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: follow-selection remains optional and unimplemented; a future input-settings slice should make camera bindings remappable.

TODO update:
- Items marked done: `Camera: WASD/edge-scroll/zoom, minimap click-to-jump and drag, frame-rate-independent feel (see Perf plan), optional follow-selection`.
- Items left open: remappable hotkeys and broader HUD/input polish.
- Reason: all non-optional camera controls are implemented and now covered by durable gates.
