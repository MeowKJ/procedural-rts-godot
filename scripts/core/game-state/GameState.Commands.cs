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

        var attackSlots = CreateAttackSlots(_legacySelectedCommandUnits, targetKind, targetId, targetPosition.Value);
        foreach (var unit in _legacySelectedCommandUnits)
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
