# Review Record - M9 Legacy Selected Stance Command Buffer

Step: #184 `[M9] Reuse legacy selected stance command buffer`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / GameStateAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/game-state/GameState.Commands.cs`, `tools/ReviewGateRuntime/GameStateAllocationReviewGate.cs`.
- Non-goals: 不改变 selection rules、stance semantics、move mode mapping、attack target clearing、threat memory、或 command pulse behavior。

Implementation summary:
- `SetSelectedStance(...)` now fills `_legacySelectedCommandUnits` via `CollectSelectedCommandUnits(...)`.
- Stance updates iterate the reusable selected-command-unit buffer instead of directly enumerating `SelectedUnits()`.
- `ReviewGate regression` locks this command path against returning to the selected-unit iterator.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass，legacy move / attack / stance command player loop coverage preserved。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，0 errors / 0 warnings；validation tool source budget lock updated to the exact current summary。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-legacy-selected-stance-command-buffer`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.
- Residual risks: `SelectedUnits()` remains as a public compatibility API for non-hot/readout callers；this slice only removes the runtime stance command use.

TODO update:
- Items marked done: none，#10 parent remains open。
- Items left open: broader profiler-guided selected readout cleanup remains future work。
