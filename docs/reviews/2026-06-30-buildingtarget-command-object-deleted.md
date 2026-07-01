# Review Record - UnitBattlefieldBuildingTarget command object overload deletion

Step: UnitBattlefieldBuildingTarget command object overload deletion
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetcommandobjectdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/UnitBattlefieldEnemyAttackWaveAi.cs`, `tools/PlayerLoopQa/Program.cs`, `tools/AiOpponentLoopQa/Program.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-command-object-deleted.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, replacing the `Buildings` list, changing unit-target command APIs, changing production queue/rally accessor APIs, changing AI target selection, or changing combat balance.

Implementation summary:
- Removed public selected-building repair and building attack command overloads that accepted `UnitBattlefieldBuildingTarget`.
- Kept id-based building command APIs and made `UnitBattlefield` resolve the target wrapper internally.
- Migrated enemy wave/defense AI, PlayerLoopQa, AiOpponentLoopQa, and CombatBehavior to submit building commands by id.
- Updated ReviewGate historical checks and added `buildingtargetcommandobjectdeleted` so command-object overloads cannot return.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetcommandobjectdeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-command-object-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 steps successfully, including build, SimReplay, CombatBehavior, FogOfWarQa, PerfSmoke, ReviewGate, PlayerLoopQa, AiOpponentLoopQa, and Godot headless QA.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: command API cleanup only; no rendering or layout behavior changed.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: `UnitBattlefieldBuildingTarget` still remains as a migration wrapper for building lists, events, and production/rally/accessor state until later slices move those surfaces to id/projection APIs.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget command object overload deletion`.
- Items left open: broader building-runtime migration cleanup and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: building commands no longer accept target wrapper objects, but other target wrapper public surfaces remain.
