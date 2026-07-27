using ProceduralRts.Core;

static class MapValidationDiagnosticScenarios
{
    public static IReadOnlyList<MapValidationDiagnostic> Run(List<string> failures)
    {
        var observed = new List<MapValidationDiagnostic>();
        Add(observed, UnknownCatalog(), MapValidationCodes.CatalogUnknown, failures);
        Add(observed, EmptyId(), MapValidationCodes.IdEmpty, failures);
        Add(observed, DuplicateId(), MapValidationCodes.IdDuplicate, failures, requireConflict: true);
        Add(observed, RuntimeIdInvalid(), MapValidationCodes.RuntimeIdInvalid, failures);
        Add(observed, RuntimeIdDuplicate(), MapValidationCodes.RuntimeIdDuplicate, failures, requireConflict: true);
        Add(observed, MissingOwnerStart(), MapValidationCodes.OwnerStartCount, failures);
        Add(observed, UnsupportedOwner(), MapValidationCodes.OwnerUnsupported, failures);
        Add(observed, MissingOwnerReference(), MapValidationCodes.OwnerReference, failures);
        Add(observed, InvalidWorld(), MapValidationCodes.WorldInvalidSize, failures);
        Add(observed, InvalidRect(), MapValidationCodes.GeometryInvalidRect, failures);
        Add(observed, InvalidCircle(), MapValidationCodes.GeometryInvalidCircle, failures);
        Add(observed, InvalidCost(), MapValidationCodes.GeometryInvalidCost, failures);
        observed.Add(MapValidationService.UnrepresentableTransform(
            new MapValidationSource(MapValidationSourceKind.Obstacle, 0, "transform"), "scaled"));
        Add(observed, Outside(), MapValidationCodes.BoundsOutside, failures);
        Add(observed, Unsnapped(), MapValidationCodes.GridUnsnapped, failures);
        Add(observed, NonCardinal(), MapValidationCodes.RotationNonCardinal, failures);
        Add(observed, Overlap(), MapValidationCodes.BuildingOverlap, failures, requireConflict: true);
        Add(observed, Clearance(), MapValidationCodes.BuildingClearance, failures, requireConflict: true);
        Add(observed, Reserved(), MapValidationCodes.BuildingReserved, failures, requireConflict: true);
        Add(observed, TerrainConflict(), MapValidationCodes.BuildingTerrain, failures, requireConflict: true);
        Add(observed, ObstacleConflict(), MapValidationCodes.BuildingStaticObstacle, failures, requireConflict: true);
        Add(observed, ResourceConflict(), MapValidationCodes.BuildingResource, failures, requireConflict: true);
        Add(observed, MissingReference(), MapValidationCodes.ReferenceMissing, failures);
        Add(observed, MapValidationFixtures.SolidWall(), MapValidationCodes.ReachabilityOwnerStart, failures, requireConflict: true);
        ValidateContract(observed, failures);
        ValidateEqualBuildingIdentity(failures);
        ValidateReadOnly(failures);
        ValidateExceptionBehavior(failures);
        return MapValidationOrdering.Sort(observed);
    }

    private static void Add(
        List<MapValidationDiagnostic> observed,
        MapSpec map,
        string code,
        List<string> failures,
        bool requireConflict = false)
    {
        var match = MapValidationService.Validate(map).FirstOrDefault(value => value.Code == code);
        MapValidationFixtures.Require(match is not null, $"Expected diagnostic {code} for {map.Id}.", failures);
        if (match is null) return;
        MapValidationFixtures.Require(!requireConflict || match.Conflict is not null,
            $"Diagnostic {code} must carry a conflict source.", failures);
        observed.Add(match);
    }

