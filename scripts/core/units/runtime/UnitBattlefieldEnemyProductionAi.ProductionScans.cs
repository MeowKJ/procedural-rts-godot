namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyProductionAi
{
    private void CollectQueueableDesignOptions(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        _queueableDesignOptions.Clear();
        var options = battlefield.ProductionDesignOptionStates(enemyPlayerSlotId);
        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            if (option.CanQueue)
            {
                _queueableDesignOptions.Add(option);
            }
        }
    }

    private ProductionOptionState? FirstQueueableOption(
        UnitBattlefield battlefield,
        PlayerSlotId enemyPlayerSlotId,
        ProductionCategory category)
    {
        ProductionOptionState? best = null;
        var bestArmyCount = int.MaxValue;
        for (var index = 0; index < _queueableDesignOptions.Count; index++)
        {
            var option = _queueableDesignOptions[index];
            if (option.Category != category)
            {
                continue;
            }

            var armyCount = ArmyCountForDesign(battlefield, enemyPlayerSlotId, option.UnitDesignId);
            if (best is null || IsBetterProductionOption(option, armyCount, best, bestArmyCount))
            {
                best = option;
                bestArmyCount = armyCount;
            }
        }

        return best;
    }

    private ProductionOptionState? FirstFallbackCombatOption(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        ProductionOptionState? best = null;
        var bestArmyCount = int.MaxValue;
        for (var index = 0; index < _queueableDesignOptions.Count; index++)
        {
            var option = _queueableDesignOptions[index];
            if (option.Category == ProductionCategory.Economy)
            {
                continue;
            }

            var armyCount = ArmyCountForDesign(battlefield, enemyPlayerSlotId, option.UnitDesignId);
            if (best is null || IsBetterFallbackOption(option, armyCount, best, bestArmyCount))
            {
                best = option;
                bestArmyCount = armyCount;
            }
        }

        return best;
    }

    private int QueuedCount(UnitBattlefield battlefield, PlayerSlotId playerSlotId)
    {
        CollectOwnedBuildings(battlefield, playerSlotId, _ownedBuildingBuffer, liveOnly: false);
        var count = 0;
        for (var index = 0; index < _ownedBuildingBuffer.Count; index++)
        {
            count += battlefield.BuildingProductionQueue(_ownedBuildingBuffer[index].Id).Count;
        }

        return count;
    }

    private int ArmyCountForDesign(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId, string designId)
    {
        return battlefield.LiveUnitDesignCount(enemyPlayerSlotId, designId)
            + QueuedDesignCount(battlefield, enemyPlayerSlotId, designId);
    }

    private int QueuedCategoryCount(UnitBattlefield battlefield, PlayerSlotId playerSlotId, ProductionCategory category)
    {
        CollectOwnedBuildings(battlefield, playerSlotId, _ownedBuildingBuffer, liveOnly: false);
        var count = 0;
        for (var buildingIndex = 0; buildingIndex < _ownedBuildingBuffer.Count; buildingIndex++)
        {
            var queue = battlefield.BuildingProductionQueue(_ownedBuildingBuffer[buildingIndex].Id);
            for (var itemIndex = 0; itemIndex < queue.Count; itemIndex++)
            {
                if (UnitDesignCatalog.Spec(queue[itemIndex].DesignId).Production?.Category == category)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
