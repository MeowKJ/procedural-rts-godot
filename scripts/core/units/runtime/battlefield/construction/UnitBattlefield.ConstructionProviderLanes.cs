namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public IReadOnlyList<ProductionProviderLaneState> ConstructionProviderLaneStates(PlayerSlotId playerSlotId)
    {
        _constructionProviderLaneStateBuffer.Clear();
        _specificConstructionProviderLaneBuffer.Clear();
        _constructionProviderLaneKindCounts.Clear();
        CollectConstructionProviderKinds(_constructionProviderKinds);
        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        CollectReadyConstructionTickets(playerSlotId, includeQueued: true, _constructionTicketBuffer);

        var providerCount = 0;
        var availableCount = 0;
        for (var index = 0; index < _buildingTargetIdBuffer.Count; index++)
        {
            var buildingId = _buildingTargetIdBuffer[index];
            if (BuildingSnapshot(buildingId) is not { } building
                || building.PlayerSlotId != playerSlotId
                || building.Hp <= 0
                || !_constructionProviderKinds.Contains(building.Kind))
            {
                continue;
            }

            providerCount++;
            var available = BuildingPowered(building.Id) && BuildingBuildProgress(building.Id) >= 1;
            if (available)
            {
                availableCount++;
            }

            var spec = BuildSpecCatalog.For(building.Kind);
            var ordinal = NextConstructionProviderLaneOrdinal(building.Kind);
            _specificConstructionProviderLaneBuffer.Add(new ProductionProviderLaneState(
                ProductionProviderLaneScope.Specific,
                building.Id,
                building.Kind,
                GameText.Format("ui.constructionProviderLane.specific", spec.Label, ordinal),
                $"{spec.ShortCode}{ordinal}",
                1,
                0,
                0,
                available,
                available ? "" : ConstructionProviderLaneDisabledReason(building.Id)));
        }

        var aggregateMetrics = ConstructionTicketMetrics();
        var aggregateAvailable = availableCount > 0;
        var aggregateDisabledReason = aggregateAvailable ? "" : "ui.constructionProviderLane.none";
        _constructionProviderLaneStateBuffer.Add(new ProductionProviderLaneState(
            ProductionProviderLaneScope.Auto,
            0,
            "",
            GameText.T("ui.constructionProviderLane.auto"),
            "AUTO",
            providerCount,
            aggregateMetrics.QueuedCount,
            aggregateMetrics.ActiveProgress,
            aggregateAvailable,
            aggregateDisabledReason));
        _constructionProviderLaneStateBuffer.Add(new ProductionProviderLaneState(
            ProductionProviderLaneScope.All,
            0,
            "",
            GameText.T("ui.constructionProviderLane.all"),
            "ALL",
            providerCount,
            aggregateMetrics.QueuedCount,
            aggregateMetrics.ActiveProgress,
            aggregateAvailable,
            aggregateDisabledReason));
        for (var index = 0; index < _specificConstructionProviderLaneBuffer.Count; index++)
        {
            _constructionProviderLaneStateBuffer.Add(_specificConstructionProviderLaneBuffer[index]);
        }

        return _constructionProviderLaneStateBuffer;
    }

    private static void CollectConstructionProviderKinds(HashSet<string> result)
    {
        result.Clear();
        foreach (var entry in BuildSpecCatalog.Definitions)
        {
            if (entry.Value.RequiredProducer is { } requiredProducer)
            {
                result.Add(requiredProducer);
            }
        }
    }

    private int NextConstructionProviderLaneOrdinal(string providerKind)
    {
        _constructionProviderLaneKindCounts.TryGetValue(providerKind, out var current);
        var next = current + 1;
        _constructionProviderLaneKindCounts[providerKind] = next;
        return next;
    }

    private (int QueuedCount, float ActiveProgress) ConstructionTicketMetrics()
    {
        var queued = 0;
        var activeProgress = 0f;
        for (var index = 0; index < _constructionTicketBuffer.Count; index++)
        {
            queued++;
            activeProgress = MathF.Max(activeProgress, _constructionTicketBuffer[index].Progress);
        }

        return (queued, activeProgress);
    }

    private string ConstructionProviderLaneDisabledReason(int buildingId)
    {
        if (BuildingBuildProgress(buildingId) < 1)
        {
            return "ui.constructionProviderLane.incomplete";
        }

        return BuildingPowered(buildingId) ? "" : "ui.constructionProviderLane.offline";
    }
}
