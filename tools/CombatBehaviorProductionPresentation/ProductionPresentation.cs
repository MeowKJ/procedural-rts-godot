static partial class Program
{
    private static void AssertProductionPresentationAndBuildOptions()
    {
        var expectedDogPlayableDesignIds = ExpectedDogPlayableDesignIds();
        var expectedCatPlayableDesignIds = ExpectedCatPlayableDesignIds();

        var productionUiState = new GameState();
        productionUiState.SetCredits(Owner.Player, 10000);
        if (!productionUiState.EnqueueProduction(ProductionKind.InfantrySquad, Owner.Player, out _))
        {
            throw new InvalidOperationException("production UI state test should be able to queue infantry");
        }

        var productionOptions = productionUiState.ProductionOptionStates(Owner.Player);
        var infantryOption = productionOptions.Single(option => option.UnitDesignId == "dog.infantry");
        var tankOption = productionOptions.Single(option => option.UnitDesignId == "dog.guard_tank");
        if (!infantryOption.CanQueue
            || infantryOption.QueuedCount != 1
            || infantryOption.UnitDesignId != "dog.infantry"
            || string.IsNullOrWhiteSpace(infantryOption.ShortCode)
            || infantryOption.Icon != IconGlyph.Infantry
            || infantryOption.RoleGlyph == IconGlyph.None
            || infantryOption.Accent.A <= 0
            || tankOption.HasProducer)
        {
            throw new InvalidOperationException("production option states should expose queue counts, icons, role glyphs, and producer-disabled states");
        }

        var dogTankProductionPresentation = UnitPresentationCatalog.For(UnitFactionId.Dog, ProductionKind.LightTank);
        var catTankProductionPresentation = UnitPresentationCatalog.For(UnitFactionId.Cat, ProductionKind.LightTank);
        if (dogTankProductionPresentation.OutputDesignId != "dog.guard_tank"
            || catTankProductionPresentation.OutputDesignId != "cat.tank"
            || dogTankProductionPresentation.ShortCode != UnitDesignCatalog.Spec("dog.guard_tank").ShortCode
            || catTankProductionPresentation.ShortCode != UnitDesignCatalog.Spec("cat.tank").ShortCode
            || dogTankProductionPresentation.ShortCode == catTankProductionPresentation.ShortCode)
        {
            throw new InvalidOperationException("UnitSpec production presentation should resolve faction-specific UnitDesign metadata instead of legacy UnitKind presentation duplicates");
        }

        var productionPresentationBattlefield = new UnitBattlefield();
        productionPresentationBattlefield.SetCredits(PlayerSlotId.One, 2000);
        productionPresentationBattlefield.SetCredits(PlayerSlotId.Two, 2000);
        productionPresentationBattlefield.UpsertBuildingTarget(
            910,
            BuildingDesignIds.VehicleFactory,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(160, 180),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.VehicleFactory).MaxHp,
            rallyPoint: new Vector2(420, 120));
        productionPresentationBattlefield.UpsertBuildingTarget(
            912,
            BuildingDesignIds.Barracks,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(160, 300),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Barracks).MaxHp,
            rallyPoint: new Vector2(420, 300));
        productionPresentationBattlefield.UpsertBuildingTarget(
            915,
            BuildingDesignIds.Airfield,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(160, 420),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Airfield).MaxHp,
            rallyPoint: new Vector2(420, 480));
        productionPresentationBattlefield.UpsertBuildingTarget(
            916,
            BuildingDesignIds.PowerPlant,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(160, 540),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.PowerPlant).MaxHp);
        productionPresentationBattlefield.UpsertBuildingTarget(
            911,
            BuildingDesignIds.VehicleFactory,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(640, 180),
            Mathf.Pi,
            BuildSpecCatalog.For(BuildingDesignIds.VehicleFactory).MaxHp,
            rallyPoint: new Vector2(380, 240));
        productionPresentationBattlefield.UpsertBuildingTarget(
            913,
            BuildingDesignIds.Barracks,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(640, 300),
            Mathf.Pi,
            BuildSpecCatalog.For(BuildingDesignIds.Barracks).MaxHp,
            rallyPoint: new Vector2(380, 360));
        productionPresentationBattlefield.UpsertBuildingTarget(
            914,
            BuildingDesignIds.Airfield,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(640, 420),
            Mathf.Pi,
            BuildSpecCatalog.For(BuildingDesignIds.Airfield).MaxHp,
            rallyPoint: new Vector2(380, 540));
        var dogRuntimeTankOption = productionPresentationBattlefield.ProductionOptionStates(PlayerSlotId.One).Single(option => option.Kind == ProductionKind.LightTank);
        var catRuntimeTankOption = productionPresentationBattlefield.ProductionOptionStates(PlayerSlotId.Two).Single(option => option.Kind == ProductionKind.LightTank);
        var dogRuntimeTankSpec = UnitDesignCatalog.Spec("dog.guard_tank");
        var catRuntimeTankSpec = UnitDesignCatalog.Spec("cat.tank");
        if (!dogRuntimeTankOption.CanQueue
            || !catRuntimeTankOption.CanQueue
            || dogRuntimeTankOption.UnitDesignId != dogRuntimeTankSpec.Id
            || catRuntimeTankOption.UnitDesignId != catRuntimeTankSpec.Id
            || dogRuntimeTankOption.ShortCode != dogRuntimeTankSpec.ShortCode
            || catRuntimeTankOption.ShortCode != catRuntimeTankSpec.ShortCode
            || dogRuntimeTankOption.Cost != dogRuntimeTankSpec.Stats.Cost
            || catRuntimeTankOption.Cost != catRuntimeTankSpec.Stats.Cost
            || dogRuntimeTankOption.Icon != dogRuntimeTankSpec.Icon
            || catRuntimeTankOption.Icon != catRuntimeTankSpec.Icon
            || dogRuntimeTankOption.Accent != SoftOldCityPalette.FactionColor(UnitFactionId.Dog)
            || catRuntimeTankOption.Accent != SoftOldCityPalette.FactionColor(UnitFactionId.Cat))
        {
            throw new InvalidOperationException("UnitBattlefield production options should expose faction-specific UnitDesign presentation data without legacy UnitKind production presentation");
        }

        var dogDesignProductionOptions = productionPresentationBattlefield.ProductionDesignOptionStates(PlayerSlotId.One)
            .Where(option => option.UnitDesignId is not null)
            .ToDictionary(option => option.UnitDesignId!);
        var catDesignProductionOptions = productionPresentationBattlefield.ProductionDesignOptionStates(PlayerSlotId.Two)
            .Where(option => option.UnitDesignId is not null)
            .ToDictionary(option => option.UnitDesignId!);
        if (!expectedDogPlayableDesignIds.All(dogDesignProductionOptions.ContainsKey)
            || !expectedCatPlayableDesignIds.All(catDesignProductionOptions.ContainsKey)
            || dogDesignProductionOptions.Values.Any(option => !option.CanQueue)
            || catDesignProductionOptions.Values.Any(option => !option.CanQueue)
            || dogDesignProductionOptions["dog.siege_artillery"].Duration <= dogDesignProductionOptions["dog.guard_tank"].Duration
            || dogDesignProductionOptions["dog.sky_patrol_aircraft"].ProducerKind != BuildingDesignIds.Airfield
            || catDesignProductionOptions["cat.crescent_artillery"].Duration <= catDesignProductionOptions["cat.tank"].Duration
            || catDesignProductionOptions["cat.scout_aircraft"].ProducerKind != BuildingDesignIds.Airfield)
        {
            throw new InvalidOperationException("player can train T1-T3 from UnitDesign production options across infantry, vehicle, defense, economy, and air producers");
        }

        var selectedPowerPlantOptions = productionPresentationBattlefield.ProductionDesignOptionStatesForSelectedProducers(
            PlayerSlotId.One,
            new[] { 916 },
            out var hasSelectedPowerPlantProducer);
        if (hasSelectedPowerPlantProducer || selectedPowerPlantOptions.Count != 0)
        {
            throw new InvalidOperationException("selected non-production buildings should let the HUD fall back to aggregate production options");
        }

        var expectedDogBarracksDesignIds = expectedDogPlayableDesignIds
            .Where(designId => UnitDesignCatalog.Spec(designId).Production!.ProducerKind == BuildingDesignIds.Barracks)
            .ToArray();
        var dogBarracksSelectedOptions = productionPresentationBattlefield.ProductionDesignOptionStatesForSelectedProducers(
                PlayerSlotId.One,
                new[] { 912 },
                out var hasDogBarracksProducer)
            .Where(option => option.UnitDesignId is not null)
            .ToDictionary(option => option.UnitDesignId!);
        if (!hasDogBarracksProducer
            || dogBarracksSelectedOptions.Count != expectedDogBarracksDesignIds.Length
            || !expectedDogBarracksDesignIds.All(dogBarracksSelectedOptions.ContainsKey)
            || dogBarracksSelectedOptions.ContainsKey("dog.guard_tank")
            || dogBarracksSelectedOptions.ContainsKey("dog.sky_patrol_aircraft")
            || dogBarracksSelectedOptions.Values.Any(option => option.ProducerKind != BuildingDesignIds.Barracks || !option.CanQueue))
        {
            throw new InvalidOperationException("selected Dog Barracks command card should show only barracks-trainable UnitDesign options");
        }

        var expectedDogVehicleFactoryDesignIds = expectedDogPlayableDesignIds
            .Where(designId => UnitDesignCatalog.Spec(designId).Production!.ProducerKind == BuildingDesignIds.VehicleFactory)
            .ToArray();
        var dogVehicleFactorySelectedOptions = productionPresentationBattlefield.ProductionDesignOptionStatesForSelectedProducers(
                PlayerSlotId.One,
                new[] { 910 },
                out var hasDogVehicleFactoryProducer)
            .Where(option => option.UnitDesignId is not null)
            .ToDictionary(option => option.UnitDesignId!);
        if (!hasDogVehicleFactoryProducer
            || dogVehicleFactorySelectedOptions.Count != expectedDogVehicleFactoryDesignIds.Length
            || !expectedDogVehicleFactoryDesignIds.All(dogVehicleFactorySelectedOptions.ContainsKey)
            || dogVehicleFactorySelectedOptions.ContainsKey("dog.infantry")
            || dogVehicleFactorySelectedOptions.ContainsKey("dog.sky_patrol_aircraft")
            || dogVehicleFactorySelectedOptions.Values.Any(option => option.ProducerKind != BuildingDesignIds.VehicleFactory || !option.CanQueue))
        {
            throw new InvalidOperationException("selected Dog Vehicle Factory command card should show only vehicle-factory UnitDesign options");
        }

        var expectedDogBarracksAirfieldDesignIds = expectedDogPlayableDesignIds
            .Where(designId =>
            {
                var producerKind = UnitDesignCatalog.Spec(designId).Production!.ProducerKind;
                return producerKind == BuildingDesignIds.Barracks || producerKind == BuildingDesignIds.Airfield;
            })
            .ToArray();
        var dogBarracksAirfieldSelectedOptions = productionPresentationBattlefield.ProductionDesignOptionStatesForSelectedProducers(
                PlayerSlotId.One,
                new[] { 912, 915 },
                out var hasDogBarracksAirfieldProducer)
            .Where(option => option.UnitDesignId is not null)
            .ToDictionary(option => option.UnitDesignId!);
        if (!hasDogBarracksAirfieldProducer
            || dogBarracksAirfieldSelectedOptions.Count != expectedDogBarracksAirfieldDesignIds.Length
            || !expectedDogBarracksAirfieldDesignIds.All(dogBarracksAirfieldSelectedOptions.ContainsKey)
            || !dogBarracksAirfieldSelectedOptions.ContainsKey("dog.sky_patrol_aircraft")
            || dogBarracksAirfieldSelectedOptions.ContainsKey("dog.guard_tank")
            || dogBarracksAirfieldSelectedOptions.Values.Any(option => option.ProducerKind == BuildingDesignIds.VehicleFactory || !option.CanQueue))
        {
            throw new InvalidOperationException("multi-selected Dog producer command card should union selected producer lanes and exclude unselected factories");
        }

        AssertSelectedProducerQueueSummaryAndCancel();

        var trainedDesignIds = new List<string>();
        productionPresentationBattlefield.ProductionCompleted += (_, _, unit) => trainedDesignIds.Add(unit.Spec.Id);
        productionPresentationBattlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Allied);
        if (!productionPresentationBattlefield.EnqueueProductionDesign("dog.infantry", PlayerSlotId.One, out _)
            || !productionPresentationBattlefield.EnqueueProductionDesign("dog.assault_tank", PlayerSlotId.One, out _)
            || !productionPresentationBattlefield.EnqueueProductionDesign("dog.sky_patrol_aircraft", PlayerSlotId.One, out _)
            || !productionPresentationBattlefield.EnqueueProductionDesign("dog.siege_artillery", PlayerSlotId.One, out var dogTierThreeStatus)
            || !productionPresentationBattlefield.EnqueueProductionDesign("cat.scout_aircraft", PlayerSlotId.Two, out _)
            || !productionPresentationBattlefield.EnqueueProductionDesign("cat.crescent_artillery", PlayerSlotId.Two, out var catTierThreeStatus)
            || !dogTierThreeStatus.Contains(UnitDesignCatalog.Spec("dog.siege_artillery").Label, StringComparison.Ordinal)
            || !catTierThreeStatus.Contains(UnitDesignCatalog.Spec("cat.crescent_artillery").Label, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("UnitDesign production requests should queue T1, T2, T3, and Airfield outputs by design id");
        }

        for (var step = 0; step < 900; step++)
        {
            productionPresentationBattlefield.Update(1 / 30.0);
        }

        var expectedTrainedDesignIds = new[] { "dog.infantry", "dog.assault_tank", "dog.sky_patrol_aircraft", "dog.siege_artillery", "cat.scout_aircraft", "cat.crescent_artillery" };
        if (!expectedTrainedDesignIds.All(trainedDesignIds.Contains)
            || expectedTrainedDesignIds.Any(designId => productionPresentationBattlefield.Units.All(unit => unit.Spec.Id != designId))
            || productionPresentationBattlefield.HasQueuedProduction(PlayerSlotId.One)
            || productionPresentationBattlefield.HasQueuedProduction(PlayerSlotId.Two))
        {
            throw new InvalidOperationException("players should train UnitDesign-driven T1-T3 production options into UnitInstance outputs through EntityWorld production");
        }

        var productionLanes = productionUiState.ProductionLaneSnapshots(Owner.Player);
        var barracksLane = productionLanes.Single(lane => lane.ProducerKind == BuildingDesignIds.Barracks);
        var dogInfantryProductionSpec = UnitDesignCatalog.Spec("dog.infantry");
        if (!barracksLane.Completed
            || !barracksLane.Powered
            || barracksLane.FactionId != FactionId.Dog
            || barracksLane.Queue.Count != 1
            || barracksLane.Queue[0].DesignId != "dog.infantry"
            || !barracksLane.Queue[0].CanCancel
            || barracksLane.Queue[0].Refund != Mathf.RoundToInt(dogInfantryProductionSpec.Stats.Cost * 0.5f))
        {
            throw new InvalidOperationException("production lane snapshots should expose per-building queue/progress/cancel/faction hooks");
        }

        var buildOptions = productionUiState.BuildOptionSnapshots(Owner.Player);
        var airfieldOptionBeforeFactory = buildOptions.Single(option => option.Kind == BuildingDesignIds.Airfield);
        var antiAirOptionBeforeAirfield = buildOptions.Single(option => option.Kind == BuildingDesignIds.AntiAirTurret);
        if (buildOptions.Count != BuildSpecCatalog.Definitions.Count
            || buildOptions.Any(option => option.Icon == IconGlyph.None || option.Cost <= 0 || option.BuildTime <= 0 || option.BuildRadius <= 0)
            || !buildOptions.Single(option => option.Kind == BuildingDesignIds.PowerPlant).CanStart
            || !buildOptions.Single(option => option.Kind == BuildingDesignIds.Barracks).HasPrerequisites
            || !buildOptions.Single(option => option.Kind == BuildingDesignIds.GroundTurret).HasPrerequisites
            || airfieldOptionBeforeFactory.Category != BuildCategory.Air
            || airfieldOptionBeforeFactory.HasPrerequisites)
        {
            throw new InvalidOperationException("build option snapshots should expose categories, costs, prerequisites, power, and disabled-state hooks for UI");
        }

        foreach (var option in buildOptions)
        {
            var spec = BuildSpecCatalog.For(option.Kind);
            if (option.Category != spec.Category
                || option.Icon != spec.Icon
                || option.Cost != spec.Cost
                || Math.Abs(option.BuildTime - spec.BuildTime) > 0.001f
                || option.Footprint != spec.Footprint
                || option.PowerProvided != spec.PowerProvided
                || option.PowerUsed != spec.PowerUsed
                || Math.Abs(option.BuildRadius - spec.BuildRadius) > 0.001f)
            {
                throw new InvalidOperationException($"{option.Kind} build option snapshot should derive runtime UI fields from BuildSpecCatalog");
            }
        }

        if (antiAirOptionBeforeAirfield.Category != BuildCategory.Defense || antiAirOptionBeforeAirfield.HasPrerequisites)
        {
            throw new InvalidOperationException("anti-air turret build option should stay locked until airfield tech is ready");
        }

        var insideBuildRadius = productionUiState.ValidateBuildingPlacement(BuildingDesignIds.VehicleFactory, Owner.Player, new Vector2(1040, 520));
        var outsideBuildRadius = productionUiState.ValidateBuildingPlacement(BuildingDesignIds.VehicleFactory, Owner.Player, new Vector2(2300, 360));
        if (!insideBuildRadius.IsValid
            || outsideBuildRadius.IsValid
            || outsideBuildRadius.Reason != "placement.outsideBuildRadius"
            || !GameText.HasTranslation("placement.outsideBuildRadius", GameLanguage.English)
            || !GameText.HasTranslation("placement.outsideBuildRadius", GameLanguage.ChineseSimplified))
        {
            throw new InvalidOperationException("player building placement should be constrained by powered owner build radius with localized failure feedback");
        }

        var placedFactory = productionUiState.PlaceBuildingWithinBuildRadius(BuildingDesignIds.VehicleFactory, Owner.Player, new Vector2(1040, 520));
        if (placedFactory is null)
        {
            throw new InvalidOperationException("airfield prerequisite test should be able to place a player vehicle factory inside build radius");
        }

        if (Math.Abs(placedFactory.Hp - BuildSpecCatalog.For(BuildingDesignIds.VehicleFactory).MaxHp) > 0.001f)
        {
            throw new InvalidOperationException("placed building HP should derive from BuildSpecCatalog");
        }

        var airfieldOptionAfterFactory = productionUiState.BuildOptionSnapshots(Owner.Player).Single(option => option.Kind == BuildingDesignIds.Airfield);
        if (!airfieldOptionAfterFactory.HasPrerequisites || !airfieldOptionAfterFactory.CanStart)
        {
            throw new InvalidOperationException("airfield build option should unlock after HQ, power, and vehicle factory prerequisites are ready");
        }

        if (productionUiState.PlaceBuilding(BuildingDesignIds.Airfield, Owner.Player, new Vector2(1260, 520)) is null)
        {
            throw new InvalidOperationException("anti-air turret prerequisite test should be able to place a player airfield");
        }

        var antiAirOptionAfterAirfield = productionUiState.BuildOptionSnapshots(Owner.Player).Single(option => option.Kind == BuildingDesignIds.AntiAirTurret);
        if (!antiAirOptionAfterAirfield.HasPrerequisites || !antiAirOptionAfterAirfield.CanStart)
        {
            throw new InvalidOperationException("anti-air turret build option should unlock after airfield tech is ready");
        }
    }
}
