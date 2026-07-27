using Godot;

namespace ProceduralRts.Core;

public static class BuildingEntityFactory
{
    public static EntitySpec ToEntitySpec(this BuildSpec spec)
    {
        var tags = TagsFor(spec);
        return new EntitySpec
        {
            Id = spec.EntitySpecId,
            Kind = spec.WeaponId is null ? EntityKind.Building : EntityKind.Turret,
            Display = new EntityDisplaySpec(
                spec.Label,
                spec.NameKey,
                spec.RoleKey,
                spec.ShortCode,
                spec.Icon),
            Tags = tags,
            Stats = new StatsSpec(
                UnitWeightClass.Heavy,
                spec.ArmorTag,
                spec.MaxHp,
                spec.SightRange,
                spec.Cost,
                TechTierFor(spec.Kind),
                spec.ElementDefense,
                spec.TargetTraits),
            Collision = new CollisionSpec(
                Mathf.Max(spec.LogicalFootprint().X, spec.LogicalFootprint().Y) * 0.5f,
                8,
                100,
                BlocksMovement: true),
            Weapons = spec.WeaponId is null
                ? []
                : [WeaponMountSpec.Omni("main", spec.WeaponId, Vector2.Zero, fireWhileMoving: false)],
            Production = null,
            Authoring = new EntityAuthoringMetadata(
                BuildingSpecId: spec.Kind,
                TechTier: TechTierFor(spec.Kind),
                RosterTags: tags),
        };
    }

    public static EntityInstance SpawnBuildingTarget(
        this EntityWorld world,
        BuildingEntitySeed seed,
        BuildSpec spec,
        Vector2? rallyPoint = null,
        float rallyPulse = 0,
        float hitPulse = 0,
        float deliveryPulse = 0,
        bool powered = true,
        float buildProgress = 1,
        int? dockReservedByEntityId = null,
        int? dockedEntityId = null,
        WeaponUserComponentState? weaponState = null,
        string? repeatOutputSpecId = null)
    {
        return world.Spawn(
            spec.ToEntitySpec(),
            OwnerId.FromPlayerSlot(seed.PlayerSlotId),
            EntityTransform.At(seed.Position, seed.Facing),
            CreateBuildingComponents(
                seed,
                spec,
                rallyPoint: rallyPoint,
                rallyPulse: rallyPulse,
                hitPulse: hitPulse,
                deliveryPulse: deliveryPulse,
                powered: powered,
                buildProgress: buildProgress,
                dockReservedByEntityId: dockReservedByEntityId,
                dockedEntityId: dockedEntityId,
                weaponState: weaponState,
                repeatOutputSpecId: repeatOutputSpecId));
    }

    public static IReadOnlyList<EntityComponentState> ToEntityComponents(
        this BuildingEntitySeed seed,
        BuildSpec spec,
        bool selected = false,
        float selectableAlertPulse = 0,
        IReadOnlyList<UnitProductionQueueItem>? productionQueue = null,
        Vector2? rallyPoint = null,
        float rallyPulse = 0,
        float hitPulse = 0,
        float deliveryPulse = 0,
        bool powered = true,
        float buildProgress = 1,
        int? dockReservedByEntityId = null,
        int? dockedEntityId = null,
        WeaponUserComponentState? weaponState = null,
        string? repeatOutputSpecId = null)
    {
        return CreateBuildingComponents(
            seed,
            spec,
            selected,
            selectableAlertPulse,
            productionQueue,
            rallyPoint,
            rallyPulse,
            hitPulse,
            deliveryPulse,
            powered,
            buildProgress,
            dockReservedByEntityId,
            dockedEntityId,
            weaponState,
            repeatOutputSpecId);
    }

