namespace ProceduralRts.Core;

public static class UpgradeResolver
{
    public static UpgradeModifier ModifierFor(EntityWorld world, OwnerId owner)
    {
        if (!world.UpgradeStates.TryGetValue(owner.Value, out var state))
        {
            return new UpgradeModifier();
        }

        var modifier = new UpgradeModifier();
        foreach (var id in state.CompletedIds)
        {
            modifier = modifier.Compose(UpgradeCatalog.Definitions[id].Modifier);
        }

        return modifier;
    }

    public static UpgradeModifier ModifierFor(EntityWorld world, EntityInstance entity)
    {
        var modifier = ModifierFor(world, entity.OwnerId);
        var veterancy = entity.Components.TryGet<VeterancyComponentState>(out var state) ? state : null;
        return modifier.Compose(VeterancyRules.ModifierFor(veterancy));
    }

    public static float Damage(EntityWorld world, OwnerId owner, float baseDamage)
    {
        return baseDamage * ModifierFor(world, owner).DamageMultiplier;
    }

    public static float Damage(EntityWorld world, EntityInstance entity, float baseDamage)
    {
        return baseDamage * ModifierFor(world, entity).DamageMultiplier;
    }

    public static float WeaponRange(EntityWorld world, EntityInstance entity, float baseRange)
    {
        return baseRange * ModifierFor(world, entity).WeaponRangeMultiplier;
    }

    public static float SightRange(EntityWorld world, EntityInstance entity, float baseRange)
    {
        return baseRange * ModifierFor(world, entity).SightRangeMultiplier;
    }

    public static float MoveSpeed(EntityWorld world, EntityInstance entity, float baseSpeed)
    {
        return baseSpeed * ModifierFor(world, entity).MoveSpeedMultiplier;
    }

    public static float MaxHp(EntityWorld world, EntityInstance entity, float baseMaxHp)
    {
        return baseMaxHp * ModifierFor(world, entity).MaxHpMultiplier;
    }

    public static float HealthRegen(EntityWorld world, EntityInstance entity, float baseHpPerSecond)
    {
        return baseHpPerSecond * ModifierFor(world, entity).HealthRegenMultiplier;
    }
}
