# Review Record - M5 turret entities

Step: Turrets as entities and ordinary building weapon boundary.
Milestone: M5 unit progression and combat elements.
Owner AI: Worker C.
Reviewer AI: Integrator sanity review.
Integrator AI: Main Codex thread.

Scope:
- Files/folders: `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-30-m5-turret-entities.md`.
- Non-goals: No ProjectileSystem, no UI changes, no unit data migration, no construction-system changes, no deletion of legacy `BuildingKind`.

Implementation summary:
- Added deterministic `m5-turret-entities` SimReplay coverage.
- Proved `GroundTurret` and `AntiAirTurret` project from `BuildSpec` into `EntityKind.Turret` with `WeaponMountSpec` plus `WeaponUserComponentState`.
- Proved ordinary producer/resource/power/airfield buildings remain `EntityKind.Building`, do not gain `WeaponMountSpec` entries, and do not receive `WeaponUserComponentState` from `BuildingKind`.
- Proved `TurretCombatSystem` processes only `EntityKind.Turret` by adding a fake armed `EntityKind.Building` that remains untouched and never fires.
- Added `m5turretentities` ReviewGate coverage for the source contract.

Automated gates:
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: Pass.
  Evidence: `OK [m5-turret-entities]: turret shots 2, ordinary building shots 0, target hp 453.8.` and `SimReplay PASSED`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- m5turretentities`
  Result: Pass.
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.

Manual/visual gates:
- Check: None.
  Result: Not applicable.
  Evidence: Pure simulation/review-gate slice.

Reviewer result:
- Status: pass with residual risk.
- Required fixes: None for this slice.
- Residual risks: `Headquarters` is currently authored with `WeaponKind.IonEmitter` and therefore projects as an armed fixed defense; this slice proves ordinary producer/resource/power/airfield buildings do not gain weapons. Broader projectile, upgrade, and veterancy work remains open.

TODO update:
- Items marked done: M5 turret entity boundary and ordinary-building weapon boundary.
- Items left open: Projectile/ammo systems, upgrade resolver, veterancy, and full deterministic progression coverage.