    private static EntityComponentState[] CreateBuildingComponents(
        BuildingEntitySeed seed,
        BuildSpec spec,
        bool selected = false,
        float selectableAlertPulse = 0,
        IReadOnlyList<UnitProductionQueueItem>? productionQueue = null,
        Vector2? rallyPoint = null,
        float rallyPulse = 0,
        float hitPulse = 0,
        float deliveryPulse = 0,
        bool powered = true,
        float buildProgress = 1,
        int? dockReservedByEntityId = null,
        int? dockedEntityId = null,
        WeaponUserComponentState? weaponState = null,
        string? repeatOutputSpecId = null)
    {
        productionQueue ??= [];
        var logicalFootprint = spec.LogicalFootprint(seed.Facing);
        var components = new EntityComponentState[BuildingComponentCount(seed, spec, productionQueue, rallyPoint, repeatOutputSpecId)];
        var index = 0;
        components[index++] = new BuildingIdentityComponentState(
            seed.Id,
            seed.Kind,
            seed.PlayerSlotId,
            seed.Faction);
        components[index++] = new HealthComponentState(seed.Hp, spec.MaxHp);
        components[index++] = new SelectableComponentState(selected, selectableAlertPulse);
        components[index++] = new VisionComponentState(spec.SightRange);
        components[index++] = new CollisionComponentState(
            Mathf.Max(logicalFootprint.X, logicalFootprint.Y) * 0.5f,
            8,
            100,
            BlocksMovement: true);
        components[index++] = new FootprintComponentState(
            logicalFootprint,
            spec.PlacementDomain);
        components[index++] = new ConstructionComponentState(
            buildProgress,
            spec.BuildTime,
            spec.Cost,
            spec.RefundRatio);
        components[index++] = new PowerComponentState(
            spec.PowerProvided,
            spec.PowerUsed,
            powered);
        if (rallyPoint is not null)
        {
            components[index++] = new RallyPointComponentState(rallyPoint);
        }

        components[index++] = new PresentationPulseComponentState(
            CommandPulse: rallyPulse,
            AlertPulse: deliveryPulse,
            HitPulse: hitPulse);

        if (spec.WeaponId is { } weaponId)
        {
            components[index++] = weaponState ?? new WeaponUserComponentState(
                CreateWeaponMountStates(weaponId, seed.Facing));
        }

        if (seed.Kind == BuildingDesignIds.Refinery)
        {
            components[index++] = new DockComponentState(dockReservedByEntityId, dockedEntityId);
        }

        if (productionQueue.Count > 0 || !string.IsNullOrWhiteSpace(repeatOutputSpecId) || ProducesUnits(seed.Kind))
        {
            components[index++] = new ProductionQueueComponentState(CreateProductionQueueItems(productionQueue), RepeatOutputSpecId: repeatOutputSpecId);
        }

        if (spec.BuildRadius > 0)
        {
            components[index++] = new BuildRadiusComponentState(spec.BuildRadius);
        }

        return components;
    }

    private static int BuildingComponentCount(
        BuildingEntitySeed seed,
        BuildSpec spec,
        IReadOnlyList<UnitProductionQueueItem> productionQueue,
        Vector2? rallyPoint,
        string? repeatOutputSpecId)
    {
        var count = 9;
        if (rallyPoint is not null)
        {
            count++;
        }

        if (spec.WeaponId is not null)
        {
            count++;
        }

        if (seed.Kind == BuildingDesignIds.Refinery)
        {
            count++;
        }

        if (productionQueue.Count > 0 || !string.IsNullOrWhiteSpace(repeatOutputSpecId) || ProducesUnits(seed.Kind))
        {
            count++;
        }

        if (spec.BuildRadius > 0)
        {
            count++;
        }

        return count;
    }

    private static WeaponMountRuntimeState[] CreateWeaponMountStates(string weaponId, float facing)
    {
        var states = new WeaponMountRuntimeState[1];
        states[0] = new WeaponMountRuntimeState("main", weaponId, facing, 0);
        return states;
    }

    private static UnitProductionQueueItem[] CreateProductionQueueItems(IReadOnlyList<UnitProductionQueueItem> items)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var copy = new UnitProductionQueueItem[items.Count];
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            copy[index] = new UnitProductionQueueItem
            {
                Id = item.Id,
                DesignId = item.DesignId,
                Faction = item.Faction,
                Progress = item.Progress,
            };
        }

        return copy;
    }

    private static HashSet<string> TagsFor(BuildSpec spec)
    {
        var tags = new HashSet<string>
        {
            "Structure",
            spec.Kind.ToString(),
            spec.Category.ToString(),
        };

        if (spec.WeaponId is not null)
        {
            tags.Add("Turret");
            tags.Add("Weapon");
        }

        if (spec.PowerProvided > 0)
        {
            tags.Add("PowerProvider");
        }

        if (spec.PowerUsed > 0)
        {
            tags.Add("PowerConsumer");
        }

        if (ProducesUnits(spec.Kind))
        {
            tags.Add("Producer");
        }

        if (spec.Kind == BuildingDesignIds.Refinery)
        {
            tags.Add("Dock");
            tags.Add("Economy");
        }

        return tags;
    }

    private static bool ProducesUnits(string kind)
    {
        return kind is BuildingDesignIds.Barracks
            or BuildingDesignIds.VehicleFactory
            or BuildingDesignIds.Airfield;
    }

    private static int TechTierFor(string kind)
    {
        return kind switch
        {
            BuildingDesignIds.Headquarters => 0,
            BuildingDesignIds.PowerPlant => 0,
            BuildingDesignIds.Barracks => 3,
            BuildingDesignIds.VehicleFactory => 3,
            BuildingDesignIds.Airfield => 3,
            BuildingDesignIds.Refinery => 1,
            _ => 0,
        };
    }

}
