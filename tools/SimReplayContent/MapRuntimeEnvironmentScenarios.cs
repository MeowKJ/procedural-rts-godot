using Godot;
using ProceduralRts.Core;

static partial class Program
{
    private static void AssertMapRuntimeEnvironment()
    {
        AssertEnvironmentPlacementParity();
        AssertEnvironmentPathingParity();
        AssertEnvironmentRuntimeHash();
        Console.WriteLine("OK [map-runtime-environment]: placement, pathing, and hash share authored environment authority.");
    }

    private static void AssertEnvironmentPlacementParity()
    {
        var owner = new OwnerId(1);
        var system = new ConstructionSystem();
        var power = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
        var desired = new Vector2(320, 336);
        var blockedWorld = PlacementWorld(EnvironmentSpec("runtime.static.blocked") with
        {
            Obstacles = [new("rock", new MapRect(304, 320, 32, 32))],
        }, owner);
        var blocked = system.QueryBuildingPlacement(
            blockedWorld,
            owner,
            power,
            desired,
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!blocked.IsValid && blocked.Reason == "placement.blocked",
            $"authored static hard overlap should map to placement.blocked; got {blocked}");

        var powerSize = power.FootprintCells.WorldSize;
        var hard = PlacementMath.RectFromCenter(desired.X, desired.Y, powerSize.X, powerSize.Y);
        var clearance = power.PlacementClearanceCells * PlacementMath.GridSize;
        var clearanceWorld = PlacementWorld(EnvironmentSpec("runtime.static.clearance") with
        {
            Obstacles = [new("rock", new MapRect(hard.EndX + clearance - 0.001f, hard.Y, 32, 32))],
        }, owner);
        var clearanceResult = system.QueryBuildingPlacement(
            clearanceWorld,
            owner,
            power,
            desired,
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!clearanceResult.IsValid && clearanceResult.Reason == "placement.clearance",
            $"authored static clearance should map to placement.clearance; got {clearanceResult}");

        var terrainWorld = PlacementWorld(EnvironmentSpec("runtime.terrain") with
        {
            TerrainCells =
            [
                GroundCell(),
                new MapTerrainCellSpec("water", new MapRect(256, 272, 128, 128), "water", BlocksLand: true),
            ],
        }, owner);
        var terrain = system.QueryBuildingPlacement(
            terrainWorld,
            owner,
            power,
            desired,
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!terrain.IsValid && terrain.Reason == "placement.impassable",
            $"authored water should map to placement.impassable; got {terrain}");

        const float radius = 16;
        var resourceClearance = MapPlacementRules.ResourceClearance(power);
        var exactX = hard.EndX + radius + resourceClearance;
        var belowResourceWorld = PlacementWorld(EnvironmentSpec("runtime.resource.below") with
        {
            Resources = [Resource("ore", exactX - 0.001f, desired.Y, radius, amount: 0)],
        }, owner);
        var exactResourceWorld = PlacementWorld(EnvironmentSpec("runtime.resource.exact") with
        {
            Resources = [Resource("ore", exactX, desired.Y, radius, amount: 0)],
        }, owner);
        var below = system.QueryBuildingPlacement(
            belowResourceWorld,
            owner,
            power,
            desired,
            0,
            ConstructionPlacementIntent.Direct);
        var exact = system.QueryBuildingPlacement(
            exactResourceWorld,
            owner,
            power,
            desired,
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!below.IsValid && below.Reason == "placement.reserved",
            $"depleted resource hard gap 31.999 should remain reserved; got {below}");
        Assert(exact.IsValid,
            $"depleted resource hard exact 32 clearance should be valid; got {exact}");

        var barracks = BuildSpecCatalog.For(BuildingDesignIds.Barracks);
        var barracksPosition = new Vector2(320, 320);
        var reservation = PlacementReservationMath.WorldRect(
            barracks,
            barracks.PlacementReservations.Single(),
            barracksPosition,
            0);
        var barracksClearance = MapPlacementRules.ResourceClearance(barracks);
        var reservationExactX = reservation.EndX + radius + barracksClearance;
        var reservationBelowWorld = PlacementWorld(EnvironmentSpec("runtime.resource.reservation.below") with
        {
            Resources = [Resource("ore", reservationExactX - 0.001f, reservation.Y + reservation.Height * 0.5f, radius)],
        }, owner);
        var reservationExactWorld = PlacementWorld(EnvironmentSpec("runtime.resource.reservation.exact") with
        {
            Resources = [Resource("ore", reservationExactX, reservation.Y + reservation.Height * 0.5f, radius)],
        }, owner);
        var reservationBelow = system.QueryBuildingPlacement(
            reservationBelowWorld,
            owner,
            barracks,
            barracksPosition,
            0,
            ConstructionPlacementIntent.Direct);
        var reservationExact = system.QueryBuildingPlacement(
            reservationExactWorld,
            owner,
            barracks,
            barracksPosition,
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!reservationBelow.IsValid && reservationBelow.Reason == "placement.reserved",
            $"resource reservation gap 31.999 should reject; got {reservationBelow}");
        Assert(reservationExact.IsValid,
            $"resource reservation exact 32 clearance should be valid; got {reservationExact}");
    }

