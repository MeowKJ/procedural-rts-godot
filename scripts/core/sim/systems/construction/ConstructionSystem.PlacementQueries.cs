using Godot;

namespace ProceduralRts.Core;

public sealed partial class ConstructionSystem
{
    public PlacementResult QueryBuildingPlacement(
        EntityWorld world,
        OwnerId ownerId,
        BuildSpec spec,
        Vector2 desiredPosition,
        float facing,
        ConstructionPlacementIntent intent)
    {
        var isCardinal = PlacementMath.TryNormalizeCardinalFacing(facing, out var cardinalFacing);
        var footprintCells = spec.FootprintCells.Rotated(cardinalFacing);
        var footprintSize = footprintCells.IsValid ? footprintCells.WorldSize : spec.Footprint;
        var snappedX = PlacementMath.SnapAnchor(desiredPosition.X, footprintCells.WidthCells);
        var snappedY = PlacementMath.SnapAnchor(desiredPosition.Y, footprintCells.HeightCells);
        var footprint = PlacementMath.RectFromCenter(snappedX, snappedY, footprintSize.X, footprintSize.Y);

        if (!isCardinal)
        {
            return new PlacementResult(snappedX, snappedY, false, "placement.rotation");
        }

        if (footprint.X < 0
            || footprint.Y < 0
            || footprint.EndX > world.WorldWidth
            || footprint.EndY > world.WorldHeight)
        {
            return new PlacementResult(snappedX, snappedY, false, "placement.outside");
        }

        for (var reservationIndex = 0; reservationIndex < spec.PlacementReservations.Count; reservationIndex++)
        {
            var reservation = PlacementReservationMath.WorldRect(
                spec,
                spec.PlacementReservations[reservationIndex],
                new Vector2(snappedX, snappedY),
                cardinalFacing);
            if (reservation.X < 0
                || reservation.Y < 0
                || reservation.EndX > world.WorldWidth
                || reservation.EndY > world.WorldHeight)
            {
                return new PlacementResult(snappedX, snappedY, false, "placement.outside");
            }
        }

        CollectPlacementSnapshot(
            world,
            ownerId,
            _placementBuildAnchors,
            _placementObstacles,
            _placementReservations,
            _placementResourceObstacles,
            _placementVisibility);

        if (intent == ConstructionPlacementIntent.ReadyTicket || RequiresBuildAuthority(spec))
        {
            var authority = BuildAuthorityAt(snappedX, snappedY, footprintSize.X, footprintSize.Y, _placementBuildAnchors);
            if (authority == PlacementBuildAuthority.Unpowered)
            {
                return new PlacementResult(snappedX, snappedY, false, "placement.unpowered");
            }

            if (authority == PlacementBuildAuthority.Outside)
            {
                return new PlacementResult(snappedX, snappedY, false, "placement.outsideBuildRadius");
            }
        }

        var candidatePosition = new Vector2(snappedX, snappedY);
        if (EnvironmentPlacementRejectionReason(
                world,
                spec,
                footprint,
                candidatePosition,
                cardinalFacing,
                _placementVisibility) is { } environmentReason)
        {
            return new PlacementResult(snappedX, snappedY, false, environmentReason);
        }

        if (ObstacleAndReservationRejectionReason(
                spec,
                footprint,
                footprintSize,
                candidatePosition,
                cardinalFacing,
                _placementObstacles,
                _placementReservations,
                _placementResourceObstacles) is { } obstacleReason)
        {
            return new PlacementResult(snappedX, snappedY, false, obstacleReason);
        }

        return new PlacementResult(snappedX, snappedY, true, "placement.ready");
    }

    private enum PlacementBuildAuthority
    {
        Outside,
        Unpowered,
        Powered,
    }

    private static PlacementBuildAuthority BuildAuthorityAt(
        float centerX,
        float centerY,
        float width,
        float height,
        List<PlacementBuildAnchor> buildAnchors)
    {
        var footprintRadius = MathF.Max(width, height) * 0.5f;
        var foundUnpoweredAnchor = false;
        for (var index = 0; index < buildAnchors.Count; index++)
        {
            var anchor = buildAnchors[index];
            if (anchor.Radius <= 0)
            {
                continue;
            }

            var dx = centerX - anchor.X;
            var dy = centerY - anchor.Y;
            var allowed = anchor.Radius + footprintRadius;
            if (dx * dx + dy * dy > allowed * allowed)
            {
                continue;
            }

            if (anchor.Powered)
            {
                return PlacementBuildAuthority.Powered;
            }

            foundUnpoweredAnchor = true;
        }

        return foundUnpoweredAnchor ? PlacementBuildAuthority.Unpowered : PlacementBuildAuthority.Outside;
    }

}
