# Review Record - M9 legacy EnemyAttackWaveAi scans

Step: #126 Remove legacy EnemyAttackWaveAi scan LINQ
Milestone: M9 Elegance & Decoupling / allocation paydown
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression
Integrator AI: Remote Linux Codex

Scope:
- 文件：`scripts/core/ai/EnemyAttackWaveAi.cs`、`tools/ReviewGateRuntime/EnemyAttackWaveAiReviewGate.cs`、`TODO.md`。
- 目标：旧 GameState `EnemyAttackWaveAi` 的 wave-unit selection、target scan、enemy-center scan 不再使用 LINQ / `ToList()` materialization。
- 非目标：不修改 attack wave timing、minimum/maximum wave sizes、HQ-first target priority、aggression radius、combat balance 或父项 #10 状态。

Implementation summary:
- 增加 `_waveUnits` reusable buffer，`CollectAvailableCombatUnits(...)` 填充并在原 storage 中按 X / id 排序，再执行 maximum cap。
- `TryFindTarget(...)` 改为显式 HQ scan、nearest building scan、nearest unit scan，distance tie 仍保留 first encountered candidate。
- `EnemyCenter(...)` 改为显式 sum/count。
- `ReviewGate regression` 现在要求旧 `EnemyAttackWaveAi` 保持 reusable wave storage、explicit nearest scans、no LINQ materialization 合约。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass，覆盖 enemy AI / economy / production / outcomes。
- Command: `dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore`
  Result: pass，96s runtime loop 通过，6 waves，HQ damage 1200，command proof total applied 161。
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
- Residual risks: 本 slice 只处理旧 GameState attack-wave AI；父项 #10 的 broader runtime allocation cleanup 仍保持 open。

TODO update:
- 已在 M9 per-tick allocation paydown 父项记录 #126/#127 follow-up。
- 父项 #10 保持 open。
