# Review Record - M5 veterancy core

Step: Veterancy component, rank modifiers, and deterministic replay proof
Milestone: M5 Unit Progression & Combat Elements
Owner AI: Codex
Reviewer AI: SimReplay, BalanceReport, CounterReadabilityQa, ReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityComponentState.cs`,
  `scripts/core/entities/EntityStateHash.cs`,
  `scripts/core/entities/UnitSpecEntityBridge.cs`,
  `scripts/core/progression/UpgradeResolver.cs`,
  `scripts/core/progression/VeterancyRules.cs`,
  `scripts/core/sim/WeaponMath.cs`, `scripts/core/sim/EntityProjection.cs`,
  `scripts/core/sim/systems/VeterancySystem.cs`,
  `scripts/core/sim/systems/combat/CombatDamageSystem.cs`,
  `scripts/core/sim/systems/combat/CombatEngagementSystem.cs`,
  `scripts/core/sim/systems/BuildingTargetCombatSystem.cs`,
  `scripts/core/sim/systems/TurretCombatSystem.cs`,
  `scripts/core/sim/SimInvariants.*.cs`, `scripts/world/UnitInstanceView.cs`,
  `tools/SimReplay/Combat/VeterancyProgressionScenarios.cs`,
  `tools/SimReplay/Program.cs`, `TODO.md`.
- Non-goals: full WeaponSystem convergence, research UI, upgrade tree UI,
  self-repair/regen gameplay, legacy `GameState` veterancy, or balance redesign.

Implementation summary:
- Added `VeterancyComponentState` for kills, XP, and rank, plus deterministic hash
  and invariant coverage.
- Added `VeterancyRules` and `VeterancySystem`: combat kills award target-value XP,
  compute rank thresholds, and apply rank-up max-HP deltas.
- Made `UpgradeResolver` compose owner-level upgrades with per-entity veterancy
  modifiers, so damage/range/sight/speed/max-HP stay derived from immutable specs.
- Added entity-aware `WeaponMath.BaseDamage` and moved mobile, turret, and
  building-target damage paths to entity-aware upgrade resolution.
- Added owner-neutral projection fields for rank/kills and rank dots in
  `UnitInstanceView`.
- Added `veterancy-progression` SimReplay coverage proving real combat kills
  promote a unit to rank 3, increase derived max HP and damage, expose projection
  fields, preserve immutable weapon/spec data, and remain deterministic.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main Godot C# project compiled with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: includes `veterancy-progression` with kills 2, rank 3, max HP 115.0,
  and damage 27.3.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior suite completed with no failures.
- Command: `dotnet run --project tools/BalanceReport/BalanceReport.csproj --no-restore`
  Result: pass
  Evidence: canonical parity and counter duels stayed inside required bands after
  veterancy promotion thresholds were tuned to long-game pacing.
- Command: `dotnet run --project tools/CounterReadabilityQa/CounterReadabilityQa.csproj --no-restore`
  Result: pass
  Evidence: counter-readability scenarios stayed readable with veterancy enabled.
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
- Required fixes: `WritableMounts` accepted read-only collection-expression lists
  as writable, which crashed the new replay; it now only reuses arrays/lists and
  copies other mount collections. Initial rank thresholds also caused short-duel
  snowballing in `BalanceReport`; thresholds were raised to long-game pacing.
- Residual risks: regen/self-repair is still tracked separately; legacy
  `GameState` presentation does not own authoritative veterancy; full WeaponSystem
  convergence remains open.

TODO update:
- Items marked done: deterministic progression tests now include upgrade,
  turret, projectile, and veterancy rank coverage.
- Items left open: full WeaponSystem, tech/research UI, upgrade/veterancy UI, and
  self-repair/regen.
