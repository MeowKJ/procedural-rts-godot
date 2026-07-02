# Review Record - M9 BattleRoot Legacy Selection Buffers

Step:
M9 BattleRoot legacy selection buffer reuse (#121) - m9-battleroot-legacy-selection-buffers

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
- Non-goals: HUD icon summary grouping, `GameState` selection command semantics, runtime `UnitBattlefield` selection semantics, and visual/layout changes.

Implementation summary:
- Added `_selectedLegacyUnitBuffer` and `_selectedLegacyBuildingBuffer` on `BattleRoot`.
- Replaced legacy selection HUD `_state.SelectedUnits().ToList()` and `_state.SelectedBuildings().ToList()` materialization with explicit buffer collectors.
- Replaced legacy multi-selection `Count` / `Sum` / `Average` LINQ stats with explicit unit/building loops.
- Extended `BattleRootHudAllocationReviewGate` so `ReviewGate presentation` locks the reusable legacy selection buffer and no-LINQ stats contract.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj`
  Result: pass
  Evidence: Selection stress passed 100 cases.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation`
  Result: pass
  Evidence: ReviewGate presentation passed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Allocation-only fallback HUD data path change; no drawing, layout, text, or style changed.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: The legacy path is not the default runtime path while `UseUnitDesignRuntime` is true, so coverage is primarily through static ReviewGate and selection stress behavior.

TODO update:
- Items marked done: none.
- Items left open: HUD icon summary grouping remains tracked by #122.
