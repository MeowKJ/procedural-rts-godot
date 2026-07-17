using Godot;
using ProceduralRts.Core;

static class MapValidationGeometryScenarios
{
    public static void Run(List<string> failures)
    {
        ValidateSharedGeometry(failures);
        ValidateClearanceBoundary(failures);
        ValidateReachability(failures);
        ValidateEndpointCellParity(failures);
        ValidateGeneratedMap(failures);
    }

    private static void ValidateSharedGeometry(List<string> failures)
    {
        var facings = new[] { 0f, Mathf.Pi * 0.5f, Mathf.Pi, Mathf.Pi * 1.5f };
        foreach (var facing in facings)
        {
            var building = MapValidationFixtures.Building(BuildingDesignIds.Barracks, 640, 512, facing: facing);
            var geometry = MapBuildingPlacementGeometry.Create(building);
            var expected = geometry.Spec.PlacementReservations.Select(reservation =>
                PlacementReservationMath.WorldRect(
                    geometry.Spec, reservation, building.Position.ToVector2(), geometry.CardinalFacing));
            MapValidationFixtures.Require(geometry.Reservations.SequenceEqual(expected),
                $"Shared reservation geometry differs at facing {facing:R}.", failures);
            MapValidationFixtures.Require(
                MapBuildingPlacementValidator.GridCoordinate(building) == (geometry.GridX, geometry.GridY),
                $"Shared placement grid differs at facing {facing:R}.", failures);
        }
    }

    private static void ValidateClearanceBoundary(List<string> failures)
    {
        var first = MapValidationFixtures.Building(BuildingDesignIds.PowerPlant, 384, 384);
        var firstGeometry = MapBuildingPlacementGeometry.Create(first);
        var secondSpec = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
        var exactX = firstGeometry.Hard.EndX
            + Math.Max(firstGeometry.Spec.PlacementClearanceCells, secondSpec.PlacementClearanceCells)
            * PlacementMath.GridSize
            + secondSpec.FootprintCells.WorldSize.X * 0.5f;
        var exact = MapValidationFixtures.Building(BuildingDesignIds.PowerPlant, exactX, first.Position.Y, owner: 2);
        var exactMap = MapValidationFixtures.WithBuildings(first, exact) with { Id = "qa.clearance.exact" };
        MapValidationFixtures.Require(MapBuildingPlacementValidator.Validate(exactMap).Count == 0,
            "Exact shared clearance boundary must be valid.", failures);
        var belowMap = exactMap with
        {
            Id = "qa.clearance.below",
            Buildings = [first, exact with { Position = new MapPoint(exact.Position.X - 0.001f, exact.Position.Y) }],
        };
        MapValidationFixtures.Require(MapBuildingPlacementValidator.Validate(belowMap)
                .Any(value => value.Conflict == MapBuildingPlacementConflictKind.Clearance),
            "Below shared clearance boundary must report clearance.", failures);
    }

    private static void ValidateReachability(List<string> failures)
    {
        MapValidationFixtures.Require(MapReachabilityValidator.Validate(MapValidationFixtures.WallWithGap()).Count == 0,
            "Owner starts must remain reachable through an authored gap.", failures);
        MapValidationFixtures.Require(MapReachabilityValidator.Validate(MapValidationFixtures.SolidWall()).Count == 1,
            "A full authored wall must make owner starts unreachable.", failures);
        var decorative = MapValidationFixtures.Valid("qa.reachability.nonblocking") with
        {
            Resources = [new("ore", new MapPoint(768, 512), 96, 100, new MapColor("#ffffff"))],
            Buildings = [MapValidationFixtures.Building(BuildingDesignIds.Barracks, 384, 512)],
        };
        MapValidationFixtures.Require(MapReachabilityValidator.Validate(decorative).Count == 0,
            "Resources and building reservations must not become static path blockers.", failures);
    }

    private static void ValidateGeneratedMap(List<string> failures)
    {
        var map = SkirmishMapGenerator.GenerateSpec(MatchConfig.Default);
        var first = map.StartFor(new OwnerId(1));
        var second = map.StartFor(new OwnerId(2));
        var grid = PathfindingStaticGrid.Build(map, MovementDomain.Land);
        var result = PathfindingMath.FindPathWithDebug(
            first.Position.X, first.Position.Y, second.Position.X, second.Position.Y,
            map.WorldSize.Width, map.WorldSize.Height, PathfindingStaticGrid.RuntimeCellSize,
            grid.Obstacles, MovementDomain.Land, grid.Terrain);
        MapValidationFixtures.Require(
            first.Position == new MapPoint(505, 610) && second.Position == new MapPoint(2860, 1535),
            "Generated reference owner anchors must remain unchanged.", failures);
        MapValidationFixtures.Require(map.Buildings[0].Position == new MapPoint(512, 624),
            "Generated reference HQ-relative geometry must remain unchanged.", failures);
        MapValidationFixtures.Require(!result.Reached && MapReachabilityValidator.Validate(map).Count == 1,
            "Generated metadata anchors enclosed by HQ blockers must report editor reachability honestly.", failures);
        try { _ = MapLoader.Load(map); }
        catch (Exception exception)
        {
            failures.Add($"Generated maps must remain loadable outside editor reachability diagnostics: {exception.GetType().Name}.");
        }
    }

    private static void ValidateEndpointCellParity(List<string> failures)
    {
        var building = MapValidationFixtures.Building(BuildingDesignIds.Headquarters, 512, 512);
        var map = MapValidationFixtures.WithBuildings(building) with
        {
            Id = "qa.endpoint.inside-building",
            OwnerStarts =
            [
                new(new OwnerId(1), FactionId.Dog, building.Position, 0, 0),
                new(new OwnerId(2), FactionId.Cat, new MapPoint(1408, 896), 0, 0),
            ],
        };
        var grid = PathfindingStaticGrid.Build(map, MovementDomain.Land);
        var result = PathfindingMath.FindPathWithDebug(
            building.Position.X, building.Position.Y, 1408, 896,
            map.WorldSize.Width, map.WorldSize.Height, PathfindingStaticGrid.RuntimeCellSize,
            grid.Obstacles, MovementDomain.Land, grid.Terrain);
        MapValidationFixtures.Require(!result.Reached,
            "Runtime search must open only the endpoint cell, not its multi-cell building circle.", failures);
        MapValidationFixtures.Require(MapReachabilityValidator.Validate(map).Count == 1,
            "Reachability validation must exactly match the runtime blocker grid for enclosed endpoints.", failures);
        try { _ = MapLoader.Load(map); }
        catch (Exception exception)
        {
            failures.Add($"Reachability-only invalid maps must remain loadable: {exception.GetType().Name}.");
        }
        var placementInvalid = map with { Buildings = [building, building] };
        try
        {
            _ = MapLoader.Load(placementInvalid);
            failures.Add("Placement-invalid maps must still fail the runtime loader boundary.");
        }
        catch (MapBuildingPlacementValidationException) { }
        var structuralInvalid = map with { OwnerStarts = [map.OwnerStarts[0]] };
        try
        {
            _ = MapLoader.Load(structuralInvalid);
            failures.Add("Structurally invalid maps must still fail the runtime loader boundary.");
        }
        catch (MapOwnerTopologyValidationException) { }
    }
}
