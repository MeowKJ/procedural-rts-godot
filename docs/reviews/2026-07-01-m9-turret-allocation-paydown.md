# Review Record - M9 turret allocation paydown

Step: Remove allocation-heavy turret/building-target combat loops
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Codex
Reviewer AI: SimReplay / ReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/WeaponEngagementState.cs`, `scripts/core/sim/systems/TurretCombatSystem.cs`, `scripts/core/sim/systems/BuildingTargetCombatSystem.cs`, `TODO.md`.
- Non-goals: changing combat target priorities, changing firing cadence, changing weapon balance, removing all remaining M9 allocation debt, or rewriting Construction/Production/Command hot paths.

Implementation summary:
- Replaced `TurretCombatSystem.ResolveTarget(...)` LINQ `Where/Select/OrderBy/FirstOrDefault` chain with a deterministic single-pass best-candidate loop.
- Reused `WeaponEngagementState.WritableMounts(...)` in turret and building-target engagement instead of allocating `weapon.Mounts.ToArray()` every engage.
- Added `WeaponEngagementState.ReadOnlyMounts(...)` so mutated mount storage can be written back without allocating for arrays/lists.
- Changed `WeaponEngagementState.CoolMountsCopy(...)` to allocate lazily only when at least one cooldown value changes.
- Preserved target ordering semantics: highest priority, then nearest distance, then lowest EntityId.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: deterministic replay suite completed, including turret, projectile, combat, group attack, and firing-anchor scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: weapon hit rules, turret states, economy, enemy AI, and outcomes passed.
- Command: `dotnet run --project tools/CounterReadabilityQa/CounterReadabilityQa.csproj --no-restore`
  Result: pass
  Evidence: counter cases including AA turret pressure passed.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: broader allocation paydown remains open for ConstructionSystem, ProductionSystem, CommandSystem, PathfindingSystem, and profiler-guided GC cleanup.

TODO update:
- Items marked done: none.
- Items left open: `Per-tick allocation paydown`.
- Reason: this removes one hotspot family but does not complete the full allocation-debt item.
