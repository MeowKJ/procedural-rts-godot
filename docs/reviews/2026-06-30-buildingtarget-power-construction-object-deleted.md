# Review Record - UnitBattlefieldBuildingTarget power/construction object API deletion

Step: UnitBattlefieldBuildingTarget power/construction object API deletion
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetpowerconstructionobjectdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-power-construction-object-deleted.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, changing power/construction component semantics, changing production eligibility, changing vision source eligibility, or migrating dock/weapon/pick APIs.

Implementation summary:
- Added public `BuildingPowered(int buildingId)` and `BuildingBuildProgress(int buildingId)` read APIs.
- Preserved existing migration defaults: missing building/entity reads as powered and fully built.
- Kept private resolved-target helpers inside `UnitBattlefield` for internal production, vision, and refinery eligibility code.
- Updated CombatBehavior QA to read power/construction state by id.
- Added `ReviewGate buildingtargetpowerconstructionobjectdeleted` and updated the historical power/construction gate to lock the id-based API boundary.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpowerconstructionobjectdeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpowerconstructionentitystate`
  Result: pass
  Evidence: historical power/construction EntityWorld gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-power-construction-object-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full VerifyAll completed successfully, 23/23 steps passed.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: API cleanup only; power/construction behavior remains covered by CombatBehavior and VerifyAll.

Reviewer result:
- Status: pass for build, narrow gate, migrated historical gate, CombatBehavior, full ReviewGate, review-record gate, and VerifyAll.
- Required fixes: none.
- Residual risks: several public building accessors still accept `UnitBattlefieldBuildingTarget` until later id/projection cleanup slices.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget power/construction object API deletion`.
- Items left open: broader building-runtime migration cleanup and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: power/construction reads no longer need public target-wrapper parameters, but other wrapper public APIs remain.
