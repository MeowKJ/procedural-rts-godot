using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    private void UpdateProductionQueues(float dt)
    {
        foreach (var building in Buildings.ToList())
        {
            if (building.ProductionQueue.Count == 0 || building.Hp <= 0 || !building.Powered || building.BuildProgress < 1)
            {
                continue;
            }

            var item = building.ProductionQueue[0];
            var spec = UnitDesignCatalog.Spec(item.DesignId);
            var production = spec.Production
                ?? throw new InvalidOperationException($"UnitDesign '{spec.Id}' cannot advance production queue item {item.Kind}.");
            var advance = ProductionMath.Advance(item.Progress, dt, production.Duration);
            item.Progress = advance.Progress;

            if (!advance.IsComplete)
            {
                continue;
            }

            var spawn = ProducedUnitSpawnPoint(building, item.DesignId);
            building.ProductionQueue.RemoveAt(0);
            var completed = new CompletedProductionItem(
                building.Id,
                item.Kind,
                item.DesignId,
                item.FactionId,
                new Vector2(spawn.X, spawn.Y),
                building.Facing);
            CompletedProduction.Add(completed);
            SpawnProducedUnit(building, completed);
            ProductionCompleted?.Invoke(building, completed);
        }
    }

    private UnitModel SpawnProducedUnit(BuildingModel producer, CompletedProductionItem completed)
    {
        var spawn = completed.SpawnPosition;
        if (spawn is null)
        {
            var spawnPoint = ProducedUnitSpawnPoint(producer, completed.DesignId);
            spawn = new Vector2(spawnPoint.X, spawnPoint.Y);
        }

        var unit = AddUnit(completed.DesignId, producer.Owner, spawn.Value, completed.Facing, completed.FactionId);
        unit.CommandPulse = 1;
        if (producer.RallyPoint is { } rallyPoint)
        {
            AssignPath(unit, rallyPoint, rallyPoint);
            unit.AnchorPosition = rallyPoint;
            unit.CommandPulse = 1;
        }
        return unit;
    }

    private SpawnPoint ProducedUnitSpawnPoint(BuildingModel producer, string designId)
    {
        var producerSpec = BuildSpecCatalog.For(producer.Kind);
        var unitDescriptor = UnitDesignDefinitionCatalog.RuntimeDescriptors[designId];
        return ProductionSpawnMath.FindSpawnPoint(
            producer.Position.X,
            producer.Position.Y,
            producer.Facing,
            producerSpec.Footprint.X,
            producerSpec.Footprint.Y,
            unitDescriptor.Radius,
            WorldSize.X,
            WorldSize.Y,
            UnitObstacles());
    }

    private void UpdateHarvester(UnitModel unit, float dt)
    {
        if (!IsHarvesterUnit(unit) || unit.HarvesterMode == HarvesterMode.Idle)
        {
            return;
        }

        var field = unit.HarvestFieldId is null ? null : ResourceFieldById(unit.HarvestFieldId.Value);
        if (field is null || (field.Amount <= 0 && unit.Cargo <= 0))
        {
            StopHarvesting(unit);
            return;
        }

        switch (unit.HarvesterMode)
        {
            case HarvesterMode.MovingToField:
                UpdateHarvesterMovingToField(unit, field);
                break;
            case HarvesterMode.Gathering:
                UpdateHarvesterGathering(unit, field, dt);
                break;
            case HarvesterMode.ReturningToRefinery:
                UpdateHarvesterReturning(unit);
                break;
            case HarvesterMode.Unloading:
                UpdateHarvesterUnloading(unit, field, dt);
                break;
        }
    }

    private void UpdateHarvesterMovingToField(UnitModel unit, ResourceFieldModel field)
    {
        var gatherDistance = field.Radius * 0.72f + unit.RuntimeDescriptor.Radius;
        if (unit.Position.DistanceTo(field.Position) > gatherDistance)
        {
            AssignPath(unit, field.Position, field.Position);
            return;
        }

        ClearMoveTarget(unit);
        unit.HarvesterMode = HarvesterMode.Gathering;
        unit.HarvestPulse = 1;
    }

    private void UpdateHarvesterGathering(UnitModel unit, ResourceFieldModel field, float dt)
    {
        if (unit.Cargo >= HarvesterCargoCapacity || field.Amount <= 0)
        {
            SendHarvesterToRefinery(unit);
            return;
        }

        var amount = Math.Min(
            Math.Min(Mathf.CeilToInt(HarvestRate * dt), HarvesterCargoCapacity - unit.Cargo),
            field.Amount);
        if (amount <= 0)
        {
            SendHarvesterToRefinery(unit);
            return;
        }

        unit.Cargo += amount;
        field.Amount -= amount;
        field.Pulse = 1;
        unit.HarvestPulse = 1;

        if (unit.Cargo >= HarvesterCargoCapacity || field.Amount <= 0)
        {
            SendHarvesterToRefinery(unit);
        }
    }

    private void SendHarvesterToRefinery(UnitModel unit)
    {
        var refinery = unit.HarvestRefineryId is null
            ? FindBestRefineryForHarvester(unit.Owner, unit.Position, unit.Id)
            : BuildingById(unit.HarvestRefineryId.Value);
        refinery ??= FindBestRefineryForHarvester(unit.Owner, unit.Position, unit.Id);
        if (refinery is null)
        {
            StopHarvesting(unit);
            return;
        }

        ReserveRefineryDock(unit, refinery);
        unit.HarvesterMode = HarvesterMode.ReturningToRefinery;
        var deliveryPoint = RefineryDeliveryPoint(refinery);
        AssignPath(unit, deliveryPoint, deliveryPoint);
        unit.CommandPulse = 1;
    }

    private void UpdateHarvesterReturning(UnitModel unit)
    {
        var refinery = unit.HarvestRefineryId is null ? null : BuildingById(unit.HarvestRefineryId.Value);
        if (refinery is null || refinery.Hp <= 0)
        {
            refinery = FindBestRefineryForHarvester(unit.Owner, unit.Position, unit.Id);
            if (refinery is null)
            {
                StopHarvesting(unit);
                return;
            }

            ReserveRefineryDock(unit, refinery);
        }

        if (!CanUseRefineryDock(refinery, unit.Id))
        {
            var waitPoint = RefineryWaitPoint(refinery, unit.Id);
            AssignPath(unit, waitPoint, waitPoint);
            return;
        }

        ReserveRefineryDock(unit, refinery);
        var deliveryPoint = RefineryDeliveryPoint(refinery);
        if (unit.Position.DistanceTo(deliveryPoint) > 8)
        {
            AssignPath(unit, deliveryPoint, deliveryPoint);
            return;
        }

        ClearMoveTarget(unit);
        unit.HarvesterMode = HarvesterMode.Unloading;
        refinery.DockedHarvesterId = unit.Id;
        refinery.DockReservedByHarvesterId = null;
        unit.HarvestPulse = 1;
    }

    private void UpdateHarvesterUnloading(UnitModel unit, ResourceFieldModel field, float dt)
    {
        if (unit.Cargo <= 0)
        {
            if (field.Amount <= 0)
            {
                StopHarvesting(unit);
                return;
            }

            ClearRefineryDockClaim(unit.Id);
            unit.HarvesterMode = HarvesterMode.MovingToField;
            AssignPath(unit, field.Position, field.Position);
            unit.CommandPulse = 1;
            return;
        }

        var amount = Mathf.Min(Mathf.CeilToInt(UnloadRate * dt), unit.Cargo);
        unit.Cargo -= amount;
        var inventory = ResourceInventory(unit.Owner);
        inventory.Credits += amount;
        ResourceInventoryChanged?.Invoke(unit.Owner, inventory);
        if (unit.HarvestRefineryId is not null && BuildingById(unit.HarvestRefineryId.Value) is { } refinery)
        {
            refinery.DeliveryPulse = 1;
        }
        unit.HarvestPulse = 1;
    }

    public Vector2 RefineryDeliveryPoint(BuildingModel refinery)
    {
        var spec = BuildSpecCatalog.For(refinery.Kind);
        var forward = Vector2.FromAngle(refinery.Facing);
        return refinery.Position + forward * (Mathf.Max(spec.Footprint.X, spec.Footprint.Y) * 0.5f + 54);
    }

    private Vector2 RefineryWaitPoint(BuildingModel refinery, int harvesterId)
    {
        var deliveryPoint = RefineryDeliveryPoint(refinery);
        var side = Vector2.FromAngle(refinery.Facing).Orthogonal();
        var direction = harvesterId % 2 == 0 ? 1 : -1;
        return deliveryPoint + side * direction * 86;
    }

    private BuildingModel? FindBestRefineryForHarvester(Owner owner, Vector2 position, int harvesterId)
    {
        BuildingModel? best = null;
        var bestLoad = int.MaxValue;
        var bestDistance = float.PositiveInfinity;
        foreach (var building in Buildings)
        {
            if (building.Owner != owner
                || building.Kind != BuildingDesignIds.Refinery
                || building.Hp <= 0
                || building.BuildProgress < 1)
            {
                continue;
            }

            var load = RefineryDockLoad(building, harvesterId);
            var distance = building.Position.DistanceTo(position);
            if (load < bestLoad || (load == bestLoad && distance < bestDistance))
            {
                best = building;
                bestLoad = load;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static int RefineryDockLoad(BuildingModel refinery, int harvesterId)
    {
        var load = 0;
        if (refinery.DockedHarvesterId is not null && refinery.DockedHarvesterId != harvesterId)
        {
            load += 2;
        }

        if (refinery.DockReservedByHarvesterId is not null && refinery.DockReservedByHarvesterId != harvesterId)
        {
            load += 1;
        }

        return load;
    }

    private bool CanUseRefineryDock(BuildingModel refinery, int harvesterId)
    {
        return (refinery.DockedHarvesterId is null || refinery.DockedHarvesterId == harvesterId)
            && (refinery.DockReservedByHarvesterId is null || refinery.DockReservedByHarvesterId == harvesterId);
    }

    private void ReserveRefineryDock(UnitModel unit, BuildingModel refinery)
    {
        ClearRefineryDockClaim(unit.Id);
        unit.HarvestRefineryId = refinery.Id;
        if (refinery.DockedHarvesterId != unit.Id)
        {
            refinery.DockReservedByHarvesterId = unit.Id;
        }
    }

    private void ClearRefineryDockClaim(int harvesterId)
    {
        foreach (var refinery in Buildings)
        {
            if (refinery.Kind != BuildingDesignIds.Refinery)
            {
                continue;
            }

            if (refinery.DockReservedByHarvesterId == harvesterId)
            {
                refinery.DockReservedByHarvesterId = null;
            }

            if (refinery.DockedHarvesterId == harvesterId)
            {
                refinery.DockedHarvesterId = null;
            }
        }
    }

    private void StopCombatCommand(UnitModel unit)
    {
        unit.AttackTargetId = null;
        unit.AttackTargetKind = CombatTargetKind.Unit;
        unit.AttackTargetIsManual = false;
        unit.AttackTargetAllowsPursuit = false;
        ClearAttackTrackingMemory(unit);
        unit.ReturnToAnchorAfterAttack = false;
        unit.RetaliationTargetId = null;
        unit.LastSharedThreatKey = null;
        unit.ThreatShareCooldownRemaining = SharedThreatMemorySeconds;
    }

    private void StopHarvesting(UnitModel unit)
    {
        ClearRefineryDockClaim(unit.Id);
        unit.HarvesterMode = HarvesterMode.Idle;
        unit.HarvestFieldId = null;
        unit.HarvestRefineryId = null;
        unit.HarvestPulse = 0;
    }
}
