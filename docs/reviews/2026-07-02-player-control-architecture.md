# Review Record - Player control architecture

Step: 设计 PlayerSlot / Controller / Agent 统一玩家控制架构
Milestone: M8 AI, Campaign, Sandbox
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate architecture / review
Integrator AI: Remote Linux Codex

Scope:
- Issue: #78.
- Files/folders: `docs/PlayerControlArchitecture.md`, `TODO.md`, `docs/reviews/2026-07-02-player-control-architecture.md`.
- Non-goals: 不实现网络同步，不接入真实 LLM API，不训练 RL，不改平衡/剧情/UI 视觉，不让 controller 或 agent 绕过 `CommandSystem`。

Implementation summary:
- 新增中文 ADR，明确 `Faction` 是内容、`PlayerSlot` 是身份、`PlayerController` 是输入来源、`PlayerAgent` 是思考方式、`Transport` 是通道、`SimulationAuthority` 是裁判、`CommandSystem` 是唯一入口。
- 文档覆盖单机、本地多人、云联机、回放、AI QA、RL/LLM 测试和玩家自带外部 AI 的数据流。
- 文档锁定 `ObservationView -> PlayerCommand[] -> CommandGateway -> CommandSystem -> Simulation Tick` 边界，并列出隐藏信息、越权控制、限频和 sandbox policy 要求。
- TODO M8 记录本 ADR 的进展，同时保持 broad AI planner TODO 打开。
- 已拆出后续小 Issue：#106 PlayerController contract、#107 ObservationView read-only snapshot、#108 CommandGateway validation shell。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- architecture`
  Result: pass
  Evidence: ReviewGate architecture completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=player-control-architecture`
  Result: pass
  Evidence: ReviewGate found this durable review record and completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.

Reviewer result:
- Status: pass.
- Required fixes: none known before gates.
- Residual risks: 当前只是架构锁定，不包含 runtime contract、ObservationView 或 CommandGateway 代码；这些会拆成后续小 Issue 实现。

TODO update:
- Items marked done: none.
- Items left open: M8 AI planners through command buffer.
- Reason: #78 只完成统一玩家控制架构设计和后续 Issue 拆分，不完成 AI planner runtime wiring。
