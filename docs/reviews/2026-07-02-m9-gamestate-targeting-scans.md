# Review Record - M9 GameState Targeting Scans

Step: m9-gamestate-targeting-scans (#135)
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate simhot / self-review
Integrator AI: Remote Linux Codex

Scope:
- 将 legacy `GameState` 单位/建筑自动索敌从 LINQ filter/order chains 改为 `BestUnitTargetForWeapon(...)` 显式 best-candidate scan。
- 保留 `TargetScore` 最大值选择与 LINQ `OrderByDescending(...).FirstOrDefault()` 的稳定 first-tie 行为。
- 新增 `GameState.TargetScans.cs`，避免 `GameState.TargetingThreat.cs` 超过 400 行 warning 线。
- 扩展 `GameStateAllocationReviewGate`，锁住旧 `Where/OrderByDescending` 自动索敌链不回归。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: PASS，0 warnings / 0 errors。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: PASS，Errors: 0，Warnings: 0。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: PASS，weapon hit rules、turret states、shared threat、economy、enemy AI、outcomes 全部通过。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: PASS，23/23 steps 全部通过，包含 full ReviewGate、PerfSmoke 与 Godot headless QA。

Reviewer result:
- pass

Status:
- pass

Residual risks:
- 这是 legacy `GameState` 路径的 allocation cleanup，不改变 EntityWorld authoritative combat pipeline。
- ReviewGate 是 source-string guard，不是语义 analyzer；行为一致性依赖 CombatBehavior 与 VerifyAll 覆盖。

TODO update:
- M9 per-tick allocation paydown 保持打开；本 slice 作为 #135 follow-up 记录到 TODO 进度。
