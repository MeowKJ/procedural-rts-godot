# Review Record - UnitBattlefieldBuildingTarget visibility projection read cleanup

Step:
- UnitBattlefieldBuildingTarget visibility projection read cleanup

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
  - docs/reviews/2026-07-01-buildingtarget-visibility-projection-reads.md
- Non-goals:
  - Do not change gameplay fog rules, sight ranges, explored memory, enemy AI
    target visibility, or visual fog shader behavior.
  - Do not optimize fog upload cadence or camera-scoped fog in this slice.
  - Do not remove the private migration wrapper list yet.

Implementation summary:
- Changed building vision-source enumeration in `MarkVisibleBuildingFootprints()`
  to read `BuildingTargetIds()` and immutable `BuildingSnapshot(int)` data.
- Changed visible building footprint target enumeration in
  `MarkVisibleBuildingFootprints(PlayerSlotId, Vector2, float)` to read
  `BuildingTargetIds()` and immutable `BuildingSnapshot(int)` data.
- Preserved alive/completed source filtering, owner-relation target filtering,
  id-based radius reads, snapshot-position distance math, and EntityWorld
  `VisibilityIndex` writes.
- Added `ReviewGate buildingtargetvisibilityprojectionreads`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetvisibilityprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetvisibilityinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass
  Evidence: Fog-of-war QA passed, including mask channels, explored memory, camera-scoped texture updates, and 100-source smoke.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-visibility-projection-reads`
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
  Evidence: This slice changes internal fog/visibility read plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Building footprint visibility now follows the same id/snapshot projection path
    as building picking and public building projections.
  - The actual `VisionSystem`, `VisibilityIndex`, fog mask upload path, and visual
    fog shader are unchanged.
- Residual risks:
  - `BuildingTargetIds()` still has a wrapper fallback during the M1 migration
    window.
  - Snapshot allocation in this path preserves current behavior direction but is
    not the final performance endpoint for fog; later slices can move to direct
    EntityWorld projection iteration.
  - Direct private `Buildings` reads remain in construction/sync, placement,
    owner-relation, dock/refinery, and cleanup paths.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget visibility projection read cleanup
- Items left open:
  - Remaining direct wrapper-list reads in construction/sync, placement,
    owner-relation, dock/refinery, and cleanup paths.
- Reason:
  - Fog/visibility footprint marking no longer scans the second building runtime
    list for building sources or targets.
