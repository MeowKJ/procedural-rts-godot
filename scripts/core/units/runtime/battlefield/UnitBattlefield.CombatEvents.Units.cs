namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void ApplyUnitDamageEventsFromBuildings(IReadOnlyList<SimEvent> events)
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

            if (!_entityWorld.TryGet(target.EntityId, out var entity)
                || !entity.Components.Has<HealthComponentState>())
            {
                continue;
            }

            var ammoId = BuildSpecCatalog.For(attackerSnapshot.Kind).WeaponId is { } weaponId
                ? WeaponCatalog.WeaponDefinitions[weaponId].AmmoId
                : null;
            ApplyUnitDamageProjection(target, entity, damaged.Damage, ammoId);
            UnitAttackedByBuilding?.Invoke(target, attackerSnapshot);
        }
    }
}
