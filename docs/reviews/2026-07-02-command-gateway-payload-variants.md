# Review Record - CommandGateway payload validation variants

Step: CommandGateway payload validation variants (#110)
Milestone: M8 unified player control architecture
Owner AI: Codex
Reviewer AI: SimReplay / ReviewGate commandgateway
Integrator AI: Codex

Scope:
- 文件范围：`tools/SimReplayCommandGateway/CommandGatewayScenarios.cs`、`docs/reviews/2026-07-02-command-gateway-payload-variants.md`。
- 目标：补齐 CommandGateway deterministic coverage，覆盖 build point+spec accept、produce missing spec reject、rally point accept、invalid subject reject、sink rejection structured error。
- 非目标：不改变 Gateway validation 规则，不迁移 live controllers 或 AI，不实现真实 PlayerCommand->EntityCommand 转换。

Implementation summary:
- 新增 `AssertCommandGatewayPayloadVariants(...)`，使用独立 gateway sequence state 测试 build / produce / rally / invalid subject / sink rejection。
- 扩展 `RecordingGatewaySink`，可模拟 sink rejection，验证 Gateway 返回 `EntityCommandSinkRejected` 且提供结构化反馈文本。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 主项目 build 0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `OK [command-gateway]` 覆盖 payload shape variants，完整 SimReplay PASSED。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- commandgateway`
  Result: pass
  Evidence: focused ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED，23/23 steps 全部通过，包含 build、SimReplay、ReviewGate、PerfSmoke 和 Godot headless QA。

Manual/visual gates:
- Check: GUI visual QA
  Result: not run
  Evidence: 这是 validation coverage slice，没有改动渲染或 UI。

Reviewer result:
- Status: pass
- Required fixes: none after final gate rerun.
- Residual risks: coverage 扩展不代表 live input 已经迁移到 CommandGateway，也不验证真实 EntityCommandBuffer translation。

TODO update:
- Items marked done: none.
- Items left open: M8 live controller migration remains open.
- Reason: 该 slice 只补测试覆盖，不改变 roadmap 状态。
