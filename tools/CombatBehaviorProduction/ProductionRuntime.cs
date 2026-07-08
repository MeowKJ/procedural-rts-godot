static partial class Program
{
    private static void AssertUnitBattlefieldProduction()
    {
        var unitProductionBattlefield = new UnitBattlefield();
        unitProductionBattlefield.WorldSize = new Vector2(900, 700);
        unitProductionBattlefield.SetCredits(PlayerSlotId.One, 500);
        var unitProductionBarracksSpec = BuildSpecCatalog.For(BuildingDesignIds.Barracks);
        var unitProductionBarracksRadius = Mathf.Max(unitProductionBarracksSpec.Footprint.X, unitProductionBarracksSpec.Footprint.Y) * 0.5f;
        var unitProductionBarracks = unitProductionBattlefield.UpsertBuildingTarget(
            88,
            BuildingDesignIds.Barracks,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(240, 460),
            0,
            unitProductionBarracksSpec.MaxHp);
        var unitProductionEvents = new List<UnitInstance>();
        unitProductionBattlefield.ProductionCompleted += (_, _, unit) => unitProductionEvents.Add(unit);
        var hoveredProductionBuildingId = unitProductionBattlefield.PickAnyBuildingTargetId(unitProductionBarracks.Position + new Vector2(3, 0), pickPadding: 8);
        var hoveredProductionProjection = unitProductionBattlefield.PickAnyBuildingHoverProjection(unitProductionBarracks.Position + new Vector2(3, 0), PlayerSlotId.One, pickPadding: 8);
        if (hoveredProductionBuildingId != unitProductionBarracks.Id
            || hoveredProductionProjection is null
            || hoveredProductionProjection.Value.Id != unitProductionBarracks.Id
            || hoveredProductionProjection.Value.Position != unitProductionBarracks.Position
            || hoveredProductionProjection.Value.Radius != unitProductionBarracksRadius
            || hoveredProductionProjection.Value.Relation != PlayerRelation.Self)
        {
            throw new InvalidOperationException("building hover affordance should pick ids and project buildings through UnitBattlefield EntityWorld state instead of returning mutable building targets to input callers");
        }

        unitProductionBattlefield.SetBuildingHitPulse(unitProductionBarracks.Id, 0.75f);
        var buildingHitPulseProjections = unitProductionBattlefield.BuildingHitPulseProjections();
        if (buildingHitPulseProjections.Count != 1
            || buildingHitPulseProjections[0].Id != unitProductionBarracks.Id
            || buildingHitPulseProjections[0].Position != unitProductionBarracks.Position
            || buildingHitPulseProjections[0].Radius != unitProductionBarracksRadius
            || buildingHitPulseProjections[0].HitPulse <= 0)
        {
            throw new InvalidOperationException("building hit pulses should project through UnitBattlefield EntityWorld presentation pulses instead of legacy GameState building pulses");
        }

        var unitProductionEnemyHq = unitProductionBattlefield.UpsertBuildingTarget(
            89,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(760, 520),
            Mathf.Pi,
            900);
        var hiddenBuildingPips = unitProductionBattlefield.BuildingMinimapProjections(PlayerSlotId.One, _ => false);
        var exploredBuildingPips = unitProductionBattlefield.BuildingMinimapProjections(PlayerSlotId.One, _ => true);
        if (!hiddenBuildingPips.Any(building => building.Id == unitProductionBarracks.Id)
            || hiddenBuildingPips.Any(building => building.Id == unitProductionEnemyHq.Id)
            || !exploredBuildingPips.Any(building => building.Id == unitProductionEnemyHq.Id)
            || exploredBuildingPips.First(building => building.Id == unitProductionEnemyHq.Id).Footprint != BuildSpecCatalog.For(unitProductionEnemyHq.Kind).Footprint)
        {
            throw new InvalidOperationException("building minimap pips should project from UnitBattlefield EntityWorld state and keep enemy buildings gated by explored fog");
        }

        var playerPowerBeforeProvider = unitProductionBattlefield.PowerStatus(PlayerSlotId.One);
        if (playerPowerBeforeProvider.IsStable
            || playerPowerBeforeProvider.Provided != 0
            || playerPowerBeforeProvider.Used < 6
            || playerPowerBeforeProvider.HasProvider)
        {
            throw new InvalidOperationException("building power alert should read unpowered player demand from UnitBattlefield EntityWorld power components");
        }

        unitProductionBattlefield.UpsertBuildingTarget(
            90,
            BuildingDesignIds.PowerPlant,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(180, 350),
            0,
            520,
            powered: true,
            buildProgress: 0.5f);
        if (unitProductionBattlefield.PowerStatus(PlayerSlotId.One).IsStable)
        {
            throw new InvalidOperationException("building power alert should ignore unfinished power providers in UnitBattlefield EntityWorld power projections");
        }

        unitProductionBattlefield.UpsertBuildingTarget(
            90,
            BuildingDesignIds.PowerPlant,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(180, 350),
            0,
            520);
        var playerPowerWithProvider = unitProductionBattlefield.PowerStatus(PlayerSlotId.One);
        if (!playerPowerWithProvider.IsStable
            || !playerPowerWithProvider.HasProvider
            || playerPowerWithProvider.Provided < 24
            || playerPowerWithProvider.Used < 6)
        {
            throw new InvalidOperationException("building power alert should project stable player power from completed UnitBattlefield EntityWorld providers");
        }

        var commandsBeforeProductionQueue = unitProductionBattlefield.AppliedInputCommandCount;
        if (!unitProductionBattlefield.SetRallyPoint(unitProductionBarracks.Id, new Vector2(420, 500), out _)
            || !unitProductionBattlefield.EnqueueProduction(ProductionKind.InfantrySquad, PlayerSlotId.One, out _)
            || unitProductionBattlefield.Credits(PlayerSlotId.One) != 380
            || !unitProductionBattlefield.HasQueuedProduction(PlayerSlotId.One))
        {
            throw new InvalidOperationException("new unit battlefield should queue production, deduct credits, and track rally-capable producer state");
        }

        var producerEntityAfterQueue = BuildingEntityForTargetId(unitProductionBattlefield, unitProductionBarracks.Id);
        if (unitProductionBattlefield.AppliedInputCommandCount != commandsBeforeProductionQueue + 2
            || producerEntityAfterQueue is null
            || unitProductionBattlefield.BuildingRallyPoint(unitProductionBarracks.Id) != new Vector2(420, 500)
            || !producerEntityAfterQueue.Components.TryGet<RallyPointComponentState>(out var producerRallyAfterSet)
            || producerRallyAfterSet.Target != new Vector2(420, 500)
            || !producerEntityAfterQueue.Components.TryGet<ProductionQueueComponentState>(out var producerQueueAfterEnqueue)
            || producerQueueAfterEnqueue.Items.Count != 1
            || producerQueueAfterEnqueue.Items[0].DesignId != "dog.infantry"
            || unitProductionBattlefield.BuildingProductionQueue(unitProductionBarracks.Id).Count != 1
            || unitProductionBattlefield.EntityWorld.ResourceInventory(OwnerId.FromPlayerSlot(PlayerSlotId.One)).Credits != 380)
        {
            throw new InvalidOperationException("UnitBattlefield rally and production enqueue should route through EntityWorld SetRallyPointEntityCommand/ProduceEntityCommand; production enqueue should route through EntityWorld ProduceEntityCommand and sync credits/queue back to legacy runtime");
        }

        for (var step = 0; step < 210; step++)
        {
            unitProductionBattlefield.Update(1 / 30.0);
        }

        if (unitProductionEvents.Count != 1
            || unitProductionEvents[0].Spec.Id != "dog.infantry"
            || unitProductionEvents[0].PlayerSlotId != PlayerSlotId.One
            || unitProductionEvents[0].CommandVisualTarget != new Vector2(420, 500)
            || unitProductionEvents[0].FormationSlot != new Vector2(420, 500)
            || unitProductionBattlefield.HasQueuedProduction(PlayerSlotId.One))
        {
            throw new InvalidOperationException("new unit battlefield should complete production directly into UnitInstance runtime units and apply producer rally points");
        }

        var commandsBeforeSelectedBuildingRally = unitProductionBattlefield.AppliedInputCommandCount;
        var commandsBeforeBuildingSelection = unitProductionBattlefield.AppliedInputCommandCount;
        unitProductionBattlefield.SelectUnitsByIds(PlayerSlotId.One, [unitProductionEvents[0].Id]);
        var selectedBuildingCount = unitProductionBattlefield.SelectBuildingTargetAt(PlayerSlotId.One, unitProductionBarracks.Position, additive: false, pickPadding: 8);
        if (selectedBuildingCount != 1
            || unitProductionBattlefield.AppliedInputCommandCount != commandsBeforeBuildingSelection + 2
            || unitProductionBattlefield.SelectedUnits(PlayerSlotId.One).Any()
            || !unitProductionBattlefield.HasSelectedBuildings(PlayerSlotId.One)
            || unitProductionBattlefield.BuildingProjection(unitProductionBarracks.Id)?.Selected != true
            || BuildingEntityForTargetId(unitProductionBattlefield, unitProductionBarracks.Id)?.Components.Require<SelectableComponentState>().Selected != true)
        {
            throw new InvalidOperationException("building click selection should route through UnitBattlefield EntityWorld SetSelectionEntityCommand instead of legacy GameState.SelectPlayerBuildingAt");
        }

        if (!unitProductionBattlefield.SetSelectedBuildingRallyPoints(PlayerSlotId.One, new Vector2(5000, -200), out var selectedBuildingRallyStatus)
            || unitProductionBattlefield.AppliedInputCommandCount != commandsBeforeSelectedBuildingRally + 3
            || unitProductionBattlefield.BuildingRallyPoint(unitProductionBarracks.Id) != new Vector2(820, 80)
            || unitProductionBattlefield.BuildingRallyPulse(unitProductionBarracks.Id) <= 0
            || !selectedBuildingRallyStatus.Contains("rally", StringComparison.OrdinalIgnoreCase)
            || BuildingEntityForTargetId(unitProductionBattlefield, unitProductionBarracks.Id)?.Components.Require<RallyPointComponentState>().Target != new Vector2(820, 80))
        {
            throw new InvalidOperationException("selected building rally input should route through UnitBattlefield EntityWorld SetRallyPointEntityCommand and keep rally state in EntityWorld components");
        }

        if (!unitProductionBattlefield.HasSelectedBuildings(PlayerSlotId.One))
        {
            throw new InvalidOperationException("building command preview should query selected buildings through UnitBattlefield EntityWorld projections instead of legacy GameState selections");
        }

        var buildingSellBattlefield = new UnitBattlefield();
        buildingSellBattlefield.SetCredits(PlayerSlotId.One, 500);
        var buildingSellBarracks = buildingSellBattlefield.UpsertBuildingTarget(
            188,
            BuildingDesignIds.Barracks,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(240, 460),
            0,
            unitProductionBarracksSpec.MaxHp);
        buildingSellBattlefield.SelectBuildingTargetAt(PlayerSlotId.One, buildingSellBarracks.Position, additive: false, pickPadding: 8);
        var noRallySelectionProjection = buildingSellBattlefield.SelectedBuildingSelectionProjections(PlayerSlotId.One);
        if (noRallySelectionProjection.Count != 1
            || noRallySelectionProjection[0].HasRallyPoint
            || noRallySelectionProjection[0].RallyPoint is not null)
        {
            throw new InvalidOperationException("selected producer details should preserve the empty rally state when no rally destination exists");
        }

        var buildingSellEvents = new List<UnitBattlefieldBuildingDeathInfo>();
        buildingSellBattlefield.BuildingsRemoved += deaths => buildingSellEvents.AddRange(deaths);
        var creditsBeforeSell = buildingSellBattlefield.Credits(PlayerSlotId.One);
        var expectedBuildingSellRefund = Mathf.RoundToInt(unitProductionBarracksSpec.Cost * Math.Clamp(unitProductionBarracksSpec.RefundRatio, 0, 1));
        if (buildingSellBattlefield.SellSelectedBuildings(PlayerSlotId.One, out var buildingSellStatus) != 1
            || !buildingSellStatus.Contains(expectedBuildingSellRefund.ToString(), StringComparison.Ordinal)
            || buildingSellBattlefield.Credits(PlayerSlotId.One) != creditsBeforeSell + expectedBuildingSellRefund
            || buildingSellBattlefield.EntityWorld.ResourceInventory(OwnerId.FromPlayerSlot(PlayerSlotId.One)).Credits != creditsBeforeSell + expectedBuildingSellRefund
            || buildingSellBattlefield.BuildingSnapshot(buildingSellBarracks.Id) is not null
            || buildingSellBattlefield.HasSelectedBuildings(PlayerSlotId.One)
            || buildingSellEvents.Count != 1
            || buildingSellEvents[0].Id != buildingSellBarracks.Id
            || buildingSellEvents[0].RemovalCause != UnitBattlefieldBuildingRemovalCause.Sold)
        {
            throw new InvalidOperationException("selected building sell should refund by BuildSpec refund ratio, clear selection, remove the EntityWorld building, and publish a sold removal event");
        }

        if (buildingSellBattlefield.SellSelectedBuildings(PlayerSlotId.One, out var emptyBuildingSellStatus) != 0
            || emptyBuildingSellStatus.Length == 0)
        {
            throw new InvalidOperationException("selected building sell should report an empty status when no player building is selected");
        }

        var selectedBuildingSelectionProjection = unitProductionBattlefield.SelectedBuildingSelectionProjections(PlayerSlotId.One);
        if (selectedBuildingSelectionProjection.Count != 1
            || selectedBuildingSelectionProjection[0].Kind != BuildingDesignIds.Barracks
            || selectedBuildingSelectionProjection[0].PlayerSlotId != PlayerSlotId.One
            || selectedBuildingSelectionProjection[0].Faction != UnitFactionId.Dog
            || selectedBuildingSelectionProjection[0].Hp != unitProductionBarracks.Hp
            || selectedBuildingSelectionProjection[0].MaxHp != unitProductionBarracksSpec.MaxHp
            || !selectedBuildingSelectionProjection[0].HasRallyPoint
            || selectedBuildingSelectionProjection[0].RallyPoint != new Vector2(820, 80)
            || selectedBuildingSelectionProjection[0].ProductionQueue.Count != unitProductionBattlefield.BuildingProductionQueue(unitProductionBarracks.Id).Count
            || selectedBuildingSelectionProjection[0].Icon == IconGlyph.None
            || string.IsNullOrWhiteSpace(selectedBuildingSelectionProjection[0].ShortCode))
        {
            throw new InvalidOperationException("building selection HUD should read selected building data and compact rally destinations from UnitBattlefield EntityWorld projections instead of legacy GameState selected buildings");
        }

        var previousLanguage = GameText.CurrentLanguage;
        try
        {
            GameText.CurrentLanguage = GameLanguage.English;
            if (GameText.Format("ui.rally.destination", 820, 80) != "RALLY 820,80")
            {
                throw new InvalidOperationException("English selected producer details should show compact rally destination coordinates");
            }

            GameText.CurrentLanguage = GameLanguage.ChineseSimplified;
            if (GameText.Format("ui.rally.destination", 820, 80) != "集结 820,80")
            {
                throw new InvalidOperationException("Chinese selected producer details should show compact rally destination coordinates");
            }
        }
        finally
        {
            GameText.CurrentLanguage = previousLanguage;
        }

        var selectedBuildingRallyProjection = unitProductionBattlefield.SelectedBuildingRallyProjections(PlayerSlotId.One);
        if (selectedBuildingRallyProjection.Count != 1
            || selectedBuildingRallyProjection[0].Position != unitProductionBarracks.Position
            || selectedBuildingRallyProjection[0].RallyPoint != new Vector2(820, 80)
            || selectedBuildingRallyProjection[0].RallyPulse <= 0)
        {
            throw new InvalidOperationException("selected building rally projection should expose EntityWorld-selected producer rally lines without reading legacy GameState selected buildings");
        }

        var producedEntity = unitProductionBattlefield.UnitEntityByInstanceId(unitProductionEvents[0].Id);
        var producerEntity = BuildingEntityForTargetId(unitProductionBattlefield, unitProductionBarracks.Id);
        if (producedEntity is null
            || producerEntity is null
            || producedEntity.Id != unitProductionEvents[0].EntityId
            || producedEntity.SpecId != unitProductionEvents[0].Spec.Id
            || producedEntity.Transform.Position != unitProductionEvents[0].Position
            || !producedEntity.Components.TryGet<CommandableComponentState>(out var producedCommandable)
            || producedCommandable.CommandVisualTarget != new Vector2(420, 500)
            || !producerEntity.Components.TryGet<ProductionQueueComponentState>(out var producerQueueAfterCompletion)
            || producerQueueAfterCompletion.Items.Count != 0)
        {
            throw new InvalidOperationException("UnitBattlefield production completion should adopt EntityWorld-spawned units and sync producer queues back to legacy runtime");
        }

        unitProductionBattlefield.SetCredits(PlayerSlotId.One, 500);
        if (unitProductionBattlefield.EnqueueProduction(ProductionKind.Harvester, PlayerSlotId.One, out var unsupportedProductionStatus)
            || unsupportedProductionStatus.Length == 0)
        {
            throw new InvalidOperationException("new unit battlefield should reject production when no matching UnitDesign producer building is available");
        }

        var providerLaneBattlefield = new UnitBattlefield();
        providerLaneBattlefield.SetCredits(PlayerSlotId.One, 500);
        var providerLaneBarracksA = providerLaneBattlefield.UpsertBuildingTarget(
            288,
            BuildingDesignIds.Barracks,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(240, 460),
            0,
            unitProductionBarracksSpec.MaxHp);
        var providerLaneBarracksB = providerLaneBattlefield.UpsertBuildingTarget(
            289,
            BuildingDesignIds.Barracks,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(300, 460),
            0,
            unitProductionBarracksSpec.MaxHp);
        var providerLaneStates = providerLaneBattlefield.ProductionProviderLaneStates(PlayerSlotId.One);
        if (providerLaneStates.Count < 4
            || providerLaneStates[0].Scope != ProductionProviderLaneScope.Auto
            || providerLaneStates[1].Scope != ProductionProviderLaneScope.All
            || providerLaneStates[2].ProducerId != providerLaneBarracksA.Id
            || providerLaneStates[3].ProducerId != providerLaneBarracksB.Id)
        {
            throw new InvalidOperationException("train provider lanes should expose Auto, All, and stable specific provider lanes from UnitBattlefield production providers");
        }

        if (!providerLaneBattlefield.TryCreateProductionDesignPayloadForProvider(
                "dog.infantry",
                PlayerSlotId.One,
                providerLaneBarracksB.Id,
                out var scopedProductionPayload,
                out _)
            || providerLaneBattlefield.SubmitLiveLocalPlayerCommand(PlayerSlotId.One, PlayerCommandKind.Produce, scopedProductionPayload).AcceptedCount != 1
            || providerLaneBattlefield.BuildingProductionQueue(providerLaneBarracksA.Id).Count != 0
            || providerLaneBattlefield.BuildingProductionQueue(providerLaneBarracksB.Id).Count != 1)
        {
            throw new InvalidOperationException("specific train provider lanes should route production payloads only into the selected provider building");
        }

        if (!providerLaneBattlefield.TryCreateProductionDesignPayload(
                "dog.infantry",
                PlayerSlotId.One,
                out var autoProductionPayload,
                out _)
            || providerLaneBattlefield.SubmitLiveLocalPlayerCommand(PlayerSlotId.One, PlayerCommandKind.Produce, autoProductionPayload).AcceptedCount != 1
            || providerLaneBattlefield.BuildingProductionQueue(providerLaneBarracksA.Id).Count != 1
            || providerLaneBattlefield.BuildingProductionQueue(providerLaneBarracksB.Id).Count != 1)
        {
            throw new InvalidOperationException("Auto train provider lane should keep the existing shortest-queue production routing");
        }

        var repeatArmed = providerLaneBattlefield.ToggleRepeatProductionForProvider("dog.infantry", PlayerSlotId.One, providerLaneBarracksB.Id, out var repeatArmStatus);
        var repeatAfterArm = providerLaneBattlefield.BuildingProductionRepeatOutputSpecId(providerLaneBarracksB.Id);
        var laneRepeatAfterArm = RepeatOutputForProviderLane(providerLaneBattlefield.ProductionProviderLaneStates(PlayerSlotId.One), providerLaneBarracksB.Id);
        var repeatCleared = providerLaneBattlefield.ToggleRepeatProductionForProvider("dog.infantry", PlayerSlotId.One, providerLaneBarracksB.Id, out var repeatClearStatus);
        var repeatAfterClear = providerLaneBattlefield.BuildingProductionRepeatOutputSpecId(providerLaneBarracksB.Id);
        if (!repeatArmed
            || repeatAfterArm != "dog.infantry"
            || laneRepeatAfterArm != "dog.infantry"
            || !repeatCleared
            || repeatAfterClear is not null)
        {
            var detail = $"armed={repeatArmed} armStatus='{repeatArmStatus}' repeatAfterArm='{repeatAfterArm}' "
                + $"laneRepeat='{laneRepeatAfterArm}' cleared={repeatCleared} clearStatus='{repeatClearStatus}' repeatAfterClear='{repeatAfterClear}'";
            throw new InvalidOperationException(
                "specific train provider repeat toggle should arm and clear the selected provider's deterministic repeat production state; "
                + detail);
        }

        var constructionProviderBattlefield = new UnitBattlefield();
        constructionProviderBattlefield.SetCredits(PlayerSlotId.One, 1200);
        var constructionHeadquartersA = constructionProviderBattlefield.UpsertBuildingTarget(
            388,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(240, 520),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Headquarters).MaxHp);
        var constructionHeadquartersB = constructionProviderBattlefield.UpsertBuildingTarget(
            389,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(360, 520),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Headquarters).MaxHp);
        var constructionProviderLanes = constructionProviderBattlefield.ConstructionProviderLaneStates(PlayerSlotId.One);
        if (constructionProviderLanes.Count < 4
            || constructionProviderLanes[0].Scope != ProductionProviderLaneScope.Auto
            || constructionProviderLanes[1].Scope != ProductionProviderLaneScope.All
            || constructionProviderLanes[2].ProducerId != constructionHeadquartersA.Id
            || constructionProviderLanes[3].ProducerId != constructionHeadquartersB.Id)
        {
            throw new InvalidOperationException("Build provider lanes should expose Auto, All, and stable specific construction source lanes from UnitBattlefield construction providers");
        }

        var specificConstructionTicket = constructionProviderBattlefield.QueueConstructionTicket(
            PlayerSlotId.One,
            BuildingDesignIds.PowerPlant,
            constructionHeadquartersB.Id,
            out _);
        if (specificConstructionTicket is null
            || specificConstructionTicket.Value.Position != constructionHeadquartersB.Position)
        {
            throw new InvalidOperationException("specific Build provider lanes should queue construction tickets from the selected construction provider only");
        }

        var autoConstructionTicket = constructionProviderBattlefield.QueueConstructionTicket(PlayerSlotId.One, BuildingDesignIds.PowerPlant, out _);
        if (autoConstructionTicket is null
            || autoConstructionTicket.Value.Position != constructionHeadquartersA.Position)
        {
            throw new InvalidOperationException("Auto Build provider lane should preserve the existing first-valid construction provider routing");
        }

        constructionProviderLanes = constructionProviderBattlefield.ConstructionProviderLaneStates(PlayerSlotId.One);
        if (constructionProviderLanes[0].QueueCount != 2
            || constructionProviderLanes[0].ActiveProgress < 0
            || constructionProviderLanes[2].QueueCount != 0
            || constructionProviderLanes[3].QueueCount != 0)
        {
            throw new InvalidOperationException("Build provider lanes should keep construction-ticket queue totals on aggregate lanes while ticket source selection remains command-scoped");
        }

        if (constructionProviderBattlefield.QueueConstructionTicket(
                PlayerSlotId.One,
                BuildingDesignIds.PowerPlant,
                constructionProviderId: 999_999,
                out var invalidConstructionProviderStatus) is not null
            || invalidConstructionProviderStatus != "placement.missingProducer")
        {
            throw new InvalidOperationException("stale specific Build provider lanes should reject instead of falling back to Auto construction routing");
        }

        var unitCancelBattlefield = new UnitBattlefield();
        unitCancelBattlefield.SetCredits(PlayerSlotId.One, 500);
        var cancelBarracks = unitCancelBattlefield.UpsertBuildingTarget(
            89,
            BuildingDesignIds.Barracks,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(240, 560),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Barracks).MaxHp);
        if (!unitCancelBattlefield.EnqueueProduction(ProductionKind.InfantrySquad, PlayerSlotId.One, out _)
            || !unitCancelBattlefield.CancelFirstProduction(PlayerSlotId.One, out _)
            || unitCancelBattlefield.BuildingProductionQueue(cancelBarracks.Id).Count != 0
            || unitCancelBattlefield.Credits(PlayerSlotId.One) != 440)
        {
            throw new InvalidOperationException("new unit battlefield should cancel queued production and refund credits");
        }

        var cancelProducerEntity = BuildingEntityForTargetId(unitCancelBattlefield, cancelBarracks.Id);
        if (unitCancelBattlefield.AppliedInputCommandCount != 2
            || cancelProducerEntity is null
            || !cancelProducerEntity.Components.TryGet<ProductionQueueComponentState>(out var cancelProducerQueue)
            || cancelProducerQueue.Items.Count != 0
            || unitCancelBattlefield.EntityWorld.ResourceInventory(OwnerId.FromPlayerSlot(PlayerSlotId.One)).Credits != 440)
        {
            throw new InvalidOperationException("UnitBattlefield production cancel should route through EntityWorld CancelProductionEntityCommand and sync credits/queue back to legacy runtime");
        }

        var enemyProductionBattlefield = new UnitBattlefield();
        enemyProductionBattlefield.WorldSize = new Vector2(1200, 900);
        enemyProductionBattlefield.SetCredits(PlayerSlotId.Two, 2500);
        var enemyFactory = enemyProductionBattlefield.UpsertBuildingTarget(
            188,
            BuildingDesignIds.VehicleFactory,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(820, 540),
            Mathf.Pi,
            720);
        enemyProductionBattlefield.UpsertBuildingTarget(
            189,
            BuildingDesignIds.Barracks,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(760, 620),
            Mathf.Pi,
            540);
        var runtimeProductionAi = new UnitBattlefieldEnemyProductionAi(new EnemyDifficultyProfile(
            EnemyDifficulty.Normal,
            ProductionInitialDelay: 0,
            ProductionDecisionInterval: 1,
            DesiredHarvesters: 2,
            MaxQueuedItems: 3,
            AttackInitialDelay: 0,
            AttackWaveInterval: 12,
            MinimumWaveUnits: 2,
            MaximumWaveUnits: 4,
            AggressionRadius: float.PositiveInfinity));
        runtimeProductionAi.Update(enemyProductionBattlefield, PlayerSlotId.Two, 0.1);
        if (runtimeProductionAi.SuccessfulOrders != 1
            || enemyProductionBattlefield.BuildingProductionQueue(enemyFactory.Id).Count != 1
            || enemyProductionBattlefield.BuildingProductionQueue(enemyFactory.Id)[0].DesignId != "cat.harvester"
            || enemyProductionBattlefield.BuildingRallyPoint(enemyFactory.Id) is null
            || enemyProductionBattlefield.Credits(PlayerSlotId.Two) != 2375)
        {
            throw new InvalidOperationException("new enemy production AI should queue faction production through UnitBattlefield instead of hidden GameState units");
        }

        for (var step = 0; step < 330; step++)
        {
            enemyProductionBattlefield.Update(1 / 30.0);
        }

        if (enemyProductionBattlefield.Units.All(unit => unit.Spec.Id != "cat.harvester" || unit.PlayerSlotId != PlayerSlotId.Two))
        {
            throw new InvalidOperationException("new enemy production AI should complete production into visible UnitInstance runtime units");
        }

        var enemyWaveBattlefield = new UnitBattlefield();
        enemyWaveBattlefield.WorldSize = new Vector2(1200, 900);
        var playerHqTarget = enemyWaveBattlefield.UpsertBuildingTarget(
            288,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(260, 360),
            0,
            900);
        var enemyWaveA = enemyWaveBattlefield.Spawn("cat.tank", PlayerSlotId.Two, new Vector2(760, 360), Mathf.Pi);
        var enemyWaveB = enemyWaveBattlefield.Spawn("cat.tank", PlayerSlotId.Two, new Vector2(820, 390), Mathf.Pi);
        var runtimeWaveAi = new UnitBattlefieldEnemyAttackWaveAi(new EnemyDifficultyProfile(
            EnemyDifficulty.Normal,
            ProductionInitialDelay: 0,
            ProductionDecisionInterval: 1,
            DesiredHarvesters: 0,
            MaxQueuedItems: 0,
            AttackInitialDelay: 0,
            AttackWaveInterval: 12,
            MinimumWaveUnits: 2,
            MaximumWaveUnits: 4,
            AggressionRadius: float.PositiveInfinity));
        runtimeWaveAi.Update(enemyWaveBattlefield, PlayerSlotId.Two, 0.1);
        if (runtimeWaveAi.WavesLaunched != 1
            || enemyWaveA.AttackTargetKind != CombatTargetKind.Building
            || enemyWaveA.AttackTargetId != playerHqTarget.Id
            || enemyWaveB.AttackTargetKind != CombatTargetKind.Building
            || enemyWaveB.AttackTargetId != playerHqTarget.Id
            || enemyWaveA.MoveMode != MoveCommandMode.Attack
            || enemyWaveB.CommandVisualTarget != playerHqTarget.Position)
        {
            throw new InvalidOperationException("new enemy attack wave AI should command UnitInstance waves against UnitBattlefield building targets");
        }
    }

    private static string? RepeatOutputForProviderLane(IReadOnlyList<ProductionProviderLaneState> states, int producerId)
    {
        for (var index = 0; index < states.Count; index++)
        {
            var state = states[index];
            if (state.Scope == ProductionProviderLaneScope.Specific && state.ProducerId == producerId)
            {
                return state.RepeatOutputSpecId;
            }
        }

        return null;
    }
}
