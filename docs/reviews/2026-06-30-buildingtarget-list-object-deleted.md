# Review Record - UnitBattlefieldBuildingTarget public building list object API deletion

Step: UnitBattlefieldBuildingTarget public building list object API deletion
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetlistobjectdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/UnitBattlefieldEnemyAttackWaveAi.cs`, `scripts/core/units/runtime/UnitBattlefieldEnemyProductionAi.cs`, `scripts/BattleRoot.cs`, `scripts/world/CombatEffectsLayer.cs`, `tools/CombatBehavior/Program.cs`, `tools/AiOpponentLoopQa/Program.cs`, `tools/PlayerLoopQa/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-list-object-deleted.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, changing `UpsertBuildingTarget`, changing production/combat AI priorities, changing balance, or changing visual style.

Implementation summary:
- Made `UnitBattlefield.Buildings` private so external callers cannot hold or mutate building wrapper objects.
- Added `BuildingSnapshots()`, `BuildingSnapshot(int id)`, and `LiveBuildingCount(...)` as the public building read surface.
- Migrated BattleRoot, combat effects, enemy production AI, enemy wave AI, AiOpponentLoopQa, PlayerLoopQa, and CombatBehavior to snapshots/counts.
- Added `ReviewGate buildingtargetlistobjectdeleted` to lock the public building-list boundary.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetlistobjectdeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- aiopponentloop`
  Result: pass
  Evidence: historical AI opponent loop gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-list-object-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full VerifyAll completed successfully, 23/23 steps passed.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: API boundary cleanup only; AI/combat/production behavior is covered by automated gates.

Reviewer result:
- Status: pass for build, narrow gate, historical AI opponent gate, CombatBehavior, full ReviewGate, review-record gate, and VerifyAll.
- Required fixes: none.
- Residual risks: `UpsertBuildingTarget` still returns `UnitBattlefieldBuildingTarget`, and internal migration code still uses the private wrapper list until later slices remove the wrapper entirely.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget public building list object API deletion`.
- Items left open: `UpsertBuildingTarget` return cleanup, broader building-runtime migration cleanup, and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: external building list reads no longer need public target-wrapper objects, but building creation/upsert still returns the wrapper during migration.
