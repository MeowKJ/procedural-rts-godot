using Godot;
using ProceduralRts.Core;

static partial class PlacementValidationScenarios
{
    private static void ValidateEnvironmentContracts(List<string> failures)
    {
        ValidateEnvironmentReasonOrderAndLayering(failures);
        ValidateEnvironmentSchemaGuards(failures);
        ValidateAuthoredTerrainRasterBounds(failures);
        ValidateStaticObstacleBoundary(failures);
        ValidateResourceBoundaries(failures);
        ValidateEnvironmentHash(failures);
    }

    private static void ValidateEnvironmentReasonOrderAndLayering(List<string> failures)
    {
        var building = Building(BuildingDesignIds.PowerPlant, new MapPoint(320, 336));
        var invalid = EnvironmentMap("qa.environment-reasons", building) with
        {
            TerrainCells =
            [
                new("water.first", new MapRect(0, 0, 1024, 768), "water", BlocksLand: true),
                new("water.last", new MapRect(240, 256, 160, 160), "water", BlocksLand: true),
            ],
            Obstacles =
            [
                new("rock.first", new MapRect(304, 320, 32, 32)),
                new("rock.second", new MapRect(312, 328, 16, 16)),
            ],
            Resources =
            [
                Resource("ore.first", 320, 336, 16),
                Resource("ore.second", 324, 336, 16),
            ],
        };
        var conflicts = MapBuildingPlacementValidator.Validate(invalid);
        var expected = new[]
        {
            MapBuildingPlacementConflictKind.Terrain,
            MapBuildingPlacementConflictKind.StaticObstacle,
            MapBuildingPlacementConflictKind.Resource,
        };
        Require(
            conflicts.Select(conflict => conflict.Conflict).SequenceEqual(expected),
            $"environment conflicts should follow terrain/static/resource order; got {string.Join("; ", conflicts)}.",
            failures);
        Require(
            conflicts.Count == 3
                && conflicts[0].Target?.Id == "water.last"
                && conflicts[1].Target?.Id == "rock.first"
                && conflicts[2].Target?.Id == "ore.first"
                && conflicts.All(conflict => conflict.ToString().Contains("geometry=", StringComparison.Ordinal)),
            "environment diagnostics should preserve last terrain layer, first same-class source, and stable geometry evidence.",
            failures);

        var layeredGround = invalid with
        {
            Id = "qa.environment-last-ground-wins",
            TerrainCells =
            [
                new("water.base", new MapRect(0, 0, 1024, 768), "water", BlocksLand: true),
                new("ground.pad", new MapRect(240, 256, 160, 160), "ground", BlocksLand: false),
            ],
            Obstacles = [],
            Resources = [],
        };
        Require(
            MapBuildingPlacementValidator.Validate(layeredGround).Count == 0,
            "the last authored terrain cell should override earlier containing cells.",
            failures);
    }

    private static void ValidateEnvironmentSchemaGuards(List<string> failures)
    {
        var invalid = new MapSpec
        {
            Id = "qa.environment-schema",
            Seed = 559,
            WorldSize = new MapSize(1024, 768),
            TerrainCells =
            [
                new("terrain.nan", new MapRect(float.NaN, 0, 64, 64), "ground"),
            ],
            Obstacles =
            [
                new("obstacle.infinity", new MapRect(0, 0, float.PositiveInfinity, 64)),
            ],
            Resources =
            [
                Resource("resource.nan", 320, 320, float.NaN),
            ],
        };
        MapBuildingPlacementValidationException? failure = null;
        try
        {
            MapBuildingPlacementValidator.EnsureValid(invalid);
        }
        catch (MapBuildingPlacementValidationException exception)
        {
            failure = exception;
        }

        Require(
            failure is not null
                && failure.EnvironmentConflicts.Select(conflict => conflict.Target.Id)
                    .SequenceEqual(new[] { "terrain.nan", "obstacle.infinity", "resource.nan" })
                && failure.EnvironmentConflicts.All(conflict => conflict.ToString().Contains("geometry=", StringComparison.Ordinal)),
            "finite environment schema guards should reject NaN/Infinity in stable source order with typed evidence.",
            failures);
    }

    private static void ValidateStaticObstacleBoundary(List<string> failures)
    {
        var building = Building(BuildingDesignIds.PowerPlant, new MapPoint(320, 336));
        var spec = BuildSpecCatalog.For(building.Kind);
        var size = spec.FootprintCells.WorldSize;
        var hard = PlacementMath.RectFromCenter(building.Position.X, building.Position.Y, size.X, size.Y);
        var clearance = spec.PlacementClearanceCells * PlacementMath.GridSize;
        var exactX = hard.EndX + clearance;
        var exact = EnvironmentMap("qa.static-exact", building) with
        {
            Obstacles = [new("rock", new MapRect(exactX, hard.Y, 32, 32))],
        };
        var below = exact with
        {
            Id = "qa.static-below",
            Obstacles = [new("rock", new MapRect(exactX - 0.001f, hard.Y, 32, 32))],
        };
        Require(MapBuildingPlacementValidator.Validate(exact).Count == 0,
            "static obstacle exact building clearance should be valid.", failures);
        Require(MapBuildingPlacementValidator.Validate(below).Single().Conflict == MapBuildingPlacementConflictKind.StaticObstacle,
            "static obstacle gap below exact clearance should reject.", failures);
    }