    private static void ValidateContract(List<MapValidationDiagnostic> observed, List<string> failures)
    {
        var sorted = MapValidationOrdering.Sort(observed.AsEnumerable().Reverse());
        MapValidationFixtures.Require(sorted.Select(value => value.Code).SequenceEqual(MapValidationCodes.Ordered),
            $"Diagnostic order mismatch: {string.Join(',', sorted.Select(value => value.Code))}.", failures);
        foreach (var diagnostic in sorted)
        {
            MapValidationFixtures.Require(diagnostic.Severity == MapValidationSeverity.Error, $"{diagnostic.Code} must be Error.", failures);
            MapValidationFixtures.Require(diagnostic.Code.Length <= 64, $"{diagnostic.Code} exceeds 64 chars.", failures);
            MapValidationFixtures.Require(diagnostic.Source.Id.Length <= 48, $"{diagnostic.Code} source id exceeds 48 chars.", failures);
            MapValidationFixtures.Require(diagnostic.Message.Length <= 240, $"{diagnostic.Code} message exceeds 240 chars.", failures);
        }
    }

    private static void ValidateReadOnly(List<string> failures)
    {
        var map = Reserved();
        var before = MapValidationFixtures.Fingerprint(map);
        var first = MapValidationService.Validate(map);
        var second = MapValidationService.Validate(map);
        MapValidationFixtures.Require(before == MapValidationFixtures.Fingerprint(map), "Validation must not mutate MapSpec.", failures);
        MapValidationFixtures.Require(first.SequenceEqual(second), "Repeated validation must be deterministic.", failures);
    }

    private static void ValidateEqualBuildingIdentity(List<string> failures)
    {
        var building = MapValidationFixtures.Building(BuildingDesignIds.PowerPlant, 512, 512);
        var map = MapValidationFixtures.WithBuildings(building, building) with { Id = "qa.equal-buildings" };
        var diagnostic = MapValidationService.Validate(map)
            .Single(value => value.Code == MapValidationCodes.BuildingOverlap);
        MapValidationFixtures.Require(
            diagnostic.Source.Index == 0 && diagnostic.Conflict?.Index == 1,
            "Value-equal buildings must retain explicit Building[0]/Building[1] identity.", failures);
    }

    private static void ValidateExceptionBehavior(List<string> failures)
    {
        var map = MissingOwnerStart();
        var messages = MapOwnerTopologyValidator.Validate(map).Select(value => value.Message).ToArray();
        try { MapOwnerTopologyValidator.EnsureValid(map); }
        catch (MapOwnerTopologyValidationException exception)
        {
            MapValidationFixtures.Require(exception.Diagnostics.SequenceEqual(messages), "Owner EnsureValid diagnostics changed.", failures);
            return;
        }
        failures.Add("Owner EnsureValid must still throw for invalid topology.");
    }

