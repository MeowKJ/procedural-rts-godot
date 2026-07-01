Step: Route selected-building HUD selection details through UnitBattlefield projections as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/BuildingPresentationProjection.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `BuildingSelectionProjection` for selected building HUD details and summaries.
- Added `UnitBattlefield.SelectedBuildingSelectionProjections` to assemble selected building HUD snapshots from EntityWorld projections plus BuildSpec/presentation metadata.
- Updated `BattleRoot.RefreshSelectionInfo` so selected buildings in the UnitDesign runtime use UnitBattlefield projections instead of legacy `State.SelectedBuildings()`.
- Added single-building and multi-building HUD rendering helpers for the UnitBattlefield building projection path.
- Non-goals: no legacy `SetBuildingSelectionInfo` deletion, no command-card rewrite, no full building selection command-buffer migration.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed with the selected building HUD projection assertion.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingselectionhud --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=building-selection-hud --no-restore`
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
- Design note: selected building HUD details now read the EntityWorld bridge, but the source selection gesture still mirrors from legacy until a later command-buffer slice.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- Command-card production availability already uses UnitBattlefield, but some lower-level legacy building selection helpers remain for the old runtime.
- Building selection input still originates in legacy `GameState`.
- Full `UnitBattlefieldBuildingTarget` removal remains open.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield selected building HUD selection bridge`.
- Left open: parent migration cleanup, building selection command-buffer routing, and legacy runtime deletion.
