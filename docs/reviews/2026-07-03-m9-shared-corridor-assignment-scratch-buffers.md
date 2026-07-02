# Review Record - M9 Shared Corridor Assignment Scratch Buffers

Step: #179 `[M9] Reuse shared-corridor assignment scratch buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / PathfindingAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/pathing/PathfindingWorkspace.cs`, `scripts/core/pathing/PathfindingMath.SharedCorridor.cs`, `tools/ReviewGateRuntime/PathfindingAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 A*、LOS smoothing、terrain passability、shared-corridor grouping、movement behavior、或 `PathfindingCorridorAssignment.Path` / `RawCells` 的 durable ownership 语义。

Implementation summary:
- `PathfindingWorkspace` now owns `SharedCorridorPoints` and `SharedCorridorRawCells` scratch buffers for assignment construction.
- `BuildSharedCorridorAssignment(...)` reuses those buffers instead of allocating per-member raw-cell and point scratch lists.
- Assignment raw cells are copied only at the durable result boundary, and `DurableSharedCorridorPath(...)` prevents returned paths from aliasing workspace point scratch storage.
- `ReviewGate regression` locks the workspace scratch buffers and forbids the old per-assignment scratch list allocations from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，shared-corridor and entity-shared-corridor replay hashes preserved。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-shared-corridor-assignment-scratch-buffers`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.
- Residual risks: returned path/raw-cell lists still allocate by design; this slice only removes per-member scratch list allocations inside assignment construction.

TODO update:
- Items marked done: none，#10 parent remains open.
- Items left open: path smoothing/pruning result buffers and broader profiler-guided GC cleanup remain future work.
