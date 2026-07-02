# Review Record - M9 SelectionController Command Buffers

Step: #150 `[M9] Reuse SelectionController legacy command buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate presentation / SelectionControllerAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/controllers/SelectionController.cs`, `scripts/controllers/SelectionController.Commands.cs`, `scripts/controllers/SelectionController.Utilities.cs`, `scripts/controllers/SelectionController.Preview.cs`, `tools/ReviewGateRuntime/SelectionControllerAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不修改 UnitBattlefield command path、selection/preview 视觉、热键、命令语义或音效。

Implementation summary:
- `SelectionController` 新增 `_legacySelectedUnitCommandBuffer`，right-click legacy fallback 先填充 reusable selected-unit buffer，再用 count 判定 attack/move。
- preview/harvest/building helper 改为显式 early-exit scan，移除 `State.SelectedUnits().Any()`、`State.SelectedBuildings().Any()` 和 runtime harvester LINQ `Any(...)`。
- `ReviewGate presentation` 新增 controller allocation evidence，防止 selected-unit list materialization 回归。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: legacy preview helpers now scan `State.Units` directly; this preserves the same selected-player-unit predicate but still leaves broader legacy command move/attack allocations for later slices.

TODO update:
- Items marked done: none，#10 parent 仍保持打开。
- Items left open: broader M9 allocation debt remains, including legacy move/attack command internals.
