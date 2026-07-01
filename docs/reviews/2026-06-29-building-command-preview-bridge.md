Step: Route selected-building command-preview/HUD context checks through UnitBattlefield projections as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/controllers/SelectionController.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `UnitBattlefield.HasSelectedBuildings` to query selected building state through `EntityProjection` snapshots.
- Updated `SelectionController` command preview to use a UnitBattlefield-aware selected-building helper when UnitDesign runtime input is enabled.
- Updated `BattleRoot.RefreshCommandPreview` HUD context to use UnitBattlefield selected-building projection state when UnitDesign runtime is enabled.
- Kept legacy `State.SelectedBuildings()` checks for the old runtime path.
- Non-goals: no command-card selection summary migration, no full building selection command-buffer routing, no removal of `GameState.SelectedBuildings`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed with the selected-building command preview assertion.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingpreviewprojection --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=building-command-preview-bridge --no-restore`
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
- Design note: the preview path still mirrors legacy building selection first because building selection commands themselves have not yet moved fully into EntityWorld.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `RefreshSelectionInfo` and command-card/detail surfaces still read legacy selected building collections.
- Building selection input still originates in legacy `GameState` during migration.
- Full removal of `UnitBattlefieldBuildingTarget` remains open.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield selected building command-preview bridge`.
- Left open: parent migration cleanup, command-card/selection summary migration, selected building command-buffer routing, and legacy runtime deletion.
