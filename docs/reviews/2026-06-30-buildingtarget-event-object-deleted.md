# Review Record - UnitBattlefieldBuildingTarget event object API deletion

Step: UnitBattlefieldBuildingTarget event object API deletion
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargeteventobjectdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldBuildingSnapshot.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-event-object-deleted.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, migrating the public `Buildings` list, changing building production/combat behavior, changing unit balance, or changing UI visual style.

Implementation summary:
- Added immutable `UnitBattlefieldBuildingSnapshot` records for building combat and production event payloads.
- Changed `UnitAttackedByBuilding`, `BuildingAttacked`, `ProductionQueued`, and `ProductionCompleted` to publish building snapshots instead of mutable `UnitBattlefieldBuildingTarget` wrappers.
- Kept wrapper resolution private in `UnitBattlefield.BuildingSnapshot(...)` while BattleRoot and QA tools consume only id/owner/kind/position/facing/HP/footprint event data.
- Added `ReviewGate buildingtargeteventobjectdeleted` to lock the public event boundary.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargeteventobjectdeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- productionbridge`
  Result: pass
  Evidence: historical production bridge gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-event-object-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full VerifyAll completed successfully, 23/23 steps passed.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: API boundary cleanup only; existing combat/production behavior is covered by automated gates.

Reviewer result:
- Status: pass for build, narrow gate, historical production gate, CombatBehavior, full ReviewGate, review-record gate, and VerifyAll.
- Required fixes: none.
- Residual risks: public `Buildings`, `UpsertBuildingTarget`, and some tool fixtures still expose `UnitBattlefieldBuildingTarget` until later migration slices.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget event object API deletion`.
- Items left open: broader building-runtime migration cleanup and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: building events no longer need public target-wrapper parameters, but the wrapper itself remains during migration.
