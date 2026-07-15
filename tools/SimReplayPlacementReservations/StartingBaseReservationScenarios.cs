using Godot;

static partial class Program
{
    private static IReadOnlyList<string> InitialBlockingUnitBuildingConflicts(MapSpec map)
    {
        var conflicts = new List<string>();
        foreach (var unit in map.Units)
        {
            var unitSpec = UnitDesignCatalog.Spec(unit.DesignId);
            if (!unitSpec.Collision.BlocksMovement || unitSpec.Collision.Radius <= 0)
            {
                continue;
            }

            var faction = map.OwnerStarts.First(start => start.OwnerId == unit.OwnerId).Faction;
            foreach (var building in map.Buildings)
            {
                var buildingSpec = BuildSpecCatalog.For(building.Kind);
                PlacementMath.TryNormalizeCardinalFacing(building.Facing, out var cardinalFacing);
                var footprint = buildingSpec.LogicalFootprint(cardinalFacing);
                var rect = PlacementMath.RectFromCenter(
                    building.Position.X,
                    building.Position.Y,
                    footprint.X,
                    footprint.Y);
                var conflictsWithBuilding = CircleIntersectsRect(unit.Position, unitSpec.Collision.Radius, rect);
                for (var reservationIndex = 0;
                     !conflictsWithBuilding && reservationIndex < buildingSpec.PlacementReservations.Count;
                     reservationIndex++)
                {
                    var reservation = PlacementReservationMath.WorldRect(
                        buildingSpec,
                        buildingSpec.PlacementReservations[reservationIndex],
                        building.Position.ToVector2(),
                        cardinalFacing);
                    conflictsWithBuilding = CircleIntersectsRect(unit.Position, unitSpec.Collision.Radius, reservation);
                }

                if (!conflictsWithBuilding
                    && PlacementReservationMath.TryCenter(
                        buildingSpec,
                        PlacementReservationKind.ProductionEgress,
                        building.Position.ToVector2(),
                        cardinalFacing,
                        out var egress))
                {
                    var producerFaction = ProductionKindDesignBridge.UnitFactionFor(faction);
                    var spawnRadius = UnitDesignCatalog.Designs.Values
                        .Where(design => design.Faction == producerFaction
                            && design.Production?.ProducerKind == building.Kind)
                        .Select(design => design.Collision.Radius)
                        .DefaultIfEmpty(0)
                        .Max();
                    var requiredDistance = spawnRadius + unitSpec.Collision.Radius + 6;
                    conflictsWithBuilding = unit.Position.ToVector2().DistanceSquaredTo(egress)
                        < requiredDistance * requiredDistance;
                }

                if (!conflictsWithBuilding)
                {
                    continue;
                }

                conflicts.Add(
                    $"owner={unit.OwnerId.Value} faction={faction} unit={unit.DesignId} "
                    + $"building={building.Kind}@owner={building.OwnerId.Value}");
            }
        }

        return conflicts;
    }

    private static IReadOnlyList<string> InitialBuildingReservationConflicts(MapSpec map)
    {
        var conflicts = new List<string>();
        for (var firstIndex = 0; firstIndex < map.Buildings.Count; firstIndex++)
        {
            var first = map.Buildings[firstIndex];
            var firstSpec = BuildSpecCatalog.For(first.Kind);
            PlacementMath.TryNormalizeCardinalFacing(first.Facing, out var firstFacing);
            var firstHard = PlacementMath.RectFromCenter(
                first.Position.X,
                first.Position.Y,
                firstSpec.LogicalFootprint(firstFacing).X,
                firstSpec.LogicalFootprint(firstFacing).Y);
            for (var secondIndex = firstIndex + 1; secondIndex < map.Buildings.Count; secondIndex++)
            {
                var second = map.Buildings[secondIndex];
                var secondSpec = BuildSpecCatalog.For(second.Kind);
                PlacementMath.TryNormalizeCardinalFacing(second.Facing, out var secondFacing);
                var secondSize = secondSpec.LogicalFootprint(secondFacing);
                var secondHard = PlacementMath.RectFromCenter(
                    second.Position.X,
                    second.Position.Y,
                    secondSize.X,
                    secondSize.Y);
                var clearance = Math.Max(firstSpec.PlacementClearanceCells, secondSpec.PlacementClearanceCells)
                    * PlacementMath.GridSize;
                var hasConflict = false;
                for (var reservationIndex = 0;
                     !hasConflict && reservationIndex < firstSpec.PlacementReservations.Count;
                     reservationIndex++)
                {
                    var reservation = PlacementReservationMath.WorldRect(
                        firstSpec,
                        firstSpec.PlacementReservations[reservationIndex],
                        first.Position.ToVector2(),
                        firstFacing);
                    hasConflict = ReservationRectsConflict(reservation, secondHard, clearance);
                    for (var otherIndex = 0;
                         !hasConflict && otherIndex < secondSpec.PlacementReservations.Count;
                         otherIndex++)
                    {
                        var other = PlacementReservationMath.WorldRect(
                            secondSpec,
                            secondSpec.PlacementReservations[otherIndex],
                            second.Position.ToVector2(),
                            secondFacing);
                        hasConflict = ReservationRectsConflict(reservation, other, clearance);
                    }
                }

                for (var reservationIndex = 0;
                     !hasConflict && reservationIndex < secondSpec.PlacementReservations.Count;
                     reservationIndex++)
                {
                    var reservation = PlacementReservationMath.WorldRect(
                        secondSpec,
                        secondSpec.PlacementReservations[reservationIndex],
                        second.Position.ToVector2(),
                        secondFacing);
                    hasConflict = ReservationRectsConflict(firstHard, reservation, clearance);
                }

                if (hasConflict)
                {
                    conflicts.Add($"{first.Kind}@{first.Position} <-> {second.Kind}@{second.Position}");
                }
            }
        }

        return conflicts;
    }

    private static bool ReservationRectsConflict(PlacementRect first, PlacementRect second, float clearance)
    {
        var xGap = first.EndX <= second.X
            ? second.X - first.EndX
            : second.EndX <= first.X
                ? first.X - second.EndX
                : 0;
        var yGap = first.EndY <= second.Y
            ? second.Y - first.EndY
            : second.EndY <= first.Y
                ? first.Y - second.EndY
                : 0;
        var overlaps = first.X < second.EndX
            && first.EndX > second.X
            && first.Y < second.EndY
            && first.EndY > second.Y;
        return overlaps || (clearance > 0 && xGap < clearance && yGap < clearance);
    }

    private static bool CircleIntersectsRect(MapPoint center, float radius, PlacementRect rect)
    {
        var closestX = Math.Clamp(center.X, rect.X, rect.EndX);
        var closestY = Math.Clamp(center.Y, rect.Y, rect.EndY);
        var deltaX = center.X - closestX;
        var deltaY = center.Y - closestY;
        return deltaX * deltaX + deltaY * deltaY <= radius * radius;
    }
}
