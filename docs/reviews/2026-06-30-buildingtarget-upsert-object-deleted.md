# Review Record - UnitBattlefieldBuildingTarget upsert object API deletion

Step: UnitBattlefieldBuildingTarget upsert object API deletion
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetupsertobjectdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/UnitBattlefieldBuildingSnapshot.cs`, `tools/CombatBehavior/Program.cs`, `tools/PlayerLoopQa/Program.cs`, `tools/AiOpponentLoopQa/Program.cs`, `tools/AiDifficultySmoke/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-upsert-object-deleted.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, changing building creation behavior, changing AI priorities, changing balance, or changing visual style.

Implementation summary:
- Changed `public UnitBattlefieldBuildingSnapshot UpsertBuildingTarget(...)` to return immutable snapshots instead of mutable `UnitBattlefieldBuildingTarget` wrappers.
- Made `UnitBattlefieldBuildingSnapshot` an explicit readonly struct with get-only fields.
- Updated PlayerLoopQa, AiOpponentLoopQa, AiDifficultySmoke, and CombatBehavior fixtures to store snapshots and re-read current building HP through `BuildingSnapshot(int id)`.
- Replaced damaged-building fixture mutation with an id-based upsert so tests do not mutate stale local snapshots.
- Added `ReviewGate buildingtargetupsertobjectdeleted` and updated historical BuildSpec upsert gates to expect snapshot-returning upsert.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa completed successfully.
- Command: `dotnet run --project tools/AiDifficultySmoke/AiDifficultySmoke.csproj --no-restore`
  Result: pass
  Evidence: AiDifficultySmoke completed successfully.
- Command: `dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore`
  Result: pass
  Evidence: AiOpponentLoopQa completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetupsertobjectdeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetupsertspec`
  Result: pass
  Evidence: historical BuildSpec upsert gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-upsert-object-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full VerifyAll completed successfully, 23/23 steps passed.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: API boundary cleanup only; runtime combat, AI, production, and Godot headless flows are covered by automated gates.

Reviewer result:
- Status: pass for build, targeted QA tools, narrow gate, historical upsert gate, full ReviewGate, review-record gate, and VerifyAll.
- Required fixes: none.
- Residual risks: `UnitBattlefieldBuildingTarget` still exists internally as private migration state and private helper parameters until the internal building runtime is fully collapsed into EntityWorld projections/components.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget upsert object API deletion`.
- Items left open: broader internal building-runtime migration cleanup and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: public building upsert no longer returns target-wrapper objects, but private migration helpers still use the wrapper while EntityWorld becomes authoritative.
