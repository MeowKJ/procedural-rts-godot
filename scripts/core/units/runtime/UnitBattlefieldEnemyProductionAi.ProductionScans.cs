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
            if (option.CanQueue && option.UnitDesignId is not null)
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

            var armyCount = ArmyCountForDesign(battlefield, enemyPlayerSlotId, option.UnitDesignId!);
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

            var armyCount = ArmyCountForDesign(battlefield, enemyPlayerSlotId, option.UnitDesignId!);
            if (best is null || IsBetterFallbackOption(option, armyCount, best, bestArmyCount))
            {
                best = option;
                bestArmyCount = armyCount;
            }
        }

        return best;
    }

    private static bool CanQueue(UnitBattlefield battlefield, PlayerSlotId playerSlotId, ProductionKind kind)
    {
        var states = battlefield.ProductionOptionStates(playerSlotId);
        for (var index = 0; index < states.Count; index++)
        {
            var state = states[index];
            if (state.Kind == kind && state.HasProducer && state.EnoughCredits)
            {
                return true;
            }
        }

        return false;
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
        var alive = 0;
        foreach (var unit in battlefield.Units)
        {
            if (unit.PlayerSlotId == enemyPlayerSlotId && unit.Hp > 0 && unit.Spec.Id == designId)
            {
                alive++;
            }
        }

        return alive + QueuedDesignCount(battlefield, enemyPlayerSlotId, designId);
    }

    private static int LiveHarvesterCount(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var count = 0;
        foreach (var unit in battlefield.Units)
        {
            if (unit.PlayerSlotId == enemyPlayerSlotId
                && unit.Hp > 0
                && unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
            {
                count++;
            }
        }

        return count;
    }

    private int QueuedKindCount(UnitBattlefield battlefield, PlayerSlotId playerSlotId, ProductionKind kind)
    {
        CollectOwnedBuildings(battlefield, playerSlotId, _ownedBuildingBuffer, liveOnly: false);
        var count = 0;
        for (var buildingIndex = 0; buildingIndex < _ownedBuildingBuffer.Count; buildingIndex++)
        {
            var queue = battlefield.BuildingProductionQueue(_ownedBuildingBuffer[buildingIndex].Id);
            for (var itemIndex = 0; itemIndex < queue.Count; itemIndex++)
            {
                if (queue[itemIndex].Kind == kind)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
