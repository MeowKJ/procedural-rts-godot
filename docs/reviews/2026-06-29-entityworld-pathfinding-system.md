# Review Record - EntityWorld PathfindingSystem

Step:
Add a minimal EntityWorld pathfinding slice that stores LOS-simplified waypoints
in component state and feeds MovementSystem one waypoint at a time.

Milestone:
M2 Movement Algorithms & Unit Autonomy.

Owner AI:
Worker-M2 / Codex.

Reviewer AI:
Codex self-review with SimReplay and `ReviewGate entitypathfinding`.

Integrator AI:
Main integration thread.

Scope:
- Files/folders:
  - `scripts/core/entities/EntityComponentState.cs`
  - `scripts/core/entities/EntityStateHash.cs`
  - `scripts/core/sim/SimInvariants.cs`
  - `scripts/core/sim/systems/PathfindingSystem.cs`
  - `scripts/core/sim/systems/CommandSystem.cs`
  - `scripts/BattleRoot.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `docs/reviews/2026-06-29-entityworld-pathfinding-system.md`
- Non-goals:
  - No live GameState shared-corridor changes.
  - No flow-field implementation.
  - No build, UnitSpec, CombatSystem, or UI changes.
  - No TODO.md update in this worker slice.

Implementation summary:
- Added `PathfindingComponentState` with final goal, simplified waypoints, and
  next waypoint index.
- Added deterministic hash and invariant coverage for pathfinding state.
- Added `PathfindingSystem`, which plans through `PathfindingMath.FindPathWithDebug`
  and therefore reuses existing LOS simplification.
- Kept MovementSystem as the waypoint consumer through its existing `MoveTarget`
  behavior.
- Limited blockers to static EntityWorld building/turret/objective collision
  bodies so dynamic unit avoidance remains in the existing local avoidance and
  separation systems.
- Inserted `PathfindingSystem` before `MovementSystem` in the BattleRoot
  EntityWorld pipeline.
- Added SimReplay coverage for an EntityWorld mover navigating a static wall.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build reported 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result:
  Fail after this slice's new pathfinding scenario passed.
  Evidence:
  SimReplay printed `OK [entity-pathfinding]: 2 LOS-pruned waypoints, inflation 1.28.` before failing later in existing `construction-loop` with `construction placement should reject positions outside owner build radius with a reason`.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj entitypathfinding --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.

Reviewer result:
- Status: pass-with-warnings.
- Required fixes:
  - None known before automated gates.
- Residual risks:
  - Full SimReplay is currently blocked by an unrelated construction-loop
    assertion after the EntityWorld pathfinding checks pass.
  - This is a per-entity waypoint follower, not a shared EntityWorld corridor or
    flow-field planner.
  - Static blocker extraction is intentionally narrow and does not yet include
    dense dynamic unit blobs.
  - Funnel smoothing remains represented by existing LOS pruning, not a full
    portal funnel implementation.

TODO update:
- Items marked done:
  - None; this worker slice intentionally does not edit TODO.md.
- Items left open:
  - Broader EntityWorld shared-corridor/funnel work.
  - Full flow-field scale pathing.
- Reason:
  - The slice proves deterministic EntityWorld path generation and waypoint
    consumption while leaving larger pathfinding work separate.
