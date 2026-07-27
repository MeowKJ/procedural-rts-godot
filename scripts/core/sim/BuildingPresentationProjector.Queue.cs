namespace ProceduralRts.Core;

public static partial class BuildingPresentationProjector
{
    private static UnitProductionQueueItem[] CloneProductionQueue(IReadOnlyList<UnitProductionQueueItem> items)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var copy = new UnitProductionQueueItem[items.Count];
        for (var index = 0; index < items.Count; index++)
        {
            copy[index] = CloneQueueItem(items[index]);
        }

        return copy;
    }

    private static UnitProductionQueueItem CloneQueueItem(UnitProductionQueueItem item)
    {
        return new UnitProductionQueueItem
        {
            Id = item.Id,
            DesignId = item.DesignId,
            Faction = item.Faction,
            Progress = item.Progress,
        };
    }
}
