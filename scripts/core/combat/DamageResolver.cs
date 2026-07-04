namespace ProceduralRts.Core;

public static class DamageResolver
{
    public static float Resolve(
        AmmoDefinition ammo,
        UnitWeightClass weightClass,
        MovementDomain movementDomain,
        ArmorTag armorTag,
        float attackerDamageMultiplier = 1f)
    {
        var profileMultiplier = ammo.DamageProfile.Multiplier(weightClass, movementDomain, armorTag);
        var elementMultiplier = DamageElementCatalog.For(ammo.DamageElementId).DamageMultiplier;
        return ammo.BaseDamage * profileMultiplier * elementMultiplier * attackerDamageMultiplier;
    }
}
