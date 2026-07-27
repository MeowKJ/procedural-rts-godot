using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private enum HoverMatchup
    {
        None,
        Good,
        Even,
        Poor,
        CannotTarget,
    }

    private string RuntimeUnitAttackPreviewLabel(UnitSpec target)
    {
        return AttackMatchupLabel(MatchupForRuntimeSelection(target), UnitTargetLabel(target));
    }

    private string RuntimeBuildingAttackPreviewLabel(BuildingHoverProjection target)
    {
        return AttackMatchupLabel(MatchupForRuntimeSelection(BuildSpecCatalog.For(target.Kind)), GameText.T("preview.target.structure"));
    }

    private static string AttackMatchupLabel(HoverMatchup matchup, string targetLabel)
    {
        return matchup switch
        {
            HoverMatchup.Good => GameText.Format("preview.attack.matchup", GameText.Format("preview.matchup.good", targetLabel)),
            HoverMatchup.Even => GameText.Format("preview.attack.matchup", GameText.Format("preview.matchup.even", targetLabel)),
            HoverMatchup.Poor => GameText.Format("preview.attack.matchup", GameText.Format("preview.matchup.poor", targetLabel)),
            HoverMatchup.CannotTarget => GameText.Format("preview.attack.matchup", GameText.Format("preview.matchup.cannotTarget", targetLabel)),
            _ => GameText.T("preview.attack"),
        };
    }

    private HoverMatchup MatchupForRuntimeSelection(UnitSpec target)
    {
        var selectedArmed = 0;
        var targeters = 0;
        var bestScore = 0f;
        foreach (var unit in UnitBattlefield.Units)
        {
            if (unit.PlayerSlotId != LocalPlayerSlotId || !unit.Selected || unit.Spec.Weapons.Count == 0)
            {
                continue;
            }

            selectedArmed++;
            var score = BestWeaponScore(unit.Spec, target);
            if (score <= 0)
            {
                continue;
            }

            targeters++;
            bestScore = MathF.Max(bestScore, score);
        }

        return MatchupFromScore(selectedArmed, targeters, bestScore);
    }

    private HoverMatchup MatchupForRuntimeSelection(BuildSpec target)
    {
        var selectedArmed = 0;
        var targeters = 0;
        var bestScore = 0f;
        foreach (var unit in UnitBattlefield.Units)
        {
            if (unit.PlayerSlotId != LocalPlayerSlotId || !unit.Selected || unit.Spec.Weapons.Count == 0)
            {
                continue;
            }

            selectedArmed++;
            var score = BestWeaponScore(unit.Spec, target);
            if (score <= 0)
            {
                continue;
            }

            targeters++;
            bestScore = MathF.Max(bestScore, score);
        }

        return MatchupFromScore(selectedArmed, targeters, bestScore);
    }


    private static HoverMatchup MatchupFromScore(int selectedArmed, int targeters, float bestScore)
    {
        if (selectedArmed == 0)
        {
            return HoverMatchup.None;
        }

        if (targeters == 0)
        {
            return HoverMatchup.CannotTarget;
        }

        var coverage = (float)targeters / selectedArmed;
        if (bestScore >= 1.22f && coverage >= 0.5f)
        {
            return HoverMatchup.Good;
        }

        if (bestScore < 0.82f || coverage < 0.34f)
        {
            return HoverMatchup.Poor;
        }

        return HoverMatchup.Even;
    }

    private static float BestWeaponScore(UnitSpec attacker, UnitSpec target)
    {
        var bestScore = 0f;
        foreach (var mount in attacker.Weapons)
        {
            if (!WeaponCatalog.WeaponDefinitions.TryGetValue(mount.WeaponId, out var weapon)
                || !WeaponCanTarget(weapon, target)
                || !WeaponCatalog.AmmoDefinitions.TryGetValue(weapon.AmmoId, out var ammo))
            {
                continue;
            }

            var multiplier = ammo.DamageProfile.Multiplier(target.Stats.WeightClass, target.Movement.Domain, target.Stats.ArmorTag);
            bestScore = MathF.Max(bestScore, multiplier * TargetPriority(weapon, target));
        }

        return bestScore;
    }

    private static float BestWeaponScore(UnitSpec attacker, BuildSpec target)
    {
        var bestScore = 0f;
        foreach (var mount in attacker.Weapons)
        {
            if (!WeaponCatalog.WeaponDefinitions.TryGetValue(mount.WeaponId, out var weapon)
                || !weapon.TargetProfile.CanTarget(target)
                || !WeaponCatalog.AmmoDefinitions.TryGetValue(weapon.AmmoId, out var ammo))
            {
                continue;
            }

            var multiplier = ammo.DamageProfile.Multiplier(UnitWeightClass.Heavy, MovementDomain.Land, target.ArmorTag);
            bestScore = MathF.Max(bestScore, multiplier * weapon.TargetProfile.Priority(target));
        }

        return bestScore;
    }

    private static bool WeaponCanTarget(WeaponDefinition weapon, UnitSpec target)
    {
        return UnitDesignDefinitionCatalog.RuntimeDescriptors.TryGetValue(target.Id, out var descriptor)
            ? weapon.TargetProfile.CanTarget(descriptor)
            : weapon.TargetProfile.AllowedDomains.Contains(target.Movement.Domain)
                && weapon.TargetProfile.AllowedArmorTags.Contains(target.Stats.ArmorTag);
    }

    private static float TargetPriority(WeaponDefinition weapon, UnitSpec target)
    {
        return UnitDesignDefinitionCatalog.RuntimeDescriptors.TryGetValue(target.Id, out var descriptor)
            ? weapon.TargetProfile.Priority(descriptor)
            : 1;
    }

    private static string UnitTargetLabel(UnitSpec target)
    {
        if (target.Movement.Domain == MovementDomain.Air)
        {
            return GameText.T("preview.target.air");
        }

        if (target.RoleTags.Contains(UnitRoleTag.Economy) || target.RoleTags.Contains(UnitRoleTag.Worker))
        {
            return GameText.T("preview.target.economy");
        }

        if (target.RoleTags.Contains(UnitRoleTag.Infantry))
        {
            return GameText.T("preview.target.infantry");
        }

        return target.Stats.WeightClass is UnitWeightClass.Medium or UnitWeightClass.Heavy
            ? GameText.T("preview.target.armor")
            : GameText.T("preview.target.unit");
    }
}
