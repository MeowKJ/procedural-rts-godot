# Review Record - M9 enemy production AI candidate buffers

Step: M9 enemy production AI candidate buffer reuse (#109)
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Codex
Reviewer AI: AiOpponentLoopQa / ReviewGate simhot
Integrator AI: Codex

Scope:
- 文件范围：`scripts/core/units/runtime/UnitBattlefieldEnemyProductionAi*.cs`、`tools/ReviewGateRuntime/EnemyProductionAiReviewGate.cs`、`tools/ReviewGateDomains/RegressionReviewGate.cs`、`TODO.md`。
- 目标：复用 enemy production AI 的 queueable option、owned-building、idle-harvester 和 command-id buffers，并用显式扫描替换 production / construction / economy candidate LINQ materialization。
- 非目标：不改变 AI 策略、经济节奏、建筑优先级、生产选择权重、战斗平衡或 CommandGateway 迁移状态；不关闭 broad #10。

Implementation summary:
- `ChooseNextProductionDesign(...)` 现在复用 `_queueableDesignOptions`，用显式 best-option scan 保持 army-count / queued-count / cost / design-id tie-break。
- `NextNeededBuilding(...)`、base-center、faction lookup 和 construction candidate positions 改为显式扫描；固定 build offsets 改成 static arrays，避免每次决策分配 offset arrays/yield iterator。
- Harvester economy assignment 复用 `_idleHarvesterBuffer` 和 `_idleHarvesterIds`，最近可见资源场也改为显式 nearest scan。
- 新增 `EnemyProductionAiReviewGate`，通过 `ReviewGate simhot` 锁住 targeted partials 中的 `.Where/.OrderBy/.Select/.ToList/.Any/.Sum/.FirstOrDefault` 回退。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 主项目 build 0 warnings / 0 errors。
- Command: `dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore`
  Result: pass
  Evidence: runtime loop 和 construction command probe 均 PASSED；生产、建筑、经济和 wave command proof 保持通过。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot`
  Result: pass
  Evidence: focused ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: focused filesize ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED，23/23 steps 全部通过，包含 build、SimReplay、AiOpponentLoopQa、ReviewGate、PerfSmoke 和 Godot headless QA。

Manual/visual gates:
- Check: GUI visual QA
  Result: not run
  Evidence: 这是 runtime AI allocation slice，没有改动渲染或 UI。

Reviewer result:
- Status: pass
- Required fixes: none after final gate rerun.
- Residual risks: `ProductionOptionStates(...)` / `ProductionDesignOptionStates(...)` 仍由 `UnitBattlefield` 构造 snapshot list；本 slice 只移除 enemy production AI 自身的候选 materialization。

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: broad #10 allocation cleanup and profiler-guided GC work.
- Reason: This closes only the enemy production AI candidate-buffer child slice.
