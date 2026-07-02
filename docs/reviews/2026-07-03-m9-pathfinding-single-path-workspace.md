# Review Record - M9 Pathfinding Single Path Workspace

Step: #176 `[M9] Reuse PathfindingSystem single-path workspace`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / PathfindingAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/pathing/PathfindingMath.cs`, `scripts/core/pathing/PathfindingMath.Search.cs`, `scripts/core/pathing/PathfindingWorkspace.cs`, `scripts/core/sim/systems/PathfindingSystem.cs`, `tools/ReviewGateRuntime/PathfindingAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 returned path/raw-cell ownership，不处理 shared-corridor assignment/connector allocations，不重写 A*、LOS smoothing 或 terrain passability。

Implementation summary:
- Added `PathfindingWorkspace` for reusable blocker/terrain/cameFrom/gScore/open/neighbor search state.
- Added `FindPathWithDebug(PathfindingWorkspace, ...)` while preserving existing compatibility overloads.
- Routed `PathfindingSystem.PlanPath(...)` through `_pathWorkspace` for single-entity path planning.
- Split `PathfindingMath.Search.cs` so the added overload does not push `PathfindingMath.cs` into the yellow file-size watchlist.
- `ReviewGate regression` locks the PathfindingSystem workspace call and forbids the old allocating single-path call shape.

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
- Required fixes: split `PathfindingMath.Search.cs` after `ReviewGate filesize` caught `PathfindingMath.cs` at 412 lines.
- Residual risks: compatibility callers still allocate a new `PathfindingWorkspace`; this slice only targets `PathfindingSystem` single-path planning.

TODO update:
- Items marked done: none，#10 parent remains open.
- Items left open: shared-corridor assignment/connector scratch buffers and broader pathfinding allocation cleanup remain future child slices.
