# Review Record - UnitBattlefieldBuildingTarget radius internal id cleanup

Step:
- UnitBattlefieldBuildingTarget radius internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Singer the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-radius-internal-id.md
- Non-goals:
  - Do not change building footprints, collision radii, selection padding, fog range,
    production spawn ordering, or balance.
  - Do not migrate producer candidate lists, spawn-point ownership, repair helpers,
    snapshots, or final wrapper storage.
  - Do not change UI, art recipes, or presentation palette behavior.

Implementation summary:
- Replaced internal building-radius reads that accepted `UnitBattlefieldBuildingTarget`
  with `BuildingTargetRadiusCore(int buildingId)`.
- Added an id plus fallback-kind path for existing building loops so picking, visible
  footprint marking, and produced-unit spawn obstacle generation do not repeatedly
  scan the migration wrapper list.
- Preserved the EntityWorld `BuildingPresentationProjection` radius as the preferred
  source and the BuildSpec footprint-derived radius as the migration fallback.
- Added `ReviewGate buildingtargetradiusinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetradiusinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpickinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildspeccleanupnext`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass
  Evidence: Fog-of-war QA passed, including static memory and camera-scoped texture
    updates.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-radius-internal-id`
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
  Evidence: This slice changes internal radius helper parameters only.

Reviewer result:
- Status: fail-for-completion before integrator fixes; accepted after required fixes
  were applied and gates reran.
- Required fixes:
  - Singer the 2nd found the first radius gate only forbade the exact old
    `private float BuildingTargetRadius(UnitBattlefieldBuildingTarget building)`
    signature. Fixed by replacing exact-string checks with regex checks that
    reject `BuildingTargetRadius(...)` and `BuildingTargetRadiusCore(...)`
    overloads taking `UnitBattlefieldBuildingTarget` in both the new narrow gate
    and the older `buildspeccleanupnext` gate.
- Residual risks:
  - The migration helper still resolves `UnitBattlefieldBuildingTarget` internally
    until the second building runtime is deleted.
  - `BuildingTargetById` is still linear; current hot loops use the id plus
    fallback-kind overload to avoid repeated wrapper-list scans, and future loop
    callers should keep doing the same until wrapper storage is deleted.
  - The string/regex-based gate may need adjustment during the final EntityWorld
    authority migration.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget radius internal id cleanup
- Items left open:
  - Producer candidate list, spawn-point producer wrapper, repair helper, snapshot,
    spec helper, and final wrapper deletion migrations.
- Reason:
  - This slice only removes wrapper flow from internal building radius helper
    parameters while preserving projection-first radius lookup and BuildSpec
    fallback behavior.
