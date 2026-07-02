# Review Record - Player observation view

Step: Add ObservationView read-only snapshot
Milestone: M8 AI, Campaign, Sandbox
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate playerobservationview / architecture
Integrator AI: Remote Linux Codex

Scope:
- Issue: #107.
- Files/folders: `scripts/core/players/ObservationView.cs`, `scripts/core/players/PlayerControllerContracts.cs`, `tools/ReviewGateDomains/ArchitectureReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-02-player-observation-view.md`.
- Non-goals: 不接入 live controllers，不迁移敌人 AI，不实现完整 fog memory UI，不改变 `VisionSystem`、`VisibilityIndex` 或 combat 行为。

Implementation summary:
- 将 #106 的 `ObservationView` 占位移到独立文件，并扩展为只读玩家观测快照。
- 新增 `ObservedEntity`、`ObservedPlayerState`、`ObservedCommandAffordance`，覆盖 viewer slot/owner/tick、可见实体摘要、己方状态、已知玩家列表和可用命令 affordance。
- 继续保持 `scripts/core/players` 不引用 presentation 或 authority mutation 类型。
- 扩展 `ReviewGate architecture`，要求 ObservationView 只读集合和快照类型存在。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- playerobservationview`
  Result: pass
  Evidence: narrow architecture mode completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- architecture`
  Result: pass
  Evidence: ReviewGate architecture completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: ReviewGate filesize completed with 0 errors and 0 warnings after updating validation-suite source budget evidence.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=player-observation-view`
  Result: pass
  Evidence: ReviewGate found this durable review record and completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.

Reviewer result:
- Status: pass.
- Required fixes: none known before gates.
- Residual risks: 该 slice 只定义观测快照形状，不构建 live visibility-fed snapshot；后续需要 builder/gateway/controller 接线。

TODO update:
- Items marked done: none.
- Items left open: M8 AI planners through command buffer.
- Reason: #107 只建立只读观测数据模型，不完成 AI planner runtime wiring。
