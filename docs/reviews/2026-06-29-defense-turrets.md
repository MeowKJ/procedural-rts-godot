# Review Record - Defense turrets

Step: Add buildable anti-ground and anti-air defense turrets for the playable factions.
Milestone: Playable 1v1 Skirmish - defense structures.
Owner AI: Codex.
Reviewer AI: Codex self-review (CombatBehavior and ReviewGate provide durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/BuildingKind.cs`, `scripts/core/WeaponKind.cs`, `scripts/core/WeaponCatalog.cs`, `scripts/core/BuildCatalog.cs`, `scripts/core/BuildingPresentationCatalog.cs`, `scripts/core/GameState.cs`, `scripts/core/GameText.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-defense-turrets.md`.
- Non-goals: no EntityKind.Turret migration, no per-faction unique turret art, no full tower balance pass, no separate turret production UI redesign.

Implementation summary:
- Added `BuildingKind.GroundTurret` and `BuildingKind.AntiAirTurret`.
- Added `WeaponKind.SkySpear`, an air-only static turret weapon using seeker rockets.
- Added defense turret build definitions, runtime building definitions, presentation descriptors, text keys, and runtime labels.
- Ground turret uses `VectorCannon`, which cannot target aircraft.
- Anti-air turret uses `SkySpear`, which only targets aircraft.
- Dog and Cat inherit access through their existing faction building availability.
- CombatBehavior now proves both factions expose both defense turret classes, and that ground/air turrets acquire and fire at the intended target classes.
- Added `ReviewGate turrets`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including defense turret availability, target filtering, firing, and anti-air tech gating.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj turrets --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for turret coverage.

Manual/visual gates:
- Check: visual QA
  Result: not run
  Evidence: this slice verifies build/combat data behavior headlessly; per-faction turret art remains part of the visual style TODO.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: turrets are still implemented as armed buildings in the legacy runtime; the broader TODO for `EntityKind.Turret` remains open.

TODO update:
- Items marked done: `Turrets: at least one anti-ground and one anti-air defense turret per faction.`
- Items left open: `Turrets as entities, mounts as bindings`, full EntityWorld authority migration, turret art differentiation, and balance polish.
- Reason: both playable factions now expose buildable anti-ground and anti-air defense turrets with tested target filtering and firing behavior.
