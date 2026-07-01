# Review Record - UnitBattlefieldBuildingTarget pulse read object API deletion

Step: UnitBattlefieldBuildingTarget pulse read object API deletion
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetpulsereadobjectdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-pulse-read-object-deleted.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, changing hit/delivery pulse component semantics, changing combat damage feedback, changing refinery delivery behavior, or migrating power/dock/weapon accessors.

Implementation summary:
- Removed public target-wrapper pulse read APIs from `UnitBattlefield`.
- Kept a private `BuildingHitPulse(UnitBattlefieldBuildingTarget)` helper only for internal selection fallback during migration.
- Moved `BattleRoot` delivery-pulse sync to the existing id-based `BuildingPresentationProjection(target.Id)` read path.
- Added `ReviewGate buildingtargetpulsereadobjectdeleted` and updated the historical presentation-pulse gate to lock the projection read boundary.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpulsereadobjectdeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpresentationpulseentitystate`
  Result: pass
  Evidence: historical presentation-pulse EntityWorld gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-pulse-read-object-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full VerifyAll completed successfully, 23/23 steps passed.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: API cleanup only; pulse behavior remains covered by CombatBehavior and VerifyAll.

Reviewer result:
- Status: pass for build, narrow gate, migrated historical gate, CombatBehavior, full ReviewGate, review-record gate, and VerifyAll.
- Required fixes: none.
- Residual risks: several public building accessors still accept `UnitBattlefieldBuildingTarget` until later id/projection cleanup slices.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget pulse read object API deletion`.
- Items left open: broader building-runtime migration cleanup and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: pulse reads no longer need public target-wrapper parameters, but other wrapper public APIs remain.
