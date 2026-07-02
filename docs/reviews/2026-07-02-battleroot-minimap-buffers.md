# BattleRoot minimap buffer reuse review

Step: BattleRoot minimap buffer reuse
Milestone: M9
Owner AI: remote-codex
Reviewer AI: ReviewGate presentation / Integrator
Integrator AI: remote-codex

Scope:
- 将 `BattleRoot.RefreshMinimap` 移到 focused partial，并复用 HUD 小地图 unit/building/resource 双缓冲列表。
- 用显式扫描替换小地图刷新路径中的 `ToList()` / `Select()` materialization。
- 增加 `BattleRootHudAllocationReviewGate`，通过 `ReviewGate presentation` 锁定该路径。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，主项目构建通过，0 warnings / 0 errors。
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass，fog explored-memory / minimap projection source evidence 通过。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation`
  Result: pass，ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass，VerifyAll 23/23 通过，包含 PerfSmoke、DesktopHudQa、ActiveBattlePerfQa 和 Godot headless QA。

Reviewer result:
- pass。小地图同步行为保持原有数据来源和绘制 contract，只改变列表填充方式与文件归属。

Status:
- pass

Residual risks:
- ReviewGate 仍是文本检查，不能证明运行时分配为零；风险由后续 PerfSmoke/ActiveBattlePerfQa 继续覆盖。

TODO update:
- TODO.md 的 M9 per-tick allocation paydown 条目已记录 #117 follow-up。
