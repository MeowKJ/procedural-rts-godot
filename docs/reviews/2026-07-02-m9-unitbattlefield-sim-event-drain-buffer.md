# Review Record - M9 UnitBattlefield sim-event drain buffer

Step:
M9 UnitBattlefield sim-event drain buffer (#112)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex / ReviewGate simhot

Integrator AI:
Remote Linux Codex

Scope:
- 文件/系统：`scripts/core/units/runtime/UnitBattlefield.cs`, `UnitBattlefield.BuildingTargetCombatBridge.cs`, `UnitBattlefield.TurretCombat.cs`, `UnitBattlefield.ConstructionTickets.cs`, `tools/ReviewGateRuntime/UnitBattlefieldRuntimeAllocationReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- 目标：让 UnitBattlefield combat/construction bridge 复用 sim-event drain buffer，移除 `_entityWorld.Events.Drain()` snapshot allocation。
- 非目标：不修改 event payload/order、combat damage、construction validation、public `SimEventSink.Drain()` API、工具便利 `Drain()` 调用，且不关闭 broad #10。

Implementation summary:
- 在 `UnitBattlefield` 增加 `_simEventDrainBuffer`。
- `UpdateBuildingTargetCombatFromEntityWorld(...)` 和 `UpdateBuildingCombatFromEntityWorld(...)` 改用 `_entityWorld.Events.DrainInto(_simEventDrainBuffer)`，应用事件后清空 buffer。
- `DrainConstructionRejection(...)` 改用同一 buffer 和显式反向扫描，保持“最后一个匹配 tick/owner/spec rejection”语义。
- `ReviewGate simhot` 的 `UnitBattlefieldRuntimeAllocationReviewGate` 现在锁定 bridge drains 不回退到 `_entityWorld.Events.Drain()` / `OfType(...).LastOrDefault(...)`。
- 同步 `TODO.md` 和 file-size discipline review record 的 validation tool suite exact budget evidence。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed build radius, construction ticket placement, harvest/bank, T1-T3 production, rally, commands, victory, and defeat.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including turret states, construction/economy/AI paths, and outcomes.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: File-size gate completed with 0 errors and 0 warnings after exact budget evidence was updated to 142 C# source files / 19162 tool lines.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore --max-warnings=0`
  Result: pass
  Evidence: Full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed all 23 steps, including SimReplay, PlayerLoopQa, CombatBehavior, ReviewGate, PerfSmoke, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: runtime bridge allocation refactor only; no UI, rendering, palette, or VFX behavior changed.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: tool/test convenience calls to `SimEventSink.Drain()` remain intentionally supported; broader #10 allocation work still tracks other immutable array and projection allocations.

TODO update:
- Items marked done: none; #112 is a child slice under broad M9 allocation paydown.
- Items left open: #10 per-tick allocation paydown and profiler-guided GC cleanup.
- Reason: this slice only removes UnitBattlefield bridge event snapshot drains and locks that contract in `ReviewGate simhot`.
