# Review Record - M9 GameState Harvest Rally Command Buffers

Step: #151 `[M9] Reuse GameState harvest rally command buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / GameStateAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `scripts/core/game-state/GameState.Commands.cs`, `tools/ReviewGateRuntime/GameStateAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不修改 harvest/refinery 选择、rally point、status 文案、production 建筑判定或 UnitBattlefield command bridge。

Implementation summary:
- `GameState` 新增 `_legacySelectedHarvesters`、`_legacySelectedBuildings`、`_legacySelectedProducers`。
- `CommandHarvestSelected(...)` 改为显式扫描 selected player harvesters 并复用 harvester buffer。
- `CommandSetSelectedBuildingRallyPoint(...)` 改为复用 selected-building / selected-producer buffers，移除 selected building list 和 producer list materialization。
- `ReviewGate regression` 锁定 legacy harvest/rally command no-list contract。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: move/attack command internals still retain separate allocation debt and remain outside this slice.

TODO update:
- Items marked done: none，#10 parent 仍保持打开。
- Items left open: legacy GameState move/attack command allocation cleanup remains future work.
