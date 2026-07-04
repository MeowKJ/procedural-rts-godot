namespace ProceduralRts.Core;

public sealed record ElementDefenseProfile
{
    public static ElementDefenseProfile Neutral { get; } = new();

    public IReadOnlyDictionary<string, float> ElementMultipliers { get; }

    public ElementDefenseProfile(IReadOnlyDictionary<string, float>? ElementMultipliers = null)
    {
        var multipliers = new SortedDictionary<string, float>(StringComparer.Ordinal);
        if (ElementMultipliers is not null)
        {
            foreach (var pair in ElementMultipliers)
            {
                _ = DamageElementCatalog.For(pair.Key);
                if (pair.Value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(ElementMultipliers), "Element defense multipliers must be positive.");
                }

                multipliers[pair.Key] = pair.Value;
            }
        }

        this.ElementMultipliers = multipliers;
    }

    public float MultiplierFor(string damageElementId)
    {
        _ = DamageElementCatalog.For(damageElementId);
        return ElementMultipliers.TryGetValue(damageElementId, out var multiplier) ? multiplier : 1f;
    }
}
