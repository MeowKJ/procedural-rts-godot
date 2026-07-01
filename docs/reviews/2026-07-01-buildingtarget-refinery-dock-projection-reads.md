# Review Record - UnitBattlefieldBuildingTarget refinery/dock projection read cleanup

Step:
- UnitBattlefieldBuildingTarget refinery/dock projection read cleanup

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
  - docs/reviews/2026-07-01-buildingtarget-refinery-dock-projection-reads.md
- Non-goals:
  - Do not change harvesting state transitions, refinery choice semantics, dock
    reservation rules, delivery pulse behavior, resource amounts, or unit balance.
  - Do not remove the private migration wrapper list yet.
  - Do not change movement feel, UI, fog, art, or production logic in this slice.

Implementation summary:
- Changed `FindBestRefineryIdForHarvester(...)` to enumerate refinery candidates
  through `BuildingTargetIds()` and immutable `BuildingSnapshot(int)` reads instead
  of the private wrapper list.
- Changed `ClearRefineryDockClaim(...)` to enumerate refinery ids through
  `BuildingTargetIds()`, filter refinery kind through `BuildingIdentity(int)`, and
  update `DockComponentState` through `BuildingEntityByTargetId(int)`.
- Changed `SyncDockStateFromEntities()` to enumerate refinery ids through
  `BuildingTargetIds()`, filter through `BuildingIdentity(int)`, and read dock state
  through `BuildingEntityByTargetId(int)`.
- Preserved owner/kind/alive/completed/nearest filtering, nullable refinery-id
  projection, dock reservation cleanup, dock occupancy cleanup, legacy docked
  harvester projection, and delivery-pulse writes.
- Added `ReviewGate buildingtargetrefinerydockprojectionreads`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetrefinerydockprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetrefinerylookupinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetdockinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-refinery-dock-projection-reads`
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
  Evidence: This slice changes internal refinery/dock read plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - The changed refinery/dock paths follow the same id/state boundary as the recent
    placement, authority, visibility, combat, and production projection cleanup
    slices.
  - This slice deliberately leaves ordered wrapper fallback, construction/sync, and
    unit-death cleanup reads for later bounded architecture slices.
- Residual risks:
  - `BuildingTargetIds()` still has a wrapper fallback during the M1 migration
    window.
  - ReviewGate is string/regex-based rather than semantic type analysis.
  - Direct private `Buildings` reads remain in ordered fallback, sync, add/remove,
    and unit-death cleanup paths.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget refinery/dock projection read cleanup
- Items left open:
  - Remaining direct wrapper-list reads in ordered fallback, sync, add/remove, and
    cleanup paths.
- Reason:
  - Refinery lookup, dock claim cleanup, and dock state sync no longer scan the
    second building runtime list.
