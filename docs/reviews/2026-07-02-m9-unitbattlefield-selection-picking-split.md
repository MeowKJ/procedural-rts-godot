# Review Record - M9 UnitBattlefield selection/picking split

Step:
M9 UnitBattlefield selection/picking split (#116)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex / ReviewGate filesize

Integrator AI:
Remote Linux Codex

Scope:
- 文件/系统: `scripts/core/units/runtime/battlefield/UnitBattlefield.SelectionPicking.cs`, `scripts/core/units/runtime/battlefield/selection/UnitBattlefield.BuildingSelectionProjections.cs`, `scripts/core/units/runtime/battlefield/selection/UnitBattlefield.SelectionCommands.cs`, `TODO.md`.
- 目标: 将 selected-unit API / picking wrappers、building selection projections、selection command helpers 分开，降低宽 partial 的维护风险。
- 非目标: 不修改 selection、picking、building hover、minimap、power status、HUD 或视觉语义。

实现摘要:
- `UnitBattlefield.SelectionPicking.cs` 现在只保留 selected-unit API 和 pick wrappers，降到 78 行。
- building hover / hit pulse / minimap / power status projections 移到 `selection/UnitBattlefield.BuildingSelectionProjections.cs`。
- select single/same/building target 和 selection buffer helpers 移到 `selection/UnitBattlefield.SelectionCommands.cs`。
- 新增 selection partials 放入 `battlefield/selection/` domain directory，避免扩大 `battlefield/` 根目录 source-count warning。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass
  Evidence: SelectionStress passed 100 cases.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: ReviewGate filesize completed with 0 errors and 0 warnings after selection domain-directory placement.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore --max-warnings=0`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed all 23 steps, including SimReplay, SelectionStress, PlayerLoopQa, full ReviewGate, PerfSmoke, and Godot headless QA.

人工/视觉验证:
- Result: not applicable
- Evidence: 纯 partial move，不改变 UI、rendering、palette、layout 或 VFX。

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: 该 slice 没有清理 `SelectedUnits(...)` enumerable allocation；如 profiling 需要，应拆成单独 #10 子任务。

TODO update:
- Items marked done: none; #116 是既有 UnitBattlefield god-file split 的 follow-up。
- Items left open: M9 combat convergence 和 #10 allocation paydown。
