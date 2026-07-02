# Review Record - M9 Legacy Command Allocation Buffers

Step: #153 `[M9] Reuse legacy move command formation buffers`; #154 `[M9] Reuse legacy attack slot buffers`; #155 `[M9] Reuse legacy manual attack refresh buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / CombatBehavior / PlayerLoopQa / SimReplay
Integrator AI: Remote Linux Codex

Scope:
- 复用 legacy `GameState` move/attack command 的 selected-unit scratch buffer。
- 将 `CommandMoveSelected(...)` 从 `SelectedUnits().ToList()`、formation `Select().ToList()`、destination `ToDictionary(...)`、movement-domain `GroupBy(...)`、moving-id `ToHashSet()`、corridor-member `Select().ToList()` 改为 caller-owned buffers。
- 将 `CommandAttackSelected(...)` 和 `CreateAttackSlots(...)` 从 selected attacker LINQ、occupied-slot `ToList()`、in-range filter、`OrderBy(...).ThenBy(...)` 改为显式扫描和复用的 slot/occupied buffers。
- 将 `RefreshManualAttackSlot(...)` 复用同一个 occupied-slot collector，移除 per-refresh `Where/Select/ToList()`。
- 新增 `GameState.CommandBuffers.cs`，避免继续膨胀 `GameState.Commands.cs`。
- 扩展 `GameStateAllocationReviewGate`，在 `ReviewGate regression` 下锁定 legacy command no-LINQ/no-snapshot contract。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，Errors: 0，Warnings: 0。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass，覆盖 shared corridor、move/attack/stance、victory/defeat。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，所有 deterministic replay 场景通过。
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj --no-restore -c Release`
  Result: pass，400u avg 2.927ms，p99 3.248ms，alloc/tick 192620。
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass，100 cases。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，Errors: 0，Warnings: 0。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=legacy-move-command-buffers`
  Result: pass，Errors: 0，Warnings: 0。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=legacy-attack-slot-buffers`
  Result: pass，Errors: 0，Warnings: 0。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=legacy-manual-attack-refresh-buffers`
  Result: pass，Errors: 0，Warnings: 0。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，Errors: 0，Warnings: 0。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass，23/23 steps。

Reviewer result:
- Status: pass
- Required fixes: none.

Status:
- pass

Residual risks:
- `PathfindingMath.FindSharedCorridor(...)` 内部仍有自己的 shared path、blocked set、terrain dictionary 分配；本 slice 只清理 legacy command caller 侧的 per-command collection churn。
- `AssignPath(...)` 仍有 `GlobalCorridor.Skip(1)` iterator debt，后续可按独立 child issue 处理。
- `PerfSmoke` allocation/tick 未下降，因为当前 smoke 主要覆盖 EntityWorld sim hot loop，不直接施压 legacy `GameState` UI command path。

TODO update:
- M9 per-tick allocation paydown parent 保持打开；#153/#154/#155 作为 legacy GameState command allocation follow-up 记录到 TODO。
