# Review Record - UnitBattlefieldBuildingTarget internal pick id cleanup

Step:
- UnitBattlefieldBuildingTarget internal pick id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Harvey

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-pick-internal-id.md
- Non-goals:
  - Do not delete private building wrapper storage.
  - Do not change public building pick, hover, attack, or repair APIs.
  - Do not tune balance, unit stats, movement, fog, or UI art.

Implementation summary:
- Replaced private building pick helpers that returned `UnitBattlefieldBuildingTarget?`
  with id-returning helpers: `PickHostileBuildingIdCore`,
  `PickBuildingTargetIdCore`, and `PickAnyBuildingTargetIdCore`.
- Kept public callers on `Pick*BuildingId(...)` and hover-projection APIs.
- Preserved historical hostile distance-only ordering and kept deterministic id
  tie-breaks for owned/any building picking.
- Added `ReviewGate buildingtargetpickinternalid` and updated older pick/hover/
  selection gates so they no longer require private wrapper-returning pick helpers.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpublicsurface`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpickobjectdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingselectioninput`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildinghoverprojection`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpickinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-pick-internal-id`
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
  Evidence: This slice changes internal pick return shape only.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - Initial review found the new gate would fail until TODO/review evidence existed.
    Fixed by adding this record and the TODO entry.
  - Initial review recommended locking hostile pick ordering. Fixed by adding a
    `buildingtargetpickinternalid` check forbidding `.ThenBy(` in the hostile pick
    method body.
- Residual risks:
  - The new gate is intentionally string-based and may reject equivalent rewrites
    until the wrapper migration is complete.
  - Legacy `GameState` building pick paths still exist and are outside this
    `UnitBattlefield` internal wrapper slice.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget internal pick id cleanup
- Items left open:
  - Broader deletion of `UnitBattlefieldBuildingTarget` storage and remaining
    private helpers.
- Reason:
  - This is a narrow internal API cleanup; the migration wrapper still exists for
    later M1 slices.
