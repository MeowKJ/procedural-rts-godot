using Godot;

namespace ProceduralRts.Core;

public sealed partial class ConstructionSystem
{
    private static string? ObstacleAndReservationRejectionReason(
        BuildSpec spec,
        PlacementRect footprint,
        Vector2 footprintSize,
        Vector2 candidatePosition,
        float cardinalFacing,
        List<PlacementObstacle> obstacles,
        List<PlacementReservationObstacle> reservations,
        List<PlacementResourceObstacle> resourceObstacles)
    {
        for (var index = 0; index < obstacles.Count; index++)
        {
            if (PlacementMath.Intersects(footprint, ObstacleRect(obstacles[index])))
            {
                return "placement.blocked";
            }
        }

        for (var index = 0; index < obstacles.Count; index++)
        {
            var obstacle = obstacles[index];
            var clearanceCells = Math.Max(spec.PlacementClearanceCells, obstacle.ClearanceCells);
            if (clearanceCells <= 0)
            {
                continue;
            }

            var clearance = clearanceCells * PlacementMath.GridSize;
            var clearanceRect = PlacementMath.RectFromCenter(
                candidatePosition.X,
                candidatePosition.Y,
                footprintSize.X + clearance * 2,
                footprintSize.Y + clearance * 2);
            if (PlacementMath.Intersects(clearanceRect, ObstacleRect(obstacle)))
            {
                return "placement.clearance";
            }
        }

        for (var reservationIndex = 0; reservationIndex < spec.PlacementReservations.Count; reservationIndex++)
        {
            var candidateReservation = PlacementReservationMath.WorldRect(
                spec,
                spec.PlacementReservations[reservationIndex],
                candidatePosition,
                cardinalFacing);
            for (var obstacleIndex = 0; obstacleIndex < obstacles.Count; obstacleIndex++)
            {
                var obstacle = obstacles[obstacleIndex];
                if (!obstacle.IsMapEnvironment)
                {
                    continue;
                }

                var obstacleRect = ObstacleRect(obstacle);
                if (PlacementMath.Intersects(candidateReservation, obstacleRect))
                {
                    return "placement.blocked";
                }

                var clearance = spec.PlacementClearanceCells * PlacementMath.GridSize;
                if (PlacementMath.ViolatesClearance(candidateReservation, obstacleRect, clearance))
                {
                    return "placement.clearance";
                }
            }
        }

        var resourceClearance = MapPlacementRules.ResourceClearance(spec);
        for (var resourceIndex = 0; resourceIndex < resourceObstacles.Count; resourceIndex++)
        {
            if (PlacementMath.ViolatesClearance(
                    footprint,
                    resourceObstacles[resourceIndex],
                    resourceClearance))
            {
                return "placement.reserved";
            }
        }

        for (var reservationIndex = 0; reservationIndex < spec.PlacementReservations.Count; reservationIndex++)
        {
            var candidateReservation = PlacementReservationMath.WorldRect(
                spec,
                spec.PlacementReservations[reservationIndex],
                candidatePosition,
                cardinalFacing);
            for (var resourceIndex = 0; resourceIndex < resourceObstacles.Count; resourceIndex++)
            {
                if (PlacementMath.ViolatesClearance(
                        candidateReservation,
                        resourceObstacles[resourceIndex],
                        resourceClearance))
                {
                    return "placement.reserved";
                }
            }
        }

        for (var reservationIndex = 0; reservationIndex < spec.PlacementReservations.Count; reservationIndex++)
        {
            var candidateReservation = PlacementReservationMath.WorldRect(
                spec,
                spec.PlacementReservations[reservationIndex],
                candidatePosition,
                cardinalFacing);
            for (var obstacleIndex = 0; obstacleIndex < obstacles.Count; obstacleIndex++)
            {
                var obstacle = obstacles[obstacleIndex];
                if (obstacle.IsMapEnvironment)
                {
                    continue;
                }

                var pairClearance = Math.Max(spec.PlacementClearanceCells, obstacle.ClearanceCells)
                    * PlacementMath.GridSize;
                if (PlacementMath.ViolatesClearance(candidateReservation, ObstacleRect(obstacle), pairClearance))
                {
                    return "placement.reserved";
                }
            }

            for (var existingIndex = 0; existingIndex < reservations.Count; existingIndex++)
            {
                var existing = reservations[existingIndex];
                var pairClearance = Math.Max(spec.PlacementClearanceCells, existing.ClearanceCells)
                    * PlacementMath.GridSize;
                if (PlacementMath.ViolatesClearance(candidateReservation, ReservationRect(existing), pairClearance))
                {
                    return "placement.reserved";
                }
            }
        }

        for (var existingIndex = 0; existingIndex < reservations.Count; existingIndex++)
        {
            var existing = reservations[existingIndex];
            var pairClearance = Math.Max(spec.PlacementClearanceCells, existing.ClearanceCells)
                * PlacementMath.GridSize;
            if (PlacementMath.ViolatesClearance(footprint, ReservationRect(existing), pairClearance))
            {
                return "placement.reserved";
            }
        }

        return null;
    }
}
