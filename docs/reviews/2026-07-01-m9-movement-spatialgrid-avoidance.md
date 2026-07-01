# Review Record - M9 movement SpatialGrid avoidance

Step: M9 retire second grid style
Milestone: M9 Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate architecture / SelectionStress / SimReplay
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/SpatialGrid.cs`, `scripts/core/sim/systems/MovementSystem.cs`, `scripts/core/sim/systems/movement/MovementSystem.Arrival.cs`, `scripts/core/pathing/LocalAvoidanceMath.cs`, `scripts/core/pathing/LocalAvoidanceTypes.cs`, `scripts/core/pathing/AdvancedPathingPolicy.cs`, `tools/SelectionStress/`, `tools/ReviewGate/ArchitectureReviewGate.cs`, `TODO.md`.
- Non-goals: changing local avoidance tuning, changing A* `GridObstacle` pathfinding, or deleting legacy `GameState` compatibility movement in this slice.

Implementation summary:
- Replaced MovementSystem's private `Dictionary<GridObstacle, List<LocalAvoidanceBody>>` hash with a shared `SpatialGrid<LocalAvoidanceBody>`.
- Added coordinate overloads to `SpatialGrid<T>` so non-Godot pure math can use the same grid idiom without constructing a `Vector2`.
- Renamed `SpatialHashAvoidanceMath` to `LocalAvoidanceMath` and split `LocalAvoidanceBody` / `LocalAvoidanceVector` into a focused type file; the math file is now 197 lines.
- Updated crowded-arrival and local-avoidance queries to use the shared grid while preserving deterministic neighbor order.
- Switched `SelectionStress` to reference the main project instead of hand-linking copied core math files.
- Added ReviewGate architecture checks that prevent MovementSystem from reintroducing `BuildHashInto` or a second grid dictionary style.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass
  Evidence: selection/pathing stress completed 80 cases.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay completed all deterministic scenarios with stable hashes after the grid swap.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- architecture`
  Result: pass
  Evidence: ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully after closing the interactive Godot process, including `selection-stress`, SimReplay, full ReviewGate, PerfSmoke, and Godot headless QA.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: legacy `GameState` compatibility movement still uses the dictionary overloads in `LocalAvoidanceMath`; delete that path only when the remaining presentation compatibility layer is retired.

TODO update:
- Items marked done: `Retire the second grid style`.
- Items left open: broader per-tick allocation paydown and combat system convergence.
- Reason: MovementSystem now uses the shared grid idiom, the old private hash path is guarded against regression, and deterministic replay evidence stayed green.
