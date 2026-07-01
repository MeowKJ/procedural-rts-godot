# Review Record - MatchConfig seeding

Step: Add immutable MatchConfig and feed GameState setup from it.
Milestone: Design Reference - Match Lifecycle & Map Generation.
Owner AI: Codex.
Reviewer AI: Codex self-review (subagents unavailable in this continuation turn; ReviewGate and CombatBehavior provide durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/MatchConfig.cs`, `scripts/core/SkirmishOptions.cs`, `scripts/core/GameState.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-matchconfig-seeding.md`.
- Non-goals: no faction-select UI, no new map generator, no full match lifecycle teardown/rematch system, no live EntityWorld authority migration.

Implementation summary:
- Added immutable `MatchConfig` with starting credits, map seed, AI difficulty, world size, player faction, AI faction, and launch mode.
- Kept `SkirmishOptions` as the current menu-facing compatibility object and added conversion to/from `MatchConfig`.
- `GameState` now stores `MatchConfig`, derives world size and starting resources from it, and uses configured factions for owner seeding/relation defaults.
- `BattleRoot` presentation relation lookups now use the configured player faction.
- Added `ReviewGate matchconfig`.
- Extended `CombatBehavior` to prove direct `MatchConfig` construction, owner faction seeding, and same-config stable resources/buildings.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj matchconfig --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior smoke passed, including MatchConfig stable resource/building setup checks.

Manual/visual gates:
- Check: visual QA
  Result: not applicable
  Evidence: config/seeding data path only; no visual layout changed.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: faction selection UI is still open; seeded map generation is still the existing resource-position perturbation rather than a full symmetric map generator; starting loadout is not yet fully data-authored per MatchConfig.

TODO update:
- Items marked done: `MatchConfig`.
- Items left open: deterministic seeded map generation; match lifecycle; starting loadout; faction select.
- Reason: the exact immutable config object and same-config seeding proof now exist, while the broader map/lifecycle items remain separate TODOs.
