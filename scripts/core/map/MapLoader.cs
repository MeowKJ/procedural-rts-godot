using Godot;

namespace ProceduralRts.Core;

public sealed record MapLoadOptions(
    bool ConfigureLiveSystems = false,
    OwnerId? OutcomeViewer = null);

public static class MapLoader
{
    public static EntityWorld Load(MapSpec spec, ulong? seed = null, MapLoadOptions? options = null)
    {
        var world = new EntityWorld(seed ?? unchecked((ulong)spec.Seed));
        LoadInto(world, spec, options);
        return world;
    }

    public static void LoadInto(EntityWorld world, MapSpec spec, MapLoadOptions? options = null)
    {
        MapBuildingPlacementValidator.EnsureValid(spec);

        world.WorldWidth = spec.WorldSize.Width;
        world.WorldHeight = spec.WorldSize.Height;
        ConfigureOwners(world, spec);

        if (options?.ConfigureLiveSystems == true)
        {
            SimSystemPipeline.ConfigureLiveGameplay(world, options.OutcomeViewer ?? new OwnerId(1));
        }

        var nextBuildingId = 1;
        foreach (var resource in spec.Resources)
        {
            SpawnResource(world, resource);
        }

        foreach (var building in spec.Buildings)
        {
            var buildSpec = BuildSpecCatalog.For(building.Kind);
            var legacyId = building.LegacyId ?? nextBuildingId++;
            world.SpawnBuildingTarget(
                new BuildingEntitySeed(
                    legacyId,
                    building.Kind,
                    building.OwnerId.ToPlayerSlot(),
                    ProductionKindDesignBridge.UnitFactionFor(building.Faction),
                    building.Position.ToVector2(),
                    building.Facing,
                    building.Hp ?? buildSpec.MaxHp),
                buildSpec,
                buildProgress: building.BuildProgress);
        }

        foreach (var unit in spec.Units)
        {
            world.SpawnUnit(
                UnitDesignCatalog.Spec(unit.DesignId),
                unit.OwnerId,
                unit.Position.ToVector2(),
                unit.Facing);
        }

        foreach (var objective in spec.Objectives)
        {
            SpawnObjective(world, objective);
        }
    }

    private static void ConfigureOwners(EntityWorld world, MapSpec spec)
    {
        foreach (var start in spec.OwnerStarts)
        {
            world.ResourceInventory(start.OwnerId).Credits = start.StartingCredits;
        }

        foreach (var first in spec.OwnerStarts)
        {
            foreach (var second in spec.OwnerStarts)
            {
                world.Relations.Set(first.OwnerId, second.OwnerId, PlayerRelation.Hostile);
            }
        }
    }

    private static void SpawnResource(EntityWorld world, MapResourceNodeSpec resource)
    {
        var spec = new EntitySpec
        {
            Id = $"map.resource.{resource.Id}",
            Kind = EntityKind.Resource,
            Display = new EntityDisplaySpec(resource.Id, "resource.field.name", "resource.field.role", "RES", IconGlyph.Harvester),
            Tags = new HashSet<string> { "Resource", "Credit" },
            Collision = new CollisionSpec(resource.Radius, 10, 0, BlocksMovement: false),
        };
        world.Spawn(
            spec,
            OwnerId.None,
            EntityTransform.At(resource.Position.ToVector2()),
            new EntityComponentState[]
            {
                new ResourceNodeComponentState(resource.Amount, resource.Amount),
                new CollisionComponentState(resource.Radius, 10, 0, BlocksMovement: false),
            });
    }

    private static void SpawnObjective(EntityWorld world, MapObjectiveNodeSpec objective)
    {
        world.Spawn(
            new EntitySpec
            {
                Id = $"map.objective.{objective.Id}",
                Kind = EntityKind.Objective,
                Display = new EntityDisplaySpec(objective.Id, objective.ObjectiveKey, "objective.role", "OBJ", IconGlyph.StanceHold),
                Tags = new HashSet<string> { "Objective", objective.Primary ? "Primary" : "Secondary" },
            },
            OwnerId.None,
            EntityTransform.At(objective.Position.ToVector2()));
    }
}
