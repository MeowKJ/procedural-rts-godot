namespace ProceduralRts.Core;

public static class VeterancySystem
{
    public static void AwardKill(EntityWorld world, EntityInstance attacker, EntityInstance target)
    {
        if (attacker.Id == target.Id
            || !attacker.Components.TryGet<VeterancyComponentState>(out var veterancy))
        {
            return;
        }

        var oldRank = veterancy.Rank;
        var experience = veterancy.Experience + VeterancyRules.ExperienceForKill(world, target);
        var rank = VeterancyRules.RankForExperience(experience);
        attacker.Components.Set(veterancy with
        {
            Kills = veterancy.Kills + 1,
            Experience = experience,
            Rank = rank,
        });

        if (rank > oldRank)
        {
            ApplyRankHealthDelta(world, attacker, oldRank, rank);
        }
    }

    private static void ApplyRankHealthDelta(EntityWorld world, EntityInstance entity, int oldRank, int newRank)
    {
        if (!entity.Components.TryGet<HealthComponentState>(out var health))
        {
            return;
        }

        var ownerModifier = UpgradeResolver.ModifierFor(world, entity.OwnerId);
        var oldModifier = ownerModifier.Compose(VeterancyRules.ModifierForRank(oldRank));
        var newModifier = ownerModifier.Compose(VeterancyRules.ModifierForRank(newRank));
        var baseMaxHp = BaseMaxHp(world, entity, health, oldModifier);
        var oldMaxHp = baseMaxHp * oldModifier.MaxHpMultiplier;
        var newMaxHp = baseMaxHp * newModifier.MaxHpMultiplier;
        var bonusHp = MathF.Max(0, newMaxHp - oldMaxHp);

        entity.Components.Set(health with
        {
            Hp = MathF.Min(newMaxHp, health.Hp + bonusHp),
            MaxHp = newMaxHp,
        });
    }

    private static float BaseMaxHp(
        EntityWorld world,
        EntityInstance entity,
        HealthComponentState health,
        UpgradeModifier oldModifier)
    {
        if (world.TryGetSpec(entity.SpecId, out var spec) && spec.Stats is not null)
        {
            return spec.Stats.MaxHp;
        }

        return health.MaxHp / MathF.Max(0.001f, oldModifier.MaxHpMultiplier);
    }
}
