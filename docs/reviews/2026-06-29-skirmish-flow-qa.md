# Review Record - Skirmish flow QA

Step: Prove boot-to-battle skirmish setup through a Godot runtime QA scene.
Milestone: Playable 1v1 Skirmish - setup flow.
Owner AI: Codex.
Reviewer AI: Codex self-review (runtime QA plus ReviewGate provide durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/MainMenuRoot.cs`, `scripts/BattleRoot.cs`, `scripts/SkirmishFlowQaRoot.cs`, `scenes/SkirmishFlowQa.tscn`, `tools/VerifyAll/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-skirmish-flow-qa.md`.
- Non-goals: no full visual screenshot QA, no third-faction placeholder, no complete playable-slice claim, no campaign or multiplayer work.

Implementation summary:
- Added stable QA names to main-menu skirmish controls and the start button.
- Added a read-only `BattleRoot.State` accessor for runtime QA inspection.
- Added `SkirmishFlowQa.tscn` and `SkirmishFlowQaRoot`, which boots the real main menu, sets player faction, AI faction, difficulty, credits, and seed, presses the real start button, waits for `BattleRoot`, and validates the resulting `GameState`.
- Added `godot-skirmish-flow-qa` to `tools/VerifyAll`.
- Added `ReviewGate skirmishflow`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `Godot_v4.7-stable_mono_win64_console.exe --headless --path . --scene res://scenes/SkirmishFlowQa.tscn`
  Result: pass
  Evidence: printed `Skirmish flow QA passed: main menu setup launched Battle with selected faction, seed, credits, and difficulty.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj skirmishflow --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for skirmish flow.

Manual/visual gates:
- Check: visual QA
  Result: not run
  Evidence: this slice validates runtime scene flow and state transfer headlessly; visual menu polish remains a separate UI/art task.

Reviewer result:
- Status: pass
- Required fixes: initial QA implementation was replaced with a root-level runner and deferred scene load so the test survives the menu-to-battle scene transition.
- Residual risks: the QA checks state transfer and loadout seeding, not user-facing visual layout or every possible combination of setup options.

TODO update:
- Items marked done: `Boot -> main menu -> skirmish setup (faction, map seed, AI difficulty) -> battle.`
- Items left open: player can complete all gameplay loops, AI command-buffer-only behavior, counter readability, full 60 FPS playable-slice proof, Soft Old City readability.
- Reason: the real Godot menu now drives a real Battle scene and automated QA verifies the selected setup reaches `GameState`.
