namespace ProceduralRts.Core;

public static class CombatProfileDesign
{
    public static DamageProfile DamageProfile(
        Dictionary<UnitWeightClass, float>? weights = null,
        Dictionary<MovementDomain, float>? domain = null,
        Dictionary<ArmorTag, float>? armor = null)
    {
        return new DamageProfile(weights ?? [], domain ?? [], armor ?? []);
    }

    public static WeaponTargetProfile TargetProfile(
        HashSet<MovementDomain> domains,
        HashSet<ArmorTag> armor,
        Dictionary<UnitWeightClass, float>? weights = null,
        Dictionary<MovementDomain, float>? domainPriority = null,
        Dictionary<ArmorTag, float>? armorPriority = null)
    {
        return new WeaponTargetProfile(
            domains,
            armor,
            weights ?? [],
            domainPriority ?? [],
            armorPriority ?? []);
    }

    public static ElementDefenseProfile ElementDefense(Dictionary<string, float>? elements = null)
    {
        return new ElementDefenseProfile(elements);
    }

    public static TargetTraitProfile TargetTraits(
        HashSet<TargetTrait>? traits = null,
        HashSet<UnitRoleTag>? roles = null)
    {
        return new TargetTraitProfile(traits, roles);
    }

    public static CounterRuleProfile CounterRules(params CounterRule[] rules)
    {
        return new CounterRuleProfile(rules);
    }
}
