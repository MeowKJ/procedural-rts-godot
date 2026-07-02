# Review Record - M9 GameState Selection Buffers

Step: #152 `[M9] Reuse GameState selection buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / GameStateAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `scripts/core/game-state/GameState.Selection.cs`, `tools/ReviewGateRuntime/GameStateAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 selection priority、additive selection、double-click same-unit 行为、pick math 或 UnitBattlefield selection bridge。

Implementation summary:
- `SelectedCount()` 和 `SelectSameUnitsAt(...)` 改为显式 selected-unit/building scan，避免 selected enumerable `Count()`。
- `SelectedUnitIds()` 复用 `_legacySelectedUnitIds`，`SelectUnitsByIds(...)` 复用 `_legacyRequestedSelectionIds`。
- `SelectRect(...)` 复用 harvester/combat selection buffers，并用 `NearestSelectionDistance(...)` 替代 center-distance `Min(...)`。
- `ReviewGate regression` 锁定 legacy selection count/id/rect no-LINQ materialization contract。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: `SelectedUnitIds()` now returns reusable internal storage; current repo callers do not hold this API result long-term, but future callers should treat it as an immediate snapshot view.

TODO update:
- Items marked done: none，#10 parent 仍保持打开。
- Items left open: broader legacy GameState command allocation cleanup remains future work.
