using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private readonly record struct ProductionQueueSummaryEntry(int BuildingId, UnitProductionQueueItem Item);

    public bool CancelFirstProduction(PlayerSlotId playerSlotId, out string status)
    {
        CollectQueuedProductionSummary(playerSlotId, _productionQueueSummaryBuffer);
        return CancelFirstQueuedProduction(playerSlotId, _productionQueueSummaryBuffer, out status);
    }

    public bool CancelFirstProductionForSelectedProducers(
        PlayerSlotId playerSlotId,
        IReadOnlyList<int> selectedBuildingIds,
        out bool hasSelectedProducers,
        out string status)
    {
        CollectSelectedProductionProducerIds(playerSlotId, selectedBuildingIds, _selectedProductionProducerIdBuffer);
        hasSelectedProducers = _selectedProductionProducerIdBuffer.Count > 0;
        if (!hasSelectedProducers)
        {
            status = GameText.T("production.noneQueued");
            return false;
        }

        CollectQueuedProductionSummary(playerSlotId, _selectedProductionProducerIdBuffer, _productionQueueSummaryBuffer);
        return CancelFirstQueuedProduction(playerSlotId, _productionQueueSummaryBuffer, out status);
    }

    private bool CancelFirstQueuedProduction(
        PlayerSlotId playerSlotId,
        List<ProductionQueueSummaryEntry> queueEntries,
        out string status)
    {
        if (queueEntries.Count == 0)
        {
            status = GameText.T("production.noneQueued");
            return false;
        }

        queueEntries.Sort(CompareProductionQueueSummaryEntries);
        var first = queueEntries[0];
        var spec = UnitDesignCatalog.Spec(first.Item.DesignId);
        var refund = Mathf.RoundToInt(spec.Stats.Cost * 0.5f);
        SubmitProductionCommand(new CancelProductionEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            [_buildingTargetEntityIds[first.BuildingId]],
            NextInputCommandTick()));
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
                || BuildingProductionQueue(identity.BuildingId).Count == 0)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public bool HasQueuedProductionForSelectedProducers(
        PlayerSlotId playerSlotId,
        IReadOnlyList<int> selectedBuildingIds,
        out bool hasSelectedProducers)
    {
        CollectSelectedProductionProducerIds(playerSlotId, selectedBuildingIds, _selectedProductionProducerIdBuffer);
        hasSelectedProducers = _selectedProductionProducerIdBuffer.Count > 0;
        if (!hasSelectedProducers)
        {
            return false;
        }

        return HasQueuedProduction(playerSlotId, _selectedProductionProducerIdBuffer);
    }

    public string ProductionQueueSummary(PlayerSlotId playerSlotId)
    {
        CollectQueuedProductionSummary(playerSlotId, _productionQueueSummaryBuffer);
        return ProductionQueueSummary(_productionQueueSummaryBuffer);
    }

    public string ProductionQueueSummaryForSelectedProducers(
        PlayerSlotId playerSlotId,
        IReadOnlyList<int> selectedBuildingIds,
        out bool hasSelectedProducers,
        out bool hasQueuedProduction)
    {
        CollectSelectedProductionProducerIds(playerSlotId, selectedBuildingIds, _selectedProductionProducerIdBuffer);
        hasSelectedProducers = _selectedProductionProducerIdBuffer.Count > 0;
        if (!hasSelectedProducers)
        {
            hasQueuedProduction = false;
            return GameText.T("ui.queue.empty");
        }

        CollectQueuedProductionSummary(playerSlotId, _selectedProductionProducerIdBuffer, _productionQueueSummaryBuffer);
        hasQueuedProduction = _productionQueueSummaryBuffer.Count > 0;
        return ProductionQueueSummary(_productionQueueSummaryBuffer);
    }

    private bool HasQueuedProduction(PlayerSlotId playerSlotId, IReadOnlyList<int> producerBuildingIds)
    {
        for (var index = 0; index < producerBuildingIds.Count; index++)
        {
            var buildingId = producerBuildingIds[index];
            if (BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId
                && BuildingProductionQueue(buildingId).Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string ProductionQueueSummary(List<ProductionQueueSummaryEntry> queueEntries)
    {
        if (queueEntries.Count == 0)
        {
            return GameText.T("ui.queue.empty");
        }

        queueEntries.Sort(CompareProductionQueueSummaryEntries);
        var first = queueEntries[0];
        var spec = UnitDesignCatalog.Spec(first.Item.DesignId);
        var progress = spec.Production is null ? 0 : Mathf.RoundToInt(Mathf.Clamp(first.Item.Progress / spec.Production.Duration, 0, 1) * 100);
        var refund = Mathf.RoundToInt(spec.Stats.Cost * 0.5f);
        return GameText.Format("ui.queue.summary", spec.Label.ToUpperInvariant(), progress, queueEntries.Count, refund);
    }

    private void CollectQueuedProductionSummary(PlayerSlotId playerSlotId, List<ProductionQueueSummaryEntry> result)
    {
        result.Clear();
        _productionQueueSummarySeenIds.Clear();
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (!entity.Components.TryGet<BuildingIdentityComponentState>(out var identity)
                || identity.PlayerSlotId != playerSlotId
                || !_productionQueueSummarySeenIds.Add(identity.BuildingId))
            {
                continue;
            }

            var queue = BuildingProductionQueue(identity.BuildingId);
            for (var index = 0; index < queue.Count; index++)
            {
                result.Add(new ProductionQueueSummaryEntry(identity.BuildingId, queue[index]));
            }
        }
    }

    private void CollectQueuedProductionSummary(
        PlayerSlotId playerSlotId,
        IReadOnlyList<int> producerBuildingIds,
        List<ProductionQueueSummaryEntry> result)
    {
        result.Clear();
        for (var producerIndex = 0; producerIndex < producerBuildingIds.Count; producerIndex++)
        {
            var buildingId = producerBuildingIds[producerIndex];
            if (BuildingIdentity(buildingId)?.PlayerSlotId != playerSlotId)
            {
                continue;
            }

            var queue = BuildingProductionQueue(buildingId);
            for (var queueIndex = 0; queueIndex < queue.Count; queueIndex++)
            {
                result.Add(new ProductionQueueSummaryEntry(buildingId, queue[queueIndex]));
            }
        }
    }

    private static int CompareProductionQueueSummaryEntries(ProductionQueueSummaryEntry left, ProductionQueueSummaryEntry right)
    {
        var itemOrder = left.Item.Id.CompareTo(right.Item.Id);
        return itemOrder != 0 ? itemOrder : left.BuildingId.CompareTo(right.BuildingId);
    }
}
