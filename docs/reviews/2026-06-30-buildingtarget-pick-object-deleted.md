# Review Record - UnitBattlefieldBuildingTarget pick object API deletion

Step: UnitBattlefieldBuildingTarget pick object API deletion
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetpickobjectdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-pick-object-deleted.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, changing selection/hover visuals, changing building command APIs, migrating the public `Buildings` list, or changing unit pick APIs.

Implementation summary:
- Removed public target-wrapper building pick APIs from `UnitBattlefield`.
- Kept private resolved-target pick helpers for internal id/projection APIs.
- Public building picking remains available through `PickHostileBuildingId(...)`, `PickBuildingTargetId(...)`, `PickAnyBuildingTargetId(...)`, `PickHostileBuildingHoverProjection(...)`, and `PickAnyBuildingHoverProjection(...)`.
- Preserved existing pick priority: distance first, with deterministic id tie-breaks for owned/any building picks.
- Added `ReviewGate buildingtargetpickobjectdeleted` and updated historical public-surface, selection-input, and hover-projection gates.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpickobjectdeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpublicsurface`
  Result: pass
  Evidence: historical public-surface gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingselectioninput`
  Result: pass
  Evidence: historical building selection input gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildinghoverprojection`
  Result: pass
  Evidence: historical building hover projection gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-pick-object-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full VerifyAll completed successfully, 23/23 steps passed.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: API cleanup only; pick behavior remains covered by CombatBehavior and VerifyAll.

Reviewer result:
- Status: pass for build, narrow gate, migrated historical gates, CombatBehavior, full ReviewGate, review-record gate, and VerifyAll.
- Required fixes: none.
- Residual risks: public `Buildings`, building events, `UpsertBuildingTarget`, and `IsVisibleTo(viewer, building)` still expose `UnitBattlefieldBuildingTarget` until later migration slices.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget pick object API deletion`.
- Items left open: broader building-runtime migration cleanup and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: building picks no longer need public target-wrapper returns, but other wrapper public APIs remain.
