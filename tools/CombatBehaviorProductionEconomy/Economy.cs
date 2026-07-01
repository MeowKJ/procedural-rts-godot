static partial class Program
{
    private static void AssertLegacyProductionEconomy()
    {
        var rallyState = new GameState();
        var initialUnitCount = rallyState.Units.Count;
        var initialCredits = rallyState.Credits(Owner.Player);
        var selectedCount = rallyState.SelectSingleAt(new Vector2(520, 845), additive: false);
        if (selectedCount != 1 || !rallyState.SelectedBuildings().Any(building => building.Kind == BuildingDesignIds.Barracks))
        {
            throw new InvalidOperationException("clicking the player barracks should select it for rally commands");
        }

        var rallyPoint = new Vector2(860, 940);
        if (!rallyState.CommandSetSelectedBuildingRallyPoint(rallyPoint, out _))
        {
            throw new InvalidOperationException("selected barracks should accept a rally point");
        }

        if (!rallyState.EnqueueProduction(ProductionKind.InfantrySquad, Owner.Player, out _))
        {
            throw new InvalidOperationException("barracks should queue infantry for rally test");
        }

        var infantryCost = UnitDesignCatalog.Spec("dog.infantry").Stats.Cost;
        if (rallyState.Credits(Owner.Player) != initialCredits - infantryCost)
        {
            throw new InvalidOperationException("queueing production should spend credits immediately");
        }

        Advance(rallyState, 6.0f);

        var produced = rallyState.Units
            .Where(unit => unit.Owner == Owner.Player && unit.DesignId == UnitDesignIds.DogInfantry)
            .OrderByDescending(unit => unit.Id)
            .FirstOrDefault();

        if (rallyState.Units.Count <= initialUnitCount || produced is null)
        {
            throw new InvalidOperationException("production should create a new infantry unit");
        }

        if (produced.MoveTarget is null || FinalMoveDestination(produced).DistanceTo(rallyPoint) > 0.01f)
        {
            throw new InvalidOperationException("produced unit should move toward the producer rally point");
        }

        var pathCommandState = new GameState();
        var pathUnit = pathCommandState.Units.First(unit => unit.Owner == Owner.Player && IsCombatUnit(unit));
        pathCommandState.ClearSelection();
        pathUnit.Selected = true;
        var pathDestination = new Vector2(1320, 1120);
        pathCommandState.CommandMoveSelected(pathDestination);

        if (pathUnit.MoveTarget is null)
        {
            throw new InvalidOperationException("move command should assign an initial path waypoint");
        }

        if (FinalMoveDestination(pathUnit).DistanceTo(pathDestination) > 220)
        {
            throw new InvalidOperationException("move command path should end near the requested destination");
        }

        if (pathUnit.Path.Count == 0 && pathUnit.MoveTarget.Value.DistanceTo(pathDestination) > 0.01f)
        {
            throw new InvalidOperationException("clear long move command should use the requested destination as a direct waypoint");
        }

        var occupancyState = new GameState();
        var hqObstacle = occupancyState.Buildings.First(building => building.Owner == Owner.Player && building.Kind == BuildingDesignIds.Headquarters);
        var occupancyHqSpec = BuildSpecCatalog.For(hqObstacle.Kind);
        var occupancyUnit = occupancyState.Units.First(unit => unit.Owner == Owner.Player && IsCombatUnit(unit));
        occupancyUnit.Position = hqObstacle.Position + new Vector2(-260, 0);
        occupancyUnit.AnchorPosition = occupancyUnit.Position;
        occupancyState.ClearSelection();
        occupancyUnit.Selected = true;
        var acrossHq = hqObstacle.Position + new Vector2(260, 0);
        occupancyState.CommandMoveSelected(acrossHq);
        var occupancyPath = new[] { occupancyUnit.MoveTarget ?? occupancyUnit.Position }
            .Concat(occupancyUnit.Path)
            .ToList();
        var hqLeft = hqObstacle.Position.X - (occupancyHqSpec.Footprint.X + 24) / 2f;
        var hqRight = hqObstacle.Position.X + (occupancyHqSpec.Footprint.X + 24) / 2f;
        var hqTop = hqObstacle.Position.Y - (occupancyHqSpec.Footprint.Y + 24) / 2f;
        var hqBottom = hqObstacle.Position.Y + (occupancyHqSpec.Footprint.Y + 24) / 2f;

        if (occupancyPath.Count < 2)
        {
            throw new InvalidOperationException("building occupancy should force multiple path waypoints around HQ");
        }

        if (occupancyPath.Any(point =>
            point.X >= hqLeft
            && point.X <= hqRight
            && point.Y >= hqTop
            && point.Y <= hqBottom))
        {
            throw new InvalidOperationException("building occupancy path should avoid HQ footprint");
        }

        var brokeState = new GameState();
        brokeState.SetCredits(Owner.Player, 100);
        var creditsBeforeFailedQueue = brokeState.Credits(Owner.Player);
        if (brokeState.EnqueueProduction(ProductionKind.InfantrySquad, Owner.Player, out var brokeStatus))
        {
            throw new InvalidOperationException("production should fail when credits are insufficient");
        }

        if (brokeState.Credits(Owner.Player) != creditsBeforeFailedQueue)
        {
            throw new InvalidOperationException("failed production should not spend credits");
        }

        if (!brokeStatus.Contains("Need 120 credits", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"insufficient funds status should explain cost, got: {brokeStatus}");
        }

        var cancelState = new GameState();
        var cancelStartCredits = cancelState.Credits(Owner.Player);
        if (!cancelState.EnqueueProduction(ProductionKind.InfantrySquad, Owner.Player, out _))
        {
            throw new InvalidOperationException("production should queue before cancel test");
        }

        var cancelCost = UnitDesignCatalog.Spec("dog.infantry").Stats.Cost;
        if (!cancelState.CancelFirstProduction(Owner.Player, out var cancelStatus))
        {
            throw new InvalidOperationException("cancel should remove oldest queued production");
        }

        if (cancelState.Buildings.Any(building => building.ProductionQueue.Count > 0))
        {
            throw new InvalidOperationException("cancel should remove queued production item");
        }

        if (cancelState.Credits(Owner.Player) != cancelStartCredits - cancelCost + cancelCost / 2)
        {
            throw new InvalidOperationException("cancel should refund half of production cost");
        }

        if (!cancelStatus.Contains("canceled", StringComparison.Ordinal) || !cancelStatus.Contains("+60 credits", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"cancel status should mention refund, got: {cancelStatus}");
        }

        var fieldState = new GameState();
        if (fieldState.ResourceFields.Count < 3)
        {
            throw new InvalidOperationException("seeded map should include multiple resource fields");
        }

        var firstField = fieldState.ResourceFields.OrderBy(field => field.Id).First();
        if (firstField.Amount <= 0 || firstField.MaxAmount != firstField.Amount || firstField.Radius < 120)
        {
            throw new InvalidOperationException("resource field should expose amount, capacity, and harvest radius");
        }

        if (fieldState.PickResourceField(firstField.Position) != firstField)
        {
            throw new InvalidOperationException("resource field should be pickable at its center");
        }

        if (fieldState.PickResourceField(firstField.Position + new Vector2(firstField.Radius + 80, 0)) is not null)
        {
            throw new InvalidOperationException("resource field picking should respect field radius");
        }

        var harvestState = new GameState();
        var harvester = harvestState.Units.First(unit => unit.Owner == Owner.Player && IsHarvesterUnit(unit));
        var field = harvestState.ResourceFields.OrderBy(resource => resource.Position.DistanceTo(harvester.Position)).First();
        var startingFieldAmount = field.Amount;
        var startingCredits = harvestState.Credits(Owner.Player);
        harvestState.ClearSelection();
        harvester.Selected = true;

        if (!harvestState.CommandHarvestSelected(field, out var harvestStatus))
        {
            throw new InvalidOperationException($"harvest command should start with refinery and field available: {harvestStatus}");
        }

        Advance(harvestState, 30.0f);

        if (field.Amount >= startingFieldAmount)
        {
            throw new InvalidOperationException("harvesting should reduce resource field amount");
        }

        if (harvestState.Credits(Owner.Player) <= startingCredits)
        {
            throw new InvalidOperationException("harvester unloading should add credits to owner inventory");
        }

        if (harvester.HarvesterMode == HarvesterMode.Idle || harvester.HarvestFieldId != field.Id)
        {
            throw new InvalidOperationException("harvester should keep looping between field and refinery after unloading");
        }

        var deliveryState = new GameState();
        var deliveryHarvester = deliveryState.Units.First(unit => unit.Owner == Owner.Player && IsHarvesterUnit(unit));
        var deliveryField = deliveryState.ResourceFields.OrderBy(resource => resource.Position.DistanceTo(deliveryHarvester.Position)).First();
        var refinery = deliveryState.Buildings.First(building => building.Owner == Owner.Player && building.Kind == BuildingDesignIds.Refinery);
        var deliveryStartCredits = deliveryState.Credits(Owner.Player);
        deliveryHarvester.Position = deliveryState.RefineryDeliveryPoint(refinery);
        deliveryHarvester.Cargo = GameState.HarvesterCargoCapacity;
        deliveryHarvester.HarvestFieldId = deliveryField.Id;
        deliveryHarvester.HarvestRefineryId = refinery.Id;
        deliveryHarvester.HarvesterMode = HarvesterMode.ReturningToRefinery;

        Advance(deliveryState, 0.1f);

        if (deliveryHarvester.HarvesterMode != HarvesterMode.Unloading || refinery.DockedHarvesterId != deliveryHarvester.Id)
        {
            throw new InvalidOperationException("harvester should occupy refinery dock before unloading");
        }

        Advance(deliveryState, 0.2f);

        if (deliveryState.Credits(Owner.Player) <= deliveryStartCredits || deliveryHarvester.Cargo >= GameState.HarvesterCargoCapacity)
        {
            throw new InvalidOperationException("refinery dock unloading should transfer cargo into credits");
        }

        if (refinery.DeliveryPulse <= 0)
        {
            throw new InvalidOperationException("refinery should pulse during delivery");
        }

        Advance(deliveryState, 2.0f);

        if (refinery.DockedHarvesterId == deliveryHarvester.Id || refinery.DockReservedByHarvesterId == deliveryHarvester.Id)
        {
            throw new InvalidOperationException("refinery dock should release harvester after unloading");
        }

        if (deliveryHarvester.HarvesterMode != HarvesterMode.MovingToField || deliveryHarvester.MoveTarget is null)
        {
            throw new InvalidOperationException("harvester should return to its field after refinery delivery");
        }
    }
}
