using ProceduralRts.Core;

static partial class PlacementValidationScenarios
{
    private static void ValidateAtomicLoaderFailure(List<string> failures)
    {
        var invalid = Map(
            "qa.loader-atomic-rejection",
            new MapSize(512, 512),
            Building(BuildingDesignIds.Barracks, new MapPoint(480, 320)));
        var world = new EntityWorld(seed: 550);
        MapLoader.LoadInto(
            world,
            new MapSpec
            {
                Id = "qa.loader-existing-environment",
                Seed = 550,
                WorldSize = new MapSize(777, 555),
                OwnerStarts =
                [
                    new(new OwnerId(1), FactionId.Dog, new MapPoint(64, 64), 0, 0),
                    new(new OwnerId(2), FactionId.Cat, new MapPoint(713, 491), MathF.PI, 0),
                ],
                TerrainCells = [new("existing.ground", new MapRect(0, 0, 777, 555), "ground")],
                Obstacles = [new("existing.rock", new MapRect(32, 32, 32, 32))],
            });
        world.ResourceInventory(new OwnerId(1)).Credits = 123;
        var environmentBefore = world.MapEnvironment;
        var hashBefore = world.DeterministicStateHash();
        MapBuildingPlacementValidationException? first = null;
        try
        {
            MapLoader.LoadInto(
                world,
                invalid,
                new MapLoadOptions(ConfigureLiveSystems: true, OutcomeViewer: new OwnerId(1)));
        }
        catch (MapBuildingPlacementValidationException exception)
        {
            first = exception;
        }

        Require(first is not null, "MapLoader should throw the typed placement validation exception.", failures);
        Require(world.DeterministicStateHash() == hashBefore
            && ReferenceEquals(world.MapEnvironment, environmentBefore)
            && Math.Abs(world.WorldWidth - 777) < 0.001f
            && Math.Abs(world.WorldHeight - 555) < 0.001f,
            "MapLoader rejection should preserve the previous environment/hash before world dimensions, systems, owners, or entities change.", failures);

        MapBuildingPlacementValidationException? second = null;
        try
        {
            MapLoader.Load(invalid);
        }
        catch (MapBuildingPlacementValidationException exception)
        {
            second = exception;
        }

        Require(first is not null
            && second is not null
            && first.MapId == invalid.Id
            && first.Message == second.Message
            && first.Conflicts.Select(conflict => conflict.ToString())
                .SequenceEqual(second.Conflicts.Select(conflict => conflict.ToString())),
            "MapLoader placement exceptions should carry deterministic map id, conflict order, and message.", failures);
    }

    private static void ValidateBakerFailure(List<string> failures)
    {
        var invalidScene = $$"""
            [node name="Map" type="Node2D"]
            metadata/world_width = 512
            metadata/world_height = 512

            [node name="Barracks" type="Marker2D" parent="."]
            metadata/map_kind = "building"
            metadata/building_kind = "{{BuildingDesignIds.Barracks}}"
            metadata/owner_id = 1
            metadata/faction = "Dog"
            metadata/position = Vector2(480, 320)
            metadata/facing = 0
            """;
        MapBuildingPlacementValidationException? failure = null;
        try
        {
            GodotSceneMapBaker.Bake(invalidScene, "qa.invalid-baked-map", 550);
        }
        catch (MapBuildingPlacementValidationException exception)
        {
            failure = exception;
        }

        Require(failure is not null
            && failure.MapId == "qa.invalid-baked-map"
            && failure.Conflicts.Any(conflict => conflict.Conflict == MapBuildingPlacementConflictKind.Outside),
            "Godot scene baking should fail through the shared typed placement guard.", failures);
    }
}
