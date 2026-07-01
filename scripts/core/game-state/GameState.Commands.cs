using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    public void CommandMoveSelected(Vector2 target, MoveCommandMode mode = MoveCommandMode.Direct)
    {
        var selected = SelectedUnits().ToList();
        if (selected.Count == 0)
        {
            return;
        }

        var formationUnits = selected
            .Select(unit => new FormationUnit(unit.Id, unit.Position.X, unit.Position.Y, unit.RuntimeDescriptor.Radius))
            .ToList();
        var destinations = FormationMath.CreateMoveDestinations(
            formationUnits,
            target.X,
            target.Y,
            WorldSize.X,
            WorldSize.Y)
            .ToDictionary(destination => destination.Id);

        var sharedAssignments = new Dictionary<int, PathfindingCorridorAssignment>();
        if (selected.Count > 1)
        {
            var terrainCells = TerrainCells();
            foreach (var group in selected.GroupBy(unit => unit.RuntimeDescriptor.MovementDomain))
            {
                var groupUnits = group.ToList();
                if (groupUnits.Count <= 1)
                {
                    continue;
                }

                var movingIds = groupUnits.Select(unit => unit.Id).ToHashSet();
                var members = groupUnits
                    .Select(unit =>
                    {
                        var destination = destinations[unit.Id];
                        return new PathfindingCorridorMember(
                            unit.Id,
                            unit.Position.X,
                            unit.Position.Y,
                            destination.X,
                            destination.Y);
                    })
                    .ToList();
                var corridor = PathfindingMath.FindSharedCorridor(
                    members,
                    target.X,
                    target.Y,
                    WorldSize.X,
                    WorldSize.Y,
                    PathCellSize,
                    PathObstacles(group.Key, movingUnitIds: movingIds),
                    group.Key,
                    terrainCells);

                foreach (var assignment in corridor.Assignments)
                {
                    sharedAssignments[assignment.Id] = assignment;
                }
            }
        }

        foreach (var unit in selected)
        {
            var formationDestination = destinations[unit.Id];
            var destination = new Vector2(formationDestination.X, formationDestination.Y);

            if (sharedAssignments.TryGetValue(unit.Id, out var sharedPath))
            {
                AssignPath(unit, destination, target, sharedPath.Path, sharedPath.RawCells);
            }
            else
            {
                AssignPath(unit, destination, target);
            }

            unit.AnchorPosition = destination;
            unit.MoveMode = mode;
            unit.AttackTargetId = null;
            unit.AttackTargetKind = CombatTargetKind.Unit;
            unit.AttackTargetIsManual = false;
            unit.AttackTargetAllowsPursuit = false;
            ClearAttackTrackingMemory(unit);
            unit.PlayerIntentTarget = target;
            unit.CommandVisualTarget = target;
            unit.ReturnToAnchorAfterAttack = false;
            unit.LastSharedThreatKey = null;
            unit.ThreatShareCooldownRemaining = SharedThreatMemorySeconds;
            if (mode == MoveCommandMode.Attack && unit.Stance == UnitStance.Ignore)
            {
                unit.Stance = UnitStance.Aggressive;
            }
            else if (mode == MoveCommandMode.Ignore)
            {
                unit.Stance = UnitStance.Ignore;
                unit.RetaliationTargetId = null;
            }

            StopHarvesting(unit);
            unit.CommandPulse = 1;
        }
    }

    public bool CommandHarvestSelected(ResourceFieldModel field, out string status)
    {
        var harvesters = SelectedUnits()
            .Where(IsHarvesterUnit)
            .ToList();

        if (harvesters.Count == 0)
        {
            status = GameText.T("harvest.selectHarvester");
            return false;
        }

        if (field.Amount <= 0)
        {
            status = GameText.T("harvest.depleted");
            return false;
        }

        var assigned = 0;
        foreach (var harvester in harvesters)
        {
            var refinery = FindBestRefineryForHarvester(harvester.Owner, field.Position, harvester.Id);
            if (refinery is null)
            {
                continue;
            }

            StopCombatCommand(harvester);
            harvester.HarvesterMode = HarvesterMode.MovingToField;
            harvester.HarvestFieldId = field.Id;
            ReserveRefineryDock(harvester, refinery);
            AssignPath(harvester, field.Position, field.Position);
            harvester.AnchorPosition = field.Position;
            harvester.CommandPulse = 1;
            assigned++;
        }

        status = assigned == 0
            ? GameText.T("harvest.needRefinery")
            : GameText.Format("harvest.assigned", assigned, assigned == 1 ? "" : "s", field.Id);
        return assigned > 0;
    }

    public bool CommandSetSelectedBuildingRallyPoint(Vector2 target, out string status)
    {
        var selected = SelectedBuildings().ToList();
        if (selected.Count == 0)
        {
            status = GameText.T("rally.selectProducer");
            return false;
        }

        var producers = selected
            .Where(IsProductionBuilding)
            .ToList();
        if (producers.Count == 0)
        {
            status = GameText.T("rally.unsupported");
            return false;
        }

        var clamped = ClampInsideWorld(target, 80);
        foreach (var building in producers)
        {
            building.RallyPoint = clamped;
            building.RallyPulse = 1;
        }

        status = producers.Count == 1
            ? GameText.Format("rally.singleSet", BuildSpecCatalog.For(producers[0].Kind).Label)
            : GameText.Format("rally.multiSet", producers.Count);
        return true;
    }

    public void CommandAttackSelected(UnitModel target)
    {
        CommandAttackSelected(CombatTargetKind.Unit, target.Id);
    }

    public void CommandAttackSelected(BuildingModel target)
    {
        CommandAttackSelected(CombatTargetKind.Building, target.Id);
    }

    private void CommandAttackSelected(CombatTargetKind targetKind, int targetId)
    {
        var selected = SelectedUnits()
            .Where(unit => IsCombatTargetHostile(unit.Owner, targetKind, targetId))
            .Where(unit => CanUnitTarget(unit, targetKind, targetId))
            .ToList();
        var targetPosition = CombatTargetPosition(targetKind, targetId);
        if (selected.Count == 0 || targetPosition is null)
        {
            return;
        }

        var attackSlots = CreateAttackSlots(selected, targetKind, targetId, targetPosition.Value);
        foreach (var unit in selected)
        {
            unit.AttackTargetId = targetId;
            unit.AttackTargetKind = targetKind;
            unit.AttackTargetIsManual = true;
            unit.AttackTargetAllowsPursuit = true;
            RememberAttackTargetPosition(unit, targetPosition.Value);
            unit.PlayerIntentTarget = targetPosition.Value;
            unit.CommandVisualTarget = targetPosition.Value;
            unit.ReturnToAnchorAfterAttack = false;
            unit.LastSharedThreatKey = null;
            unit.ThreatShareCooldownRemaining = SharedThreatMemorySeconds;
            StopHarvesting(unit);
            if (IsUnitAtEngagementRange(unit, targetKind, targetId, targetPosition.Value))
            {
                SetCombatAnchor(unit);
            }
            else if (attackSlots.TryGetValue(unit.Id, out var slot))
            {
                AssignPath(unit, slot, targetPosition.Value);
                unit.AnchorPosition = slot;
            }
            else
            {
                AssignPath(unit, targetPosition.Value, targetPosition.Value);
            }

            unit.CommandPulse = 1;
        }
    }

    public void SetSelectedStance(UnitStance stance)
    {
        foreach (var unit in SelectedUnits())
        {
            unit.Stance = stance;
            unit.MoveMode = stance == UnitStance.Ignore ? MoveCommandMode.Ignore : MoveCommandMode.Direct;
            unit.AnchorPosition = unit.Position;
            unit.AttackTargetId = null;
            unit.AttackTargetKind = CombatTargetKind.Unit;
            unit.AttackTargetIsManual = false;
            unit.AttackTargetAllowsPursuit = false;
            ClearAttackTrackingMemory(unit);
            unit.PlayerIntentTarget = null;
            unit.ReturnToAnchorAfterAttack = false;
            unit.RetaliationTargetId = null;
            unit.LastSharedThreatKey = null;
            unit.ThreatShareCooldownRemaining = SharedThreatMemorySeconds;
            unit.CommandPulse = 1;
        }
    }

    private IReadOnlyDictionary<int, Vector2> CreateAttackSlots(
        IReadOnlyList<UnitModel> units,
        CombatTargetKind targetKind,
        int targetId,
        Vector2 targetPosition)
    {
        var occupiedSlots = Units
            .Where(unit => unit.Hp > 0 && unit.MovementState == UnitMovementState.CombatAnchor)
            .Where(unit => unit.AttackTargetId == targetId && unit.AttackTargetKind == targetKind)
            .Select(unit => (Position: unit.Position, Radius: unit.RuntimeDescriptor.Radius))
            .ToList();
        var slots = new Dictionary<int, Vector2>();

        foreach (var unit in units.Where(unit => IsUnitAtEngagementRange(unit, targetKind, targetId, targetPosition)))
        {
            slots[unit.Id] = unit.Position;
            occupiedSlots.Add((unit.Position, unit.RuntimeDescriptor.Radius));
        }

        foreach (var unit in units
            .Where(unit => !slots.ContainsKey(unit.Id))
            .OrderBy(unit => unit.Position.DistanceTo(targetPosition))
            .ThenBy(unit => unit.Id))
        {
            var slot = CreateAttackSlot(unit, targetKind, targetId, targetPosition, occupiedSlots);
            occupiedSlots.Add((slot, unit.RuntimeDescriptor.Radius));
            slots[unit.Id] = slot;
        }

        return slots;
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