    private static MapSpec UnknownCatalog() => MapValidationFixtures.Valid("qa.catalog") with
    {
        Units = [new MapUnitSeedSpec("unknown.unit", new OwnerId(1), new MapPoint(256, 256))],
    };
    private static MapSpec EmptyId() => MapValidationFixtures.Valid("qa.empty") with
    {
        Resources = [new MapResourceNodeSpec("", new MapPoint(256, 256), 32, 100, new MapColor("#ffffff"))],
    };
    private static MapSpec DuplicateId() => MapValidationFixtures.Valid("qa.duplicate") with
    {
        Resources = [new MapResourceNodeSpec("same", new MapPoint(256, 256), 32, 100, new MapColor("#ffffff"))],
        Obstacles = [new MapObstacleSpec("same", new MapRect(512, 512, 64, 64))],
    };
    private static MapSpec RuntimeIdInvalid() => MapValidationFixtures.WithBuildings(
        MapValidationFixtures.Building(BuildingDesignIds.PowerPlant, 384, 384, runtimeId: -1)) with { Id = "qa.runtime-id.invalid" };
    private static MapSpec RuntimeIdDuplicate() => MapValidationFixtures.WithBuildings(
        MapValidationFixtures.Building(BuildingDesignIds.PowerPlant, 384, 384, runtimeId: 7),
        MapValidationFixtures.Building(BuildingDesignIds.PowerPlant, 1024, 640, 2, runtimeId: 7)) with { Id = "qa.runtime-id.duplicate" };
    private static MapSpec MissingOwnerStart() => MapValidationFixtures.Valid("qa.owner.count") with
    {
        OwnerStarts = [MapValidationFixtures.Valid().OwnerStarts[0]],
    };
    private static MapSpec UnsupportedOwner() => MapValidationFixtures.Valid("qa.owner.unsupported") with
    {
        OwnerStarts = MapValidationFixtures.Valid().OwnerStarts.Concat(
            [new MapOwnerStartSpec(new OwnerId(3), FactionId.Dog, new MapPoint(768, 512), 0, 100)]).ToArray(),
    };
    private static MapSpec MissingOwnerReference() => MapValidationFixtures.Valid("qa.owner.reference") with
    {
        Units = [new MapUnitSeedSpec("dog.infantry", new OwnerId(3), new MapPoint(256, 256))],
    };
    private static MapSpec InvalidWorld() => MapValidationFixtures.Valid("qa.world") with { WorldSize = new MapSize(0, 1024) };
    private static MapSpec InvalidRect() => MapValidationFixtures.Valid("qa.rect") with
    {
        Obstacles = [new MapObstacleSpec("bad", new MapRect(128, 128, 0, 64))],
    };
    private static MapSpec InvalidCircle() => MapValidationFixtures.Valid("qa.circle") with
    {
        Resources = [new MapResourceNodeSpec("bad", new MapPoint(256, 256), 0, 100, new MapColor("#ffffff"))],
    };
    private static MapSpec InvalidCost() => MapValidationFixtures.Valid("qa.cost") with
    {
        TerrainCells = [new MapTerrainCellSpec("bad", new MapRect(128, 128, 64, 64), "base-ground", 0)],
    };
    private static MapSpec Outside() => MapValidationFixtures.Valid("qa.outside") with
    {
        Obstacles = [new MapObstacleSpec("outside", new MapRect(1500, 900, 128, 128))],
    };
    private static MapSpec Unsnapped()
    {
        var building = MapValidationFixtures.Building(BuildingDesignIds.PowerPlant, 384, 384);
        return MapValidationFixtures.WithBuildings(building with { Position = new MapPoint(building.Position.X + 1, building.Position.Y) }) with { Id = "qa.unsnapped" };
    }
    private static MapSpec NonCardinal() => MapValidationFixtures.WithBuildings(
        MapValidationFixtures.Building(BuildingDesignIds.PowerPlant, 384, 384) with { Facing = 0.1f }) with { Id = "qa.rotation" };
    private static MapSpec Overlap() => MapValidationFixtures.WithBuildings(
        MapValidationFixtures.Building(BuildingDesignIds.PowerPlant, 512, 512),
        MapValidationFixtures.Building(BuildingDesignIds.PowerPlant, 512, 512, 2)) with { Id = "qa.overlap" };
    private static MapSpec Clearance()
    {
        var first = MapValidationFixtures.Building(BuildingDesignIds.PowerPlant, 384, 384);
        var geometry = MapBuildingPlacementGeometry.Create(first);
        var exact = MapValidationFixtures.Building(BuildingDesignIds.PowerPlant,
            geometry.Hard.EndX + geometry.Spec.PlacementClearanceCells * PlacementMath.GridSize
            + geometry.Hard.Width * 0.5f, first.Position.Y, 2);
        var second = exact with { Position = new MapPoint(exact.Position.X - 0.001f, exact.Position.Y) };
        return MapValidationFixtures.WithBuildings(first, second) with { Id = "qa.clearance" };
    }
    private static MapSpec Reserved() => MapValidationPairFixtures.Reserved();
    private static MapSpec TerrainConflict() => MapValidationPairFixtures.Environment(MapValidationCodes.BuildingTerrain);
    private static MapSpec ObstacleConflict() => MapValidationPairFixtures.Environment(MapValidationCodes.BuildingStaticObstacle);
    private static MapSpec ResourceConflict() => MapValidationPairFixtures.Environment(MapValidationCodes.BuildingResource);
    private static MapSpec MissingReference() => MapValidationFixtures.Valid("qa.reference") with
    {
        NarrativeNodes = [new MapNarrativeNodeSpec("narrative", new MapPoint(256, 256), "narrative.intro", "missing")],
    };
}
