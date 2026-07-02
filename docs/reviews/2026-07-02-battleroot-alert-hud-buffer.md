# BattleRoot alert HUD buffer review

Step: BattleRoot alert HUD buffer reuse
Milestone: M9
Owner AI: remote-codex
Reviewer AI: ReviewGate presentation / Integrator
Integrator AI: remote-codex

Scope:
- 为 `BattleRoot.RefreshAlerts` 增加 `HudLayer.AlertLine` 复用缓冲。
- 用显式 newest-first 四行填充替换 alert HUD sync 中的 `OrderByDescending` / `Take` / `Select` / `ToList`。
- 扩展 `BattleRootHudAllocationReviewGate`，通过 `ReviewGate presentation` 锁定 alert buffer contract。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，主项目构建通过，0 warnings / 0 errors。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation`
  Result: pass，ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass，VerifyAll 23/23 通过，包含 DesktopHudQa、PerfSmoke、ActiveBattlePerfQa 和 Godot headless QA。

Reviewer result:
- pass。Alert 插入路径已经保持 newest-first，刷新路径只改变列表 materialization 方式，不改变 alert 文案、寿命或 HUD 行数。

Status:
- pass

Residual risks:
- ReviewGate 是文本门禁，不能直接证明运行时分配为零；整体表现继续由 PerfSmoke / ActiveBattlePerfQa 覆盖。

TODO update:
- TODO.md 的 M9 per-tick allocation paydown 条目已记录 #118 follow-up。
