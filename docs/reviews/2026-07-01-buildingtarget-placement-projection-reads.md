# Review Record - UnitBattlefieldBuildingTarget placement projection read cleanup

Step:
- UnitBattlefieldBuildingTarget placement projection read cleanup

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
  - docs/reviews/2026-07-01-buildingtarget-placement-projection-reads.md
- Non-goals:
  - Do not change placement legality, terrain rules, build radius values, power
    gating, construction visibility, produced-unit spawn authority, or building
    footprint values.
  - Do not optimize PlacementMath or ProductionSystem spawn math in this slice.
  - Do not remove the private migration wrapper list yet.

Implementation summary:
- Changed `SpawnObstacles()` to enumerate building obstacles through
  `BuildingTargetIds()` and immutable `BuildingSnapshot(int)` data.
- Changed `BuildingBuildAnchors(...)` to enumerate build-radius anchors through
  `BuildingTargetIds()` and immutable `BuildingSnapshot(int)` data.
- Changed `BuildingPlacementObstacles()` to enumerate blocking footprints through
  `BuildingTargetIds()` and immutable `BuildingSnapshot(int)` data.
- Preserved id-based radius reads, BuildSpec build radius and footprint reads,
  powered-state reads, construction-progress filtering, owner filtering, and
  PlacementMath rectangle conversion.
- Added `ReviewGate buildingtargetplacementprojectionreads` and updated historical
  radius/spawn-helper gates to accept the id/snapshot path.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetplacementprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetradiusinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetspawnpointhelperdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa PASSED.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-placement-projection-reads`
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
  Evidence: This slice changes internal placement read plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Placement-space building reads now follow the same id/snapshot boundary as
    picking, producer lookup, and visibility footprint marking.
  - The actual placement result keys, PlacementMath checks, and produced-unit spawn
    authority remain unchanged.
- Residual risks:
  - `BuildingTargetIds()` still has a wrapper fallback during the M1 migration
    window.
  - Snapshot allocation in these migration helpers is acceptable for this slice but
    not the final performance endpoint for all placement/spawn paths.
  - Direct private `Buildings` reads remain in construction/sync, owner-relation,
    construction subject, dock/refinery, and cleanup paths.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget placement projection read cleanup
- Items left open:
  - Remaining direct wrapper-list reads in construction/sync, owner-relation,
    construction subject, dock/refinery, and cleanup paths.
- Reason:
  - Placement and spawn obstacle projections no longer scan the second building
    runtime list.
