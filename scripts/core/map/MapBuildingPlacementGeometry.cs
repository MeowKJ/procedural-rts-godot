using Godot;

namespace ProceduralRts.Core;

public sealed record MapBuildingPlacementGeometry(
    MapBuildingSeedSpec Building,
    BuildSpec Spec,
    bool IsCardinal,
    float CardinalFacing,
    PlacementGridFootprint Footprint,
    float SnappedX,
    float SnappedY,
    PlacementRect Hard,
    PlacementRect Clearance,
    IReadOnlyList<PlacementRect> Reservations,
    int GridX,
    int GridY)
{
    public static MapBuildingPlacementGeometry Create(MapBuildingSeedSpec building)
    {
        var spec = BuildSpecCatalog.For(building.Kind);
        var isCardinal = PlacementMath.TryNormalizeCardinalFacing(building.Facing, out var cardinalFacing);
        var footprint = spec.FootprintCells.Rotated(cardinalFacing);
        var snappedX = PlacementMath.SnapAnchor(building.Position.X, footprint.WidthCells);
        var snappedY = PlacementMath.SnapAnchor(building.Position.Y, footprint.HeightCells);
        var hard = PlacementMath.RectFromCenter(
            building.Position.X,
            building.Position.Y,
            footprint.WorldSize.X,
            footprint.WorldSize.Y);
        var clearanceDistance = spec.PlacementClearanceCells * PlacementMath.GridSize;
        var clearance = new PlacementRect(
            hard.X - clearanceDistance,
            hard.Y - clearanceDistance,
            hard.Width + clearanceDistance * 2,
            hard.Height + clearanceDistance * 2);
        var reservations = new PlacementRect[spec.PlacementReservations.Count];
        for (var index = 0; index < reservations.Length; index++)
        {
            reservations[index] = PlacementReservationMath.WorldRect(
                spec,
                spec.PlacementReservations[index],
                new Vector2(building.Position.X, building.Position.Y),
                cardinalFacing);
        }
        var originX = snappedX - footprint.WorldSize.X * 0.5f;
        var originY = snappedY - footprint.WorldSize.Y * 0.5f;
        return new MapBuildingPlacementGeometry(
            building, spec, isCardinal, cardinalFacing, footprint, snappedX, snappedY,
            hard, clearance, Array.AsReadOnly(reservations),
            (int)MathF.Round(originX / PlacementMath.GridSize),
            (int)MathF.Round(originY / PlacementMath.GridSize));
    }
}
