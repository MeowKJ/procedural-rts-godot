namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void UpdateBuildingCombatFromEntityWorld(float dt)
    {
        if (!BuildingTargetIds()
            .Select(BuildingIdentity)
            .Any(identity => identity is not null && BuildSpecCatalog.For(identity.Kind).WeaponKind is not null))
        {
            return;
        }

        SyncBuildingTargetEntities();
        SyncUnitEntities();
        var context = new SimContext(_entityWorld, _inputCommandTick, dt, []);
        StepCombatBridgeWithProjectiles(context, _turretCombatSystem);
        ApplyTurretCombatEvents(_entityWorld.Events.Drain());
    }

    private void ApplyTurretCombatEvents(IReadOnlyList<SimEvent> events)
    {
        foreach (var simEvent in events)
        {
            if (simEvent is not EntityDamagedEvent damaged)
            {
                continue;
            }

            var attackerId = BuildingTargetIdByEntityId(damaged.Attacker);
            var target = UnitByEntityId(damaged.Target);
            if (attackerId is null || target is null)
            {
                continue;
            }

            if (BuildingSnapshot(attackerId.Value) is not { } attackerSnapshot)
            {
                continue;
            }

            if (_entityWorld.TryGet(target.EntityId, out var entity)
                && entity.Components.TryGet<HealthComponentState>(out var health))
            {
                target.Hp = health.Hp;
            }
            else
            {
                target.Hp -= damaged.Damage;
            }

            target.LastDamageAmount = damaged.Damage;
            target.LastDamageAmmoKind = BuildSpecCatalog.For(attackerSnapshot.Kind).WeaponKind is { } weaponKind
                ? WeaponCatalog.Weapons[weaponKind].AmmoKind
                : null;
            target.DeathOverkillDamage = MathF.Max(0, -target.Hp);
            target.HitPulse = 1;
            target.AlertPulse = 1;
            UnitAttackedByBuilding?.Invoke(target, attackerSnapshot);
        }
    }
}
