using Godot;
using ProceduralRts.Core;

static partial class PlacementValidationScenarios
{
    private static void ValidateReservationRotationsAndSymmetry(List<string> failures)
    {
        var facings = new[] { 0f, Mathf.Pi * 0.5f, Mathf.Pi, Mathf.Pi * 1.5f };
        var directions = new[] { Vector2.Right, Vector2.Down, Vector2.Left, Vector2.Up };
        var producerSpec = BuildSpecCatalog.For(BuildingDesignIds.Barracks);
        var blockerSpec = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
        var reservationSpec = producerSpec.PlacementReservations.Single();
        for (var rotation = 0; rotation < facings.Length; rotation++)
        {
            var facing = facings[rotation];
            var producerFootprint = producerSpec.FootprintCells.Rotated(facing);
            var producerCenter = new Vector2(
                PlacementMath.SnapAnchor(768, producerFootprint.WidthCells),
                PlacementMath.SnapAnchor(768, producerFootprint.HeightCells));
            var reservation = PlacementReservationMath.WorldRect(
                producerSpec,
                reservationSpec,
                producerCenter,
                facing);
            var direction = directions[rotation];
            var blockerFootprint = blockerSpec.FootprintCells.WorldSize;
            var clearance = Math.Max(producerSpec.PlacementClearanceCells, blockerSpec.PlacementClearanceCells)
                * PlacementMath.GridSize;
            var exactBlockerCenter = direction.X != 0
                ? new Vector2(
                    direction.X > 0
                        ? reservation.EndX + clearance + blockerFootprint.X * 0.5f
                        : reservation.X - clearance - blockerFootprint.X * 0.5f,
                    PlacementMath.SnapAnchor(
                        reservation.Y + reservation.Height * 0.5f,
                        blockerSpec.FootprintCells.HeightCells))
                : new Vector2(
                    PlacementMath.SnapAnchor(
                        reservation.X + reservation.Width * 0.5f,
                        blockerSpec.FootprintCells.WidthCells),
                    direction.Y > 0
                        ? reservation.EndY + clearance + blockerFootprint.Y * 0.5f
                        : reservation.Y - clearance - blockerFootprint.Y * 0.5f);
            var belowBlockerCenter = exactBlockerCenter - direction * 0.001f;
            var producer = Building(BuildingDesignIds.Barracks, producerCenter.ToMapPoint(), facing: facing);
            var blocker = Building(
                BuildingDesignIds.PowerPlant,
                belowBlockerCenter.ToMapPoint(),
                owner: 2,
                faction: FactionId.Cat);
            var below = Map($"qa.reserved.rotation.{rotation}.below", new MapSize(2048, 1536), producer, blocker);
            RequirePairReason(below, MapBuildingPlacementConflictKind.Reserved, failures);

            var reversed = below with
            {
                Id = $"qa.reserved.rotation.{rotation}.reversed",
                Buildings = [blocker, producer],
            };
            RequirePairReason(reversed, MapBuildingPlacementConflictKind.Reserved, failures);

            var exact = below with
            {
                Id = $"qa.reserved.rotation.{rotation}.exact",
                Buildings =
                [
                    producer,
                    blocker with { Position = exactBlockerCenter.ToMapPoint() },
                ],
            };
            var exactConflicts = MapBuildingPlacementValidator.Validate(exact);
            Require(exactConflicts.Count == 0,
                $"rotation {rotation} exact reservation clearance should be valid; got {string.Join("; ", exactConflicts)}.", failures);
        }
    }

    private static void ValidateReservationPairBoundary(List<string> failures)
    {
        var below = Map(
            "qa.reservation-pair-below",
            new MapSize(1024, 768),
            Building(BuildingDesignIds.Barracks, new MapPoint(320, 320)),
            Building(BuildingDesignIds.Barracks, new MapPoint(607.999f, 320), facing: Mathf.Pi, owner: 2, faction: FactionId.Cat));
        RequirePairReason(below, MapBuildingPlacementConflictKind.Reserved, failures);

        var exact = below with
        {
            Id = "qa.reservation-pair-exact",
            Buildings =
            [
                below.Buildings[0],
                below.Buildings[1] with { Position = new MapPoint(608, 320) },
            ],
        };
        var exactConflicts = MapBuildingPlacementValidator.Validate(exact);
        Require(exactConflicts.Count == 0,
            $"exact reservation-to-reservation clearance should be valid; got {string.Join("; ", exactConflicts)}.", failures);
    }
}
