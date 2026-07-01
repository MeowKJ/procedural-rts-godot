# Review Record - UnitInstanceView redraw throttle

Step: Reduce idle UnitInstanceView redraws
Milestone: M6 Performance
Owner AI: Worker C
Reviewer AI: ReviewGate presentation
Integrator AI: Main thread

Scope:
- Files/folders: scripts/world/UnitInstanceView.cs; tools/ReviewGate/Program.cs; docs/reviews/2026-06-30-unit-instance-redraw-throttle.md
- Non-goals: No UnitBattlefield, HudLayer, BattleRoot, CombatBehavior, simulation authority, unit art redesign, or TODO.md changes.

Implementation summary:
- Added a lightweight `UnitRedrawSignature` to `UnitInstanceView` so idle units no longer queue redraws from the fixed 30Hz timer when render state is unchanged.
- Kept position/projection polling every frame, but queues redraw only when the signature changes or selected/alert/command-pulse animation needs the existing 30Hz cadence.
- Included position, facing, HP, selection, command pulse, alert pulse, cargo, visual theme, owner, and mount-facing state in the signature so movement, turning, damage, cargo, and theme changes redraw promptly.
- Added a narrow `ReviewGate presentation` check that prevents `UnitInstanceView` from regressing to a bare timer-driven redraw loop.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation`
  Result: pass
  Evidence: ReviewGate completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Godot runtime visual QA
  Result: not run
  Evidence: This slice preserves the existing draw code and only gates when `QueueRedraw()` is requested; runtime screenshot/playtest remains useful for future batching work.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: Moving or animated units still redraw at the existing 30Hz cadence; this removes idle redraws but does not batch UnitInstanceView draw calls.

TODO update:
- Items marked done: None.
- Items left open: Unit batching and broader render performance work.
- Reason: The requested slice explicitly avoids TODO.md updates and does not complete full unit rendering batching.
