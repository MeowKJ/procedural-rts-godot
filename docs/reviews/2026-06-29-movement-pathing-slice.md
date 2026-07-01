# Review Record - Movement Pathing Slice

Step:
Add a minimal shared-corridor pathing helper and route selected group moves
through it so same-intent units reuse a cleaner path spine before fanning out to
formation slots.

Milestone:
M2 Movement Algorithms & Unit Autonomy.

Owner AI:
Worker-M2 / Codex.

Reviewer AI:
Codex self-review with SimReplay evidence and `ReviewGate movementpathing`.

Integrator AI:
Main integration thread.

Scope:
- Files/folders:
  - `scripts/core/PathfindingMath.cs`
  - `scripts/core/GameState.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `docs/reviews/2026-06-29-movement-pathing-slice.md`
- Non-goals:
  - No HUD/UI changes.
  - No build/economy changes.
  - No flow-field rewrite.
  - No TODO.md update in this worker slice.

Implementation summary:
- Added `PathfindingSharedCorridorResult` plus corridor member/assignment
  records.
- Added `PathfindingMath.FindSharedCorridor`, which plans one centroid-to-intent
  spine, stitches each member onto the farthest visible spine point, preserves
  raw A* cells, and reuses existing LOS pruning.
- Updated selected group move pathing in `GameState` so same-domain selected
  movers share that corridor before peeling off to their formation slots.
- Kept single-unit, harvest, attack, and stalled-unit repath paths on their
  existing per-unit planner.
- Added SimReplay coverage for a wall-detour group pathing scene and a narrow
  ReviewGate mode.

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
  Fail after this slice's new pathing scenario passed.
  Evidence:
  SimReplay printed `OK [shared-corridor]: spine 2 waypoints, 4/4 members reused it, max inflation 1.53.` before failing later in existing `construction-loop` with `only HQ, accepted power plant, and accepted barracks should exist; got 2 ... credits 500`.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj movementpathing --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.

Reviewer result:
- Status: pass-with-warnings.
- Required fixes:
  - None for this pathing slice.
- Residual risks:
  - EntityWorld `MovementSystem` still has no path component/corridor follower;
    this slice improves the live selected group move path and provides reusable
    math for the EntityWorld migration.
  - Full SimReplay is currently blocked by an unrelated construction-loop
    failure outside this movement/pathing scope.
  - This is a minimal shared spine, not a flow field or full funnel/corridor
    rewrite.

TODO update:
- Items marked done:
  - None; this worker slice intentionally does not edit TODO.md.
- Items left open:
  - Broader EntityWorld path/corridor following.
  - Flow-field/shared-corridor scale work beyond this minimal selected-group
    helper.
- Reason:
  - The code and gates prove the bounded helper and selected-group hookup, while
    larger pathfinding migration work remains separate.
