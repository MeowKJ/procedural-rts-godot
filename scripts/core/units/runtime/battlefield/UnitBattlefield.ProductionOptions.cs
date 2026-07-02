using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private static readonly ProductionKind[] ProductionOptionKinds = Enum.GetValues<ProductionKind>();

    public IReadOnlyList<ProductionOptionState> ProductionOptionStates(PlayerSlotId playerSlotId)
    {
        var credits = Credits(playerSlotId);
        var states = new List<ProductionOptionState>(ProductionOptionKinds.Length);
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
            states.Add(new ProductionOptionState(
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

        states.Sort(CompareLegacyProductionOptionStates);
        return states;
    }

    public IReadOnlyList<ProductionOptionState> ProductionDesignOptionStates(PlayerSlotId playerSlotId)
    {
        var credits = Credits(playerSlotId);
        CollectProductionDesignSpecs(playerSlotId, _productionDesignSpecBuffer);
        var states = new List<ProductionOptionState>(_productionDesignSpecBuffer.Count);
        foreach (var spec in _productionDesignSpecBuffer)
        {
            var production = spec.Production!;
            CollectCandidateProducerIds(spec, playerSlotId, _productionCandidateProducerIds);
            var metrics = ProductionDesignQueueMetrics(spec);
            var presentation = UnitPresentationCatalog.ForProductionSpec(ProductionKindFor(spec), spec);
            var hasProducer = _productionCandidateProducerIds.Count > 0;
            var enoughCredits = credits >= spec.Stats.Cost;
            var disabledReason = hasProducer
                ? enoughCredits ? "" : "ui.needCredits"
                : "ui.producerUnavailable";
            states.Add(new ProductionOptionState(
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

        states.Sort(CompareDesignProductionOptionStates);
        return states;
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
