# Review Record - UnitBattlefieldBuildingTarget weapon read object API deletion

Step: UnitBattlefieldBuildingTarget weapon read object API deletion
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetweaponreadobjectdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-weapon-read-object-deleted.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, changing turret combat targeting, changing weapon cooldown semantics, changing combat balance, or migrating building pick/event APIs.

Implementation summary:
- Added public `BuildingAttackTargetId(int buildingId)`, `BuildingAttackTargetKind(int buildingId)`, and `BuildingAttackCooldownRemaining(int buildingId)` read APIs.
- Kept private resolved-target helpers inside `UnitBattlefield` for internal cleanup and migration code.
- Updated CombatBehavior QA to read building weapon target state by id.
- Added `ReviewGate buildingtargetweaponreadobjectdeleted` and updated historical weapon/turret gates to lock the id-based API boundary.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetweaponreadobjectdeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetweaponuserentitystate`
  Result: pass
  Evidence: historical weapon-user EntityWorld gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- turretcombatsystembridge`
  Result: pass
  Evidence: historical turret combat bridge gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-weapon-read-object-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full VerifyAll completed successfully, 23/23 steps passed.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: API cleanup only; weapon behavior remains covered by CombatBehavior and VerifyAll.

Reviewer result:
- Status: pass for build, narrow gate, migrated historical gates, CombatBehavior, full ReviewGate, review-record gate, and VerifyAll.
- Required fixes: none.
- Residual risks: building pick APIs and public building list/events still expose `UnitBattlefieldBuildingTarget` until later id/projection cleanup slices.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget weapon read object API deletion`.
- Items left open: broader building-runtime migration cleanup and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: weapon reads no longer need public target-wrapper parameters, but other wrapper public APIs remain.
