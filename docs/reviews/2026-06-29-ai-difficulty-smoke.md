# Review Record - AI difficulty smoke

Step:
Add a headless AI difficulty smoke test that checks Easy/Normal/Hard pacing and
that Hard's wave sizing beats Easy in a direct wave scenario.

Milestone:
Design Reference - AI Difficulty Design.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread is operating at the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `tools/AiDifficultySmoke/AiDifficultySmoke.csproj`
  - `tools/AiDifficultySmoke/Program.cs`
  - `scripts/core/EnemyDifficultyProfile.cs`
  - `tools/VerifyAll/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-ai-difficulty-smoke.md`
- Non-goals:
  - Do not implement a full strategic AI planner.
  - Do not claim no-cheat VisibilityIndex planning; current runtime AI still uses
    UnitBattlefield state directly.
  - Do not rebalance all AI economy behavior.

Implementation summary:
- Added `tools/AiDifficultySmoke`, a .NET 8 tool that uses the real
  `UnitBattlefieldEnemyProductionAi` and `UnitBattlefieldEnemyAttackWaveAi`.
- The pacing probe runs Easy, Normal, and Hard against the same mirrored base setup
  and asserts production orders and attack waves scale upward with difficulty.
- The wave duel isolates wave-size behavior by giving Easy and Hard their own max
  wave sizes with the same opening timing; Hard must deal more HQ damage and keep
  more survivors.
- Adjusted the Hard profile to wait for at least 5 units before launching a wave,
  avoiding the previous behavior where Hard sent tiny early waves into defenders.
- Added `ReviewGate aidifficulty` and included AiDifficultySmoke in VerifyAll.

Automated gates:
- Command:
  `dotnet run --project tools/AiDifficultySmoke/AiDifficultySmoke.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Easy/Normal/Hard pacing reported 7/10/13 production orders and 1/2/3 waves;
  Easy-vs-Hard wave duel ended with Hard destroying the Easy HQ.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj aidifficulty`
  Result:
  Pass.
  Evidence:
  ReviewGate checks the smoke, profile hook, and VerifyAll integration.

Manual/visual gates:
- Check:
  In-game skirmish AI observation.
  Result:
  Not run.
  Evidence:
  This slice adds deterministic headless AI pacing coverage; live AI feel remains
  a future gameplay review.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded AI smoke slice.
- Residual risks:
  - AI still needs command-buffer/VisibilityIndex authority in the EntityWorld phase.
  - The smoke verifies pacing and wave pressure, not full strategic scouting or micro.

TODO update:
- Items marked done:
  - `tools/BalanceReport (or an AI smoke) runs AI-vs-AI per difficulty ...`.
- Items left open:
  - The broader Easy/Normal/Hard design bullets remain open until full planners and
    visibility-limited AI are implemented.
- Reason:
  - A real headless smoke now exercises the current difficulty profiles and fails if
    Hard no longer outpaces or out-pressures Easy.
