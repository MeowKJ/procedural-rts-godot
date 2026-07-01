Step: Route building hover picking and preview affordance through UnitBattlefield projections as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/BuildingPresentationProjection.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/controllers/SelectionController.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `BuildingHoverProjection` for building hover/preview data.
- Added UnitBattlefield-owned any-building hover picking and hover projection queries.
- Updated `SelectionController` so UnitDesign runtime building hover affordance and building-hover command preview use UnitBattlefield projections instead of legacy `GameState.PickAnyBuilding`.
- Kept legacy `BuildingModel` hover path for the old runtime.
- Non-goals: no rewrite of legacy `DrawBuildingHoverAffordance`, no building attack command changes, no removal of `GameState.PickAnyBuilding`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior assertions completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildinghoverprojection --no-restore`
  Result: pass
  Evidence: building hover projection gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=building-hover-projection --no-restore`
  Result: pass
  Evidence: review record gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: this migrates hover reads only; hostile building right-click attack already used UnitBattlefield before this slice.
- Required fixes: none before gates.

Status:
- Pass.

Residual risks:
- Legacy building hover path remains for old runtime.
- Some legacy GameState building pickers remain for non-UnitDesign runtime and future cleanup.
- Full `UnitBattlefieldBuildingTarget` removal remains open.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield building hover projection bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
