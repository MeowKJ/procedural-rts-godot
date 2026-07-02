namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyProductionAi
{
    private static bool IsBetterProductionOption(
        ProductionOptionState candidate,
        int candidateArmyCount,
        ProductionOptionState best,
        int bestArmyCount)
    {
        var armyOrder = candidateArmyCount.CompareTo(bestArmyCount);
        if (armyOrder != 0)
        {
            return armyOrder < 0;
        }

        var queuedOrder = candidate.QueuedCount.CompareTo(best.QueuedCount);
        if (queuedOrder != 0)
        {
            return queuedOrder < 0;
        }

        var costOrder = candidate.Cost.CompareTo(best.Cost);
        return costOrder != 0
            ? costOrder < 0
            : string.Compare(candidate.UnitDesignId, best.UnitDesignId, StringComparison.Ordinal) < 0;
    }

    private static bool IsBetterFallbackOption(
        ProductionOptionState candidate,
        int candidateArmyCount,
        ProductionOptionState best,
        int bestArmyCount)
    {
        var armyOrder = candidateArmyCount.CompareTo(bestArmyCount);
        if (armyOrder != 0)
        {
            return armyOrder < 0;
        }

        var queuedOrder = candidate.QueuedCount.CompareTo(best.QueuedCount);
        if (queuedOrder != 0)
        {
            return queuedOrder < 0;
        }

        return candidate.Cost < best.Cost;
    }

    private int QueuedDesignCount(UnitBattlefield battlefield, PlayerSlotId playerSlotId, string designId)
    {
        CollectOwnedBuildings(battlefield, playerSlotId, _ownedBuildingBuffer, liveOnly: false);
        var count = 0;
        for (var buildingIndex = 0; buildingIndex < _ownedBuildingBuffer.Count; buildingIndex++)
        {
            var queue = battlefield.BuildingProductionQueue(_ownedBuildingBuffer[buildingIndex].Id);
            for (var itemIndex = 0; itemIndex < queue.Count; itemIndex++)
            {
                if (queue[itemIndex].DesignId == designId)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
