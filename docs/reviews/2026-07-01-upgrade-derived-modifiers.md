# Review Record - Upgrade derived modifiers

Step: Upgrade derived modifiers
Milestone: M5 Unit Progression & Combat Elements
Owner AI: Codex
Reviewer AI: SimReplay / ReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/progression/`, `scripts/core/entities/EntityWorld.cs`, `scripts/core/sim/WeaponMath.cs`, `scripts/core/sim/systems/`, `tools/SimReplay/Combat/UpgradeProgressionScenarios.cs`, `tools/ReviewGate/`, `TODO.md`.
- Non-goals: upgrade UI, research/economy commands, veterancy XP/ranks, projectile entity lifetime, or balance tuning.

Implementation summary:
- Added owner-scoped `UpgradeState`, sample `UpgradeCatalog` definitions, and `UpgradeResolver` for damage, weapon range, sight range, move speed, and future max-HP modifiers.
- Added `EntityWorld.UpgradeStates` / `EntityWorld.Upgrades(...)` and folded completed upgrade ids into deterministic state hash.
- Routed CombatSystem, BuildingTargetCombatSystem, TurretCombatSystem, CommandSystem attack-slot range, VisionSystem, MovementSystem, MovementSystem anchors, and SeparationSystem through derived upgrade values.
- Added SimReplay `upgrade-progression` coverage proving derived damage/range/sight/speed, immutable base specs/weapon definitions, system read paths, and hash sensitivity.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay printed `OK [upgrade-progression]` and completed all deterministic scenarios.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: upgrades can be completed programmatically but do not yet have research commands, player UI, AI planning, or veterancy producers.

TODO update:
- Items marked done: none; this is a major progress slice inside the still-open M5 upgrade/progression items.
- Items left open: research command flow, veterancy ranks, projectile entity lifetime/tracking, and upgrade/veterancy UI.
- Reason: the architecture now has a shared derived-modifier resolver, but the full progression feature is not complete.
