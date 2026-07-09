using System.Collections.ObjectModel;

namespace ProceduralRts.Core;

public sealed record UpgradeDefinition(
    string Id,
    string Label,
    UpgradeModifier Modifier);

public sealed record UpgradeModifier
{
    public float DamageMultiplier { get; }

    public float WeaponRangeMultiplier { get; }

    public float SightRangeMultiplier { get; }

    public float MoveSpeedMultiplier { get; }

    public float MaxHpMultiplier { get; }

    public float HealthRegenMultiplier { get; }

    public IReadOnlyDictionary<string, float> OutgoingElementDamageMultipliers { get; }

    public IReadOnlyDictionary<string, float> IncomingElementDamageMultipliers { get; }

    public IReadOnlyList<string> VisualDeltaIds { get; }

    public UpgradeModifier(
        float DamageMultiplier = 1,
        float WeaponRangeMultiplier = 1,
        float SightRangeMultiplier = 1,
        float MoveSpeedMultiplier = 1,
        float MaxHpMultiplier = 1,
        float HealthRegenMultiplier = 1,
        IReadOnlyDictionary<string, float>? OutgoingElementDamageMultipliers = null,
        IReadOnlyDictionary<string, float>? IncomingElementDamageMultipliers = null,
        IReadOnlyList<string>? VisualDeltaIds = null)
    {
        this.DamageMultiplier = RequirePositive(DamageMultiplier, nameof(DamageMultiplier));
        this.WeaponRangeMultiplier = RequirePositive(WeaponRangeMultiplier, nameof(WeaponRangeMultiplier));
        this.SightRangeMultiplier = RequirePositive(SightRangeMultiplier, nameof(SightRangeMultiplier));
        this.MoveSpeedMultiplier = RequirePositive(MoveSpeedMultiplier, nameof(MoveSpeedMultiplier));
        this.MaxHpMultiplier = RequirePositive(MaxHpMultiplier, nameof(MaxHpMultiplier));
        this.HealthRegenMultiplier = RequirePositive(HealthRegenMultiplier, nameof(HealthRegenMultiplier));
        this.OutgoingElementDamageMultipliers = NormalizeElementMultipliers(OutgoingElementDamageMultipliers, nameof(OutgoingElementDamageMultipliers));
        this.IncomingElementDamageMultipliers = NormalizeElementMultipliers(IncomingElementDamageMultipliers, nameof(IncomingElementDamageMultipliers));
        this.VisualDeltaIds = NormalizeVisualDeltaIds(VisualDeltaIds);
    }

    public UpgradeModifier Compose(UpgradeModifier other)
    {
        return new UpgradeModifier(
            DamageMultiplier * other.DamageMultiplier,
            WeaponRangeMultiplier * other.WeaponRangeMultiplier,
            SightRangeMultiplier * other.SightRangeMultiplier,
            MoveSpeedMultiplier * other.MoveSpeedMultiplier,
            MaxHpMultiplier * other.MaxHpMultiplier,
            HealthRegenMultiplier * other.HealthRegenMultiplier,
            ComposeElementMultipliers(OutgoingElementDamageMultipliers, other.OutgoingElementDamageMultipliers),
            ComposeElementMultipliers(IncomingElementDamageMultipliers, other.IncomingElementDamageMultipliers),
            ComposeVisualDeltaIds(VisualDeltaIds, other.VisualDeltaIds));
    }

    public float OutgoingElementDamageMultiplierFor(string damageElementId)
    {
        _ = DamageElementCatalog.For(damageElementId);
        return OutgoingElementDamageMultipliers.TryGetValue(damageElementId, out var multiplier) ? multiplier : 1f;
    }

    public float IncomingElementDamageMultiplierFor(string damageElementId)
    {
        _ = DamageElementCatalog.For(damageElementId);
        return IncomingElementDamageMultipliers.TryGetValue(damageElementId, out var multiplier) ? multiplier : 1f;
    }

    private static float RequirePositive(float multiplier, string parameterName)
    {
        return multiplier > 0 ? multiplier : throw new ArgumentOutOfRangeException(parameterName, "Upgrade multipliers must be positive.");
    }

    private static IReadOnlyDictionary<string, float> NormalizeElementMultipliers(
        IReadOnlyDictionary<string, float>? multipliers,
        string parameterName)
    {
        var normalized = new SortedDictionary<string, float>(StringComparer.Ordinal);
        if (multipliers is null)
        {
            return new ReadOnlyDictionary<string, float>(normalized);
        }

        foreach (var pair in multipliers)
        {
            _ = DamageElementCatalog.For(pair.Key);
            normalized[pair.Key] = RequirePositive(pair.Value, parameterName);
        }

        return new ReadOnlyDictionary<string, float>(normalized);
    }

    private static IReadOnlyDictionary<string, float> ComposeElementMultipliers(
        IReadOnlyDictionary<string, float> first,
        IReadOnlyDictionary<string, float> second)
    {
        var composed = new SortedDictionary<string, float>(StringComparer.Ordinal);
        foreach (var pair in first)
        {
            composed[pair.Key] = pair.Value;
        }

        foreach (var pair in second)
        {
            composed[pair.Key] = composed.TryGetValue(pair.Key, out var existing)
                ? existing * pair.Value
                : pair.Value;
        }

        return new ReadOnlyDictionary<string, float>(composed);
    }

    private static IReadOnlyList<string> NormalizeVisualDeltaIds(IReadOnlyList<string>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return [];
        }

        var normalized = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Visual delta ids must be non-empty.", nameof(ids));
            }

            normalized.Add(id);
        }

        return normalized.ToArray();
    }

    private static IReadOnlyList<string> ComposeVisualDeltaIds(IReadOnlyList<string> first, IReadOnlyList<string> second)
    {
        var composed = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var id in first)
        {
            composed.Add(id);
        }

        foreach (var id in second)
        {
            composed.Add(id);
        }

        return composed.ToArray();
    }
}
