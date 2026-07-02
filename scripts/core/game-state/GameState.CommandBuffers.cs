using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    private void CollectSelectedCommandUnits(List<UnitModel> result)
    {
        result.Clear();
        foreach (var unit in Units)
        {
            if (unit.Owner == Owner.Player && unit.Selected)
            {
                result.Add(unit);
            }
        }
    }

    private void CollectSelectedAttackCommandUnits(CombatTargetKind targetKind, int targetId, List<UnitModel> result)
    {
        result.Clear();
        foreach (var unit in Units)
        {
            if (unit.Owner == Owner.Player
                && unit.Selected
                && IsCombatTargetHostile(unit.Owner, targetKind, targetId)
                && CanUnitTarget(unit, targetKind, targetId))
            {
                result.Add(unit);
            }
        }
    }

    private void PrepareLegacyMoveCommandBuffers(IReadOnlyList<UnitModel> selected, Vector2 target)
    {
        _legacyMoveFormationUnits.Clear();
        foreach (var unit in selected)
        {
            _legacyMoveFormationUnits.Add(new FormationUnit(unit.Id, unit.Position.X, unit.Position.Y, unit.RuntimeDescriptor.Radius));
        }

        FormationMath.CreateMoveDestinationsInto(
            _legacyMoveFormationUnits,
            target.X,
            target.Y,
            WorldSize.X,
            WorldSize.Y,
            _legacyMoveDestinationResults,
            _legacyMoveOrderedUnits,
            _legacyMoveSlots,
            _legacyMoveRemainingSlots);

        _legacyMoveDestinations.Clear();
        foreach (var destination in _legacyMoveDestinationResults)
        {
            _legacyMoveDestinations[destination.Id] = destination;
        }

        _legacySharedMoveAssignments.Clear();
        if (selected.Count <= 1)
        {
            return;
        }

        var terrainCells = TerrainCells();
        CollectLegacyMoveDomainAssignments(selected, target, MovementDomain.Land, terrainCells);
        CollectLegacyMoveDomainAssignments(selected, target, MovementDomain.Naval, terrainCells);
        CollectLegacyMoveDomainAssignments(selected, target, MovementDomain.Air, terrainCells);
        CollectLegacyMoveDomainAssignments(selected, target, MovementDomain.Amphibious, terrainCells);
    }

    private void CollectLegacyMoveDomainAssignments(
        IReadOnlyList<UnitModel> selected,
        Vector2 target,
        MovementDomain domain,
        IReadOnlyCollection<GridTerrain> terrainCells)
    {
        _legacyMoveDomainUnits.Clear();
        foreach (var unit in selected)
        {
            if (unit.RuntimeDescriptor.MovementDomain == domain)
            {
                _legacyMoveDomainUnits.Add(unit);
            }
        }

        if (_legacyMoveDomainUnits.Count <= 1)
        {
            return;
        }

        _legacyMovingUnitIds.Clear();
        _legacyPathCorridorMembers.Clear();
        foreach (var unit in _legacyMoveDomainUnits)
        {
            var destination = _legacyMoveDestinations[unit.Id];
            _legacyMovingUnitIds.Add(unit.Id);
            _legacyPathCorridorMembers.Add(new PathfindingCorridorMember(
                unit.Id,
                unit.Position.X,
                unit.Position.Y,
                destination.X,
                destination.Y));
        }

        var corridor = PathfindingMath.FindSharedCorridor(
            _legacyPathWorkspace,
            _legacyPathCorridorMembers,
            target.X,
            target.Y,
            WorldSize.X,
            WorldSize.Y,
            PathCellSize,
            PathObstacles(domain, movingUnitIds: _legacyMovingUnitIds),
            domain,
            terrainCells,
            _legacyPathCorridorAssignments);

        foreach (var assignment in corridor.Assignments)
        {
            _legacySharedMoveAssignments[assignment.Id] = assignment;
        }
    }

    private IReadOnlyDictionary<int, Vector2> CreateAttackSlots(
        IReadOnlyList<UnitModel> units,
        CombatTargetKind targetKind,
        int targetId,
        Vector2 targetPosition)
    {
        CollectOccupiedAttackSlots(targetKind, targetId, excludedUnitId: 0, _legacyAttackOccupiedSlots);
        _legacyAttackSlots.Clear();

        foreach (var unit in units)
        {
            if (IsUnitAtEngagementRange(unit, targetKind, targetId, targetPosition))
            {
                _legacyAttackSlots[unit.Id] = unit.Position;
                _legacyAttackOccupiedSlots.Add((unit.Position, unit.RuntimeDescriptor.Radius));
            }
        }

        while (_legacyAttackSlots.Count < units.Count)
        {
            var next = NextAttackSlotUnit(units, targetPosition);
            if (next is null)
            {
                break;
            }

            var slot = CreateAttackSlot(next, targetKind, targetId, targetPosition, _legacyAttackOccupiedSlots);
            _legacyAttackOccupiedSlots.Add((slot, next.RuntimeDescriptor.Radius));
            _legacyAttackSlots[next.Id] = slot;
        }

        return _legacyAttackSlots;
    }

    private UnitModel? NextAttackSlotUnit(IReadOnlyList<UnitModel> units, Vector2 targetPosition)
    {
        UnitModel? best = null;
        var bestDistance = float.MaxValue;
        var bestId = int.MaxValue;
        foreach (var unit in units)
        {
            if (_legacyAttackSlots.ContainsKey(unit.Id))
            {
                continue;
            }

            var distance = unit.Position.DistanceTo(targetPosition);
            if (distance < bestDistance || (distance == bestDistance && unit.Id < bestId))
            {
                best = unit;
                bestDistance = distance;
                bestId = unit.Id;
            }
        }

        return best;
    }

    private void CollectOccupiedAttackSlots(
        CombatTargetKind targetKind,
        int targetId,
        int excludedUnitId,
        List<(Vector2 Position, float Radius)> result)
    {
        result.Clear();
        foreach (var unit in Units)
        {
            if (unit.Id != excludedUnitId
                && unit.Hp > 0
                && unit.MovementState == UnitMovementState.CombatAnchor
                && unit.AttackTargetId == targetId
                && unit.AttackTargetKind == targetKind)
            {
                result.Add((unit.Position, unit.RuntimeDescriptor.Radius));
            }
        }
    }

    private Vector2 CreateAttackSlot(
        UnitModel unit,
        CombatTargetKind targetKind,
        int targetId,
        Vector2 targetPosition,
        IReadOnlyList<(Vector2 Position, float Radius)> occupiedSlots)
    {
        var unitDescriptor = unit.RuntimeDescriptor;
        var targetRadius = CombatTargetRadius(targetKind, targetId);
        var rangeLimit = EngagementRange(unit, targetKind, targetId);
        var minimumDistance = targetRadius + unitDescriptor.Radius + 18;
        var preferredDistance = Mathf.Clamp(
            rangeLimit * 0.88f + targetRadius * 0.12f,
            Math.Min(minimumDistance, rangeLimit - 8),
            Math.Max(minimumDistance, rangeLimit - 8));
        var preferredAngle = (unit.Position - targetPosition).Angle();
        var bestSlot = ClampInsideWorld(targetPosition + Vector2.FromAngle(preferredAngle) * preferredDistance, unitDescriptor.Radius + 28);
        var bestScore = float.MaxValue;

        for (var ring = 0; ring < 3; ring++)
        {
            var ringDistance = Mathf.Clamp(preferredDistance - ring * unitDescriptor.Radius * 0.55f, minimumDistance, rangeLimit - 6);
            for (var step = 0; step < 24; step++)
            {
                var signedStep = step == 0 ? 0 : (step % 2 == 1 ? (step + 1) / 2 : -step / 2);
                var angle = preferredAngle + signedStep * Mathf.Pi / 12f;
                var candidate = ClampInsideWorld(targetPosition + Vector2.FromAngle(angle) * ringDistance, unitDescriptor.Radius + 28);
                var score = unit.Position.DistanceTo(candidate) + Math.Abs(signedStep) * 7 + ring * 18;
                foreach (var occupied in occupiedSlots)
                {
                    var desiredSpacing = occupied.Radius + unitDescriptor.Radius + 14;
                    var distance = candidate.DistanceTo(occupied.Position);
                    if (distance < desiredSpacing)
                    {
                        score += (desiredSpacing - distance) * 35;
                    }
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestSlot = candidate;
                }
            }
        }

        return bestSlot;
    }
}
