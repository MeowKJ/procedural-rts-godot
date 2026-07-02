# Review Record - M9 UnitBattlefield Runtime Allocation Buffers

Step:
M9 UnitBattlefield runtime allocation buffer reuse (#82, #83, #84) - m9-unitbattlefield-runtime-allocation-buffers

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/UnitBattlefieldProductionQueueSnapshot.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.HarvestRepair.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.CommandApplyRemoval.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.EntityWorldSystems.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.ProductionSync.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `tools/ReviewGateDomains/UnitBattlefieldAllocationReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- Non-goals: 不改变 harvest/repair 玩法、死亡事件 API、胜负判定、ProductionSystem 内部逻辑、生产平衡、HUD 或 broader placement allocation work。

Implementation summary:
- #82: 在 `UnitBattlefield` 增加 `_unitCommandIdBuffer`、`_unitCommandBuffer`、`_unitCommandEntityBuffer`，让 harvest/repair command bridge 用显式稳定循环替代 `ToList` / `ToHashSet` / `OrderBy` / `Select` 临时集合。
- #82: 将 refinery lookup / dock claim cleanup 改为扫描 EntityWorld building identity，避免 harvest validation 继续通过 `BuildingTargetIds()` 分配快照。
- #83: 增加 `_unitDeathBuffer`、`_buildingDeathBuffer`、`_deadBuildingIdBuffer`、`_removedUnitIdBuffer`、`_removedBuildingIdBuffer`，复用 dead unit/building removal storage，并保留同步事件读取语义。
- #84: 将 `UpdateProductionQueues(...)` 移到 `UnitBattlefield.ProductionSync.cs`，复用 active producer、known entity id、queued-before snapshot、new unit entity buffers，并用 `UnitBattlefieldProductionQueueSnapshot` 替代 anonymous LINQ snapshot。
- 增加 `UnitBattlefieldAllocationReviewGate` 并接入 regression ReviewGate，锁定三类 buffer evidence 与 no-materialized-LINQ 回归规则。
- 更新 file-size source-budget evidence，因为新增了一个 ReviewGateDomains source file。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj`
  Result: pass
  Evidence: Combat behavior passed, including runtime units, production/economy, enemy AI, presentation descriptors, and outcomes.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj`
  Result: pass
  Evidence: PlayerLoopQa passed build radius, harvest/bank, T1-T3 production, rally, selection, victory, and defeat.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj simhot`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj regression`
  Result: pass
  Evidence: ReviewGate regression passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings after file-size evidence update.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj`
  Result: pass
  Evidence: VerifyAll passed 23/23, including SimReplay, CombatBehavior, PlayerLoopQa, ReviewGate, PerfSmoke, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Runtime allocation cleanup only; no rendering, art, camera, or HUD layout changed.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: `UnitsRemoved` / `BuildingsRemoved` now receive reused list instances and current subscribers consume them synchronously; callers must not retain those list references across future battlefield updates. `CommandApplyRemoval.cs` remains under 400 lines but is close to the watch threshold at 387 lines. Final VerifyAll passed, but the Godot active battle perf step still emitted a non-failing `2 ObjectDB instances were leaked at exit` warning.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open for broader profiler-guided cleanup.
- Items left open: remaining UnitBattlefield presentation/compat bridge allocations, `BuildingTargetIds()` allocation in untouched callers, and profiler-guided GC cleanup beyond these child slices.
- Reason: This closes three concrete GitHub child issues without closing the broad M9 allocation parent.
