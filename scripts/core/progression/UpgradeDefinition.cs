namespace ProceduralRts.Core;

public sealed record UpgradeDefinition(
    string Id,
    string Label,
    UpgradeModifier Modifier);

public sealed record UpgradeModifier(
    float DamageMultiplier = 1,
    float WeaponRangeMultiplier = 1,
    float SightRangeMultiplier = 1,
    float MoveSpeedMultiplier = 1,
    float MaxHpMultiplier = 1,
    float HealthRegenMultiplier = 1)
{
    public UpgradeModifier Compose(UpgradeModifier other)
    {
        return new UpgradeModifier(
            DamageMultiplier * other.DamageMultiplier,
            WeaponRangeMultiplier * other.WeaponRangeMultiplier,
            SightRangeMultiplier * other.SightRangeMultiplier,
            MoveSpeedMultiplier * other.MoveSpeedMultiplier,
            MaxHpMultiplier * other.MaxHpMultiplier,
            HealthRegenMultiplier * other.HealthRegenMultiplier);
    }
}
