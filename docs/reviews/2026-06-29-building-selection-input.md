Step: Route live building click selection through UnitBattlefield and EntityWorld as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/controllers/SelectionController.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added UnitBattlefield-owned player building picking and click selection.
- Routed building click selection through `SetSelectionEntityCommand` so EntityWorld `SelectableComponentState` is the source for selected buildings.
- Applied selection command results back to `UnitBattlefieldBuildingTarget.Selected`.
- Updated `SelectionController` so UnitDesign runtime building clicks no longer call `GameState.SelectPlayerBuildingAt` first.
- Reversed BattleRoot selection sync so legacy `BuildingModel.Selected` mirrors UnitBattlefield state as a UI fallback.
- Non-goals: no removal of legacy GameState building selection APIs, no drag-select buildings, no control-group building selection.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed with the building click selection assertion.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingselectioninput --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=building-selection-input --no-restore`
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
- Design note: this moves the live click-selection source to EntityWorld for UnitDesign runtime buildings; legacy `GameState.SelectPlayerBuildingAt` remains for the old runtime path.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- Building drag selection is still out of scope.
- Control groups remain unit-only and still clear legacy selections through their own compatibility path.
- Full `UnitBattlefieldBuildingTarget` removal remains open.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield building click selection input bridge`.
- Left open: parent migration cleanup, broader building command-buffer routing, and legacy runtime deletion.
