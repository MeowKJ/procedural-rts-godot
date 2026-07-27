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

        var enemyHarvesters = battlefield.LiveEconomyUnitCount(enemyPlayerSlotId);
        var queuedHarvesters = QueuedCategoryCount(battlefield, enemyPlayerSlotId, ProductionCategory.Economy);
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
}
