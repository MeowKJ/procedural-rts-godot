namespace ProceduralRts.Core;

public sealed record WeaponTargetProfile(
    IReadOnlySet<MovementDomain> AllowedDomains,
    IReadOnlySet<ArmorTag> AllowedArmorTags,
    IReadOnlyDictionary<UnitWeightClass, float> WeightPriority,
    IReadOnlyDictionary<MovementDomain, float> DomainPriority,
    IReadOnlyDictionary<ArmorTag, float> ArmorPriority)
{
    public bool CanTarget(UnitSpecRuntimeDescriptor target)
    {
        return AllowedDomains.Contains(target.MovementDomain)
            && AllowedArmorTags.Contains(target.ArmorTag);
    }

    public bool CanTarget(BuildSpec target)
    {
        return AllowedArmorTags.Contains(target.ArmorTag)
            && AllowedDomains.Contains(MovementDomain.Land);
    }

    public float Priority(UnitSpecRuntimeDescriptor target)
    {
        if (!CanTarget(target))
        {
            return 0;
        }

        return PriorityFor(WeightPriority, target.WeightClass)
            * PriorityFor(DomainPriority, target.MovementDomain)
            * PriorityFor(ArmorPriority, target.ArmorTag);
    }

    public float Priority(BuildSpec target)
    {
        if (!CanTarget(target))
        {
            return 0;
        }

        return PriorityFor(WeightPriority, UnitWeightClass.Heavy)
            * PriorityFor(DomainPriority, MovementDomain.Land)
            * PriorityFor(ArmorPriority, target.ArmorTag);
    }

    private static float PriorityFor<T>(IReadOnlyDictionary<T, float> values, T key)
        where T : notnull
    {
        return values.TryGetValue(key, out var value) ? value : 1;
    }
}
