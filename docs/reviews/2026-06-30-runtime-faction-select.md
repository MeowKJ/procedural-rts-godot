# Review Record - Runtime Faction Select

Step: Make UnitDesign runtime starting units respect skirmish faction selection.
Milestone: Playable 1v1 skirmish faction completeness.
Owner AI: Main thread.
Reviewer AI: ReviewGate runtimefactionselect plus Godot SkirmishFlowQa.
Integrator AI: Main thread.

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `scripts/SkirmishFlowQaRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-runtime-faction-select.md`.
- Non-goals: no new faction content, no third-faction unlock, no roster rebalance, no production UI rewrite.

Implementation summary:
- Changed `BattleRoot.ConfigureUnitBattlefield` so non-sandbox starting UnitDesigns come from `_state.Options.PlayerFaction` and `_state.Options.AiFaction`.
- Added `BattleRoot.DebugUnitBattlefieldDesignIds(PlayerSlotId)` as a narrow read-only QA seam for runtime UnitDesign loadouts.
- Extended `SkirmishFlowQa` so its Cat-player/Dog-AI menu test verifies both legacy `GameState` loadouts and the live `UnitBattlefield` UnitDesign starting ids.
- Added `ReviewGate runtimefactionselect` to reject hard-coded Dog-player/Cat-AI runtime start regressions.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- runtimefactionselect`
  Result: pass.
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.
- Command: `Godot_v4.7-stable_mono_win64_console.exe --headless --path . --scene res://scenes/SkirmishFlowQa.tscn`
  Result: pass.
  Evidence: `Skirmish flow QA passed: main menu setup launched Battle with selected faction, seed, credits, and difficulty.`

Manual/visual gates:
- Final faction readability still needs visual QA once the full Dog/Cat art pass is closed.

Reviewer result:
- Status: pass.
- Required fixes: none known.
- Residual risks: the broad Dog/Cat fully playable TODO remains open until player loop, AI loop, counters, performance, and readability are proven together.

TODO update:
- Items marked done: none.
- Items left open: broad faction vertical-slice item remains open.
