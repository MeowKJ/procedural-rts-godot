# Review Record - M9 legacy EnemyProductionAi scans

Step: #127 Remove legacy EnemyProductionAi LINQ scans
Milestone: M9 Elegance & Decoupling / allocation paydown
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression
Integrator AI: Remote Linux Codex

Scope:
- 文件：`scripts/core/ai/EnemyProductionAi.cs`、`tools/ReviewGateRuntime/EnemyProductionAiReviewGate.cs`、`TODO.md`。
- 目标：旧 GameState `EnemyProductionAi` 的生产选择、queue count、ready spec cost、rally producer scan、base-center scan 不再使用 LINQ materialization。
- 非目标：不修改生产偏好、AI difficulty、单位成本、enqueue 结果或父项 #10 状态。

Implementation summary:
- 将 harvester count、queued harvester count、queued item count 改为显式循环。
- 用 `TryMinReadyProductionCost(...)` 直接扫描 ready producer 的最低 cost，替代 `ReadyProductionSpecs(...).ToArray()` 和 `Min(...)`。
- 用 `HasPlayableProducerKind(...)` 和显式 base-center sum/count 替代 rally/base-center LINQ。
- `ReviewGate regression` 现在要求旧 `EnemyProductionAi` 保持 no-LINQ/no-ready-spec-iterator/no-combat-preference-array 合约。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass，覆盖 enemy AI / economy / production / outcomes。
- Command: `dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore`
  Result: pass，96s runtime loop 通过，6 waves，production orders 21，construction probe hash `8C1715CDCC58D60A`。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- all --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass，23/23 steps 通过，包含 AiOpponentLoopQa、PerfSmoke、DesktopHudQa 和 Godot headless QA。

Reviewer result:
- Status: pass。
- Required fixes: none。
- Residual risks: 这是旧 GameState AI allocation cleanup；新的 UnitBattlefield AI 已有独立 gates，父项 #10 仍继续追踪 broader profiler-guided GC cleanup。

TODO update:
- 已在 M9 per-tick allocation paydown 父项记录 #126/#127 follow-up。
- 父项 #10 保持 open。
