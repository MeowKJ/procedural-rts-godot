# Review Record - UnitBattlefieldBuildingTarget hit-pulse object API deletion

Step: UnitBattlefieldBuildingTarget hit-pulse object API deletion
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetpulseobjectdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-pulse-object-deleted.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, changing building pulse component semantics, changing combat damage/VFX style, changing building events, or migrating production/rally accessor APIs.

Implementation summary:
- Replaced public `SetBuildingHitPulse(UnitBattlefieldBuildingTarget, ...)` with `SetBuildingHitPulse(int buildingId, ...)`.
- Kept a private resolved-target helper inside `UnitBattlefield` so existing internal EntityWorld pulse writes stay localized.
- Updated building damage feedback and CombatBehavior QA to write hit pulses through the id-based API.
- Added `ReviewGate buildingtargetpulseobjectdeleted` and updated the presentation-pulse historical gate.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpulseobjectdeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-pulse-object-deleted`
- Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full VerifyAll completed successfully, 23/23 steps passed.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: API cleanup only; hit-pulse behavior remains covered by CombatBehavior and VerifyAll.

Reviewer result:
- Status: pass for build, narrow gate, migrated historical gate, CombatBehavior, full ReviewGate, review-record gate, and VerifyAll.
- Required fixes: none known before final gates.
- Residual risks: several public building accessors still accept `UnitBattlefieldBuildingTarget` until later id/projection cleanup slices.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget hit-pulse object API deletion`.
- Items left open: broader building-runtime migration cleanup and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: hit-pulse writes no longer expose the target wrapper, but other wrapper public APIs remain.
