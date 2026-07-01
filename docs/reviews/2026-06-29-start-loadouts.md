# Review Record - Starting loadouts

Step: Move skirmish starting units and buildings into data-driven faction loadouts.
Milestone: Design Reference - Match Lifecycle & Map Generation.
Owner AI: Codex.
Reviewer AI: Codex self-review (subagents unavailable in this continuation turn; ReviewGate and smoke tools provide durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/MatchStartLoadout.cs`, `scripts/core/FactionCatalog.cs`, `scripts/core/GameState.cs`, `scripts/core/EnemyProductionAi.cs`, `scripts/core/EnemyAttackWaveAi.cs`, `tools/CombatBehavior/Program.cs`, `tools/SimulationSmoke/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-start-loadouts.md`.
- Non-goals: no full seeded map generator, no faction-select UI, no EntityWorld live-authority migration, no production/economy system rewrite.

Implementation summary:
- Added `MatchStartLoadouts` with data records for starting buildings and units.
- `GameState.Seed()` now uses `MatchStartLoadouts.For(owner, configuredFaction)` for player and AI starts instead of hard-coded start units/buildings.
- Dog/Cat faction starts include HQ/refinery and one faction harvester; combat starts were kept at three combat units so the enemy wave smoke remains meaningful.
- Added `GameState.IsHarvesterUnit()` so Dog/Cat harvesters participate in harvest commands, selection priority, harvester updates, enemy production counts, and enemy attack-wave filtering.
- Added `ReviewGate startloadout`.
- Updated smoke tests to use role-based harvester detection.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj startloadout --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including faction loadout and MatchConfig-driven start checks.
- Command: `dotnet run --project tools/SimulationSmoke/SimulationSmoke.csproj --no-restore`
  Result: pass
  Evidence: 300s smoke passed with production, completions, waves, harvesting/resource depletion.

Manual/visual gates:
- Check: visual QA
  Result: not run
  Evidence: data seeding and simulation behavior were verified headless; no screenshot was required for this slice.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: starting positions are still hand-authored slots, not a full symmetric seeded map generator; starting production/economy remains on the legacy GameState/UnitBattlefield paths until later authority migration.

TODO update:
- Items marked done: `Starting loadout per faction`.
- Items left open: deterministic seeded map generation; match lifecycle; build/production/economy EntityWorld migration.
- Reason: starting units/buildings are now faction data, seeded from MatchConfig, and proven by ReviewGate plus headless behavior/smoke tests.
