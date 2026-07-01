using Godot;

namespace ProceduralRts.Core;

public static class BuildingTargetEntityBridge
{
    public static EntitySpec ToEntitySpec(this BuildSpec spec)
    {
        var tags = TagsFor(spec);
        return new EntitySpec
        {
            Id = spec.EntitySpecId,
            Kind = spec.WeaponKind is null ? EntityKind.Building : EntityKind.Turret,
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
                TechTierFor(spec.Kind)),
            Collision = new CollisionSpec(
                Mathf.Max(spec.Footprint.X, spec.Footprint.Y) * 0.5f,
                8,
                100,
                BlocksMovement: true),
            Weapons = spec.WeaponKind is null
                ? []
                : [WeaponMountSpec.Omni("main", spec.WeaponKind.Value, Vector2.Zero, fireWhileMoving: false)],
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
        WeaponUserComponentState? weaponState = null)
    {
        return world.Spawn(
            spec.ToEntitySpec(),
            OwnerId.FromPlayerSlot(seed.PlayerSlotId),
            EntityTransform.At(seed.Position, seed.Facing),
            InitialBuildingComponents(
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
                weaponState: weaponState));
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
        WeaponUserComponentState? weaponState = null)
    {
        return InitialBuildingComponents(
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
            weaponState).ToArray();
    }

    private static IEnumerable<EntityComponentState> InitialBuildingComponents(
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
        WeaponUserComponentState? weaponState = null)
    {
        productionQueue ??= [];
        yield return new BuildingIdentityComponentState(
            seed.Id,
            seed.Kind,
            seed.PlayerSlotId,
            seed.Faction);
        yield return new HealthComponentState(seed.Hp, spec.MaxHp);
        yield return new SelectableComponentState(selected, selectableAlertPulse);
        yield return new VisionComponentState(spec.SightRange);
        yield return new CollisionComponentState(
            Mathf.Max(spec.Footprint.X, spec.Footprint.Y) * 0.5f,
            8,
            100,
            BlocksMovement: true);
        yield return new FootprintComponentState(
            spec.Footprint,
            spec.PlacementDomain);
        yield return new ConstructionComponentState(
            buildProgress,
            spec.BuildTime,
            spec.Cost,
            spec.RefundRatio);
        yield return new PowerComponentState(
            spec.PowerProvided,
            spec.PowerUsed,
            powered);
        if (rallyPoint is not null)
        {
            yield return new RallyPointComponentState(rallyPoint);
        }

        yield return new PresentationPulseComponentState(
            CommandPulse: rallyPulse,
            AlertPulse: deliveryPulse,
            HitPulse: hitPulse);

        if (spec.WeaponKind is { } weaponKind)
        {
            yield return weaponState ?? new WeaponUserComponentState(
                new[] { new WeaponMountRuntimeState("main", weaponKind, seed.Facing, 0) });
        }

        if (seed.Kind == BuildingDesignIds.Refinery)
        {
            yield return new DockComponentState(dockReservedByEntityId, dockedEntityId);
        }

        if (productionQueue.Count > 0 || ProducesUnits(seed.Kind))
        {
            yield return new ProductionQueueComponentState(productionQueue.ToArray());
        }

        if (spec.BuildRadius > 0)
        {
            yield return new BuildRadiusComponentState(spec.BuildRadius);
        }
    }

    private static HashSet<string> TagsFor(BuildSpec spec)
    {
        var tags = new HashSet<string>
        {
            "Structure",
            spec.Kind.ToString(),
            spec.Category.ToString(),
        };

        if (spec.WeaponKind is not null)
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
        return kind is BuildingDesignIds.Barracks or BuildingDesignIds.VehicleFactory;
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
