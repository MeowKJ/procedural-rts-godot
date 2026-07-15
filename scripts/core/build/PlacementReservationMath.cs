using Godot;

namespace ProceduralRts.Core;

public static class PlacementReservationMath
{
    public static PlacementRect WorldRect(
        BuildSpec buildSpec,
        PlacementReservationSpec reservation,
        Vector2 buildingPosition,
        float cardinalFacing)
    {
        var localX = (reservation.Column
            + reservation.WidthCells * 0.5f
            - buildSpec.FootprintCells.WidthCells * 0.5f) * PlacementMath.GridSize;
        var localY = (reservation.Row
            + reservation.HeightCells * 0.5f
            - buildSpec.FootprintCells.HeightCells * 0.5f) * PlacementMath.GridSize;
        var width = reservation.WidthCells * PlacementMath.GridSize;
        var height = reservation.HeightCells * PlacementMath.GridSize;
        var quarterTurns = QuarterTurns(cardinalFacing);

        var rotatedX = localX;
        var rotatedY = localY;
        if (quarterTurns == 1)
        {
            rotatedX = -localY;
            rotatedY = localX;
            (width, height) = (height, width);
        }
        else if (quarterTurns == 2)
        {
            rotatedX = -localX;
            rotatedY = -localY;
        }
        else if (quarterTurns == 3)
        {
            rotatedX = localY;
            rotatedY = -localX;
            (width, height) = (height, width);
        }

        return PlacementMath.RectFromCenter(
            buildingPosition.X + rotatedX,
            buildingPosition.Y + rotatedY,
            width,
            height);
    }

    public static bool TryCenter(
        BuildSpec buildSpec,
        PlacementReservationKind kind,
        Vector2 buildingPosition,
        float facing,
        out Vector2 center)
    {
        var isCardinal = PlacementMath.TryNormalizeCardinalFacing(facing, out var cardinalFacing);
        for (var index = 0; index < buildSpec.PlacementReservations.Count; index++)
        {
            var reservation = buildSpec.PlacementReservations[index];
            if (reservation.Kind != kind)
            {
                continue;
            }

            var rect = WorldRect(buildSpec, reservation, buildingPosition, cardinalFacing);
            center = new Vector2(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
            return isCardinal;
        }

        center = buildingPosition;
        return false;
    }

    private static int QuarterTurns(float cardinalFacing)
    {
        var quarterTurns = (int)MathF.Round(cardinalFacing / (MathF.PI * 0.5f));
        return ((quarterTurns % 4) + 4) % 4;
    }
}
