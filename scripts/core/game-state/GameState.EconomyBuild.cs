using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    public ResourceInventory ResourceInventory(Owner owner)
    {
        return ResourceInventories[owner];
    }

    public int Credits(Owner owner)
    {
        return ResourceInventory(owner).Credits;
    }

    public IReadOnlyList<ProductionOptionState> ProductionOptionStates(Owner owner)
    {
        var credits = Credits(owner);
        CollectProductionSpecsFor(MatchConfig.FactionForOwner(owner), _legacyProductionSpecBuffer);
        var states = new List<ProductionOptionState>(_legacyProductionSpecBuffer.Count);
        foreach (var option in _legacyProductionSpecBuffer)
        {
            var kind = option.Kind;
            var spec = option.Spec;
            var production = option.Production;
            var presentation = UnitPresentationCatalog.ForProductionSpec(kind, spec);
            var metrics = ProductionOptionMetrics(owner, spec.Id, production);
            var hasProducer = metrics.ProducerCount > 0;
            var enoughCredits = credits >= spec.Stats.Cost;
            var disabledReason = hasProducer
                ? enoughCredits ? "" : "ui.needCredits"
                : "ui.producerUnavailable";
            states.Add(new ProductionOptionState(
                kind,
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

        return states;
    }

    private static void CollectProductionSpecsFor(
        FactionId faction,
        List<(ProductionKind Kind, UnitSpec Spec, ProductionSpec Production)> result)
    {
        result.Clear();
        foreach (var spec in ProductionKindDesignBridge.PlayableProductionSpecs(ProductionKindDesignBridge.UnitFactionFor(faction)))
        {
            if (spec.Production is null)
            {
                continue;
            }

            result.Add((ProductionKindDesignBridge.ProductionKindFor(spec), spec, spec.Production));
        }

        result.Sort(CompareLegacyProductionSpecs);
    }

    private (int ProducerCount, int QueuedCount, float ActiveProgress) ProductionOptionMetrics(
        Owner owner,
        string designId,
        ProductionSpec production)
    {
        var producers = 0;
        var queued = 0;
        var progress = 0f;
        foreach (var building in Buildings)
        {
            if (building.Owner != owner
                || building.Kind != production.ProducerKind
                || building.Hp <= 0
                || !building.Powered
                || building.BuildProgress < 1)
            {
                continue;
            }

            producers++;
            for (var index = 0; index < building.ProductionQueue.Count; index++)
            {
                if (building.ProductionQueue[index].DesignId == designId)
                {
                    queued++;
                }
            }

            if (building.ProductionQueue.Count > 0
                && building.ProductionQueue[0].DesignId == designId)
            {
                progress = Mathf.Max(progress, Mathf.Clamp(building.ProductionQueue[0].Progress / production.Duration, 0, 1));
            }
        }

        return (producers, queued, progress);
    }

    private static int CompareLegacyProductionSpecs(
        (ProductionKind Kind, UnitSpec Spec, ProductionSpec Production) left,
        (ProductionKind Kind, UnitSpec Spec, ProductionSpec Production) right)
    {
        var categoryOrder = left.Production.Category.CompareTo(right.Production.Category);
        if (categoryOrder != 0)
        {
            return categoryOrder;
        }

        var laneOrder = left.Production.LaneIndex.CompareTo(right.Production.LaneIndex);
        return laneOrder != 0 ? laneOrder : left.Kind.CompareTo(right.Kind);
    }

    private bool TryFindLeastQueuedProductionProducer(
        Owner owner,
        ProductionKind productionKind,
        out BuildingModel? bestProducer,
        out UnitSpec? bestSpec,
        out ProductionSpec? bestProduction)
    {
        bestProducer = null;
        bestSpec = null;
        bestProduction = null;
        var bestQueueCount = int.MaxValue;
        foreach (var building in Buildings)
        {
            if (!ProductionKindDesignBridge.TrySpecFor(building.FactionId, productionKind, out var spec)
                || spec.Production is not { } production)
            {
                continue;
            }

            if (building.Owner != owner
                || building.Hp <= 0
                || !building.Powered
                || building.BuildProgress < 1
                || production.ProducerKind != building.Kind)
            {
                continue;
            }

            var queueCount = building.ProductionQueue.Count;
            if (bestProducer is not null
                && (queueCount > bestQueueCount
                    || (queueCount == bestQueueCount && building.Id >= bestProducer.Id)))
            {
                continue;
            }

            bestProducer = building;
            bestSpec = spec;
            bestProduction = production;
            bestQueueCount = queueCount;
        }

        return bestProducer is not null;
    }

    public void SetCredits(Owner owner, int credits)
    {
        var inventory = ResourceInventory(owner);
        inventory.Credits = Mathf.Max(0, credits);
        ResourceInventoryChanged?.Invoke(owner, inventory);
    }

    public PlacementResult ValidateBuildingPlacement(string kind, Vector2 desiredPosition)
    {
        var spec = BuildSpecCatalog.For(kind);
        CollectBuildingObstacles(_legacyPlacementObstacles);
        return PlacementMath.Validate(
            desiredPosition.X,
            desiredPosition.Y,
            spec.Footprint.X,
            spec.Footprint.Y,
            WorldSize.X,
            WorldSize.Y,
            _legacyPlacementObstacles);
    }

    public PlacementResult ValidateBuildingPlacement(string kind, Owner owner, Vector2 desiredPosition)
    {
        var spec = BuildSpecCatalog.For(kind);
        var requiresBuildAuthority = spec.RequiredProducer is not null || spec.RequiredBuildings.Count > 0;
        CollectBuildingObstacles(_legacyPlacementObstacles);
        return PlacementMath.ValidateBuildableArea(
            desiredPosition.X,
            desiredPosition.Y,
            spec.Footprint.X,
            spec.Footprint.Y,
            WorldSize.X,
            WorldSize.Y,
            spec.PlacementDomain,
            BuildPlacementAnchors(owner),
            _legacyPlacementObstacles,
            requiresBuildAuthority: requiresBuildAuthority,
            padding: 12);
    }

    private List<PlacementBuildAnchor> BuildPlacementAnchors(Owner owner)
    {
        CollectBuildingBuildAnchors(owner, _legacyPlacementBuildAnchors);
        return _legacyPlacementBuildAnchors;
    }

    public BuildingModel? PlaceBuilding(string kind, Owner owner, Vector2 desiredPosition, float facing = 0)
    {
        var placement = ValidateBuildingPlacement(kind, desiredPosition);
        if (!placement.IsValid)
        {
            return null;
        }

        return AddBuilding(kind, owner, new Vector2(placement.X, placement.Y), facing);
    }

    public BuildingModel? PlaceBuildingWithinBuildRadius(string kind, Owner owner, Vector2 desiredPosition, float facing = 0)
    {
        var placement = ValidateBuildingPlacement(kind, owner, desiredPosition);
        if (!placement.IsValid)
        {
            return null;
        }

        return AddBuilding(kind, owner, new Vector2(placement.X, placement.Y), facing);
    }

    public bool EnqueueProduction(ProductionKind productionKind, Owner owner, out string status)
    {
        var requestedFaction = ProductionKindDesignBridge.UnitFactionFor(MatchConfig.FactionForOwner(owner));
        var requestedSpec = ProductionKindDesignBridge.SpecFor(requestedFaction, productionKind);
        var requestedProduction = requestedSpec.Production
            ?? throw new InvalidOperationException($"UnitDesign '{requestedSpec.Id}' cannot be queued because it has no ProductionSpec.");
        if (!TryFindLeastQueuedProductionProducer(
                owner,
                productionKind,
                out var producer,
                out var spec,
                out _))
        {
            status = GameText.Format("production.needProducer", BuildSpecCatalog.For(requestedProduction.ProducerKind).Label, requestedSpec.Label);
            return false;
        }

        var inventory = ResourceInventory(owner);
        if (inventory.Credits < spec!.Stats.Cost)
        {
            status = GameText.Format("production.needCredits", spec.Stats.Cost, spec.Label, inventory.Credits);
            return false;
        }

        var item = new ProductionQueueItem
        {
            Id = _nextProductionId++,
            Kind = productionKind,
            DesignId = spec.Id,
            FactionId = producer!.FactionId,
        };
        inventory.Credits -= spec.Stats.Cost;
        producer.ProductionQueue.Add(item);
        ProductionQueued?.Invoke(producer, item);
        ResourceInventoryChanged?.Invoke(owner, inventory);
        status = GameText.Format("production.queued", spec.Label, BuildSpecCatalog.For(producer.Kind).Label, spec.Stats.Cost, inventory.Credits);
        return true;
    }

    public bool CancelFirstProduction(Owner owner, out string status)
    {
        var producer = TryFindFirstQueuedProductionProducer(owner);

        if (producer is null)
        {
            status = GameText.T("production.noneQueued");
            return false;
        }

        var item = producer.ProductionQueue[0];
        var spec = UnitDesignCatalog.Spec(item.DesignId);
        var refund = Mathf.RoundToInt(spec.Stats.Cost * ProductionRefundRatio);
        producer.ProductionQueue.RemoveAt(0);
        var inventory = ResourceInventory(owner);
        inventory.Credits += refund;
        ResourceInventoryChanged?.Invoke(owner, inventory);
        status = GameText.Format("production.cancelled", spec.Label, refund);
        return true;
    }

    private BuildingModel? TryFindFirstQueuedProductionProducer(Owner owner)
    {
        BuildingModel? best = null;
        var bestQueueItemId = int.MaxValue;
        foreach (var building in Buildings)
        {
            if (building.Owner != owner || building.ProductionQueue.Count == 0)
            {
                continue;
            }

            var queueItemId = building.ProductionQueue[0].Id;
            if (queueItemId >= bestQueueItemId)
            {
                continue;
            }

            best = building;
            bestQueueItemId = queueItemId;
        }

        return best;
    }
}
