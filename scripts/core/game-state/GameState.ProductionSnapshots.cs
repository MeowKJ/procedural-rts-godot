using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    public IReadOnlyList<ProductionLaneSnapshot> ProductionLaneSnapshots(Owner owner)
    {
        CollectProductionLaneSnapshots(owner, _legacyProductionLaneSnapshotBuffer);
        return _legacyProductionLaneSnapshotBuffer;
    }

    public IReadOnlyList<BuildOptionSnapshot> BuildOptionSnapshots(Owner owner)
    {
        CollectBuildOptionSnapshots(owner, _legacyBuildOptionSnapshotBuffer);
        return _legacyBuildOptionSnapshotBuffer;
    }

    private void CollectProductionLaneSnapshots(Owner owner, List<ProductionLaneSnapshot> result)
    {
        result.Clear();
        foreach (var building in Buildings)
        {
            if (building.Owner != owner || !IsProductionBuilding(building))
            {
                continue;
            }

            var queue = ProductionQueueSnapshotBufferFor(building.Id);
            CollectProductionQueueSnapshots(building, queue);
            result.Add(new ProductionLaneSnapshot(
                building.Id,
                building.Kind,
                building.FactionId,
                BuildSpecCatalog.For(building.Kind).Label,
                building.Powered,
                building.BuildProgress >= 1,
                building.RallyPoint,
                queue));
        }

        result.Sort(CompareProductionLaneSnapshots);
    }

    private List<ProductionQueueSnapshot> ProductionQueueSnapshotBufferFor(int producerId)
    {
        if (!_legacyProductionQueueSnapshotBuffers.TryGetValue(producerId, out var buffer))
        {
            buffer = [];
            _legacyProductionQueueSnapshotBuffers[producerId] = buffer;
        }

        return buffer;
    }

    private static void CollectProductionQueueSnapshots(BuildingModel building, List<ProductionQueueSnapshot> result)
    {
        result.Clear();
        for (var index = 0; index < building.ProductionQueue.Count; index++)
        {
            var item = building.ProductionQueue[index];
            var spec = UnitDesignCatalog.Spec(item.DesignId);
            var production = spec.Production
                ?? throw new InvalidOperationException($"UnitDesign '{spec.Id}' cannot describe production queue item {item.Kind}.");
            _ = UnitPresentationCatalog.ForProductionSpec(item.Kind, spec);
            result.Add(new ProductionQueueSnapshot(
                item.Id,
                item.Kind,
                spec.Id,
                item.FactionId,
                Mathf.Clamp(item.Progress / production.Duration, 0, 1),
                spec.Stats.Cost,
                Mathf.RoundToInt(spec.Stats.Cost * ProductionRefundRatio),
                index == 0));
        }
    }

    private void CollectBuildOptionSnapshots(Owner owner, List<BuildOptionSnapshot> result)
    {
        result.Clear();
        CollectOwnedReadyBuildingKinds(owner, _legacyReadyBuildingKinds);
        var credits = Credits(owner);
        foreach (var entry in BuildSpecCatalog.Definitions)
        {
            var spec = entry.Value;
            var hasPrerequisites = HasBuildPrerequisites(spec, _legacyReadyBuildingKinds);
            var canAfford = credits >= spec.Cost;
            var disabledReason = hasPrerequisites
                ? canAfford ? "" : "ui.needCredits"
                : "build.disabled.prerequisites";
            result.Add(new BuildOptionSnapshot(
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
                spec.BuildRadius));
        }

        result.Sort(CompareBuildOptionSnapshots);
    }

    private void CollectOwnedReadyBuildingKinds(Owner owner, HashSet<string> result)
    {
        result.Clear();
        foreach (var building in Buildings)
        {
            if (building.Owner == owner && building.Hp > 0 && building.BuildProgress >= 1)
            {
                result.Add(building.Kind);
            }
        }
    }

    private static bool HasBuildPrerequisites(BuildSpec spec, IReadOnlySet<string> ownedReadyBuildings)
    {
        foreach (var required in spec.RequiredBuildings)
        {
            if (!ownedReadyBuildings.Contains(required))
            {
                return false;
            }
        }

        return true;
    }

    private static int CompareProductionLaneSnapshots(ProductionLaneSnapshot left, ProductionLaneSnapshot right)
    {
        var kindOrder = left.ProducerKind.CompareTo(right.ProducerKind);
        return kindOrder != 0 ? kindOrder : left.ProducerId.CompareTo(right.ProducerId);
    }

    private static int CompareBuildOptionSnapshots(BuildOptionSnapshot left, BuildOptionSnapshot right)
    {
        var categoryOrder = left.Category.CompareTo(right.Category);
        return categoryOrder != 0 ? categoryOrder : left.Kind.CompareTo(right.Kind);
    }
}
