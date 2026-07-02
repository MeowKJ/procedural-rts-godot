# Review Record - M9 BattleRoot Selection Icon Summary Buffers

Step:
M9 BattleRoot selection icon summary buffer reuse (#122) - m9-battleroot-selection-icon-summary-buffers

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/BattleRoot.HudSync.cs`, `scripts/BattleRoot.Selection.cs`, `scripts/battle-root/BattleRoot.SelectionIconSummary.cs`, `tools/ReviewGateRuntime/BattleRootHudAllocationReviewGate.cs`.
- Non-goals: HUD icon visual design, `HudLayer` draw behavior, selection semantics, `BattleRoot.Sandbox` debug-only LINQ, and non-selection HUD materialization.

Implementation summary:
- Added `scripts/battle-root/BattleRoot.SelectionIconSummary.cs` to keep icon summary aggregation out of the already-large HUD sync file and out of the root `scripts/` split family.
- Replaced runtime unit, runtime building, and legacy unit/building icon summary `GroupBy` / `OrderBy` / `Select` / `Distinct` / `ToList` chains with reusable aggregation entries and explicit loops.
- Added double-buffered `HudLayer.SelectionIconItem` result storage so `HudLayer` can retain the latest `IReadOnlyList` without it being cleared by the next refresh.
- Extended `BattleRootHudAllocationReviewGate` to lock the reusable icon summary entry buffer, double-buffered result storage, and no-LINQ icon summary contract.

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
- Command: `wc -l scripts/BattleRoot.HudSync.cs scripts/BattleRoot.Selection.cs scripts/battle-root/BattleRoot.SelectionIconSummary.cs tools/ReviewGateRuntime/BattleRootHudAllocationReviewGate.cs`
  Result: pass
  Evidence: touched C# files are 299, 263, 222, and 72 lines respectively.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Icon summary data construction changed, but icon glyphs, labels, colors, sorting policy, and HUD drawing code were preserved.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: The gate proves source structure rather than measuring allocations directly; final batch verification should include full ReviewGate and VerifyAll before closing #122.

TODO update:
- Items marked done: none.
- Items left open: broader M9 allocation paydown remains open for future profiler-guided slices.
