# Review Record - M9 UnitBattlefield runtime sync split

Step:
M9 UnitBattlefield runtime sync split (#115)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex / ReviewGate filesize

Integrator AI:
Remote Linux Codex

Scope:
- 文件/系统: `scripts/core/units/runtime/battlefield/UnitBattlefield.SyncRuntime.cs`, `scripts/core/units/runtime/battlefield/sync/UnitBattlefield.UnitEntityAdoption.cs`, `scripts/core/units/runtime/battlefield/sync/UnitBattlefield.ResourceFieldEntitySync.cs`, `scripts/core/units/runtime/battlefield/sync/UnitBattlefield.ResourceHarvesterSync.cs`, `TODO.md`.
- 目标: 拆分 runtime sync、unit entity adoption、resource field entity sync、harvester/dock sync，降低 near-400 partial 的维护风险。
- 非目标: 不修改 movement、separation、resource、dock、harvester、credit sync、AI、UI 或 balance 语义。

实现摘要:
- `UnitBattlefield.SyncRuntime.cs` 现在聚焦 unit/entity runtime motion sync，降到 163 行。
- `AdoptUnitEntity(...)` 移到 `sync/UnitBattlefield.UnitEntityAdoption.cs`。
- resource field entity sync 移到 `sync/UnitBattlefield.ResourceFieldEntitySync.cs`。
- harvester/resource/dock bridge sync 移到 `sync/UnitBattlefield.ResourceHarvesterSync.cs`。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed build radius, harvest/bank, T1-T3 production, rally, selection, move/attack/stance, victory and defeat.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: ReviewGate filesize completed with 0 errors and 0 warnings after sync domain-directory placement.
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
- Residual risks: 该 slice 没有优化 resource sync allocation；#10 仍跟踪 profiler-guided GC cleanup。

TODO update:
- Items marked done: none; #115 是既有 UnitBattlefield god-file split 的 follow-up。
- Items left open: M9 combat convergence 和 #10 allocation paydown。
