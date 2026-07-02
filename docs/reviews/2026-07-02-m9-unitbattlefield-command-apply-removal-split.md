# Review Record - M9 UnitBattlefield command apply/removal split

Step:
M9 UnitBattlefield command apply/removal split (#114)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex / ReviewGate filesize

Integrator AI:
Remote Linux Codex

Scope:
- 文件/系统: `scripts/core/units/runtime/battlefield/UnitBattlefield.CommandApplyRemoval.cs`, `scripts/core/units/runtime/battlefield/command/UnitBattlefield.EntityIdLookup.cs`, `scripts/core/units/runtime/battlefield/command/UnitBattlefield.DamageRemoval.cs`, `TODO.md`.
- 目标: 按职责拆分 command state apply、entity id lookup、damage/removal/outcome helpers，降低 near-400 partial 的维护风险。
- 非目标: 不修改 selection、combat、harvest、building removal、outcome 或 runtime allocation 语义。

实现摘要:
- `UnitBattlefield.CommandApplyRemoval.cs` 只保留 selection/entity command state application，降到 87 行。
- 旧 entity/target/resource lookup 和 harvester stop helpers 移到 `command/UnitBattlefield.EntityIdLookup.cs`，文件名避免新增 Legacy/Bridge debt。
- damage、dead unit/building removal、outcome update helpers 移到 `command/UnitBattlefield.DamageRemoval.cs`。
- 新增文件放入 `battlefield/command/` domain directory，避免扩大 `battlefield/` 根目录 source-count warning。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: ReviewGate filesize completed with 0 errors and 0 warnings after domain-directory placement.
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
- Residual risks: 该 slice 只降低文件治理风险；`UnitBattlefield.CommandBridge.cs` 仍是当前 UnitBattlefield companion 最大文件，后续可单独拆分。

TODO update:
- Items marked done: none; #114 是既有 UnitBattlefield god-file split 的 follow-up。
- Items left open: M9 combat convergence 和 #10 allocation paydown。
