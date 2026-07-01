# Review Record - M5 derived regeneration

Step: Self-repair/regen derived from upgrades and veterancy
Milestone: M5 Unit Progression & Combat Elements
Owner AI: Codex
Reviewer AI: SimReplay, BalanceReport, CounterReadabilityQa, ReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityComponentState.cs`,
  `scripts/core/entities/EntityStateHash.cs`,
  `scripts/core/progression/UpgradeDefinition.cs`,
  `scripts/core/progression/UpgradeIds.cs`,
  `scripts/core/progression/UpgradeCatalog.cs`,
  `scripts/core/progression/UpgradeResolver.cs`,
  `scripts/core/progression/VeterancyRules.cs`,
  `scripts/core/sim/systems/RegenerationSystem.cs`,
  `scripts/core/sim/SimSystemPipeline.cs`, `scripts/core/sim/SimInvariants.*.cs`,
  `tools/SimReplay/Combat/DerivedRegenerationScenarios.cs`,
  `tools/SimReplay/Program.cs`, `TODO.md`.
- Non-goals: active ability UI, per-roster regen tuning, legacy `GameState`
  self-repair, or changing normal combat balance.

Implementation summary:
- Added `RegenerationComponentState` for authored HP/sec plus deterministic
  fractional progress.
- Added `HealthRegenMultiplier` to `UpgradeModifier`; `UpgradeResolver.HealthRegen`
  now derives the final regen rate from owner upgrades and per-entity veterancy.
- Added data upgrade `UpgradeIds.FieldRepairs` / `UpgradeCatalog` entry and folded
  veterancy rank into regen through `VeterancyRules`.
- Added `RegenerationSystem` to the live sim pipeline. It heals only entities with
  `RegenerationComponentState`, never by unit id, faction id, or hard-coded class.
- Added deterministic hash and invariant coverage for regeneration state.
- Added `derived-regeneration` SimReplay coverage for baseline no-regen, base regen,
  upgrade-derived regen, veterancy-derived regen, and max-HP cap behavior.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main Godot C# project compiled with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: includes `derived-regeneration`: static 50.0, base 62.0, upgraded
  71.0, veteran 76.0, capped 100.0.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior suite completed with no failures.
- Command: `dotnet run --project tools/BalanceReport/BalanceReport.csproj --no-restore`
  Result: pass
  Evidence: canonical parity/counter scenarios stayed inside gates; no normal unit
  receives regen without authored component state.
- Command: `dotnet run --project tools/CounterReadabilityQa/CounterReadabilityQa.csproj --no-restore`
  Result: pass
  Evidence: counter-readability scenarios stayed readable.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full review gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 22 gates passed, including build, SimReplay, CombatBehavior,
  SimulationSmoke, FogOfWarQa, selection stress, AI/player/sandbox/HUD QA,
  ReviewGate, PerfSmoke, BalanceReport, CounterReadabilityQa, and Godot headless
  scene checks.

Reviewer result:
- Status: pass
- Required fixes: none after implementation; focused and full gates passed.
- Residual risks: no playable roster unit currently authors regen by default; future
  tuning should add `RegenerationComponentState` through data/specs, not system
  branches.

TODO update:
- Items marked done: M5 self-repair/regen as derived upgrade/veterancy behavior.
- Items left open: full active ability framework, support UI/tuning, full
  WeaponSystem convergence, and broader upgrade/veterancy UI.
