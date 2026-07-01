Step: Sync building selection into EntityWorld projection as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/BattleRoot.cs`, `scripts/world/BuildingView.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added mirrored building selection state to `UnitBattlefieldBuildingTarget`.
- Initialized building `SelectableComponentState` from the mirrored building target selection state.
- Added `UnitBattlefield.SetBuildingTargetSelected` so the presentation/runtime bridge can sync selection into EntityWorld without views mutating authority.
- Wired `BattleRoot.SyncUnitBattlefieldBuildingRuntimeState` to sync legacy `BuildingModel.Selected` into the EntityWorld building mirror.
- Updated `BuildingView` selection drawing to prefer `EntityProjection.Selected` with `Building.Selected` as a migration fallback.
- Non-goals: no rewrite of `GameState` building selection input, no selection command buffer routing for buildings, no deletion of `BuildingModel` or `UnitBattlefieldBuildingTarget`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed with the building selection projection assertion.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingselectionprojection --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=building-selection-projection --no-restore`
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
- Design note: this is intentionally a mirror bridge. The source of building selection is still legacy `GameState` until building selection commands move through EntityWorld.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- Building selection input still mutates `GameState` first.
- Building command/rally overlays still enumerate legacy selected buildings.
- `UnitBattlefieldBuildingTarget` remains as a migration runtime until building gameplay state is fully EntityWorld-owned.

TODO update:
- Marked done: nested M1 slice `BuildingView building selection projection bridge`.
- Left open: parent migration cleanup, building selection command-buffer routing, and legacy runtime deletion.
