namespace ProceduralRts.Core;

public sealed record DamageProfile(
    IReadOnlyDictionary<UnitWeightClass, float> WeightMultipliers,
    IReadOnlyDictionary<MovementDomain, float> DomainMultipliers,
    IReadOnlyDictionary<ArmorTag, float> ArmorMultipliers
)
{
    public float Multiplier(UnitWeightClass weightClass, MovementDomain domain, ArmorTag armorTag)
    {
        return MultiplierFor(WeightMultipliers, weightClass)
            * MultiplierFor(DomainMultipliers, domain)
            * MultiplierFor(ArmorMultipliers, armorTag);
    }

    private static float MultiplierFor<T>(IReadOnlyDictionary<T, float> values, T key)
        where T : notnull
    {
        return values.TryGetValue(key, out var value) ? value : 1;
    }
}