    private static void AssertEnvironmentPathingParity()
    {
        var staticPath = RunSingleEnvironmentPath(EnvironmentSpec("runtime.path.static") with
        {
            Obstacles = [new("wall", new MapRect(256, 128, 64, 256))],
        });
        Assert(staticPath.Waypoints.Count >= 2 && staticPath.Waypoints.All(AvoidsEnvironmentWall),
            "single-entity path should detour around authored static obstacle cells");

        var terrainPath = RunSingleEnvironmentPath(EnvironmentSpec("runtime.path.terrain") with
        {
            TerrainCells =
            [
                GroundCell(),
                new MapTerrainCellSpec("water.wall", new MapRect(256, 128, 64, 256), "water", BlocksLand: true),
            ],
        });
        Assert(terrainPath.Waypoints.Count >= 2 && terrainPath.Waypoints.All(AvoidsEnvironmentWall),
            "single-entity path should detour around authored terrain override cells");

        var resourcePath = RunSingleEnvironmentPath(EnvironmentSpec("runtime.path.resource") with
        {
            Resources = [Resource("ore", 288, 256, 48, amount: 0)],
        });
        Assert(resourcePath.Waypoints.Count == 1
            && MathF.Abs(resourcePath.Waypoints[0].X - 672) < 0.001f
            && MathF.Abs(resourcePath.Waypoints[0].Y - 256) < 0.001f,
            "resource entities should remain movement-nonblocking even when depleted");

        var shared = EnvironmentSpec("runtime.path.shared") with
        {
            TerrainCells =
            [
                GroundCell(),
                new MapTerrainCellSpec("water.wall", new MapRect(256, 128, 64, 256), "water", BlocksLand: true),
            ],
            Obstacles = [new("wall", new MapRect(320, 128, 64, 256))],
        };
        var sharedPaths = RunSharedEnvironmentPath(shared);
        Assert(sharedPaths.Count == 2
            && sharedPaths.All(path => path.Waypoints.Count >= 2)
            && sharedPaths.SelectMany(path => path.Waypoints).All(AvoidsSharedEnvironmentWalls),
            "shared corridors should consume authored terrain and static obstacle grids");
    }

    private static void AssertEnvironmentRuntimeHash()
    {
        var empty = MapLoader.Load(EnvironmentSpec("runtime.hash.empty"));
        var terrain = MapLoader.Load(EnvironmentSpec("runtime.hash.terrain") with
        {
            TerrainCells = [GroundCell(), new("ground.detail", new MapRect(64, 64, 64, 64), "ground.detail", 0.8f)],
        });
        var obstacle = MapLoader.Load(EnvironmentSpec("runtime.hash.obstacle") with
        {
            Obstacles = [new("rock", new MapRect(64, 64, 64, 64))],
        });
        Assert(new[]
            {
                empty.DeterministicStateHash(),
                terrain.DeterministicStateHash(),
                obstacle.DeterministicStateHash(),
            }
            .Distinct()
            .Count() == 3,
            "loaded runtime environment fields should participate in deterministic hash");
    }

