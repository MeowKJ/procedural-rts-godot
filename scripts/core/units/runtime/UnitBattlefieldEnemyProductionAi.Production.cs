namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyProductionAi
{
    private string? ChooseNextProductionDesign(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var options = battlefield.ProductionDesignOptionStates(enemyPlayerSlotId)
            .Where(option => option.CanQueue && option.UnitDesignId is not null)
            .ToList();
        if (options.Count == 0)
        {
            return null;
        }

        var enemyHarvesters = battlefield.Units.Count(unit =>
            unit.PlayerSlotId == enemyPlayerSlotId
            && unit.Hp > 0
            && unit.Spec.RoleTags.Contains(UnitRoleTag.Economy));
        var queuedHarvesters = battlefield.BuildingSnapshots()
            .Where(building => building.PlayerSlotId == enemyPlayerSlotId)
            .SelectMany(building => battlefield.BuildingProductionQueue(building.Id))
            .Count(item => item.Kind == ProductionKind.Harvester);
        if (enemyHarvesters + queuedHarvesters < _profile.DesiredHarvesters)
        {
            var economy = FirstQueueableOption(battlefield, enemyPlayerSlotId, options, ProductionCategory.Economy);
            if (economy?.UnitDesignId is { } economyDesignId)
            {
                return economyDesignId;
            }
        }

        for (var offset = 0; offset < MixedArmyPlan.Length; offset++)
        {
            var index = (_mixCursor + offset) % MixedArmyPlan.Length;
            var option = FirstQueueableOption(battlefield, enemyPlayerSlotId, options, MixedArmyPlan[index]);
            if (option?.UnitDesignId is not { } designId)
            {
                continue;
            }

            _mixCursor = (index + 1) % MixedArmyPlan.Length;
            return designId;
        }

        return options
            .Where(option => option.CanQueue && option.Category != ProductionCategory.Economy)
            .OrderBy(option => ArmyCountForDesign(battlefield, enemyPlayerSlotId, option.UnitDesignId!))
            .ThenBy(option => option.QueuedCount)
            .ThenBy(option => option.Cost)
            .Select(option => option.UnitDesignId)
            .FirstOrDefault();
    }

    private static ProductionOptionState? FirstQueueableOption(
        UnitBattlefield battlefield,
        PlayerSlotId enemyPlayerSlotId,
        IEnumerable<ProductionOptionState> options,
        ProductionCategory category)
    {
        return options
            .Where(option => option.Category == category)
            .Where(option => option.CanQueue)
            .OrderBy(option => ArmyCountForDesign(battlefield, enemyPlayerSlotId, option.UnitDesignId!))
            .ThenBy(option => option.QueuedCount)
            .ThenBy(option => option.Cost)
            .ThenBy(option => option.UnitDesignId)
            .FirstOrDefault();
    }

    private static int ArmyCountForDesign(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId, string designId)
    {
        var alive = battlefield.Units.Count(unit =>
            unit.PlayerSlotId == enemyPlayerSlotId
            && unit.Hp > 0
            && unit.Spec.Id == designId);
        var queued = battlefield.BuildingSnapshots()
            .Where(building => building.PlayerSlotId == enemyPlayerSlotId)
            .SelectMany(building => battlefield.BuildingProductionQueue(building.Id))
            .Count(item => item.DesignId == designId);
        return alive + queued;
    }

    private ProductionKind? ChooseNextProduction(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var enemyHarvesters = battlefield.Units.Count(unit =>
            unit.PlayerSlotId == enemyPlayerSlotId
            && unit.Hp > 0
            && unit.Spec.RoleTags.Contains(UnitRoleTag.Economy));
        var queuedHarvesters = battlefield.BuildingSnapshots()
            .Where(building => building.PlayerSlotId == enemyPlayerSlotId)
            .SelectMany(building => battlefield.BuildingProductionQueue(building.Id))
            .Count(item => item.Kind == ProductionKind.Harvester);

        if (enemyHarvesters + queuedHarvesters < _profile.DesiredHarvesters
            && CanQueue(battlefield, enemyPlayerSlotId, ProductionKind.Harvester))
        {
            return ProductionKind.Harvester;
        }

        var combatPreference = _preferTank
            ? new[] { ProductionKind.LightTank, ProductionKind.InfantrySquad }
            : [ProductionKind.InfantrySquad, ProductionKind.LightTank];
        _preferTank = !_preferTank;

        foreach (var kind in combatPreference)
        {
            if (CanQueue(battlefield, enemyPlayerSlotId, kind))
            {
                return kind;
            }
        }

        return CanQueue(battlefield, enemyPlayerSlotId, ProductionKind.Harvester)
            ? ProductionKind.Harvester
            : null;
    }

    private static bool CanQueue(UnitBattlefield battlefield, PlayerSlotId playerSlotId, ProductionKind kind)
    {
        return battlefield.ProductionOptionStates(playerSlotId)
            .Any(state => state.Kind == kind && state.HasProducer && state.EnoughCredits);
    }

    private static int QueuedCount(UnitBattlefield battlefield, PlayerSlotId playerSlotId)
    {
        return battlefield.BuildingSnapshots()
            .Where(building => building.PlayerSlotId == playerSlotId)
            .Sum(building => battlefield.BuildingProductionQueue(building.Id).Count);
    }
}
