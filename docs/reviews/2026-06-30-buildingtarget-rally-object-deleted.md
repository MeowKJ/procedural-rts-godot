# Review Record - UnitBattlefieldBuildingTarget rally object API deletion

Step: UnitBattlefieldBuildingTarget rally object API deletion
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetrallyobjectdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/UnitBattlefieldEnemyProductionAi.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-rally-object-deleted.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, changing rally command semantics, changing production spawn positions, changing enemy AI strategy, or migrating production queue/pulse/power/dock accessors.

Implementation summary:
- Added public `BuildingRallyPoint(int buildingId)` and `BuildingRallyPulse(int buildingId)` read APIs.
- Kept private resolved-target helpers inside `UnitBattlefield` so command/runtime migration code can continue reading EntityWorld components locally.
- Updated enemy production AI and CombatBehavior QA to read rally state by id.
- Added `ReviewGate buildingtargetrallyobjectdeleted` and updated historical rally/production gates to lock the id-based API boundary.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetrallyobjectdeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetrallyentitystate`
  Result: pass
  Evidence: historical rally EntityWorld gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- productionbridge`
  Result: pass
  Evidence: historical production bridge gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-rally-object-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full VerifyAll completed successfully, 23/23 steps passed.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: API cleanup only; rally behavior remains covered by CombatBehavior and VerifyAll.

Reviewer result:
- Status: pass for build, narrow gate, migrated historical gates, CombatBehavior, full ReviewGate, review-record gate, and VerifyAll.
- Required fixes: none.
- Residual risks: several public building accessors still accept `UnitBattlefieldBuildingTarget` until later id/projection cleanup slices.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget rally object API deletion`.
- Items left open: broader building-runtime migration cleanup and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: rally reads no longer need public target-wrapper parameters, but other wrapper public APIs remain.
