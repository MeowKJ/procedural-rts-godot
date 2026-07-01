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
        return ProductionSpecsFor(MatchConfig.FactionForOwner(owner))
            .OrderBy(option => option.Production.Category)
            .ThenBy(option => option.Production.LaneIndex)
            .ThenBy(option => option.Kind)
            .Select(option =>
            {
                var kind = option.Kind;
                var spec = option.Spec;
                var production = option.Production;
                var producers = Buildings
                    .Where(building => building.Owner == owner)
                    .Where(building => building.Kind == production.ProducerKind)
                    .Where(building => building.Hp > 0 && building.Powered && building.BuildProgress >= 1)
                    .ToList();
                var presentation = UnitPresentationCatalog.ForProductionSpec(kind, spec);
                var queued = producers.Sum(building => building.ProductionQueue.Count(item => item.DesignId == spec.Id));
                var progress = producers
                    .Select(building => building.ProductionQueue.FirstOrDefault())
                    .Where(item => item is not null && item.DesignId == spec.Id)
                    .Select(item => Mathf.Clamp(item!.Progress / production.Duration, 0, 1))
                    .DefaultIfEmpty(0)
                    .Max();
                var hasProducer = producers.Count > 0;
                var enoughCredits = credits >= spec.Stats.Cost;
                var disabledReason = hasProducer
                    ? enoughCredits ? "" : "ui.needCredits"
                    : "ui.producerUnavailable";
                return new ProductionOptionState(
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
                    queued,
                    progress,
                    disabledReason);
            })
            .ToList();
    }

    private static IEnumerable<(ProductionKind Kind, UnitSpec Spec, ProductionSpec Production)> ProductionSpecsFor(FactionId faction)
    {
        return ProductionKindDesignBridge.PlayableProductionSpecs(ProductionKindDesignBridge.UnitFactionFor(faction))
            .Where(spec => spec.Production is not null)
            .Select(spec => (ProductionKindDesignBridge.ProductionKindFor(spec), spec, spec.Production!));
    }

    private IEnumerable<(BuildingModel Producer, UnitSpec Spec, ProductionSpec Production)> CandidateProductionProducers(Owner owner, ProductionKind productionKind)
    {
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

            yield return (building, spec, production);
        }
    }

    public IReadOnlyList<ProductionLaneSnapshot> ProductionLaneSnapshots(Owner owner)
    {
        return Buildings
            .Where(building => building.Owner == owner && IsProductionBuilding(building))
            .OrderBy(building => building.Kind)
            .ThenBy(building => building.Id)
            .Select(building => new ProductionLaneSnapshot(
                building.Id,
                building.Kind,
                building.FactionId,
                BuildSpecCatalog.For(building.Kind).Label,
                building.Powered,
                building.BuildProgress >= 1,
                building.RallyPoint,
                building.ProductionQueue
                    .Select(item =>
                    {
                        var spec = UnitDesignCatalog.Spec(item.DesignId);
                        var production = spec.Production
                            ?? throw new InvalidOperationException($"UnitDesign '{spec.Id}' cannot describe production queue item {item.Kind}.");
                        var presentation = UnitPresentationCatalog.ForProductionSpec(item.Kind, spec);
                        return new ProductionQueueSnapshot(
                            item.Id,
                            item.Kind,
                            spec.Id,
                            item.FactionId,
                            Mathf.Clamp(item.Progress / production.Duration, 0, 1),
                            spec.Stats.Cost,
                            Mathf.RoundToInt(spec.Stats.Cost * ProductionRefundRatio),
                            building.ProductionQueue[0] == item);
                    })
                    .ToList()))
            .ToList();
    }

    public IReadOnlyList<BuildOptionSnapshot> BuildOptionSnapshots(Owner owner)
    {
        var credits = Credits(owner);
        var ownedReadyBuildings = Buildings
            .Where(building => building.Owner == owner && building.Hp > 0 && building.BuildProgress >= 1)
            .Select(building => building.Kind)
            .ToHashSet();
        return BuildSpecCatalog.Definitions
            .OrderBy(entry => entry.Value.Category)
            .ThenBy(entry => entry.Key)
            .Select(entry =>
            {
                var spec = entry.Value;
                var hasPrerequisites = spec.RequiredBuildings.All(ownedReadyBuildings.Contains);
                var canAfford = credits >= spec.Cost;
                var disabledReason = hasPrerequisites
                    ? canAfford ? "" : "ui.needCredits"
                    : "build.disabled.prerequisites";
                return new BuildOptionSnapshot(
                    spec.Kind,
                    spec.Category,
                    spec.Icon,
                    spec.Cost,
                    spec.BuildTime,
                    spec.Footprint,
                    canAfford,
                    hasPrerequisites,
                    disabledReason,
                    spec.PowerProvided,
                    spec.PowerUsed,
                    spec.BuildRadius);
            })
            .ToList();
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
        return PlacementMath.Validate(
            desiredPosition.X,
            desiredPosition.Y,
            spec.Footprint.X,
            spec.Footprint.Y,
            WorldSize.X,
            WorldSize.Y,
            BuildingObstacles());
    }

    public PlacementResult ValidateBuildingPlacement(string kind, Owner owner, Vector2 desiredPosition)
    {
        var spec = BuildSpecCatalog.For(kind);
        var requiresBuildAuthority = spec.RequiredProducer is not null || spec.RequiredBuildings.Count > 0;
        return PlacementMath.ValidateBuildableArea(
            desiredPosition.X,
            desiredPosition.Y,
            spec.Footprint.X,
            spec.Footprint.Y,
            WorldSize.X,
            WorldSize.Y,
            spec.PlacementDomain,
            BuildingBuildAnchors(owner),
            BuildingObstacles(),
            requiresBuildAuthority: requiresBuildAuthority,
            padding: 12);
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
        var producerOption = CandidateProductionProducers(owner, productionKind)
            .OrderBy(option => option.Producer.ProductionQueue.Count)
            .ThenBy(option => option.Producer.Id)
            .FirstOrDefault();

        if (producerOption.Producer is null)
        {
            status = GameText.Format("production.needProducer", BuildSpecCatalog.For(requestedProduction.ProducerKind).Label, requestedSpec.Label);
            return false;
        }

        var (producer, spec, _) = producerOption;
        var inventory = ResourceInventory(owner);
        if (inventory.Credits < spec.Stats.Cost)
        {
            status = GameText.Format("production.needCredits", spec.Stats.Cost, spec.Label, inventory.Credits);
            return false;
        }

        var item = new ProductionQueueItem
        {
            Id = _nextProductionId++,
            Kind = productionKind,
            DesignId = spec.Id,
            FactionId = producer.FactionId,
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
        var producer = Buildings
            .Where(building => building.Owner == owner)
            .Where(building => building.ProductionQueue.Count > 0)
            .OrderBy(building => building.ProductionQueue[0].Id)
            .FirstOrDefault();

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
}