    private static void ValidateResourceBoundaries(List<string> failures)
    {
        const float radius = 16;
        var powerBuilding = Building(BuildingDesignIds.PowerPlant, new MapPoint(320, 336));
        var power = BuildSpecCatalog.For(powerBuilding.Kind);
        var powerSize = power.FootprintCells.WorldSize;
        var hard = PlacementMath.RectFromCenter(
            powerBuilding.Position.X,
            powerBuilding.Position.Y,
            powerSize.X,
            powerSize.Y);
        var powerClearance = MapPlacementRules.ResourceClearance(power);
        var exactHardX = hard.EndX + radius + powerClearance;
        var exactHard = EnvironmentMap("qa.resource-hard-exact", powerBuilding) with
        {
            Resources = [Resource("ore", exactHardX, powerBuilding.Position.Y, radius, amount: 0)],
        };
        var belowHard = exactHard with
        {
            Id = "qa.resource-hard-below",
            Resources = [Resource("ore", exactHardX - 0.001f, powerBuilding.Position.Y, radius, amount: 0)],
        };
        Require(MapBuildingPlacementValidator.Validate(exactHard).Count == 0,
            "resource hard-footprint exact clearance should be valid even when amount is zero.", failures);
        Require(MapBuildingPlacementValidator.Validate(belowHard).Single().Conflict == MapBuildingPlacementConflictKind.Resource,
            "resource hard-footprint 31.999 clearance should reject.", failures);

        var barracksBuilding = Building(BuildingDesignIds.Barracks, new MapPoint(320, 320));
        var barracks = BuildSpecCatalog.For(barracksBuilding.Kind);
        var reservation = PlacementReservationMath.WorldRect(
            barracks,
            barracks.PlacementReservations.Single(),
            barracksBuilding.Position.ToVector2(),
            0);
        var reservationClearance = MapPlacementRules.ResourceClearance(barracks);
        var exactReservationX = reservation.EndX + radius + reservationClearance;
        var exactReservation = EnvironmentMap("qa.resource-reservation-exact", barracksBuilding) with
        {
            Resources = [Resource("ore", exactReservationX, reservation.Y + reservation.Height * 0.5f, radius)],
        };
        var belowReservation = exactReservation with
        {
            Id = "qa.resource-reservation-below",
            Resources = [Resource("ore", exactReservationX - 0.001f, reservation.Y + reservation.Height * 0.5f, radius)],
        };
        Require(MapBuildingPlacementValidator.Validate(exactReservation).Count == 0,
            "resource reservation exact clearance should be valid.", failures);
        var belowConflicts = MapBuildingPlacementValidator.Validate(belowReservation);
        Require(
            belowConflicts.Count == 1
                && belowConflicts[0].Conflict == MapBuildingPlacementConflictKind.Resource
                && belowConflicts[0].Target?.Geometry.Contains("relation=reservation[0]", StringComparison.Ordinal) == true,
            "resource reservation 31.999 clearance should reject with reservation evidence.",
            failures);
    }

    private static void ValidateEnvironmentHash(List<string> failures)
    {
        var empty = EnvironmentMap("qa.hash.empty");
        var terrain = empty with
        {
            Id = "qa.hash.terrain",
            TerrainCells = [new("ground", new MapRect(64, 64, 64, 64), "ground")],
        };
        var obstacle = empty with
        {
            Id = "qa.hash.obstacle",
            Obstacles = [new("rock", new MapRect(64, 64, 64, 64))],
        };
        var resized = empty with { Id = "qa.hash.resized", WorldSize = new MapSize(1056, 768) };
        var hashes = new[]
        {
            MapLoader.Load(empty).DeterministicStateHash(),
            MapLoader.Load(terrain).DeterministicStateHash(),
            MapLoader.Load(obstacle).DeterministicStateHash(),
            MapLoader.Load(resized).DeterministicStateHash(),
        };
        Require(hashes.Distinct().Count() == hashes.Length,
            "world dimensions and ordered terrain/static environment fields should change deterministic state hash.", failures);
    }

    private static void ValidateAuthoredTerrainRasterBounds(List<string> failures)
    {
        var missesCenter = MapRuntimeEnvironment.From(new MapSpec
        {
            Id = "qa.terrain-raster-misses-center",
            Seed = 559,
            WorldSize = new MapSize(128, 128),
            TerrainCells = [new("tiny", new MapRect(0, 0, 10, 10), "water", BlocksLand: true)],
        });
        var grid = new List<GridTerrain>();
        missesCenter.AppendAuthoredTerrainGrid(64, grid);
        Require(grid.Count == 0,
            "terrain bounds [0,10] should not emit cell (0,0) whose center is (32,32).", failures);

        var containsCenter = MapRuntimeEnvironment.From(new MapSpec
        {
            Id = "qa.terrain-raster-contains-center",
            Seed = 559,
            WorldSize = new MapSize(128, 128),
            TerrainCells = [new("center", new MapRect(16, 16, 32, 32), "water", BlocksLand: true)],
        });
        grid.Clear();
        containsCenter.AppendAuthoredTerrainGrid(64, grid);
        Require(grid.Count == 1 && grid[0] == new GridTerrain(0, 0, TerrainLayer.Water),
            "terrain bounds containing cell center (32,32) should emit authored cell (0,0).", failures);
    }

    private static MapSpec EnvironmentMap(string id, params MapBuildingSeedSpec[] buildings)
    {
        return new MapSpec
        {
            Id = id,
            Seed = 559,
            WorldSize = new MapSize(1024, 768),
            TerrainCells = [new("ground", new MapRect(0, 0, 1024, 768), "ground")],
            Buildings = buildings,
        };
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
}
