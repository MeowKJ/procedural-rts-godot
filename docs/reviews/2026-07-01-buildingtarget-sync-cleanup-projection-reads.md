# Review Record - UnitBattlefieldBuildingTarget sync/cleanup projection read cleanup

Step:
- UnitBattlefieldBuildingTarget sync/cleanup projection read cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Codex

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-sync-cleanup-projection-reads.md
- Non-goals:
  - Do not change building entity spawn/update semantics, attack target semantics,
    unit death handling, or building removal behavior.
  - Do not remove the private migration wrapper list or its ordered fallback yet.
  - Do not change harvesting, production, combat balance, movement, UI, fog, or art.

Implementation summary:
- Changed `SyncBuildingTargetEntities()` to enumerate building ids through
  `BuildingTargetIds()` before calling `SyncBuildingTargetEntity(int)`.
- Changed `RemoveDeadUnits()` building-weapon cleanup to enumerate building ids
  through `BuildingTargetIds()` and use id-based target read/clear helpers.
- Preserved entity sync behavior, dead-unit removal, unit attack-target cleanup,
  building weapon target-kind checks, target-id checks, and id-based target clear.
- Added `ReviewGate buildingtargetsynccleanupprojectionreads`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsynccleanupprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsyncinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetclearattackinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-sync-cleanup-projection-reads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings after evidence backfill.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes internal sync and cleanup read plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - The changed sync/cleanup paths follow the id helper boundary used by recent
    placement, authority, refinery/dock, visibility, combat, and production read
    cleanup slices.
  - This slice deliberately leaves the ordered wrapper fallback and add/remove
    storage paths for the final wrapper deletion phase.
- Residual risks:
  - `BuildingTargetIds()` still has a wrapper fallback during the M1 migration
    window.
  - ReviewGate is string/regex-based rather than semantic type analysis.
  - Direct private `Buildings` reads remain only in ordered fallback and add/remove
    storage paths after this slice.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget sync/cleanup projection read cleanup
- Items left open:
  - Final wrapper fallback/storage deletion.
- Reason:
  - Building entity sync and dead-unit building weapon cleanup no longer scan the
    second building runtime list.
