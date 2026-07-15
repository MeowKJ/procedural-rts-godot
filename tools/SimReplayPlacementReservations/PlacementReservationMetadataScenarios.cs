using Godot;

static partial class Program
{
    private static void AssertPlacementReservationMetadataAndRotation()
    {
        var expected = new[]
        {
            (Kind: BuildingDesignIds.Barracks, ReservationKind: PlacementReservationKind.ProductionEgress, Center: 96f),
            (Kind: BuildingDesignIds.VehicleFactory, ReservationKind: PlacementReservationKind.ProductionEgress, Center: 144f),
            (Kind: BuildingDesignIds.Airfield, ReservationKind: PlacementReservationKind.ProductionEgress, Center: 144f),
            (Kind: BuildingDesignIds.Refinery, ReservationKind: PlacementReservationKind.RefineryDock, Center: 128f),
        };
        var origin = new Vector2(512, 384);
        var facings = new[] { 0f, Mathf.Pi * 0.5f, Mathf.Pi, Mathf.Pi * 1.5f };
        var directions = new[] { Vector2.Right, Vector2.Down, Vector2.Left, Vector2.Up };
        foreach (var entry in expected)
        {
            var spec = BuildSpecCatalog.For(entry.Kind);
            Assert(spec.PlacementReservations.Count == 1,
                $"{entry.Kind} should declare exactly one placement reservation");
            Assert(spec.PlacementReservations[0].Kind == entry.ReservationKind,
                $"{entry.Kind} should declare {entry.ReservationKind}");
            for (var rotation = 0; rotation < facings.Length; rotation++)
            {
                Assert(PlacementReservationMath.TryCenter(
                        spec,
                        entry.ReservationKind,
                        origin,
                        facings[rotation],
                        out var center),
                    $"{entry.Kind} reservation should resolve at cardinal rotation {rotation}");
                var expectedCenter = origin + directions[rotation] * entry.Center;
                Assert(center.DistanceTo(expectedCenter) < 0.001f,
                    $"{entry.Kind} rotation {rotation} center should be {expectedCenter}, got {center}");
            }
        }

        foreach (var spec in BuildSpecCatalog.Definitions.Values)
        {
            if (spec.Kind is BuildingDesignIds.Barracks
                or BuildingDesignIds.VehicleFactory
                or BuildingDesignIds.Airfield
                or BuildingDesignIds.Refinery)
            {
                continue;
            }

            Assert(spec.PlacementReservations.Count == 0,
                $"{spec.Kind} should expose the shared empty reservation array");
        }
    }
}
