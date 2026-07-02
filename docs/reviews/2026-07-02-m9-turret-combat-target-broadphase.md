# Review Record - M9 turret combat target broadphase

Step:
M9 turret combat target broadphase (#111)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex / ReviewGate simhot

Integrator AI:
Remote Linux Codex

Scope:
- 文件/系统：`scripts/core/sim/systems/TurretCombatSystem.cs`, `tools/ReviewGateRuntime/TurretCombatAllocationReviewGate.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- 目标：让 `TurretCombatSystem` 每 tick 为 active turret 建一次 `SpatialGrid<EntityInstance>` target broadphase，并让 auto target selection 查询 grid neighbors。
- 非目标：不合并 `CombatSystem` / `TurretCombatSystem`，不修改 damage/range/priority/tie-break/balance，不改 projectile、VFX、HUD 或 AI 策略。

Implementation summary:
- `TurretCombatSystem` 新增复用的 `_targetGrid` 和 `_targetGridMaxTargetRadius`。
- `BuildTargetGrid(...)` 先扫描 active turret 的最大 range；没有 active turret 时只 reset 空 grid，不构造 target index。
- 自动目标选择改为 `_targetGrid.Neighbors(...)`，并保留 manual target 优先、targetable check、range + collision radius check、priority / distance / EntityId tie-break。
- `ReviewGate simhot` 新增 `TurretCombatAllocationReviewGate`，锁定 turret auto-target 不回退到 `world.OrderedEntities` 全量候选扫描。
- 同步 `TODO.md` 和 file-size discipline review record 的 validation tool suite exact budget evidence。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including turret states, weapon hit rules, shared threat propagation, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj --no-restore`
  Result: pass
  Evidence: 400-unit worst average 10.584ms, alloc/tick 192620 bytes, under the 16.667ms budget.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: File-size gate completed with 0 errors and 0 warnings after exact budget evidence was updated to 142 C# source files / 19144 tool lines.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore --max-warnings=0`
  Result: pass
  Evidence: Full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed all 23 steps, including SimReplay, CombatBehavior, ReviewGate, PerfSmoke, counter QA, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: runtime target-search performance refactor only; no rendering, UI layout, palette, or VFX behavior changed.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: `TurretCombatSystem` remains a transitional separate combat system; #9 still tracks full convergence into one data-driven weapon engagement system. PerfSmoke allocation pressure remains tracked by broad #10.

TODO update:
- Items marked done: none; #111 is a child slice under broad M9 allocation/convergence work.
- Items left open: #9 combat system convergence and #10 per-tick allocation paydown.
- Reason: this slice only replaces turret auto-target full scans with a broadphase index and adds regression coverage.
