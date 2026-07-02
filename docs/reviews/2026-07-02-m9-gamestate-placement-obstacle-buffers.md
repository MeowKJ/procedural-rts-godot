# Review Record - M9 GameState Placement Obstacle Buffers

Step: m9-gamestate-placement-obstacle-buffers (#140)
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: PlayerLoopQa / ReviewGate simhot / self-review
Integrator AI: Remote Linux Codex

Scope:
- 将 legacy `BuildingObstacles()` list construction 改为 `CollectBuildingObstacles(_legacyPlacementObstacles)` caller-owned buffer。
- 将 `PathObstacles(...)` 的 `_mapObstacles.Concat(...).SelectMany(...).Distinct().ToList()` 改为 `_legacyPlacementObstacles`、`_legacyPathObstacles`、`_legacyPathObstacleSet` 显式填充。
- 将 dense-unit blob obstacle `GroupBy` / `ToList` / `Min` / `Max` chain 改为 `_legacyDenseBlobObstacles` 聚合。
- 将 path obstacle helpers 拆到 `GameState.PathObstacles.cs`，让 `GameState.SeedingMap.cs` 回到 224 lines，避免 file-size yellow warning。
- 扩展 `GameStateAllocationReviewGate`，锁住 placement/path obstacle no-LINQ/no-list contract。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: PASS，0 warnings / 0 errors。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: PASS，terrain passability、production/economy、outcomes 覆盖通过。
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: PASS，build placement、shared corridor、move/attack/stance 覆盖通过。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: PASS，Errors: 0，Warnings: 0；validation-tool source-budget evidence 已同步到 144 files / 19394 lines。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: PASS，23/23 steps 全部通过，包含 full ReviewGate、PerfSmoke 与 Godot headless QA。

Reviewer result:
- pass

Status:
- pass

Residual risks:
- `PathfindingMath.FindPathWithDebug(...)` 仍会将 obstacle collection 转成内部 `HashSet`，本 slice 只移除 legacy GameState obstacle aggregation 的 list/LINQ allocation。
- Debug path obstacle callers receive the reusable path-obstacle list; current debug drawing consumes it immediately.

TODO update:
- M9 per-tick allocation paydown 保持打开；本 slice 作为 #140 follow-up 记录到 TODO 进度。
