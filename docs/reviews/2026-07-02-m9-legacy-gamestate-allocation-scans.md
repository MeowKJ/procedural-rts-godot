# Review Record - M9 Legacy GameState Allocation Scans

Step:
M9 legacy GameState allocation scan cleanup (#132, #133, #134)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- 文件/目录：`scripts/core/GameState.cs`、`scripts/core/game-state/GameState.EconomyBuild.cs`、`scripts/core/game-state/GameState.RelationsPickingFog.cs`、`scripts/core/game-state/GameState.RemovalDamageUtilities.cs`、`tools/ReviewGateDomains/RegressionReviewGate.cs`。
- 非目标：不修改 `UnitBattlefield` runtime path，不改 production balance、build radius、pick radius、selection UX、fog rules、HUD layout 或父项 #10 的完成状态。

Implementation summary:
- `GameState.ProductionOptionStates(...)` 现在通过 `_legacyProductionSpecBuffer` 收集并排序 production specs，再用显式 producer/queue scan 计算 `QueuedCount` 和 `ActiveProgress`，移除 per-option producer `ToList()` 和 LINQ metric chains。
- `GameState.ValidateBuildingPlacement(kind, owner, ...)` 现在通过 `_legacyPlacementBuildAnchors` 复用 build-radius anchor storage，移除 `BuildingBuildAnchors(...)` 的 allocating snapshot 返回。
- `PickResourceField`、`PickUnit`、`PickBuilding` 改为显式 best-candidate scan，并用 `PickScore(distance, radius)` 保持原来的 normalized distance priority。
- `IsProductionBuilding(...)` 改为显式 playable spec scan，避免引用已删除的 `ProductionSpecsFor(...)` helper。
- `ReviewGate regression` 增加 legacy `GameState` allocation evidence，锁定这些 no-LINQ/no-snapshot contract。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass
  Evidence: SelectionStress passed 100 cases, covering pick/selection stress behavior.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed build radius, cat ready-ticket placement, harvest/bank, T1-T3 production, rally, selection, shared corridor, move/attack/stance, victory, and defeat.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression`
  Result: pass
  Evidence: ReviewGate regression passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: ReviewGate filesize passed with 0 errors and 0 warnings after syncing validation-suite budget evidence.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-legacy-gamestate-allocation-scans`
  Result: pass
  Evidence: Required review record gate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed all deterministic scenarios, including production, construction, combat, movement, pathfinding, and outcome hashes.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23, including build, SimReplay, CombatBehavior, SimulationSmoke, FogOfWarQa, SelectionStress, AI QA, content QA, PlayerLoopQa, DesktopHudQa, ReviewGate, PerfSmoke, BalanceReport, CounterReadabilityQa, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: 本 slice 只改 legacy data/read query allocation paths，没有渲染或 layout 变更。

Reviewer result:
- Status: pass
- Required fixes: 初次 build 暴露 `GameState.RemovalDamageUtilities.cs` 仍引用已删除的 `ProductionSpecsFor(...)` helper；已改为显式 playable spec scan 后 build 通过。
- Residual risks: `GameState` 仍有其他 legacy LINQ read paths，例如 production lane/build option snapshots 和 fog source aggregation；本批只关闭 #132、#133、#134 的 bounded scope。

TODO update:
- Items marked done: none。
- Items left open: 父项 #10 M9 per-tick allocation paydown 继续打开，剩余 allocation debt 继续按 profiler-guided child slice 拆分。
- Reason: 本记录只证明 legacy `GameState` production option、placement anchor、pick query 三个小 slice。
