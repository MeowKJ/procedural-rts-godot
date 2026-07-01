# Review Record - M5 tracking projectile entities

Step: Tracking projectile entities
Milestone: M5 Unit Progression & Combat Elements
Owner AI: Codex
Reviewer AI: SimReplay, ReviewGate, CombatBehavior, CounterReadabilityQa
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityComponentState.cs`,
  `scripts/core/entities/EntityWorld.cs`, `scripts/core/entities/EntityStateHash.cs`,
  `scripts/core/sim/systems/ProjectileSystem.cs`,
  `scripts/core/sim/systems/combat/CombatDamageSystem.cs`,
  `scripts/core/sim/systems/TurretCombatSystem.cs`,
  `scripts/core/sim/systems/BuildingTargetCombatSystem.cs`,
  `scripts/core/sim/SimSystemPipeline.cs`, `scripts/core/sim/SimInvariants.*.cs`,
  `scripts/core/units/runtime/`, `tools/SimReplay/Combat/ProjectileTrackingScenarios.cs`,
  `tools/SimReplay/Program.cs`, `tools/BalanceReport/Program.cs`,
  `tools/CounterReadabilityQa/CounterReadabilityWorldSetup.cs`,
  `tools/PerfSmoke/Program.cs`, `TODO.md`.
- Non-goals: full WeaponSystem replacement, beam/splash/interceptable projectile
  gameplay, projectile presentation rendering, broad combat balance tuning, or
  veterancy.

Implementation summary:
- Added `ProjectileComponentState` and deterministic hash coverage for projectile
  source, target, weapon/ammo ids, damage, velocity, speed, tracking, hit radius,
  and lifetime.
- Added `ProjectileSystem` for tracking projectile movement, target following,
  impact damage, and cleanup.
- Added `EntityWorld.QueueSpawn` plus system-between `FlushQueuedSpawns`, so combat
  systems can create projectile entities without mutating `OrderedEntities` during
  an active combat iteration.
- Folded `_nextEntityId` into `EntityWorld.DeterministicStateHash` so transient
  projectile id consumption cannot disappear from replay evidence.
- Routed tracking ammo from mobile combat, turret combat, and unit-vs-building
  combat through projectile entities while leaving immediate weapons immediate.
- Tightened tracking projectile runtime semantics: fired projectiles continue after
  the original shooter dies, use swept segment impact checks, and avoid creating
  retaliation state against a projectile fallback attacker.
- Restored seeker rocket anti-vehicle readability after delayed projectile damage by
  giving `SeekerRocketAmmo` an explicit vehicle armor multiplier while leaving the
  broader balance model untouched.
- Added `projectile-tracking` SimReplay coverage proving a projectile exists before
  impact damage, moves deterministically, impacts, damages, and cleans up.
- Split fixed turret event handling into `UnitBattlefield.TurretCombat.cs` to keep
  file-size governance clean after the new projectile bridge.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main Godot C# project compiled with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: includes `projectile-tracking` deterministic replay and existing combat,
  movement, economy, ability, construction, outcome, and authored-content scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior suite completed with no failures.
- Command: `dotnet run --project tools/CounterReadabilityQa/CounterReadabilityQa.csproj --no-restore`
  Result: pass
  Evidence: counter-readability scenarios still pass after delayed tracking impacts.
- Command: `dotnet run --project tools/BalanceReport/BalanceReport.csproj --no-restore`
  Result: pass
  Evidence: anti-vehicle, anti-air, vehicle parity, air pressure, and mixed-force
  canonical duels pass after tracking projectile delay.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: 0 errors and 0 warnings; no C# file exceeds 400 lines.
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
- Required fixes: BalanceReport initially exposed that delayed tracking impacts made
  rocket dogs fail the anti-vehicle counter gate. Fixed by preserving in-flight
  projectiles after shooter death, adding swept impact checks, and restoring the
  seeker rocket vehicle damage profile.
- Residual risks: projectile presentation is not yet a first-class EntityProjection
  render path; beam/splash/interceptable projectile gameplay and the full WeaponSystem
  state machine remain separate open M5 work.

TODO update:
- Items marked done: tracking projectile entity slice under M5 projectile/ammo and
  deterministic progression tests.
- Items left open: full WeaponSystem, beam/splash/interceptable gameplay, and veterancy.
- Reason: tracking missiles now use gameplay entities and deterministic replay proof,
  but the broader M5 combat-element parent is not fully complete.
