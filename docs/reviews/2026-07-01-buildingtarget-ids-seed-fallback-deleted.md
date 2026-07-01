# Review Record - BuildingTargetIds seed fallback deletion

Step:
- UnitBattlefieldBuildingTarget id seed fallback deletion

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetidsseedfallbackdeleted

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-ids-seed-fallback-deleted.md
- Non-goals:
  - Do not delete `_buildingTargetSeedsById` in this slice.
  - Do not delete point lookup fallback in `BuildingSnapshot(int)` or
    `BuildingIdentity(int)`.
  - Do not change public building APIs, building balance, production, combat, UI,
    or art.

Implementation summary:
- Removed the seed-key fallback loop from `BuildingTargetIds()`.
- Kept the temporary `_buildingTargetSeedsById` existence guard for EntityWorld
  identities during the migration.
- Added a CombatBehavior regression check that removes
  `BuildingIdentityComponentState` and proves `BuildingSnapshots should enumerate EntityWorld building identities only`
  without resurrecting seed-only fallback ids.
- Added `ReviewGate buildingtargetidsseedfallbackdeleted` and updated affected
  historical gates to forbid the old seed-key fallback.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed, including the seed-only snapshot regression.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetidprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetorderedlistdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetseedwrapperdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetidsseedfallbackdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes building identity enumeration only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Batch building projections now rely on EntityWorld identities rather than a
    seed-only fallback list.
- Residual risks:
  - `BuildingSnapshot(int)` and `BuildingIdentity(int)` still keep point lookup
    seed fallback by design; that is the next deletion slice.
  - `_buildingTargetSeedsById` still exists as temporary lifecycle storage for add,
    sync, adoption, health sync, and removal.
  - Full `VerifyAll` passed 23/23 after the multi-slice batch.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget id seed fallback deletion
- Items left open:
  - Broader Migration cleanup and final `BuildingKind` / entity-path deletion
    remain open.
- Reason:
  - This slice deletes only the batch-list seed fallback, not the remaining direct
    lookup compatibility state.
