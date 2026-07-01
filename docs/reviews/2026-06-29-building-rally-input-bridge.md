Step: Route live selected-building rally input through UnitBattlefield and EntityWorld as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/controllers/SelectionController.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `UnitBattlefield.SetSelectedBuildingRallyPoints` so selected producer buildings submit `SetRallyPointEntityCommand` through the EntityWorld production bridge.
- Added world-boundary clamping to UnitBattlefield rally commands.
- Routed live right-click building rally input through UnitBattlefield when the UnitDesign runtime is enabled, instead of mutating `GameState` first and patching UnitBattlefield afterward.
- Mirrored UnitBattlefield rally point and rally pulse back to legacy building UI state during migration.
- Non-goals: no full building selection command-buffer migration, no removal of `GameState.CommandSetSelectedBuildingRallyPoint`, no deletion of `UnitBattlefieldBuildingTarget`, no HUD rewrite.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed with the selected building rally input assertion.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingrallyinputbridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=building-rally-input-bridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 VerifyAll steps passed.

Reviewer result:
- Status: pass
- Design note: this remains a bridge because building selection itself still originates in legacy `GameState`; the rally command now enters EntityWorld once selected targets are mirrored.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `GameState.CommandSetSelectedBuildingRallyPoint` remains for the legacy runtime and tests.
- Rally command-line drawing still enumerates legacy selected buildings, fed by migration sync.
- Full deletion of `UnitBattlefieldBuildingTarget` remains open.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield selected building rally input bridge`.
- Left open: parent migration cleanup, building selection command-buffer routing, and legacy runtime deletion.
