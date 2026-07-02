# Review Record - CommandGateway validation shell

Step: CommandGateway validation shell
Milestone: M8 unified player control architecture
Owner AI: Codex
Reviewer AI: SimReplay / ReviewGate
Integrator AI: Codex

Scope:
- 文件范围：`scripts/core/players/PlayerControllerContracts.cs`、`scripts/core/players/PlayerCommandPayload.cs`、`scripts/core/players/CommandGateway*.cs`、`tools/SimReplayCommandGateway/CommandGatewayScenarios.cs`、`tools/ReviewGateRuntime/CommandGatewayReviewGate.cs`。
- 目标：新增 Godot-free `CommandGateway` shell、value payload、结构化 `PlayerCommandResult` / `CommandGatewayValidationError`，并通过 `ICommandGatewayEntityCommandSink` 预留 EntityCommandBuffer 转发边界。
- 非目标：不迁移 live input、AI controller、网络、回放或外部模型，不改变 `CommandSystem` 语义、战斗平衡或 UI。

Implementation summary:
- `PlayerCommand` 现在携带 `PlayerCommandPayload`，payload 使用 read-only `EntityId` subject list、有限值 point、target/spec/stance/ability 字段，不依赖 Godot 类型。
- `CommandGateway` 验证 controller id、issuer slot、controller slot rights、client sequence 单调性、target tick、payload shape、spec id 长度和 sandbox-only command gate。
- Gateway 只把 accepted command 交给可选 sink；它不引用 `GameState`、`UnitBattlefield` 或 `EntityWorld`，不直接写权威 runtime state。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 主项目 build 0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `OK [command-gateway]` 覆盖 accepted sink forwarding、重复 sequence、越权 slot、move payload shape 和 sandbox gate；完整 SimReplay PASSED。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- commandgateway`
  Result: pass
  Evidence: focused ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED，23/23 steps 全部通过，包含 build、SimReplay、CombatBehavior、ReviewGate、PerfSmoke、Godot headless QA。

Manual/visual gates:
- Check: GUI visual QA
  Result: not run
  Evidence: 这是 Godot-free core shell slice，没有改动渲染或 UI。

Reviewer result:
- Status: pass
- Required fixes: none after final ReviewGate rerun.
- Residual risks: 该 slice 只是 Gateway 壳；live `SelectionController`、`ProductionController`、`BuildPlacementController` 和 enemy AI 仍未迁移到统一入口。

TODO update:
- Items marked done: none.
- Items left open: M8 live controller migration remains open.
- Reason: #108 只要求最小 validation shell，不代表所有 live input 已经统一迁移。
