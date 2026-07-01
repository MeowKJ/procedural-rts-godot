# Review Record - BuildSpec cleanup next

Step: UnitBattlefieldBuildingTarget radius projection cleanup
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Worker B
Reviewer AI: ReviewGate buildspeccleanupnext
Integrator AI: Main thread

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, deleting `BuildingKind`, changing building art, changing production behavior, or migrating all legacy building runtime state in this slice.

Implementation summary:
- Removed the duplicated `UnitBattlefieldBuildingTarget.Radius` convenience projection.
- Centralized live building target radius lookup in `UnitBattlefield.BuildingTargetRadius(...)`.
- Building picking, hover projections, and spawn obstacles now prefer EntityWorld `BuildingPresentationProjection.Radius` with a `BuildSpec` footprint fallback during migration.
- CombatBehavior now asserts projected building radius against the BuildSpec-derived expected radius instead of asserting through the target wrapper.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildspeccleanupnext --no-restore`
  Result: pass
  Evidence: ReviewGate buildspeccleanupnext completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully across combat, production, economy, enemy AI, and outcome checks.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: this slice changes radius authority/read paths, not rendering output.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: `UnitBattlefieldBuildingTarget` remains a migration wrapper for mutable building state; this slice only removes the duplicated radius convenience path.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget radius projection cleanup`.
- Items left open: parent building migration cleanup and legacy deletion.
- Reason: this is a narrow verified cleanup, not the full removal of the second building runtime.
