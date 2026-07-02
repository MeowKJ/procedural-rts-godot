# Review Record - M9 Path Reconstruction Scratch Buffers

Step: #181 `[M9] Reuse path reconstruction scratch buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / PathfindingAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/pathing/PathfindingWorkspace.cs`, `scripts/core/pathing/PathfindingMath.cs`, `scripts/core/pathing/PathfindingMath.Search.cs`, `tools/ReviewGateRuntime/PathfindingAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 A*、neighbor ordering、LOS smoothing/pruning algorithm、terrain passability、shared-corridor grouping、movement behavior、或 `PathfindingDebugResult.Path` / `RawCells` 的 durable ownership 语义。

Implementation summary:
- `PathfindingWorkspace` now owns `ReconstructedCells` and `ReconstructedPoints` scratch buffers.
- `FindPathWithDebug(...)` passes the workspace into `ReconstructPath(...)`.
- `ReconstructPath(...)` reuses workspace cells/points buffers instead of allocating local reconstruction scratch lists.
- Raw cells are copied at the durable result boundary, and `DurableSearchPath(...)` prevents returned paths from aliasing workspace point scratch storage when smoothing/pruning returns the input list.
- `ReviewGate regression` locks the reconstruction workspace buffers and forbids the old local list allocations from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，shared-corridor and entity-shared-corridor replay hashes preserved。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-path-reconstruction-scratch-buffers`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.
- Residual risks: smoothing/pruning still allocate result lists by design; this slice only removes reconstruction scratch list allocations.

TODO update:
- Items marked done: none，#10 parent remains open.
- Items left open: path smoothing/pruning result buffers and broader profiler-guided GC cleanup remain future work.
