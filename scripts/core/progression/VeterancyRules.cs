namespace ProceduralRts.Core;

public static class VeterancyRules
{
    public const int MaxRank = 3;

    public static float ExperienceForKill(EntityWorld world, EntityInstance target)
    {
        if (!world.TryGetSpec(target.SpecId, out var spec))
        {
            return 1;
        }

        var costScore = MathF.Max(0, spec.Stats?.Cost ?? 0) / 200f;
        var hpScore = MathF.Max(0, spec.Stats?.MaxHp ?? 0) / 180f;
        return MathF.Max(1, MathF.Max(costScore, hpScore));
    }

    public static int RankForExperience(float experience)
    {
        if (experience >= 12f)
        {
            return 3;
        }

        if (experience >= 7f)
        {
            return 2;
        }

        return experience >= 3f ? 1 : 0;
    }

    public static UpgradeModifier ModifierFor(VeterancyComponentState? veterancy)
    {
        return ModifierForRank(veterancy?.Rank ?? 0);
    }

    public static UpgradeModifier ModifierForRank(int rank)
    {
        rank = Math.Clamp(rank, 0, MaxRank);
        return new UpgradeModifier(
            DamageMultiplier: 1 + rank * 0.06f,
            WeaponRangeMultiplier: 1 + rank * 0.015f,
            SightRangeMultiplier: 1 + rank * 0.02f,
            MoveSpeedMultiplier: 1 + rank * 0.015f,
            MaxHpMultiplier: 1 + rank * 0.05f,
            HealthRegenMultiplier: 1 + rank * 0.08f);
    }
}
