# Review Record - Building Projection Bridge

Step: Route BuildingView position/facing/health reads through EntityWorld `EntityProjection` snapshots.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/world/BuildingView.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: deleting `BuildingModel`, replacing all building draw geometry with `FootprintComponentState`, replacing production/rally UI with projections.

Implementation summary:
- Added `UnitBattlefield.BuildingProjection(int id)` to expose EntityWorld building mirror snapshots through `EntityProjector`.
- `BuildingView` now accepts `ProjectionProvider` and prefers projection position, facing, HP, and MaxHp with `BuildingModel` as fallback.
- `BattleRoot` wires all existing and newly-added `BuildingView` instances to `_unitBattlefield.BuildingProjection`.
- `CombatBehavior` proves building projections read EntityWorld health state instead of only the legacy building target.
- `ReviewGate buildingprojectionbridge` locks the projection path.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed with building projection assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingprojectionbridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=building-projection-bridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: existing Godot headless Battle QA covers runtime boot; this slice preserves BuildingModel fallback.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Building draw geometry, production progress, rally state, and selection still read legacy `BuildingModel`/definitions until broader building runtime removal.

TODO update:
- Items marked done: nested M1 slice `BuildingView EntityProjection bridge`.
- Items left open: parent migration cleanup, `UnitBattlefieldBuildingTarget` removal, construction/build placement migration, legacy catalog deletion.
- Reason: tests and ReviewGate prove building views can consume EntityWorld projection data without claiming the second building runtime is gone.
