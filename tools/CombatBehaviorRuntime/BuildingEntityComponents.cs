static partial class Program
{
    private static void AssertBuildingEntityComponents()
    {
        var bridgeHqBuildSpec = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
        var hqEntitySpec = bridgeHqBuildSpec.ToEntitySpec();
        var bridgeBarracksBuildSpec = BuildSpecCatalog.For(BuildingDesignIds.Barracks);
        var barracksEntitySpec = bridgeBarracksBuildSpec.ToEntitySpec();
        var buildingEntityWorld = new EntityWorld();
        var supportWorld = new EntityWorld();
        var dogGuardDesign = UnitDesignCatalog.Spec<DogGuardTank>();
        var dogMirrorEntity = supportWorld.SpawnUnit(dogGuardDesign, OwnerId.FromPlayerSlot(PlayerSlotId.Two), new Vector2(130, 110), 0.35f);
        var dogHarvesterEntity = supportWorld.SpawnUnit(UnitDesignCatalog.Spec<DogHarvester>(), OwnerId.FromPlayerSlot(PlayerSlotId.One), new Vector2(90, 150), 0);

        var hqTarget = new BuildingEntitySeed(
            901,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(700, 720),
            0.2f,
            bridgeHqBuildSpec.MaxHp - 20);
        var barracksTarget = new BuildingEntitySeed(
            902,
            BuildingDesignIds.Barracks,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(780, 720),
            0,
            bridgeBarracksBuildSpec.MaxHp);
        var barracksTargetQueue = new UnitProductionQueueItem
        {
            Id = 12,
            Kind = ProductionKind.InfantrySquad,
            DesignId = "dog.infantry",
            Faction = UnitFactionId.Dog,
            Progress = 1.5f,
        };
        var hqEntity = buildingEntityWorld.SpawnBuildingTarget(hqTarget, bridgeHqBuildSpec);
        hqEntity.Components.Set(hqEntity.Components.Require<WeaponUserComponentState>() with
        {
            Mounts = new[] { new WeaponMountRuntimeState("main", bridgeHqBuildSpec.WeaponKind!.Value, hqTarget.Facing, 0.4f) },
            AttackTarget = dogMirrorEntity.Id,
            AttackTargetKind = CombatTargetKind.Unit,
        });
        var barracksEntity = buildingEntityWorld.SpawnBuildingTarget(
            barracksTarget,
            bridgeBarracksBuildSpec,
            new Vector2(840, 760),
            powered: false,
            buildProgress: 0.7f);
        barracksEntity.Components.Set(new ProductionQueueComponentState([barracksTargetQueue]));
        var bridgeRefineryBuildSpec = BuildSpecCatalog.For(BuildingDesignIds.Refinery);
        var refineryTarget = new BuildingEntitySeed(
            903,
            BuildingDesignIds.Refinery,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(860, 720),
            0,
            bridgeRefineryBuildSpec.MaxHp);
        var refineryEntity = buildingEntityWorld.SpawnBuildingTarget(
            refineryTarget,
            bridgeRefineryBuildSpec,
            deliveryPulse: 0.75f,
            dockReservedByEntityId: dogHarvesterEntity.Id.Value);
        var refineryPresentationProjection = BuildingPresentationProjector.ProjectOne(buildingEntityWorld, refineryEntity);
        var hqWeapon = hqEntity.Components.Require<WeaponUserComponentState>();
        var hqIdentity = hqEntity.Components.Require<BuildingIdentityComponentState>();
        var barracksConstruction = barracksEntity.Components.Require<ConstructionComponentState>();
        var barracksIdentity = barracksEntity.Components.Require<BuildingIdentityComponentState>();
        var barracksProduction = barracksEntity.Components.Require<ProductionQueueComponentState>();
        var refineryDock = refineryEntity.Components.Require<DockComponentState>();
        var refineryIdentity = refineryEntity.Components.Require<BuildingIdentityComponentState>();
        var firstBuildingHash = buildingEntityWorld.DeterministicStateHash();
        var changedBarracksEntity = new EntityWorld();
        changedBarracksEntity.SpawnBuildingTarget(hqTarget, bridgeHqBuildSpec);
        var changedBarracks = changedBarracksEntity.SpawnBuildingTarget(
            barracksTarget,
            bridgeBarracksBuildSpec,
            new Vector2(840, 760),
            powered: false,
            buildProgress: 0.7f);
        changedBarracks.Components.Set(new ProductionQueueComponentState([
            new UnitProductionQueueItem
            {
                Id = barracksTargetQueue.Id,
                Kind = barracksTargetQueue.Kind,
                DesignId = barracksTargetQueue.DesignId,
                Faction = barracksTargetQueue.Faction,
                Progress = 2.25f,
            },
        ]));
        changedBarracksEntity.SpawnBuildingTarget(refineryTarget, bridgeRefineryBuildSpec);
        var changedBuildingHash = changedBarracksEntity.DeterministicStateHash();
        var hqTargetSpec = BuildSpecCatalog.For(hqTarget.Kind);
        if (hqEntitySpec.Kind != EntityKind.Turret
            || hqEntitySpec.Stats?.Cost != bridgeHqBuildSpec.Cost
            || BuildSpecCatalog.Definitions.Count != BuildingDesignIds.All.Count
            || BuildSpecCatalog.Definitions.Values.Select(spec => spec.EntitySpecId).Distinct(StringComparer.Ordinal).Count() != BuildSpecCatalog.Definitions.Count
            || BuildSpecCatalog.Definitions.Values.Any(spec => spec.ToEntitySpec().Id != spec.EntitySpecId)
            || BuildSpecCatalog.Definitions.Values.Any(spec => spec.ToEntitySpec().Authoring.BuildingSpecId != spec.Kind)
            || hqTargetSpec.MaxHp != bridgeHqBuildSpec.MaxHp
            || hqTargetSpec.Footprint != bridgeHqBuildSpec.Footprint
            || hqTargetSpec.ArmorTag != bridgeHqBuildSpec.ArmorTag
            || hqTargetSpec.WeaponKind != bridgeHqBuildSpec.WeaponKind
            || !hqEntitySpec.Tags.Contains("Weapon")
            || hqIdentity.BuildingId != hqTarget.Id
            || hqIdentity.Kind != hqTarget.Kind
            || hqIdentity.PlayerSlotId != hqTarget.PlayerSlotId
            || hqIdentity.Faction != hqTarget.Faction
            || hqWeapon.Mounts.Count != 1
            || hqWeapon.AttackTarget.Value != dogMirrorEntity.Id.Value
            || barracksEntitySpec.Kind != EntityKind.Building
            || !barracksEntitySpec.Tags.Contains("Producer")
            || barracksIdentity.BuildingId != barracksTarget.Id
            || barracksIdentity.Kind != barracksTarget.Kind
            || barracksIdentity.PlayerSlotId != barracksTarget.PlayerSlotId
            || barracksIdentity.Faction != barracksTarget.Faction
            || !barracksEntity.Components.Has<RallyPointComponentState>()
            || barracksConstruction.Progress != 0.7f
            || barracksProduction.Items.Count != 1
            || barracksEntity.Components.Require<PowerComponentState>().Powered
            || refineryIdentity.BuildingId != refineryTarget.Id
            || refineryIdentity.Kind != refineryTarget.Kind
            || refineryIdentity.PlayerSlotId != refineryTarget.PlayerSlotId
            || refineryIdentity.Faction != refineryTarget.Faction
            || refineryDock.ReservedByEntityId != dogHarvesterEntity.Id.Value
            || !refineryPresentationProjection.DockOccupied
            || refineryPresentationProjection.DeliveryPulse != 0.75f
            || firstBuildingHash == 0
            || firstBuildingHash == changedBuildingHash)
        {
            throw new InvalidOperationException("BuildSpecCatalog building targets should bridge into entity specs/components for turrets, producers, construction, power, docks, projected dock delivery pulses, and deterministic hashes");
        }
    }
}
