using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void StepCombatBridgeWithProjectiles(SimContext context, ISimSystem combatSystem)
    {
        combatSystem.Step(context);
        _entityWorld.FlushQueuedSpawns();
        _projectileSystem.Step(context);
    }

    private void UpdateConstructionFromEntityWorld(float dt)
    {
        if (!_entityWorld.OrderedEntities.Any(entity =>
            entity.Components.TryGet<ConstructionComponentState>(out var construction)
            && construction.Progress < 1
            && construction.Phase is ConstructionPhase.Building or ConstructionPhase.Queued))
        {
            return;
        }

        SyncOwnerRelations();
        SyncBuildingTargetEntities();
        SyncUnitEntities();
        _constructionSystem.Step(new SimContext(
            _entityWorld,
            NextInputCommandTick(),
            dt,
            Array.Empty<SequencedCommandEnvelope>()));
        AdoptUnmappedConstructedBuildings();
    }

    private void CollectCandidateProducerIds(ProductionKind productionKind, PlayerSlotId playerSlotId, List<int> result)
    {
        result.Clear();
        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        foreach (var buildingId in _buildingTargetIdBuffer)
        {
            if (BuildingSnapshot(buildingId) is not { } building
                || building.PlayerSlotId != playerSlotId
                || building.Hp <= 0
                || !BuildingPowered(building.Id)
                || BuildingBuildProgress(building.Id) < 1
                || ProductionDesignIdCore(building.Id, productionKind) is not { } designId
                || UnitDesignCatalog.Spec(designId).Production?.ProducerKind != building.Kind)
            {
                continue;
            }

            result.Add(building.Id);
        }
    }

    private void CollectCandidateProducerIds(UnitSpec spec, PlayerSlotId playerSlotId, List<int> result)
    {
        result.Clear();
        if (spec.Production is null)
        {
            return;
        }

        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        foreach (var buildingId in _buildingTargetIdBuffer)
        {
            if (BuildingSnapshot(buildingId) is not { } building
                || building.PlayerSlotId != playerSlotId
                || building.Faction != spec.Faction
                || building.Hp <= 0
                || !BuildingPowered(building.Id)
                || BuildingBuildProgress(building.Id) < 1
                || building.Kind != spec.Production.ProducerKind
                || ProducerTechTier(building.Kind) < spec.Stats.TechTier)
            {
                continue;
            }

            result.Add(building.Id);
        }
    }

    private string? ProductionDesignIdCore(int buildingId, ProductionKind productionKind)
    {
        var identity = BuildingIdentity(buildingId);
        return identity is null
            ? null
            : UnitDesignRuntimeLoadouts.ProductionDesignId(identity.Faction, productionKind);
    }

    private string? FirstDesignIdFor(ProductionKind productionKind, PlayerSlotId playerSlotId)
    {
        return UnitDesignRuntimeLoadouts.ProductionDesignId(FactionForSlot(playerSlotId), productionKind);
    }

    private bool HasAnyProductionForCore(int buildingId)
    {
        var identity = BuildingIdentity(buildingId);
        if (identity is null)
        {
            return false;
        }

        return UnitDesignFactionRosterCatalog.For(identity.Faction)
            .PlayableDesignIds
            .Select(UnitDesignCatalog.Spec)
            .Any(spec => spec.Production?.ProducerKind == identity.Kind
                && ProducerTechTier(identity.Kind) >= spec.Stats.TechTier);
    }

    private static int ProducerTechTier(string kind)
    {
        return kind switch
        {
            BuildingDesignIds.Barracks => 3,
            BuildingDesignIds.VehicleFactory => 3,
            BuildingDesignIds.Airfield => 3,
            _ => 1,
        };
    }

    private static ProductionKind ProductionKindFor(UnitSpec spec)
    {
        return ProductionKindDesignBridge.ProductionKindFor(spec);
    }

    private void CollectBuildingBuildAnchors(PlayerSlotId playerSlotId, List<PlacementBuildAnchor> result)
    {
        result.Clear();
        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        foreach (var buildingId in _buildingTargetIdBuffer)
        {
            if (BuildingSnapshot(buildingId) is not { } building
                || building.PlayerSlotId != playerSlotId
                || building.Hp <= 0
                || BuildingBuildProgress(building.Id) < 1)
            {
                continue;
            }

            var spec = BuildSpecCatalog.For(building.Kind);
            if (spec.BuildRadius <= 0)
            {
                continue;
            }

            result.Add(new PlacementBuildAnchor(
                building.Position.X,
                building.Position.Y,
                spec.BuildRadius,
                BuildingPowered(building.Id)));
        }
    }

    private void CollectBuildingPlacementObstacles(List<PlacementObstacle> result)
    {
        result.Clear();
        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        foreach (var buildingId in _buildingTargetIdBuffer)
        {
            if (BuildingSnapshot(buildingId) is not { } building || building.Hp <= 0)
            {
                continue;
            }

            var footprint = BuildSpecCatalog.For(building.Kind).Footprint;
            var rect = PlacementMath.RectFromCenter(
                building.Position.X,
                building.Position.Y,
                footprint.X,
                footprint.Y);
            result.Add(new PlacementObstacle(rect.X, rect.Y, rect.Width, rect.Height));
        }
    }

    private TerrainLayer TerrainLayerAt(float x, float y)
    {
        var kind = TerrainFloorMath.KindAt(new Vector2(x, y), WorldSize);
        return kind switch
        {
            TerrainFloorKind.Water => TerrainLayer.Water,
            TerrainFloorKind.Coast => TerrainLayer.Coast,
            _ => TerrainLayer.Ground,
        };
    }

    private float BuildingTargetRadiusCore(int buildingId)
    {
        var identity = BuildingIdentity(buildingId);
        return BuildingTargetRadiusCore(buildingId, identity?.Kind);
    }

    private float BuildingTargetRadiusCore(int buildingId, string? fallbackKind)
    {
        var projectedRadius = BuildingPresentationProjection(buildingId)?.Radius;
        if (projectedRadius is float radius)
        {
            return radius;
        }

        return fallbackKind is null
            ? 0
            : BuildSpecRadius(fallbackKind);
    }

    private static float BuildSpecRadius(string kind)
    {
        var footprint = BuildSpecCatalog.For(kind).Footprint;
        return Mathf.Max(footprint.X, footprint.Y) * 0.5f;
    }

    private static string ProductionLabel(ProductionKind productionKind, UnitSpec? spec)
    {
        if (spec is null)
        {
            return productionKind.ToString();
        }

        return UnitDesignDefinitionCatalog.RuntimeDescriptors.TryGetValue(spec.Id, out var descriptor)
            ? descriptor.Label
            : spec.Label;
    }

    private static string ProducerLabelFor(UnitSpec? spec)
    {
        var producerKind = spec?.Production?.ProducerKind ?? BuildingDesignIds.Barracks;
        return BuildSpecCatalog.For(producerKind).Label;
    }

    private Vector2 ClampInsideWorld(Vector2 point, float margin)
    {
        return new Vector2(
            Mathf.Clamp(point.X, margin, Mathf.Max(margin, WorldSize.X - margin)),
            Mathf.Clamp(point.Y, margin, Mathf.Max(margin, WorldSize.Y - margin)));
    }

    private static Color PlayerSlotAccent(PlayerSlotId playerSlotId)
    {
        return playerSlotId.Value switch
        {
            1 => new Color("#68a6c8"),
            2 => new Color("#c86c68"),
            3 => new Color("#8abf74"),
            4 => new Color("#c5a45d"),
            _ => new Color("#b7ad9c"),
        };
    }
}
