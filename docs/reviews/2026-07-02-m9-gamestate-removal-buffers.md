# Review Record - M9 GameState Removal Buffers

Step: m9-gamestate-removal-buffers (#136)
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate simhot / self-review
Integrator AI: Remote Linux Codex

Scope:
- 将 legacy `GameState.RemoveDeadUnits()` / `RemoveDeadBuildings()` 从死亡对象/id LINQ materialization 改为复用 `_legacyUnitDeathBuffer`、removed-id buffers 与 removed-building snapshot buffer。
- 保留 `UnitsRemoved`、`BuildingsRemoved`、projectile/beam cleanup、attack target clearing 与 HQ 胜负判定顺序。
- 将 `UpdateOutcomeAfterRemovedBuildings(...)` 的 `Any(...)` 改为显式循环，避免死亡清理后续检查分配迭代器。
- 扩展 `GameStateAllocationReviewGate`，锁住旧死亡清理 `ToList` / projection chains 不回归。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: PASS，0 warnings / 0 errors。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: PASS，Errors: 0，Warnings: 0。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: PASS，包含 combat/outcome/death cleanup 相关行为覆盖。
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: PASS，victory and defeat 覆盖通过。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: PASS，23/23 steps 全部通过，包含 full ReviewGate、PerfSmoke 与 Godot headless QA。

Reviewer result:
- pass

Status:
- pass

Residual risks:
- 事件仍同步接收 reusable buffer；这与 `UnitBattlefield` 现有死亡事件模式一致，但不适合异步保存该 list 引用。
- 本 slice 未处理同文件 `UnitObstacles()` 的 spawn obstacle list allocation，保留给后续 M9 allocation slice。

TODO update:
- M9 per-tick allocation paydown 保持打开；本 slice 作为 #136 follow-up 记录到 TODO 进度。
