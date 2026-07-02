# Review Record - M9 Path Smoothing Scratch Buffers

Step: #182 `[M9] Reuse path smoothing scratch buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / PathfindingAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/pathing/PathfindingWorkspace.cs`, `scripts/core/pathing/PathfindingMath.PathSmoothing.cs`, `scripts/core/pathing/PathfindingMath.cs`, `scripts/core/pathing/PathfindingMath.SharedCorridor.cs`, `tools/ReviewGateRuntime/PathfindingAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 A*、neighbor ordering、LOS probe algorithm、terrain passability、shared-corridor grouping、movement behavior、或 returned path/raw-cell durable ownership 语义。

Implementation summary:
- `PathfindingWorkspace` 新增 `SmoothedPoints`、`PrunedPoints`、`FinalPathPoints` scratch buffers。
- `SmoothCollinear(...)` / `PruneByLineOfSight(...)` 改为 `SmoothCollinearInto(...)` / `PruneByLineOfSightInto(...)` caller-owned result-buffer path。
- `ReconstructPath(...)` 和 `BuildSharedCorridorAssignment(...)` 通过 `SmoothAndPrunePath(...)` 复用 workspace smoothing/pruning buffers。
- Search path 与 shared-corridor assignment path 只在最终 returned path ownership boundary 执行 `new List<PathPoint>(path)`，不返回 workspace scratch list。
- `ReviewGate regression` 锁定 no local smoothing/pruning result-list allocation contract，同时保持 `ReviewGateRuntime` suite 在 999-line budget 内。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，shared-corridor、entity-pathfinding、entity-shared-corridor replay hashes preserved。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-path-smoothing-scratch-buffers`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.
- Residual risks: returned path/raw-cell lists still allocate at durable ownership boundaries by design；`PriorityQueue` / search dictionary storage cleanup remains separate profiler-guided work。

TODO update:
- Items marked done: none，#10 parent remains open。
- Items left open: returned result ownership cleanup and broader profiler-guided GC cleanup remain future work。
