# Review Record - M9 Legacy GameState Pathfinding Workspace

Step: #183 `[M9] Reuse legacy GameState pathfinding workspace`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / PathfindingAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `scripts/core/game-state/GameState.CommandBuffers.cs`, `scripts/core/game-state/GameState.PathingAvoidance.cs`, `tools/ReviewGateRuntime/PathfindingAllocationReviewGate.cs`.
- Non-goals: 不改变 A*、LOS smoothing/pruning、shared-corridor grouping、terrain passability、local avoidance、formation、movement behavior、或 returned path/raw-cell durable ownership 语义。

Implementation summary:
- `GameState` 新增 `_legacyPathWorkspace`，用于 legacy single-path fallback 与 shared-corridor path planning。
- `GameState` 新增 `_legacyPathCorridorAssignments`，用于 legacy shared move corridor assignment results。
- `AssignPath(...)` no planned-path 分支改用 `FindPathWithDebug(_legacyPathWorkspace, ...)` caller-owned workspace overload。
- `CollectLegacyMoveDomainAssignments(...)` 改用 `FindSharedCorridor(_legacyPathWorkspace, ..., _legacyPathCorridorAssignments)` caller-owned workspace / assignment-buffer overload。
- `ReviewGate regression` 锁定 legacy `GameState` pathing 不回退到 allocating compatibility overloads。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，shared-corridor、entity-pathfinding、entity-shared-corridor、group-move replay hashes preserved。
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass，legacy shared corridor / move / attack / stance player loop coverage preserved。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，0 errors / 0 warnings；`tools/ReviewGateRuntime` suite remains within budget after compacting the regression checks。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-legacy-gamestate-pathfinding-workspace`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.
- Residual risks: returned path/raw-cell lists still allocate at durable ownership boundaries by design；legacy path semantics rely on existing `PathfindingMath` ownership guards。

TODO update:
- Items marked done: none，#10 parent remains open。
- Items left open: broader profiler-guided pathfinding/search storage cleanup remains future work。
