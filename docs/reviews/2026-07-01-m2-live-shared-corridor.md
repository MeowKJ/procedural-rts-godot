# Review Record - M2 live shared corridor

Step:
Route live EntityWorld group movement through shared corridor pathfinding.

Milestone:
M2 Movement Algorithms & Unit Autonomy.

Owner AI:
Codex main agent.

Reviewer AI:
Lagrange read-only M2 pathing audit, SimReplay, PlayerLoopQa, and ReviewGate.

Integrator AI:
Codex main agent.

Scope:
- Files/folders: `scripts/core/sim/systems/PathfindingSystem.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.SyncRuntime.cs`, `tools/SimReplay/Movement/SharedCorridorLiveScenarios.cs`, `tools/SimReplay/Program.cs`, `tools/PlayerLoopQa/Program.cs`, `TODO.md`, `docs/reviews/2026-07-01-m2-live-shared-corridor.md`.
- Non-goals: no kiting/min-range behavior, no flow-field heatmap implementation, no terrain/dynamic-blob migration, no group attack slot path rewrite, and no UI changes.

Implementation summary:
- Added a `PathfindingSystem` step to the live `UnitBattlefield` motion path before `MovementSystem`.
- Added a shared-corridor pass inside `PathfindingSystem` that groups same-owner, same-domain, same-intent formation-slot moves and calls `PathfindingMath.FindSharedCorridor`.
- Shared assignments write `PathfindingComponentState` and hand the first waypoint to `MovementSystem`; single movers and non-group paths continue through the existing per-entity path planner.
- Manual group attacks are excluded from shared move planning so attack-slot behavior stays owned by combat slotting.
- Added deterministic `entity-shared-corridor` replay coverage and a `PlayerLoopQa` live smoke for selected-unit group movement through static blockers.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: `OK [entity-shared-corridor]: GroupMoveEntityCommand planned a shared live corridor and reached formation slots.`
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass.
  Evidence: PlayerLoopQa passed with the new shared corridor live movement assertion.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- movementpathing`
  Result: pass.
  Evidence: ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m2-live-shared-corridor`
  Result: pass.
  Evidence: ReviewGate found this durable review record.

Manual/visual gates:
- Check: Not applicable.
  Result: not run.
  Evidence: pathing behavior is covered by deterministic replay and player-loop QA.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: this is a shared-corridor implementation rather than a full flow-field; kiting/min-range, richer terrain costs, and dynamic crowd blob pathing remain open TODO work.

TODO update:
- Items marked done: M2 shared corridor / group pathing.
- Items left open: kiting/min-range and its deterministic assertion.
- Reason: live group movement now uses the shared corridor system and is covered in both headless sim and player-loop runtime tests.
