# Review Record - Pause sim clock

Step: Prove pause halts the simulation clock and resumes cleanly through the real pause menu path.
Milestone: Runtime control and simulation authority.
Owner AI: Codex.
Reviewer AI: Codex self-review with static ReviewGate coverage and Godot runtime QA.
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `scripts/PauseQaRoot.cs`, `scenes/PauseQa.tscn`, `scripts/ui/PauseMenuLayer.cs`, `tools/ReviewGate/Program.cs`, `tools/VerifyAll/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-pause-sim-clock.md`.
- Non-goals: no redesign of pause menu visuals, no save/load pause state, no deterministic replay pause command format.

Implementation summary:
- Exposed a read-only `BattleRoot.DebugSimClockTick` for runtime QA without giving the QA scene write access to the sim clock.
- Added `PauseQa.tscn` and `PauseQaRoot.cs` to instantiate the real battle scene, pause through `PauseMenuLayer.SetPaused(true)`, observe the tick over paused frames, resume through `SetPaused(false)`, and require the tick to advance afterward.
- Kept the QA runner processing during pause while forcing the battle scene to remain pausable, so the test does not accidentally inherit the runner's always-processing mode.
- The QA scene now frees the instantiated battle and waits through cleanup frames before quitting, avoiding Godot Mono unsafe-reference failures after a logical pass.
- Added `ReviewGate pause` coverage and included the pause QA scene in `VerifyAll`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj pause --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for pause coverage.
- Command: `Godot_v4.7-stable_mono_win64_console.exe --headless --path . --scene res://scenes/PauseQa.tscn`
  Result: pass
  Evidence: Pause QA printed that sim tick held at 4 while paused and resumed at 5.

Manual/visual gates:
- Check: pause menu visual interaction
  Result: not run
  Evidence: this slice validates pause simulation semantics through runtime QA; menu visuals were not changed.

Reviewer result:
- Status: pass
- Required fixes: the battle scene was explicitly set to `ProcessModeEnum.Pausable` inside the always-processing QA runner to avoid false positives or false negatives from inherited process mode; after VerifyAll exposed a post-pass Godot Mono cleanup crash, the QA now `QueueFree`s the instantiated battle before quitting.
- Residual risks: future pause hotkeys, replay commands, or modal UI flows still need their own coverage if they bypass `PauseMenuLayer.SetPaused`.

TODO update:
- Items marked done: `Pause that truly halts the sim clock (no ticks advance) and resumes cleanly`.
- Items left open: broader timing, replay, and UI polish TODOs.
- Reason: the real pause menu path now has static coverage and a Godot runtime QA scene proving no sim tick advances while paused, then resumes cleanly.
