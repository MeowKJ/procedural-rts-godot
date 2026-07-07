using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private static readonly ProductionKind[] ProductionOptionKinds = Enum.GetValues<ProductionKind>();

    public IReadOnlyList<ProductionOptionState> ProductionOptionStates(PlayerSlotId playerSlotId)
    {
        var credits = Credits(playerSlotId);
        _legacyProductionOptionStateBuffer.Clear();
        foreach (var kind in ProductionOptionKinds)
        {
            var designId = FirstDesignIdFor(kind, playerSlotId);
            var spec = designId is null ? null : UnitDesignCatalog.Spec(designId);
            var production = spec?.Production;
            var presentation = spec is null ? null : UnitPresentationCatalog.ForProductionSpec(kind, spec);
            CollectCandidateProducerIds(kind, playerSlotId, _productionCandidateProducerIds);
            var metrics = ProductionKindQueueMetrics(kind, spec);
            var cost = spec?.Stats.Cost ?? 0;
            var hasProducer = _productionCandidateProducerIds.Count > 0;
            var enoughCredits = credits >= cost;
            var disabledReason = hasProducer
                ? enoughCredits ? "" : "ui.needCredits"
                : "ui.producerUnavailable";
            _legacyProductionOptionStateBuffer.Add(new ProductionOptionState(
                kind,
                production?.Category ?? ProductionCategory.Infantry,
                production?.ProducerKind ?? BuildingDesignIds.Barracks,
                spec?.Id,
                presentation?.ShortCode ?? kind.ToString(),
                presentation?.Icon ?? production?.CategoryIcon ?? IconGlyph.Infantry,
                presentation?.RoleGlyph ?? spec?.Icon ?? IconGlyph.None,
                presentation?.Accent ?? new Color("#8fffe1"),
                cost,
                production?.Duration ?? 0,
                hasProducer,
                enoughCredits,
                metrics.QueuedCount,
                metrics.ActiveProgress,
                disabledReason));
        }

        _legacyProductionOptionStateBuffer.Sort(CompareLegacyProductionOptionStates);
        return _legacyProductionOptionStateBuffer;
    }

    public IReadOnlyList<ProductionOptionState> ProductionDesignOptionStates(PlayerSlotId playerSlotId)
    {
        return ProductionDesignOptionStates(playerSlotId, Array.Empty<int>());
    }

    public IReadOnlyList<ProductionOptionState> ProductionDesignOptionStatesForSelectedProducers(
        PlayerSlotId playerSlotId,
        IReadOnlyList<int> selectedBuildingIds,
        out bool hasSelectedProducers)
    {
        CollectSelectedProductionProducerIds(playerSlotId, selectedBuildingIds, _selectedProductionProducerIdBuffer);
        hasSelectedProducers = _selectedProductionProducerIdBuffer.Count > 0;
        if (!hasSelectedProducers)
        {
            _designProductionOptionStateBuffer.Clear();
            return _designProductionOptionStateBuffer;
        }

        return ProductionDesignOptionStates(playerSlotId, _selectedProductionProducerIdBuffer);
    }

    public IReadOnlyList<ProductionProviderLaneState> ProductionProviderLaneStates(PlayerSlotId playerSlotId)
    {
        _productionProviderLaneStateBuffer.Clear();
        _specificProductionProviderLaneBuffer.Clear();
        _productionProviderLaneKindCounts.Clear();
        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        var providerCount = 0;
        var availableCount = 0;
        var queueCount = 0;
        var activeProgress = 0f;
        for (var index = 0; index < _buildingTargetIdBuffer.Count; index++)
        {
            var buildingId = _buildingTargetIdBuffer[index];
            if (BuildingSnapshot(buildingId) is not { } building
                || building.PlayerSlotId != playerSlotId
                || building.Hp <= 0
                || !HasAnyProductionForCore(building.Id))
            {
                continue;
            }

            providerCount++;
            var available = BuildingPowered(building.Id) && BuildingBuildProgress(building.Id) >= 1;
            if (available)
            {
                availableCount++;
            }

            var metrics = ProductionProviderQueueMetrics(building.Id);
            queueCount += metrics.QueuedCount;
            activeProgress = MathF.Max(activeProgress, metrics.ActiveProgress);
            var spec = BuildSpecCatalog.For(building.Kind);
            var ordinal = NextProductionProviderLaneOrdinal(building.Kind);
            _specificProductionProviderLaneBuffer.Add(new ProductionProviderLaneState(
                ProductionProviderLaneScope.Specific,
                building.Id,
                building.Kind,
                GameText.Format("ui.providerLane.specific", spec.Label, ordinal),
                $"{spec.ShortCode}{ordinal}",
                1,
                metrics.QueuedCount,
                metrics.ActiveProgress,
                available,
                available ? "" : ProductionProviderLaneDisabledReason(building.Id),
                BuildingProductionRepeatOutputSpecId(building.Id)));
        }

        var aggregateAvailable = availableCount > 0;
        var aggregateDisabledReason = aggregateAvailable ? "" : "ui.producerUnavailable";
        _productionProviderLaneStateBuffer.Add(new ProductionProviderLaneState(
            ProductionProviderLaneScope.Auto,
            0,
            "",
            GameText.T("ui.providerLane.auto"),
            "AUTO",
            providerCount,
            queueCount,
            activeProgress,
            aggregateAvailable,
            aggregateDisabledReason));
        _productionProviderLaneStateBuffer.Add(new ProductionProviderLaneState(
            ProductionProviderLaneScope.All,
            0,
            "",
            GameText.T("ui.providerLane.all"),
            "ALL",
            providerCount,
            queueCount,
            activeProgress,
            aggregateAvailable,
            aggregateDisabledReason));
        for (var index = 0; index < _specificProductionProviderLaneBuffer.Count; index++)
        {
            _productionProviderLaneStateBuffer.Add(_specificProductionProviderLaneBuffer[index]);
        }

        return _productionProviderLaneStateBuffer;
    }

    private IReadOnlyList<ProductionOptionState> ProductionDesignOptionStates(
        PlayerSlotId playerSlotId,
        IReadOnlyList<int> selectedProducerBuildingIds)
    {
        var credits = Credits(playerSlotId);
        var restrictToSelectedProducers = selectedProducerBuildingIds.Count > 0;
        CollectProductionDesignSpecs(playerSlotId, _productionDesignSpecBuffer);
        _designProductionOptionStateBuffer.Clear();
        foreach (var spec in _productionDesignSpecBuffer)
        {
            var production = spec.Production!;
            if (restrictToSelectedProducers && !AnySelectedProducerSupports(spec, playerSlotId, selectedProducerBuildingIds))
            {
                continue;
            }

            if (restrictToSelectedProducers)
            {
                CollectCandidateProducerIds(spec, playerSlotId, selectedProducerBuildingIds, _productionCandidateProducerIds);
            }
            else
            {
                CollectCandidateProducerIds(spec, playerSlotId, _productionCandidateProducerIds);
            }

            var metrics = ProductionDesignQueueMetrics(spec);
            var presentation = UnitPresentationCatalog.ForProductionSpec(ProductionKindFor(spec), spec);
            var hasProducer = _productionCandidateProducerIds.Count > 0;
            var enoughCredits = credits >= spec.Stats.Cost;
            var disabledReason = hasProducer
                ? enoughCredits ? "" : "ui.needCredits"
                : "ui.producerUnavailable";
            _designProductionOptionStateBuffer.Add(new ProductionOptionState(
                ProductionKindFor(spec),
                production.Category,
                production.ProducerKind,
                spec.Id,
                presentation.ShortCode,
                presentation.Icon,
                presentation.RoleGlyph,
                presentation.Accent,
                spec.Stats.Cost,
                production.Duration,
                hasProducer,
                enoughCredits,
                metrics.QueuedCount,
                metrics.ActiveProgress,
                disabledReason));
        }

        _designProductionOptionStateBuffer.Sort(CompareDesignProductionOptionStates);
        return _designProductionOptionStateBuffer;
    }

    private void CollectSelectedProductionProducerIds(PlayerSlotId playerSlotId, IReadOnlyList<int> selectedBuildingIds, List<int> result)
    {
        result.Clear();
        for (var index = 0; index < selectedBuildingIds.Count; index++)
        {
            var buildingId = selectedBuildingIds[index];
            if (BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId
                && HasAnyProductionForCore(buildingId))
            {
                result.Add(buildingId);
            }
        }

        result.Sort(CompareBuildingIds);
    }

    private bool AnySelectedProducerSupports(UnitSpec spec, PlayerSlotId playerSlotId, IReadOnlyList<int> selectedProducerBuildingIds)
    {
        for (var index = 0; index < selectedProducerBuildingIds.Count; index++)
        {
            if (ProducerSupportsSpec(selectedProducerBuildingIds[index], playerSlotId, spec))
            {
                return true;
            }
        }

        return false;
    }

    private void CollectCandidateProducerIds(
        UnitSpec spec,
        PlayerSlotId playerSlotId,
        IReadOnlyList<int> selectedProducerBuildingIds,
        List<int> result)
    {
        result.Clear();
        for (var index = 0; index < selectedProducerBuildingIds.Count; index++)
        {
            var buildingId = selectedProducerBuildingIds[index];
            if (ProducerCanQueueSpec(buildingId, playerSlotId, spec))
            {
                result.Add(buildingId);
            }
        }
    }

    private bool ProducerCanQueueSpec(int buildingId, PlayerSlotId playerSlotId, UnitSpec spec)
    {
        return ProducerSupportsSpec(buildingId, playerSlotId, spec)
            && BuildingPowered(buildingId)
            && BuildingBuildProgress(buildingId) >= 1;
    }

    private bool ProducerSupportsSpec(int buildingId, PlayerSlotId playerSlotId, UnitSpec spec)
    {
        return spec.Production is not null
            && BuildingSnapshot(buildingId) is { } building
            && building.PlayerSlotId == playerSlotId
            && building.Faction == spec.Faction
            && building.Hp > 0
            && building.Kind == spec.Production.ProducerKind
            && ProducerTechTier(building.Kind) >= spec.Stats.TechTier;
    }

    private int NextProductionProviderLaneOrdinal(string producerKind)
    {
        _productionProviderLaneKindCounts.TryGetValue(producerKind, out var current);
        var next = current + 1;
        _productionProviderLaneKindCounts[producerKind] = next;
        return next;
    }

    private (int QueuedCount, float ActiveProgress) ProductionProviderQueueMetrics(int buildingId)
    {
        var queue = BuildingProductionQueue(buildingId);
        var progress = 0f;
        if (queue.Count > 0)
        {
            var spec = UnitDesignCatalog.Spec(queue[0].DesignId);
            if (spec.Production is not null)
            {
                progress = Mathf.Clamp(queue[0].Progress / spec.Production.Duration, 0, 1);
            }
        }

        return (queue.Count, progress);
    }

    private string ProductionProviderLaneDisabledReason(int buildingId)
    {
        if (BuildingBuildProgress(buildingId) < 1)
        {
            return "ui.providerLane.incomplete";
        }

        return BuildingPowered(buildingId) ? "" : "ui.providerLane.offline";
    }

    private void CollectProductionDesignSpecs(PlayerSlotId playerSlotId, List<UnitSpec> result)
    {
        result.Clear();
        foreach (var designId in UnitDesignFactionRosterCatalog.For(FactionForSlot(playerSlotId)).PlayableDesignIds)
        {
            var spec = UnitDesignCatalog.Spec(designId);
            if (spec.Production is not null)
            {
                result.Add(spec);
            }
        }
    }

    private (int QueuedCount, float ActiveProgress) ProductionKindQueueMetrics(ProductionKind kind, UnitSpec? spec)
    {
        var queued = 0;
        var progress = 0f;
        foreach (var buildingId in _productionCandidateProducerIds)
        {
            var queue = BuildingProductionQueue(buildingId);
            for (var index = 0; index < queue.Count; index++)
            {
                if (queue[index].Kind == kind)
                {
                    queued++;
                }
            }

            if (queue.Count == 0
                || queue[0].Kind != kind
                || spec?.Production is null)
            {
                continue;
            }

            var duration = UnitDesignCatalog.Spec(queue[0].DesignId).Production!.Duration;
            progress = Mathf.Max(progress, Mathf.Clamp(queue[0].Progress / duration, 0, 1));
        }

        return (queued, progress);
    }

    private (int QueuedCount, float ActiveProgress) ProductionDesignQueueMetrics(UnitSpec spec)
    {
        var queued = 0;
        var progress = 0f;
        foreach (var buildingId in _productionCandidateProducerIds)
        {
            var queue = BuildingProductionQueue(buildingId);
            for (var index = 0; index < queue.Count; index++)
            {
                if (queue[index].DesignId == spec.Id)
                {
                    queued++;
                }
            }

            if (queue.Count == 0 || queue[0].DesignId != spec.Id)
            {
                continue;
            }

            progress = Mathf.Max(progress, Mathf.Clamp(queue[0].Progress / spec.Production!.Duration, 0, 1));
        }

        return (queued, progress);
    }

    private static int CompareLegacyProductionOptionStates(ProductionOptionState left, ProductionOptionState right)
    {
        var categoryOrder = left.Category.CompareTo(right.Category);
        return categoryOrder != 0 ? categoryOrder : left.Kind.CompareTo(right.Kind);
    }

    private static int CompareDesignProductionOptionStates(ProductionOptionState left, ProductionOptionState right)
    {
        var categoryOrder = left.Category.CompareTo(right.Category);
        if (categoryOrder != 0)
        {
            return categoryOrder;
        }

        var leftSpec = UnitDesignCatalog.Spec(left.UnitDesignId!);
        var rightSpec = UnitDesignCatalog.Spec(right.UnitDesignId!);
        var tierOrder = leftSpec.Stats.TechTier.CompareTo(rightSpec.Stats.TechTier);
        if (tierOrder != 0)
        {
            return tierOrder;
        }

        var producerOrder = string.Compare(left.ProducerKind, right.ProducerKind, StringComparison.Ordinal);
        if (producerOrder != 0)
        {
            return producerOrder;
        }

        var laneOrder = leftSpec.Production!.LaneIndex.CompareTo(rightSpec.Production!.LaneIndex);
        return laneOrder != 0 ? laneOrder : string.Compare(left.UnitDesignId, right.UnitDesignId, StringComparison.Ordinal);
    }
}
