# Review Record - UnitBattlefieldBuildingTarget visibility object API deletion

Step: UnitBattlefieldBuildingTarget visibility object API deletion
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetvisibilityobjectdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/UnitBattlefieldEnemyAttackWaveAi.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-visibility-object-deleted.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, changing fog/vision rules, changing enemy target priorities, changing unit visibility APIs, or migrating public building events/list/upsert APIs.

Implementation summary:
- Added public `IsVisibleTo(PlayerSlotId viewer, int buildingId)` for building visibility reads.
- Kept a private resolved-target helper inside `UnitBattlefield` so the visibility read still syncs and queries EntityWorld `VisibilityIndex`.
- Updated enemy attack-wave AI to filter visible hostile/base buildings by id.
- Added `ReviewGate buildingtargetvisibilityobjectdeleted` and updated the AI opponent loop historical gate to lock the id-based visibility boundary.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetvisibilityobjectdeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- aiopponentloop`
  Result: pass
  Evidence: historical AI opponent loop gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-visibility-object-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full VerifyAll completed successfully, 23/23 steps passed.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: API cleanup only; visibility behavior remains covered by AI/CombatBehavior and VerifyAll.

Reviewer result:
- Status: pass for build, narrow gate, migrated historical gate, CombatBehavior, full ReviewGate, review-record gate, and VerifyAll.
- Required fixes: none.
- Residual risks: public `Buildings`, building events, `UpsertBuildingTarget`, and some tool fixtures still expose `UnitBattlefieldBuildingTarget` until later migration slices.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget visibility object API deletion`.
- Items left open: broader building-runtime migration cleanup and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: building visibility reads no longer need public target-wrapper parameters, but other wrapper public APIs remain.
