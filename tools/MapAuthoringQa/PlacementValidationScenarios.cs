using ProceduralRts.Core;

static partial class PlacementValidationScenarios
{
    public static void Run(List<string> failures)
    {
        ValidateReasonContract(failures);
        ValidateEnvironmentContracts(failures);
        ValidateLocalReasonOrder(failures);
        ValidateHardPairReasons(failures);
        ValidateReservationRotationsAndSymmetry(failures);
        ValidateReservationPairBoundary(failures);
        ValidateAtomicLoaderFailure(failures);
        ValidateBakerFailure(failures);
    }

    private static void ValidateReasonContract(List<string> failures)
    {
        var expected = new[]
        {
            MapBuildingPlacementConflictKind.Rotation,
            MapBuildingPlacementConflictKind.Unsnapped,
            MapBuildingPlacementConflictKind.Outside,
            MapBuildingPlacementConflictKind.Terrain,
            MapBuildingPlacementConflictKind.StaticObstacle,
            MapBuildingPlacementConflictKind.Resource,
            MapBuildingPlacementConflictKind.Overlap,
            MapBuildingPlacementConflictKind.Clearance,
            MapBuildingPlacementConflictKind.Reserved,
        };
        Require(
            Enum.GetValues<MapBuildingPlacementConflictKind>().SequenceEqual(expected),
            "placement reason enum changed; update the deterministic diagnostic and boundary scenarios.",
            failures);
    }

    private static void RequirePairReason(
        MapSpec map,
        MapBuildingPlacementConflictKind expected,
        List<string> failures)
    {
        var conflicts = MapBuildingPlacementValidator.Validate(map);
        Require(
            conflicts.Any(conflict => conflict.Conflict == expected && conflict.Other is not null),
            $"{map.Id} should report pair conflict {expected.ToString().ToLowerInvariant()}; got {string.Join("; ", conflicts)}.",
            failures);
    }

    private static bool HasStableIdentity(string report, string mapId)
    {
        return report.Contains($"map={mapId}", StringComparison.Ordinal)
            && report.Contains("owner=", StringComparison.Ordinal)
            && report.Contains("faction=", StringComparison.Ordinal)
            && report.Contains("kind=", StringComparison.Ordinal)
            && report.Contains("grid=(", StringComparison.Ordinal)
            && report.Contains("conflict=", StringComparison.Ordinal);
    }

    private static MapBuildingSeedSpec Building(
        string kind,
        MapPoint position,
        float facing = 0,
        int owner = 1,
        FactionId faction = FactionId.Dog)
    {
        return new MapBuildingSeedSpec(kind, new OwnerId(owner), faction, position, facing);
    }

    private static MapSpec Map(string id, MapSize worldSize, params MapBuildingSeedSpec[] buildings)
    {
        return new MapSpec
        {
            Id = id,
            Seed = 550,
            WorldSize = worldSize,
            Buildings = buildings,
        };
    }

    private static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }
}
