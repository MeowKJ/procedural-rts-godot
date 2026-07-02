# Review Record - M9 Pathfinding Shared Assignment Buffer

Step: #177 `[M9] Reuse PathfindingSystem shared corridor assignment buffer`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / PathfindingAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/pathing/PathfindingMath.cs`, `scripts/core/sim/systems/PathfindingSystem.cs`, `tools/ReviewGateRuntime/PathfindingAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 assignment path/raw-cell ownership，不处理 connector/fallback path allocations，不修改 grouping、A*、LOS smoothing、terrain passability 或 movement behavior。

Implementation summary:
- `FindSharedCorridor(...)` now has a caller-owned `List<PathfindingCorridorAssignment>` overload.
- Existing compatibility `FindSharedCorridor(...)` remains for legacy/tools callers.
- `PathfindingSystem` now owns `_sharedAssignmentResults` and passes it to shared-corridor planning before copying assignments into the lookup dictionary.
- `ReviewGate regression` locks the shared-corridor assignment result buffer path.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，deterministic replay hashes preserved。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: connector/fallback path internals still allocate durable path/raw-cell lists by design; those remain future child slices if profiling warrants it.

TODO update:
- Items marked done: none，#10 parent remains open.
- Items left open: broader pathfinding allocation cleanup and shared-corridor connector scratch reuse remain future work.
