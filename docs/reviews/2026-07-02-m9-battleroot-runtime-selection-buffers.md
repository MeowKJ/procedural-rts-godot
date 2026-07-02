# Review Record - M9 BattleRoot Runtime Selection Buffers

Step:
M9 BattleRoot runtime selection buffer reuse (#120) - m9-battleroot-runtime-selection-buffers

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `scripts/BattleRoot.HudSync.cs`, `tools/ReviewGateRuntime/BattleRootHudAllocationReviewGate.cs`.
- Non-goals: legacy `GameState` selection fallback, HUD icon summary grouping, selection semantics, combat/economy behavior, and HUD visual layout.

Implementation summary:
- Added `_selectedUnitInstanceBuffer` on `BattleRoot`.
- Replaced the runtime selection HUD `_unitBattlefield.SelectedUnits(PlayerSlotId.One).ToList()` materialization with `CollectSelectedUnitInstances(...)`.
- Replaced runtime multi-selection `Count` / `Sum` / `Average` LINQ stats with one explicit loop over the reusable selected-unit buffer.
- Extended `BattleRootHudAllocationReviewGate` so `ReviewGate presentation` locks the reusable runtime selection buffer and no-LINQ stats contract.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj`
  Result: pass
  Evidence: Selection stress passed 100 cases. A transient MSBuild copy retry warning occurred because this run overlapped the main build; the command still exited 0.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation`
  Result: pass
  Evidence: ReviewGate presentation passed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Allocation-only selection HUD data path change; no drawing, layout, text, or style changed.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: ReviewGate is text-based and does not directly measure allocations; final batch verification should include full ReviewGate and VerifyAll before closing #120.

TODO update:
- Items marked done: none.
- Items left open: legacy selection buffers and HUD icon summary buffers remain tracked by #121 and #122.
