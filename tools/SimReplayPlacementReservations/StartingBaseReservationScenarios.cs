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
                    var producerFaction = FactionCatalog.UnitFactionFor(faction);
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

    private static bool CircleIntersectsRect(MapPoint center, float radius, PlacementRect rect)
    {
        var closestX = Math.Clamp(center.X, rect.X, rect.EndX);
        var closestY = Math.Clamp(center.Y, rect.Y, rect.EndY);
        var deltaX = center.X - closestX;
        var deltaY = center.Y - closestY;
        return deltaX * deltaX + deltaY * deltaY <= radius * radius;
    }
}
