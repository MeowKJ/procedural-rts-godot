# Review Record - UnitBattlefieldBuildingTarget production queue object API deletion

Step: UnitBattlefieldBuildingTarget production queue object API deletion
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetproductionqueueobjectdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/UnitBattlefieldEnemyProductionAi.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-production-queue-object-deleted.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, changing production timing/costs, changing rally behavior, changing production queue component semantics, or migrating other building accessors.

Implementation summary:
- Added public `BuildingProductionQueue(int buildingId)` as the building queue read API.
- Kept a private resolved-target helper inside `UnitBattlefield` so existing internal migration code can continue reading `ProductionQueueComponentState` locally.
- Updated enemy production AI and CombatBehavior QA to read building queues by id.
- Added `ReviewGate buildingtargetproductionqueueobjectdeleted` and updated the production-queue historical gate to lock the id-based API boundary.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetproductionqueueobjectdeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetproductionqueueentitystate`
  Result: pass
  Evidence: historical production-queue EntityWorld gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-production-queue-object-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full VerifyAll completed successfully, 23/23 steps passed.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: API cleanup only; production behavior remains covered by CombatBehavior and VerifyAll.

Reviewer result:
- Status: pass for build, narrow gate, migrated historical gate, CombatBehavior, full ReviewGate, review-record gate, and VerifyAll.
- Required fixes: none.
- Residual risks: several public building accessors still accept `UnitBattlefieldBuildingTarget` until later id/projection cleanup slices.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget production queue object API deletion`.
- Items left open: broader building-runtime migration cleanup and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: production queue reads no longer need public target-wrapper parameters, but other wrapper public APIs remain.
