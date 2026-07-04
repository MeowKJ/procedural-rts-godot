namespace ProceduralRts.Core;

public static class DamageResolver
{
    public static float Resolve(
        AmmoDefinition ammo,
        UnitWeightClass weightClass,
        MovementDomain movementDomain,
        ArmorTag armorTag,
        float attackerDamageMultiplier = 1f,
        ElementDefenseProfile? targetElementDefense = null,
        TargetTraitProfile? targetTraits = null)
    {
        var profileMultiplier = ammo.DamageProfile.Multiplier(weightClass, movementDomain, armorTag);
        var elementMultiplier = DamageElementCatalog.For(ammo.DamageElementId).DamageMultiplier;
        var defenseMultiplier = (targetElementDefense ?? ElementDefenseProfile.Neutral).MultiplierFor(ammo.DamageElementId);
        var counterMultiplier = ammo.CounterRules.MultiplierFor(targetTraits, weightClass, movementDomain, armorTag);
        return ammo.BaseDamage * profileMultiplier * elementMultiplier * defenseMultiplier * counterMultiplier * attackerDamageMultiplier;
    }
}
