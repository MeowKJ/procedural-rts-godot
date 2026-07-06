using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    public void CommandMoveSelected(Vector2 target, MoveCommandMode mode = MoveCommandMode.Direct)
    {
        CollectSelectedCommandUnits(_legacySelectedCommandUnits);
        if (_legacySelectedCommandUnits.Count == 0)
        {
            return;
        }

        PrepareLegacyMoveCommandBuffers(_legacySelectedCommandUnits, target);

        foreach (var unit in _legacySelectedCommandUnits)
        {
            var formationDestination = _legacyMoveDestinations[unit.Id];
            var destination = new Vector2(formationDestination.X, formationDestination.Y);

            if (_legacySharedMoveAssignments.TryGetValue(unit.Id, out var sharedPath))
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
        CollectSelectedHarvesters(_legacySelectedHarvesters);
        if (_legacySelectedHarvesters.Count == 0)
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
        foreach (var harvester in _legacySelectedHarvesters)
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
        CollectSelectedBuildings(_legacySelectedBuildings);
        if (_legacySelectedBuildings.Count == 0)
        {
            status = GameText.T("rally.selectProducer");
            return false;
        }

        CollectSelectedProductionBuildings(_legacySelectedBuildings, _legacySelectedProducers);
        if (_legacySelectedProducers.Count == 0)
        {
            status = GameText.T("rally.unsupported");
            return false;
        }

        var clamped = ClampInsideWorld(target, 80);
        foreach (var building in _legacySelectedProducers)
        {
            building.RallyPoint = clamped;
            building.RallyPulse = 1;
        }

        status = _legacySelectedProducers.Count == 1
            ? GameText.Format("rally.singleSet", BuildSpecCatalog.For(_legacySelectedProducers[0].Kind).Label)
            : GameText.Format("rally.multiSet", _legacySelectedProducers.Count);
        return true;
    }

    public int CommandSetProducerRallyPoints(Owner owner, Vector2 target)
    {
        var assigned = 0;
        var clamped = ClampInsideWorld(target, 80);
        foreach (var building in Buildings)
        {
            if (building.Owner != owner
                || building.RallyPoint is not null
                || !IsProductionBuilding(building))
            {
                continue;
            }

            building.RallyPoint = clamped;
            building.RallyPulse = 1;
            assigned++;
        }

        return assigned;
    }

    public bool CommandEnqueueProduction(ProductionKind productionKind, Owner owner, out string status)
    {
        return EnqueueProduction(productionKind, owner, out status);
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
        CollectSelectedAttackCommandUnits(targetKind, targetId, _legacySelectedCommandUnits);
        var targetPosition = CombatTargetPosition(targetKind, targetId);
        if (_legacySelectedCommandUnits.Count == 0 || targetPosition is null)
        {
            return;
        }

        CommandAttackUnitsCore(_legacySelectedCommandUnits, targetKind, targetId, targetPosition.Value);
    }

    public int CommandAttackUnits(IReadOnlyList<UnitModel> units, CombatTargetKind targetKind, int targetId)
    {
        _legacySelectedCommandUnits.Clear();
        foreach (var unit in units)
        {
            if (unit.Hp > 0
                && IsCombatTargetHostile(unit.Owner, targetKind, targetId)
                && CanUnitTarget(unit, targetKind, targetId))
            {
                _legacySelectedCommandUnits.Add(unit);
            }
        }

        var targetPosition = CombatTargetPosition(targetKind, targetId);
        if (_legacySelectedCommandUnits.Count == 0 || targetPosition is null)
        {
            return 0;
        }

        return CommandAttackUnitsCore(_legacySelectedCommandUnits, targetKind, targetId, targetPosition.Value);
    }

    private int CommandAttackUnitsCore(IReadOnlyList<UnitModel> units, CombatTargetKind targetKind, int targetId, Vector2 targetPosition)
    {
        var attackSlots = CreateAttackSlots(units, targetKind, targetId, targetPosition);
        foreach (var unit in units)
        {
            unit.AttackTargetId = targetId;
            unit.AttackTargetKind = targetKind;
            unit.AttackTargetIsManual = true;
            unit.AttackTargetAllowsPursuit = true;
            RememberAttackTargetPosition(unit, targetPosition);
            unit.PlayerIntentTarget = targetPosition;
            unit.CommandVisualTarget = targetPosition;
            unit.ReturnToAnchorAfterAttack = false;
            unit.LastSharedThreatKey = null;
            unit.ThreatShareCooldownRemaining = SharedThreatMemorySeconds;
            StopHarvesting(unit);
            if (IsUnitAtEngagementRange(unit, targetKind, targetId, targetPosition))
            {
                SetCombatAnchor(unit);
            }
            else if (attackSlots.TryGetValue(unit.Id, out var slot))
            {
                AssignPath(unit, slot, targetPosition);
                unit.AnchorPosition = slot;
            }
            else
            {
                AssignPath(unit, targetPosition, targetPosition);
            }

            unit.CommandPulse = 1;
        }

        return units.Count;
    }

    public void SetSelectedStance(UnitStance stance)
    {
        CollectSelectedCommandUnits(_legacySelectedCommandUnits);
        foreach (var unit in _legacySelectedCommandUnits)
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

    private void CollectSelectedHarvesters(List<UnitModel> result)
    {
        result.Clear();
        foreach (var unit in Units)
        {
            if (unit.Owner == Owner.Player && unit.Selected && IsHarvesterUnit(unit))
            {
                result.Add(unit);
            }
        }
    }

    private void CollectSelectedBuildings(List<BuildingModel> result)
    {
        result.Clear();
        foreach (var building in Buildings)
        {
            if (building.Owner == Owner.Player && building.Selected)
            {
                result.Add(building);
            }
        }
    }

    private static void CollectSelectedProductionBuildings(IReadOnlyList<BuildingModel> selected, List<BuildingModel> result)
    {
        result.Clear();
        foreach (var building in selected)
        {
            if (IsProductionBuilding(building))
            {
                result.Add(building);
            }
        }
    }
}
