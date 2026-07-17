using Godot;
using ProceduralRts.Core;

static class MapValidationPairFixtures
{
    public static MapSpec Reserved()
    {
        var producerSpec = BuildSpecCatalog.For(BuildingDesignIds.Barracks);
        var blockerSpec = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
        var producer = MapValidationFixtures.Building(BuildingDesignIds.Barracks, 640, 512);
        var reservation = PlacementReservationMath.WorldRect(
            producerSpec,
            producerSpec.PlacementReservations.Single(),
            producer.Position.ToVector2(),
            0);
        var clearance = Math.Max(
            producerSpec.PlacementClearanceCells,
            blockerSpec.PlacementClearanceCells) * PlacementMath.GridSize;
        var blocker = MapValidationFixtures.Building(
            BuildingDesignIds.PowerPlant,
            reservation.EndX + clearance + blockerSpec.FootprintCells.WorldSize.X * 0.5f,
            producer.Position.Y,
            owner: 2);
        return MapValidationFixtures.WithBuildings(
            producer,
            blocker with { Position = new MapPoint(blocker.Position.X - 0.001f, blocker.Position.Y) }) with
        {
            Id = "qa.reserved",
        };
    }

    public static MapSpec Environment(string code)
    {
        var building = MapValidationFixtures.Building(BuildingDesignIds.PowerPlant, 512, 512);
        var map = MapValidationFixtures.WithBuildings(building) with { Id = $"qa.{code}" };
        return code switch
        {
            MapValidationCodes.BuildingTerrain => map with
            {
                TerrainCells = [new("water", new MapRect(448, 448, 128, 128), "water", BlocksLand: true)],
            },
            MapValidationCodes.BuildingStaticObstacle => map with
            {
                Obstacles = [new("rock", new MapRect(480, 480, 64, 64))],
            },
            MapValidationCodes.BuildingResource => map with
            {
                Resources = [new("ore", new MapPoint(512, 512), 16, 100, new MapColor("#ffffff"))],
            },
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown environment fixture."),
        };
    }
}