    private static PathfindingComponentState RunSingleEnvironmentPath(MapSpec map)
    {
        var world = MapLoader.Load(map);
        world.AddSystem(new PathfindingSystem(64));
        var mover = SpawnEnvironmentMover(world, new Vector2(96, 256), new Vector2(672, 256));
        world.Step(1, 0.1f, []);
        return mover.Components.Require<PathfindingComponentState>();
    }

    private static IReadOnlyList<PathfindingComponentState> RunSharedEnvironmentPath(MapSpec map)
    {
        var world = MapLoader.Load(map);
        world.AddSystem(new PathfindingSystem(64));
        var intent = new Vector2(672, 256);
        var first = SpawnEnvironmentMover(world, new Vector2(96, 192), new Vector2(672, 192), intent);
        var second = SpawnEnvironmentMover(world, new Vector2(96, 320), new Vector2(672, 320), intent);
        world.Step(1, 0.1f, []);
        return
        [
            first.Components.Require<PathfindingComponentState>(),
            second.Components.Require<PathfindingComponentState>(),
        ];
    }

    private static EntityInstance SpawnEnvironmentMover(
        EntityWorld world,
        Vector2 start,
        Vector2 slot,
        Vector2? sharedIntent = null)
    {
        var spec = new EntitySpec
        {
            Id = $"runtime.environment.mover.{world.Count}",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Environment Mover", "runtime.environment.mover.name", "runtime.environment.mover.role", "EM", IconGlyph.Infantry),
            Movement = new MovementSpec(MovementDomain.Land, Speed: 120, TurnRate: 6),
            Collision = new CollisionSpec(12, 1, 1, BlocksMovement: true),
        };
        return world.Spawn(
            spec,
            new OwnerId(1),
            EntityTransform.At(start),
            new EntityComponentState[]
            {
                new MovementComponentState(default, MoveTarget: slot, FormationSlot: sharedIntent is null ? null : slot),
                new MovementProfileComponentState(120, 6),
                new CollisionComponentState(12, 1, 1, BlocksMovement: true),
                new CommandableComponentState(PlayerIntentTarget: sharedIntent),
            });
    }

    private static EntityWorld PlacementWorld(MapSpec map, OwnerId owner)
    {
        var world = MapLoader.Load(map);
        world.Spawn(
            PlacementAuthoritySpec($"runtime.environment.authority.{map.Id}"),
            owner,
            EntityTransform.At(new Vector2(512, 384)),
            new EntityComponentState[]
            {
                new HealthComponentState(100, 100),
                new VisionComponentState(2000),
                new BuildRadiusComponentState(2000),
                new PowerComponentState(0, 0, Powered: true),
            });
        return world;
    }

    private static MapSpec EnvironmentSpec(string id)
    {
        return new MapSpec
        {
            Id = id,
            Seed = 559,
            WorldSize = new MapSize(768, 512),
            TerrainCells = [GroundCell()],
        };
    }

    private static MapTerrainCellSpec GroundCell()
    {
        return new MapTerrainCellSpec("ground", new MapRect(0, 0, 768, 512), "ground");
    }

    private static MapResourceNodeSpec Resource(
        string id,
        float x,
        float y,
        float radius,
        int amount = 100)
    {
        return new MapResourceNodeSpec(id, new MapPoint(x, y), radius, amount, new MapColor("#ffffff"));
    }

    private static bool AvoidsEnvironmentWall(PathPoint point)
    {
        return MathF.Floor(point.X / 64f) != 4
            || MathF.Floor(point.Y / 64f) is < 2 or > 5;
    }

    private static bool AvoidsSharedEnvironmentWalls(PathPoint point)
    {
        var x = MathF.Floor(point.X / 64f);
        var y = MathF.Floor(point.Y / 64f);
        return x is not (4 or 5) || y is < 2 or > 5;
    }
}
