# Review Record - M9 Shared Corridor Connector Workspace

Step: #178 `[M9] Reuse shared-corridor connector workspace`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / PathfindingAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/pathing/PathfindingMath.cs`, `scripts/core/pathing/PathfindingMath.SharedCorridor.cs`, `scripts/core/sim/systems/PathfindingSystem.cs`, `tools/ReviewGateRuntime/PathfindingAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 A*、LOS smoothing、terrain passability、shared-corridor grouping、movement behavior，且不改变 assignment path/raw-cell durable ownership。

Implementation summary:
- `FindSharedCorridor(...)` now has a caller-owned `PathfindingWorkspace` overload in addition to the existing assignment-buffer overload.
- Shared-corridor root path, fallback path, connector path, and exit path searches now call the workspace `FindPathWithDebug(...)` overload.
- `PathfindingSystem.PlanSharedCorridors(...)` routes group path planning through its reusable `_pathWorkspace`.
- `ReviewGate regression` locks the shared-corridor workspace overload and forbids the old compatibility calls from returning in these hot paths.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，shared-corridor and entity-shared-corridor replay hashes preserved。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-shared-corridor-connector-workspace`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.
- Residual risks: returned path/raw-cell lists remain durable result allocations by design; this slice only reuses search scratch storage for shared-corridor path searches.

TODO update:
- Items marked done: none，#10 parent remains open.
- Items left open: broader pathfinding returned-list allocation and profiler-guided GC cleanup remain future work.
