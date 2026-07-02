# Review Record - Player controller contract

Step: Add minimal PlayerController contract
Milestone: M8 AI, Campaign, Sandbox
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate playercontrollercontract / architecture
Integrator AI: Remote Linux Codex

Scope:
- Issue: #106.
- Files/folders: `scripts/core/players/PlayerControllerContracts.cs`, `tools/ReviewGateDomains/ArchitectureReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-02-player-controller-contract.md`.
- Non-goals: 不重接现有 live controllers，不迁移敌人 AI，不实现网络、回放、LLM/RL，不改变平衡、剧情或 UI。

Implementation summary:
- 新增最小 `IPlayerController` / `IPlayerAgent` contract，以及 controller/agent ids、kind enums、`ObservationView` 占位、`PlayerCommand` 占位、`PlayerControllerContext` 和 `PlayerControllerResult`。
- contract 只表达受控 `PlayerSlotId`、固定 tick 上下文、只读观测输入和命令意图输出。
- 扩展 `ReviewGate architecture`，要求 controller contract 存在并拒绝 `scripts/core/players` 引用 presentation 或 authority mutation 类型。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- playercontrollercontract`
  Result: pass
  Evidence: narrow architecture mode completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- architecture`
  Result: pass
  Evidence: ReviewGate architecture completed with 0 errors and 0 warnings after the source checks were added.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: ReviewGate filesize completed with 0 errors and 0 warnings after updating the validation-suite source budget evidence.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=player-controller-contract`
  Result: pass
  Evidence: ReviewGate found this durable review record and completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.

Reviewer result:
- Status: pass.
- Required fixes: none known before gates.
- Residual risks: `ObservationView` 和 `PlayerCommand` 仍是最小占位；#107/#108 会扩展只读快照和 Gateway validation shell。

TODO update:
- Items marked done: none.
- Items left open: M8 AI planners through command buffer.
- Reason: #106 只建立统一 controller/agent contract，不完成 live controller 迁移。
