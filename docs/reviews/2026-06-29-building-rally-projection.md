Step: Route selected-building rally line drawing through UnitBattlefield building projections as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/BuildingPresentationProjection.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/controllers/SelectionController.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `BuildingRallyProjection` for selected producer rally-line drawing.
- Extended `BuildingPresentationProjection` to expose rally pulse from EntityWorld `PresentationPulseComponentState`.
- Mirrored `UnitBattlefieldBuildingTarget.RallyPulse` into EntityWorld presentation pulse state.
- Added `UnitBattlefield.SelectedBuildingRallyProjections` and switched UnitBattlefield runtime drawing to consume it.
- Kept legacy `State.SelectedBuildings()` rally drawing only for the old runtime path.
- Non-goals: no full command preview migration, no deletion of `GameState.SelectedBuildings`, no removal of `UnitBattlefieldBuildingTarget`, no visual restyle.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed with the selected building rally projection assertion.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingrallyprojection --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=building-rally-projection --no-restore`
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
- Design note: this migrates the drawing read path, not the entire selection model. Building selection is still mirrored from legacy until a later command-buffer slice.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- Command preview still checks legacy `State.SelectedBuildings().Any()`.
- Other HUD/status surfaces still read legacy selected building collections.
- The second building runtime remains until all building gameplay state is EntityWorld-owned.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield selected building rally projection bridge`.
- Left open: parent migration cleanup, command preview migration, selected building command-buffer routing, and legacy runtime deletion.
