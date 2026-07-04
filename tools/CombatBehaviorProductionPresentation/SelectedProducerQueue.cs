static partial class Program
{
    private static void AssertSelectedProducerQueueSummaryAndCancel()
    {
        var selectedQueueBattlefield = new UnitBattlefield();
        selectedQueueBattlefield.SetCredits(PlayerSlotId.One, 6000);
        var queueVehicleFactory = selectedQueueBattlefield.UpsertBuildingTarget(
            920,
            BuildingDesignIds.VehicleFactory,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(240, 180),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.VehicleFactory).MaxHp);
        var queueBarracks = selectedQueueBattlefield.UpsertBuildingTarget(
            922,
            BuildingDesignIds.Barracks,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(240, 300),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Barracks).MaxHp);
        var queueAirfield = selectedQueueBattlefield.UpsertBuildingTarget(
            925,
            BuildingDesignIds.Airfield,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(240, 420),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Airfield).MaxHp);
        var queuePowerPlant = selectedQueueBattlefield.UpsertBuildingTarget(
            926,
            BuildingDesignIds.PowerPlant,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(240, 540),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.PowerPlant).MaxHp);

        if (!selectedQueueBattlefield.EnqueueProductionDesign("dog.infantry", PlayerSlotId.One, out _)
            || !selectedQueueBattlefield.EnqueueProductionDesign("dog.assault_tank", PlayerSlotId.One, out _)
            || !selectedQueueBattlefield.EnqueueProductionDesign("dog.sky_patrol_aircraft", PlayerSlotId.One, out _))
        {
            throw new InvalidOperationException("selected producer queue test should seed infantry, vehicle, and aircraft queues");
        }

        var selectedBarracksSummary = selectedQueueBattlefield.ProductionQueueSummaryForSelectedProducers(
            PlayerSlotId.One,
            new[] { queueBarracks.Id },
            out var hasSelectedBarracksQueueProducer,
            out var hasSelectedBarracksQueue);
        var selectedVehicleSummary = selectedQueueBattlefield.ProductionQueueSummaryForSelectedProducers(
            PlayerSlotId.One,
            new[] { queueVehicleFactory.Id },
            out var hasSelectedVehicleQueueProducer,
            out var hasSelectedVehicleQueue);
        var selectedAirfieldSummary = selectedQueueBattlefield.ProductionQueueSummaryForSelectedProducers(
            PlayerSlotId.One,
            new[] { queueAirfield.Id },
            out var hasSelectedAirfieldQueueProducer,
            out var hasSelectedAirfieldQueue);
        var selectedPowerSummary = selectedQueueBattlefield.ProductionQueueSummaryForSelectedProducers(
            PlayerSlotId.One,
            new[] { queuePowerPlant.Id },
            out var hasSelectedPowerQueueProducer,
            out var hasSelectedPowerQueue);

        if (!hasSelectedBarracksQueueProducer
            || !hasSelectedBarracksQueue
            || !selectedBarracksSummary.Contains(UnitDesignCatalog.Spec("dog.infantry").Label.ToUpperInvariant(), StringComparison.Ordinal)
            || !selectedQueueBattlefield.HasQueuedProductionForSelectedProducers(PlayerSlotId.One, new[] { queueBarracks.Id }, out var hasSelectedBarracksHasQueueProducer)
            || !hasSelectedBarracksHasQueueProducer
            || !hasSelectedVehicleQueueProducer
            || !hasSelectedVehicleQueue
            || !selectedVehicleSummary.Contains(UnitDesignCatalog.Spec("dog.assault_tank").Label.ToUpperInvariant(), StringComparison.Ordinal)
            || !hasSelectedAirfieldQueueProducer
            || !hasSelectedAirfieldQueue
            || !selectedAirfieldSummary.Contains(UnitDesignCatalog.Spec("dog.sky_patrol_aircraft").Label.ToUpperInvariant(), StringComparison.Ordinal)
            || hasSelectedPowerQueueProducer
            || hasSelectedPowerQueue
            || selectedPowerSummary != GameText.T("ui.queue.empty"))
        {
            throw new InvalidOperationException("selected producer queue summary should scope to selected production buildings and report no selected producer for non-production buildings");
        }

        if (!selectedQueueBattlefield.CancelFirstProductionForSelectedProducers(
                PlayerSlotId.One,
                new[] { queueVehicleFactory.Id },
                out var hasCancelVehicleProducer,
                out var selectedVehicleCancelStatus)
            || !hasCancelVehicleProducer
            || !selectedVehicleCancelStatus.Contains(UnitDesignCatalog.Spec("dog.assault_tank").Label, StringComparison.Ordinal)
            || selectedQueueBattlefield.BuildingProductionQueue(queueVehicleFactory.Id).Count != 0
            || selectedQueueBattlefield.BuildingProductionQueue(queueBarracks.Id).Count != 1
            || selectedQueueBattlefield.BuildingProductionQueue(queueAirfield.Id).Count != 1
            || selectedQueueBattlefield.HasQueuedProductionForSelectedProducers(PlayerSlotId.One, new[] { queueVehicleFactory.Id }, out var hasVehicleProducerAfterCancel)
            || !hasVehicleProducerAfterCancel
            || !selectedQueueBattlefield.HasQueuedProduction(PlayerSlotId.One))
        {
            throw new InvalidOperationException("selected producer cancel should cancel only the selected producer queue and leave other producer queues intact");
        }
    }
}
