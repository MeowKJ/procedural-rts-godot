# Review Record - BuildingTargetIds seed guard deletion

Step:
- UnitBattlefieldBuildingTarget id seed guard deletion

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetidsseedguarddeleted

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-ids-seed-guard-deleted.md
- Non-goals:
  - Do not delete `_buildingTargetSeedsById`.
  - Do not delete `BuildingTargetById(int)` or seed lifecycle writes.
  - Do not change public building API signatures, building gameplay, UI, art, or
    production behavior.

Implementation summary:
- Removed `_buildingTargetSeedsById.ContainsKey(identity.LegacyBuildingId)` from
  `BuildingTargetIds()`.
- Kept EntityWorld ordered identity enumeration and duplicate suppression by
  legacy building id.
- Added a CombatBehavior regression check that removes the private seed entry
  through reflection while leaving EntityWorld identity intact, proving
  `BuildingTargetIds should enumerate EntityWorld building identities without requiring temporary seed storage`.
- Added `ReviewGate buildingtargetidsseedguarddeleted` and updated affected
  historical gates to forbid the old seed-storage guard.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed, including the seed-guard deletion regression.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetidsseedguarddeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetidprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetidsseedfallbackdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetorderedlistdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetseedwrapperdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes building id enumeration authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Batch building id enumeration now comes from EntityWorld identity components
    rather than temporary seed storage.
- Residual risks:
  - `_buildingTargetSeedsById` and `BuildingTargetById(int)` remain for lifecycle
    write/sync compatibility.
  - `BuildingKind` remains as the legacy building identity enum until the later
    entity-path deletion milestone.
  - Full `VerifyAll` passed 23/23 after the multi-slice batch.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget id seed guard deletion
- Items left open:
  - Broader Migration cleanup and final `BuildingKind` / entity-path deletion
    remain open.
- Reason:
  - This slice removes seed storage from batch read-model gating, but does not
    delete the temporary seed lifecycle storage.
