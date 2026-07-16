using Godot;
using ProceduralRts.Core;

static partial class PlacementValidationScenarios
{
    private static void ValidateLocalReasonOrder(List<string> failures)
    {
        var invalid = Map(
            "qa.invalid-local-placement",
            new MapSize(512, 512),
            new MapBuildingSeedSpec(
                BuildingDesignIds.Headquarters,
                new OwnerId(1),
                FactionId.Dog,
                new MapPoint(32.25f, 32.25f),
                0.2f));
        var conflicts = MapBuildingPlacementValidator.Validate(invalid);
        var expected = new[]
        {
            MapBuildingPlacementConflictKind.Rotation,
            MapBuildingPlacementConflictKind.Unsnapped,
            MapBuildingPlacementConflictKind.Outside,
        };
        Require(
            conflicts.Select(conflict => conflict.Conflict).SequenceEqual(expected),
            $"local placement reasons should be deterministic: expected {string.Join(",", expected)}, got {string.Join(",", conflicts.Select(conflict => conflict.Conflict))}.",
            failures);
        Require(
            conflicts.All(conflict => HasStableIdentity(conflict.ToString(), invalid.Id)),
            "placement diagnostics should identify map, owner, faction, kind, grid coordinate, and stable conflict.",
            failures);

        var reservationOutside = Map(
            "qa.reservation-outside",
            new MapSize(1024, 768),
            Building(BuildingDesignIds.Barracks, new MapPoint(928, 320), facing: 0));
        var outsideConflicts = MapBuildingPlacementValidator.Validate(reservationOutside);
        Require(
            outsideConflicts.Count == 1
                && outsideConflicts[0].Conflict == MapBuildingPlacementConflictKind.Outside,
            $"reservation extent outside the map should emit one outside reason; got {string.Join("; ", outsideConflicts)}.",
            failures);
    }

    private static void ValidateHardPairReasons(List<string> failures)
    {
        var overlap = Map(
            "qa.hard-overlap",
            new MapSize(1024, 768),
            Building(BuildingDesignIds.PowerPlant, new MapPoint(320, 336)),
            Building(BuildingDesignIds.PowerPlant, new MapPoint(320, 336), owner: 2, faction: FactionId.Cat));
        RequirePairReason(overlap, MapBuildingPlacementConflictKind.Overlap, failures);

        var spec = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
        var firstCenter = new Vector2(320, 336);
        var width = spec.FootprintCells.WorldSize.X;
        var clearance = spec.PlacementClearanceCells * PlacementMath.GridSize;
        var exactSecondX = firstCenter.X + width + clearance;
        var below = Map(
            "qa.hard-clearance-below",
            new MapSize(1024, 768),
            Building(BuildingDesignIds.PowerPlant, firstCenter.ToMapPoint()),
            Building(BuildingDesignIds.PowerPlant, new MapPoint(exactSecondX - 0.001f, firstCenter.Y), owner: 2, faction: FactionId.Cat));
        RequirePairReason(below, MapBuildingPlacementConflictKind.Clearance, failures);

        var exact = below with
        {
            Id = "qa.hard-clearance-exact",
            Buildings =
            [
                below.Buildings[0],
                below.Buildings[1] with { Position = new MapPoint(exactSecondX, firstCenter.Y) },
            ],
        };
        var exactConflicts = MapBuildingPlacementValidator.Validate(exact);
        Require(exactConflicts.Count == 0,
            $"exact hard clearance should be valid; got {string.Join("; ", exactConflicts)}.", failures);
    }
}
