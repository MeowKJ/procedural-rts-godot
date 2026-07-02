# Review Record - M9 Shared Corridor Passability Buffers

Step: #180 `[M9] Reuse shared-corridor passability lookup buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / PathfindingAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/pathing/PathfindingWorkspace.cs`, `scripts/core/pathing/PathfindingMath.cs`, `tools/ReviewGateRuntime/PathfindingAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 A*、LOS smoothing、terrain passability、shared-corridor grouping、movement behavior、returned path/raw-cell ownership，且不把 assignment LOS lookup 和 search lookup 合并成同一实例。

Implementation summary:
- `PathfindingWorkspace` now owns `SharedCorridorBlocked` and `SharedCorridorTerrainByCell` for shared-corridor assignment LOS checks.
- `FindSharedCorridor(...)` uses those dedicated buffers instead of allocating local `HashSet<GridObstacle>` / `Dictionary<GridObstacle, TerrainLayer>` lookup collections.
- Search lookups remain on `Blocked` / `TerrainByCell`, so connector/fallback/exit path searches can still remove start/goal cells without mutating the assignment LOS lookup.
- `ReviewGate regression` locks the dedicated buffer fields and forbids the old local lookup allocations from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，shared-corridor and entity-shared-corridor replay hashes preserved。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-shared-corridor-passability-buffers`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.
- Residual risks: returned path/raw-cell lists still allocate by design; this slice only removes shared-corridor assignment passability lookup allocation.

TODO update:
- Items marked done: none，#10 parent remains open.
- Items left open: path smoothing/pruning result buffers and broader profiler-guided GC cleanup remain future work.
