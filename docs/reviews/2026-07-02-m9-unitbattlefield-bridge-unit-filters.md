# Review Record - M9 UnitBattlefield bridge unit filters

Step:
M9 UnitBattlefield bridge unit filters (#113)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex / ReviewGate simhot

Integrator AI:
Remote Linux Codex

Scope:
- 文件/系统：`scripts/core/units/runtime/battlefield/UnitBattlefield.BuildingLifecycle.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.CommandApplyRemoval.cs`, `tools/ReviewGateRuntime/UnitBattlefieldRuntimeAllocationReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- 目标：替换 UnitBattlefield runtime bridge 中剩余的 `Units.Where(...)` filters，让建筑目标清理和选择命令同步路径使用显式循环。
- 非目标：不修改 selection command payload、building attack target clearing 语义、entity/component model、combat balance、UI、VFX 或 broad #10。

Implementation summary:
- `RemoveBuildingTarget(...)` 改为显式扫描 `Units`，用 early-continue 保持只清理 matching building target 的语义。
- `ApplySelectionCommandStateToUnits(...)` 先缓存 issuer player slot，再显式扫描 `Units`，避免对每个 unit 反复执行 command issuer 转换和 LINQ filter。
- `ReviewGate simhot` 的 `UnitBattlefieldRuntimeAllocationReviewGate` 增加 bridge filter regression guard，禁止这两个 runtime bridge 路径回退到 `Units.Where(...)`。
- 同步 `TODO.md` 和 file-size discipline review record 的 validation tool suite exact budget evidence 到 142 C# source files / 19171 total lines across 55 suites。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass
  Evidence: SelectionStress passed 100 cases.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including turret states, shared threat propagation, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: File-size gate completed with 0 errors and 0 warnings after exact budget evidence was updated to 142 C# source files / 19171 tool lines.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore --max-warnings=0`
  Result: pass
  Evidence: Full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed all 23 steps, including SimReplay, SelectionStress, CombatBehavior, ReviewGate, PerfSmoke, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: runtime bridge allocation cleanup only; no UI, rendering, palette, layout, or VFX behavior changed.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: 这次只替换两个小型 UnitBattlefield bridge filters；#10 仍继续跟踪其他 runtime allocation / immutable array paydown。

TODO update:
- Items marked done: none; #113 is a child slice under broad M9 allocation paydown.
- Items left open: #10 per-tick allocation paydown and profiler-guided GC cleanup.
- Reason: this slice only removes remaining small UnitBattlefield runtime bridge LINQ filters and locks the contract in `ReviewGate simhot`.
