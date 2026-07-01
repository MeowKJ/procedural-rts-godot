# Review Record - Player Loop QA

Step: Prove the playable player loop in one deterministic headless QA.
Milestone: Playable 1v1 skirmish vertical slice.
Owner AI: Main thread.
Reviewer AI: ReviewGate playerloopqa plus PlayerLoopQa.
Integrator AI: Main thread.

Scope:
- Files/folders: `tools/PlayerLoopQa/PlayerLoopQa.csproj`, `tools/PlayerLoopQa/Program.cs`, `tools/VerifyAll/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-player-loop-qa.md`.
- Non-goals: no AI planner changes, no balance tuning, no visual readability pass, no new UI layout.

Implementation summary:
- Added `PlayerLoopQa`, a focused deterministic QA tool for the vertical-slice player loop.
- The tool proves build-radius placement through `GameState.PlaceBuildingWithinBuildRadius`.
- It proves harvest commands bank credits through `UnitBattlefield.CommandHarvestSelected`.
- It proves producer rally plus concrete T1/T2/T3 UnitDesign production through `SetSelectedBuildingRallyPoints` and `EnqueueProductionDesign`.
- It proves group selection, move, attack, and stance commands through the live `UnitBattlefield` command bridge.
- It proves victory and defeat outcomes by destroying enemy and player HQ targets.
- Wired the tool into `VerifyAll` and locked it with `ReviewGate playerloopqa`.

Automated gates:
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass.
  Evidence: `PlayerLoopQa PASSED: build radius, harvest/bank, T1-T3 production, rally, selection, move/attack/stance, victory and defeat.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- playerloopqa`
  Result: pass.
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass.
  Evidence: `VerifyAll PASSED` with 19/19 steps, including `player-loop-qa`, `review-gate`, `perf-smoke`, and Godot headless scene checks.

Manual/visual gates:
- No new visual surface in this slice; HUD readability remains covered by separate UI/readability TODOs.

Reviewer result:
- Status: pass.
- Required fixes: none known.
- Residual risks: this closes the player capability loop, but AI opponent behavior, counter readability, performance-at-scenario, and Soft Old City readability remain separate open vertical-slice gates.

TODO update:
- Items marked done: `Player can: build base in build radius, train T1-T3 from producers, harvest and bank credits, set rally, group-select, move/attack/stance, win by destroying enemy HQ / lose if own HQ falls.`
- Items left open: AI opponent, counters, performance, and readability vertical-slice gates.
