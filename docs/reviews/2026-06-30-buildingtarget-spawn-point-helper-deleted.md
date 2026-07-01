# Review Record - UnitBattlefieldBuildingTarget produced spawn-point helper deletion

Step:
- UnitBattlefieldBuildingTarget produced spawn-point helper deletion

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Curie the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-spawn-point-helper-deleted.md
- Non-goals:
  - Do not change `GameState` legacy production spawning.
  - Do not change `ProductionSystem`, `ProductionSpawnMath`, producer footprints,
    unit collision radius, or spawn ordering.
  - Do not migrate producer candidate lists, production completion matching, or
    final building wrapper storage.

Implementation summary:
- Deleted the unused `ProducedUnitSpawnPoint(UnitBattlefieldBuildingTarget producer,
  UnitSpec spec)` helper from `UnitBattlefield`.
- Kept `SpawnObstacles()` intact for migration paths, with building obstacles still
  reading radius through `BuildingTargetRadiusCore(building.Id, building.Kind)`.
- Added `ReviewGate buildingtargetspawnpointhelperdeleted` to prevent the wrapper
  helper from returning and to confirm EntityWorld `ProductionSystem` remains the
  produced-unit spawn authority.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetspawnpointhelperdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa PASSED.
- Command: `dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore`
  Result: pass
  Evidence: AiOpponentLoopQa PASSED.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-spawn-point-helper-deleted`
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
  Evidence: This slice deletes an unused private helper and does not change rendering.

Reviewer result:
- Status: fail-for-completion before integrator fixes; accepted after required fixes
  were applied and gates reran.
- Required fixes:
  - Curie the 2nd noted the record/TODO were still pending before final evidence;
    fixed after gates passed.
  - Curie the 2nd noted the new gate only rejected one shape of
    `ProductionSpawnMath.FindSpawnPoint(...)` in `UnitBattlefield`. Fixed by
    forbidding any `ProductionSpawnMath.FindSpawnPoint` use in `UnitBattlefield`,
    leaving the unrelated legacy `GameState` helper outside this slice.
- Residual risks:
  - `GameState` still has a separate legacy `ProducedUnitSpawnPoint(BuildingModel, ...)`
    path outside this slice.
  - Producer candidate and production completion matching still use the migration
    building wrapper and remain future M1 cleanup slices.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget produced spawn-point helper deletion
- Items left open:
  - GameState legacy produced-unit spawning remains outside this slice.
  - Producer candidate and production completion matching still use the migration
    building wrapper and remain future M1 cleanup slices.
- Reason:
  - This slice only deletes the unused UnitBattlefield wrapper spawn helper and
    locks produced-unit spawn authority to EntityWorld `ProductionSystem`.
