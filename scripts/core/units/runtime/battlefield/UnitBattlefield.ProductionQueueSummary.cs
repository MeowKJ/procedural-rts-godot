using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private readonly record struct ProductionQueueSummaryEntry(int BuildingId, UnitProductionQueueItem Item);

    public bool CancelFirstProduction(PlayerSlotId playerSlotId, out string status)
    {
        CollectQueuedProductionSummary(playerSlotId, _productionQueueSummaryBuffer);
        if (_productionQueueSummaryBuffer.Count == 0)
        {
            status = GameText.T("production.noneQueued");
            return false;
        }

        _productionQueueSummaryBuffer.Sort(CompareProductionQueueSummaryEntries);
        var first = _productionQueueSummaryBuffer[0];
        var spec = UnitDesignCatalog.Spec(first.Item.DesignId);
        var refund = Mathf.RoundToInt(spec.Stats.Cost * 0.5f);
        SyncBuildingTargetEntity(first.BuildingId);
        SubmitProductionCommand(new CancelProductionEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            [_buildingTargetEntityIds[first.BuildingId]],
            NextInputCommandTick()));
        SyncCreditsFromEntityWorld(playerSlotId);
        ResourceInventoryChanged?.Invoke(playerSlotId, ResourceInventory(playerSlotId));
        status = GameText.Format("production.cancelled", spec.Label, refund);
        return true;
    }

    public bool HasQueuedProduction(PlayerSlotId playerSlotId)
    {
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (!entity.Components.TryGet<BuildingIdentityComponentState>(out var identity)
                || identity.PlayerSlotId != playerSlotId
                || BuildingProductionQueue(identity.LegacyBuildingId).Count == 0)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public string ProductionQueueSummary(PlayerSlotId playerSlotId)
    {
        CollectQueuedProductionSummary(playerSlotId, _productionQueueSummaryBuffer);
        if (_productionQueueSummaryBuffer.Count == 0)
        {
            return GameText.T("ui.queue.empty");
        }

        _productionQueueSummaryBuffer.Sort(CompareProductionQueueSummaryEntries);
        var first = _productionQueueSummaryBuffer[0];
        var spec = UnitDesignCatalog.Spec(first.Item.DesignId);
        var progress = spec.Production is null ? 0 : Mathf.RoundToInt(Mathf.Clamp(first.Item.Progress / spec.Production.Duration, 0, 1) * 100);
        var refund = Mathf.RoundToInt(spec.Stats.Cost * 0.5f);
        return GameText.Format("ui.queue.summary", spec.Label.ToUpperInvariant(), progress, _productionQueueSummaryBuffer.Count, refund);
    }

    private void CollectQueuedProductionSummary(PlayerSlotId playerSlotId, List<ProductionQueueSummaryEntry> result)
    {
        result.Clear();
        _productionQueueSummarySeenIds.Clear();
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (!entity.Components.TryGet<BuildingIdentityComponentState>(out var identity)
                || identity.PlayerSlotId != playerSlotId
                || !_productionQueueSummarySeenIds.Add(identity.LegacyBuildingId))
            {
                continue;
            }

            var queue = BuildingProductionQueue(identity.LegacyBuildingId);
            for (var index = 0; index < queue.Count; index++)
            {
                result.Add(new ProductionQueueSummaryEntry(identity.LegacyBuildingId, queue[index]));
            }
        }
    }

    private static int CompareProductionQueueSummaryEntries(ProductionQueueSummaryEntry left, ProductionQueueSummaryEntry right)
    {
        var itemOrder = left.Item.Id.CompareTo(right.Item.Id);
        return itemOrder != 0 ? itemOrder : left.BuildingId.CompareTo(right.BuildingId);
    }
}
