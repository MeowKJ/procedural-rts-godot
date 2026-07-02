namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyProductionAi
{
    private string? ChooseNextProductionDesign(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        CollectQueueableDesignOptions(battlefield, enemyPlayerSlotId);
        if (_queueableDesignOptions.Count == 0)
        {
            return null;
        }

        var enemyHarvesters = LiveHarvesterCount(battlefield, enemyPlayerSlotId);
        var queuedHarvesters = QueuedKindCount(battlefield, enemyPlayerSlotId, ProductionKind.Harvester);
        if (enemyHarvesters + queuedHarvesters < _profile.DesiredHarvesters)
        {
            var economy = FirstQueueableOption(battlefield, enemyPlayerSlotId, ProductionCategory.Economy);
            if (economy?.UnitDesignId is { } economyDesignId)
            {
                return economyDesignId;
            }
        }

        for (var offset = 0; offset < MixedArmyPlan.Length; offset++)
        {
            var index = (_mixCursor + offset) % MixedArmyPlan.Length;
            var option = FirstQueueableOption(battlefield, enemyPlayerSlotId, MixedArmyPlan[index]);
            if (option?.UnitDesignId is not { } designId)
            {
                continue;
            }

            _mixCursor = (index + 1) % MixedArmyPlan.Length;
            return designId;
        }

        return FirstFallbackCombatOption(battlefield, enemyPlayerSlotId)?.UnitDesignId;
    }

    private ProductionKind? ChooseNextProduction(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var enemyHarvesters = LiveHarvesterCount(battlefield, enemyPlayerSlotId);
        var queuedHarvesters = QueuedKindCount(battlefield, enemyPlayerSlotId, ProductionKind.Harvester);

        if (enemyHarvesters + queuedHarvesters < _profile.DesiredHarvesters
            && CanQueue(battlefield, enemyPlayerSlotId, ProductionKind.Harvester))
        {
            return ProductionKind.Harvester;
        }

        var combatPreference = _preferTank
            ? TankFirstCombatPreference
            : InfantryFirstCombatPreference;
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

    private static readonly ProductionKind[] TankFirstCombatPreference =
    [
        ProductionKind.LightTank,
        ProductionKind.InfantrySquad,
    ];

    private static readonly ProductionKind[] InfantryFirstCombatPreference =
    [
        ProductionKind.InfantrySquad,
        ProductionKind.LightTank,
    ];
}
