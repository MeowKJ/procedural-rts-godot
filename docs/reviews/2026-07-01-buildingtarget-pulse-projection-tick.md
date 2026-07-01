# Review Record - UnitBattlefieldBuildingTarget pulse projection tick cleanup

Step:
- UnitBattlefieldBuildingTarget pulse projection tick cleanup

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
  - docs/reviews/2026-07-01-buildingtarget-pulse-projection-tick.md
- Non-goals:
  - Do not change pulse decay rates, visual timing, selection, rally, refinery
    delivery, or combat feedback semantics.
  - Do not remove the private migration wrapper list yet.
  - Do not migrate fog/visibility, placement, combat, dock/refinery, or cleanup
    direct building reads in this slice.

Implementation summary:
- Changed `UnitBattlefield.Update(...)` to enumerate building pulse decay through
  `BuildingTargetIds()` instead of directly scanning `Buildings`.
- Kept the id-based `DecayBuildingPresentationPulses(int buildingId, float dt)`
  helper and its existing EntityWorld presentation pulse component reads/writes.
- Added `ReviewGate buildingtargetpulseprojectiontick`.
- Updated the historical `buildingtargetpulseinternalid` gate to require
  `DecayBuildingPresentationPulses(buildingId, dt)` instead of the older
  wrapper-shaped `building.Id` marker.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpulseprojectiontick`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpulseinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-pulse-projection-tick`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes the internal tick enumeration source only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - The every-tick presentation pulse path now follows the same shared id boundary
    as public building projections.
  - The decay helper remains id-based and continues to write EntityWorld
    `PresentationPulseComponentState`.
- Residual risks:
  - `BuildingTargetIds()` still has a wrapper fallback during the M1 migration
    window.
  - Direct private `Buildings` reads remain in unrelated construction,
    fog/visibility, combat, placement, owner-relation, dock/refinery, and cleanup
    paths.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget pulse projection tick cleanup
- Items left open:
  - Remaining non-pulse direct wrapper-list reads in construction, fog/visibility,
    combat, placement, owner-relation, dock/refinery, and cleanup paths.
- Reason:
  - The tick pulse decay path no longer scans the second building runtime list.
