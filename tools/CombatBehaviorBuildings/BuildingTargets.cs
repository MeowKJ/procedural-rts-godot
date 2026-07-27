static partial class Program
{
    private static void AssertBuildSpecBuildingRuntime()
    {
        var hqBuildSpec = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
        var hqEntitySpec = hqBuildSpec.ToEntitySpec();
        var barracksBuildSpec = BuildSpecCatalog.For(BuildingDesignIds.Barracks);
        var barracksEntitySpec = barracksBuildSpec.ToEntitySpec();
        var airfieldBuildSpec = BuildSpecCatalog.For(BuildingDesignIds.Airfield);
        if (hqEntitySpec.Id != hqBuildSpec.EntitySpecId
            || barracksEntitySpec.Id != barracksBuildSpec.EntitySpecId
            || hqEntitySpec.Display.NameKey != hqBuildSpec.NameKey
            || hqEntitySpec.Display.RoleKey != hqBuildSpec.RoleKey
            || hqEntitySpec.Display.ShortCode != hqBuildSpec.ShortCode
            || hqEntitySpec.Display.Icon != hqBuildSpec.Icon
            || hqBuildSpec.RoleGlyph != IconGlyph.StanceHold
            || airfieldBuildSpec.ShortCode != "AIR"
            || airfieldBuildSpec.RoleGlyph != airfieldBuildSpec.Icon)
        {
            throw new InvalidOperationException("BuildSpec building presentation fields should supply display metadata for EntityWorld projections");
        }

        var buildingEntityWorld = new EntityWorld();
        var buildSpecRuntimeBattlefield = new UnitBattlefield();
        var buildSpecRuntimeBarracks = buildSpecRuntimeBattlefield.UpsertBuildingTarget(
            904,
            BuildingDesignIds.Barracks,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(900, 720),
            0,
            321,
            powered: false,
            buildProgress: 0.5f,
            rallyPoint: new Vector2(930, 740));
        var buildSpecRuntimeEntity = BuildingEntityForTargetId(buildSpecRuntimeBattlefield, buildSpecRuntimeBarracks.Id);
        var buildSpecRuntimeSpec = BuildSpecCatalog.For(BuildingDesignIds.Barracks);
        if (buildSpecRuntimeEntity is not null
            && buildSpecRuntimeEntity.Components.TryGet<HealthComponentState>(out var buildSpecRuntimeHealth))
        {
            buildSpecRuntimeEntity.Components.Set(buildSpecRuntimeHealth with { Hp = 123 });
            buildSpecRuntimeEntity.Components.Set(new FootprintComponentState(new Vector2(144, 112)));
            buildSpecRuntimeEntity.Components.Set(new ConstructionComponentState(0.42f, 7f, 420, 0.5f, ConstructionPauseReason.Unpowered));
            buildSpecRuntimeEntity.Components.Set(new PowerComponentState(0, 6, Powered: false));
            buildSpecRuntimeEntity.Components.Set(new RallyPointComponentState(new Vector2(944, 755)));
            buildSpecRuntimeEntity.Components.Set(new PresentationPulseComponentState(CommandPulse: 0.61f, AlertPulse: 0.2f, HitPulse: 0.66f));
            buildSpecRuntimeEntity.Components.Set(new ProductionQueueComponentState(
            [
                new UnitProductionQueueItem
                {
                    Id = 77,
                    DesignId = "dog.infantry",
                    Faction = UnitFactionId.Dog,
                    Progress = 3.25f,
                },
            ]));
        }

        var buildSpecRuntimeProjection = buildSpecRuntimeBattlefield.BuildingProjection(buildSpecRuntimeBarracks.Id);
        var buildSpecRuntimePresentation = buildSpecRuntimeBattlefield.BuildingPresentationProjection(buildSpecRuntimeBarracks.Id);
        var buildSpecRuntimeViewProjection = buildSpecRuntimeBattlefield.BuildingViewProjection(buildSpecRuntimeBarracks.Id);
        var buildSpecRuntimeIdentity = buildSpecRuntimeEntity?.Components.Require<BuildingIdentityComponentState>();
        var buildSpecRuntimeTargetSpec = BuildSpecCatalog.For(buildSpecRuntimeBarracks.Kind);
        if (buildSpecRuntimeTargetSpec.MaxHp != buildSpecRuntimeSpec.MaxHp
            || buildSpecRuntimeTargetSpec.Footprint != buildSpecRuntimeSpec.Footprint
            || buildSpecRuntimeTargetSpec.ArmorTag != buildSpecRuntimeSpec.ArmorTag
            || buildSpecRuntimeTargetSpec.WeaponId != buildSpecRuntimeSpec.WeaponId
            || buildSpecRuntimeEntity is null
            || buildSpecRuntimeProjection is null
            || buildSpecRuntimeViewProjection is null
            || buildSpecRuntimeIdentity is null
            || buildSpecRuntimeIdentity.BuildingId != buildSpecRuntimeBarracks.Id
            || buildSpecRuntimeIdentity.Kind != BuildingDesignIds.Barracks
            || buildSpecRuntimeIdentity.PlayerSlotId != PlayerSlotId.One
            || buildSpecRuntimeIdentity.Faction != UnitFactionId.Dog
            || buildSpecRuntimeViewProjection.Value.Kind != BuildingDesignIds.Barracks
            || buildSpecRuntimeViewProjection.Value.PlayerSlotId != PlayerSlotId.One
            || buildSpecRuntimeViewProjection.Value.Faction != UnitFactionId.Dog
            || buildSpecRuntimeViewProjection.Value.Presentation.Entity.Id != buildSpecRuntimeProjection.Value.Id
            || buildSpecRuntimeProjection.Value.Hp != 123
            || buildSpecRuntimeProjection.Value.MaxHp != buildSpecRuntimeSpec.MaxHp
            || buildSpecRuntimeEntity.Components.Require<ConstructionComponentState>().Progress != 0.42f
            || buildSpecRuntimeEntity.Components.Require<PowerComponentState>().Powered
            || buildSpecRuntimeBattlefield.BuildingPowered(buildSpecRuntimeBarracks.Id)
            || buildSpecRuntimeBattlefield.BuildingBuildProgress(buildSpecRuntimeBarracks.Id) != 0.42f)
        {
            throw new InvalidOperationException("UnitBattlefield BuildSpec upsert overload and building projection should derive runtime shape and view state from EntityWorld/BuildSpecCatalog");
        }

        if (buildSpecRuntimePresentation is null
            || buildSpecRuntimePresentation.Value.Entity.Hp != 123
            || buildSpecRuntimePresentation.Value.Footprint != new Vector2(144, 112)
            || buildSpecRuntimePresentation.Value.Powered
            || buildSpecRuntimePresentation.Value.BuildProgress != 0.42f
            || !buildSpecRuntimePresentation.Value.ConstructionPaused
            || buildSpecRuntimePresentation.Value.PauseReason != ConstructionPauseReason.Unpowered
            || !buildSpecRuntimePresentation.Value.IsConstructionPaused
            || !buildSpecRuntimePresentation.Value.HasReadableOfflineState
            || buildSpecRuntimePresentation.Value.RallyPoint != new Vector2(944, 755)
            || buildSpecRuntimePresentation.Value.ProductionQueue.Count != 1
            || buildSpecRuntimePresentation.Value.ProductionQueue[0].Progress != 3.25f
            || buildSpecRuntimePresentation.Value.ProductionQueue[0].DesignId != "dog.infantry")
        {
            throw new InvalidOperationException("building presentation projection should derive production, rally, power, construction, and footprint from EntityWorld components; offline readability is included in the projected building state");
        }

        var damagedBuildingPresentation = buildSpecRuntimePresentation.Value;
        if (!damagedBuildingPresentation.HasReadableDamageState
            || damagedBuildingPresentation.DamageSeverity == BuildingDamageReadabilityLevel.None
            || damagedBuildingPresentation.MissingHealthFraction <= 0
            || BuildingPresentationProjection.DamageSeverityFor(0.99f, true) != BuildingDamageReadabilityLevel.None
            || BuildingPresentationProjection.DamageSeverityFor(0.40f, true) != BuildingDamageReadabilityLevel.Heavy
            || BuildingPresentationProjection.DamageSeverityFor(0.18f, true) != BuildingDamageReadabilityLevel.Critical
            || BuildingPresentationProjection.DamageSeverityFor(0.18f, false) != BuildingDamageReadabilityLevel.None)
        {
            throw new InvalidOperationException("damaged building readability should be derived from projected EntityWorld health without owner color state");
        }

        var projectedBuildingCullingRect = new Rect2(
            buildSpecRuntimePresentation.Value.Entity.Position - buildSpecRuntimePresentation.Value.Footprint / 2f,
            buildSpecRuntimePresentation.Value.Footprint);
        if (!projectedBuildingCullingRect.HasPoint(buildSpecRuntimePresentation.Value.Entity.Position)
            || projectedBuildingCullingRect.Size != new Vector2(144, 112))
        {
            throw new InvalidOperationException("building view culling should use UnitBattlefield EntityWorld presentation projection position and footprint");
        }

        if (!buildSpecRuntimeBattlefield.SetBuildingTargetSelected(buildSpecRuntimeBarracks.Id, true)
            || buildSpecRuntimeBattlefield.BuildingProjection(buildSpecRuntimeBarracks.Id)?.Selected != true
            || BuildingEntityForTargetId(buildSpecRuntimeBattlefield, buildSpecRuntimeBarracks.Id)?.Components.Require<SelectableComponentState>().Selected != true)
        {
            throw new InvalidOperationException("building selection projection should sync retired building selection into EntityWorld SelectableComponentState");
        }

        buildSpecRuntimeEntity!.Components.Remove<BuildingIdentityComponentState>();
        if (buildSpecRuntimeBattlefield.BuildingSnapshots().Any(snapshot => snapshot.Id == buildSpecRuntimeBarracks.Id))
        {
            throw new InvalidOperationException("BuildingSnapshots should enumerate EntityWorld building identities only and not resurrect seed-only fallback ids");
        }

        if (buildSpecRuntimeBattlefield.BuildingSnapshot(buildSpecRuntimeBarracks.Id) is not null
            || buildSpecRuntimeBattlefield.BuildingViewProjection(buildSpecRuntimeBarracks.Id) is not null)
        {
            throw new InvalidOperationException("BuildingSnapshot should require EntityWorld building identity and not synthesize direct seed fallback snapshots");
        }

        var buildSpecRuntimeResyncedBarracks = buildSpecRuntimeBattlefield.UpsertBuildingTarget(
            buildSpecRuntimeBarracks.Id,
            BuildingDesignIds.Barracks,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(905, 725),
            0.1f,
            222,
            powered: true,
            buildProgress: 1,
            rallyPoint: new Vector2(940, 745));
        if (buildSpecRuntimeResyncedBarracks.Id != buildSpecRuntimeBarracks.Id
            || buildSpecRuntimeBattlefield.BuildingSnapshot(buildSpecRuntimeBarracks.Id) is null
            || buildSpecRuntimeBattlefield.BuildingViewProjection(buildSpecRuntimeBarracks.Id) is null
            || buildSpecRuntimeEntity.Components.TryGet<BuildingIdentityComponentState>(out _) == false)
        {
            throw new InvalidOperationException("direct building snapshot/view reads must not fall back to seed identity when EntityWorld identity is missing, and upsert must restore the EntityWorld identity");
        }

        if (typeof(UnitBattlefield).GetField("_buildingTargetSeedsById", BindingFlags.Instance | BindingFlags.NonPublic) is not null)
        {
            throw new InvalidOperationException("UnitBattlefield should delete temporary building seed lifecycle storage after EntityWorld owns building targets");
        }

        if (buildSpecRuntimeBattlefield.BuildingSnapshots().All(snapshot => snapshot.Id != buildSpecRuntimeBarracks.Id)
            || buildSpecRuntimeBattlefield.BuildingSnapshot(buildSpecRuntimeBarracks.Id) is null
            || buildSpecRuntimeBattlefield.BuildingViewProjection(buildSpecRuntimeBarracks.Id) is null)
        {
            throw new InvalidOperationException("BuildingTargetIds should enumerate EntityWorld building identities without requiring temporary seed storage");
        }

        var seedlessPublicQueue = buildSpecRuntimeBattlefield.BuildingProductionQueue(buildSpecRuntimeBarracks.Id);
        if (seedlessPublicQueue.Count != 1
            || seedlessPublicQueue[0].DesignId != "dog.infantry"
            || buildSpecRuntimeBattlefield.BuildingRallyPoint(buildSpecRuntimeBarracks.Id) != new Vector2(940, 745)
            || buildSpecRuntimeBattlefield.BuildingRallyPulse(buildSpecRuntimeBarracks.Id) != 0.61f
            || !buildSpecRuntimeBattlefield.BuildingPowered(buildSpecRuntimeBarracks.Id)
            || buildSpecRuntimeBattlefield.BuildingBuildProgress(buildSpecRuntimeBarracks.Id) != 1
            || buildSpecRuntimeBattlefield.BuildingAttackTargetId(buildSpecRuntimeBarracks.Id) is not null
            || buildSpecRuntimeBattlefield.BuildingAttackTargetKind(buildSpecRuntimeBarracks.Id) != CombatTargetKind.Unit
            || buildSpecRuntimeBattlefield.BuildingAttackCooldownRemaining(buildSpecRuntimeBarracks.Id) != 0
            || buildSpecRuntimeBattlefield.PickAnyBuildingTargetId(buildSpecRuntimeBarracks.Position, pickPadding: 8) != buildSpecRuntimeBarracks.Id)
        {
            throw new InvalidOperationException("building public read APIs should read EntityWorld components without requiring temporary seed storage");
        }

        if (!buildSpecRuntimeBattlefield.SetBuildingTargetSelected(buildSpecRuntimeBarracks.Id, false)
            || buildSpecRuntimeBattlefield.BuildingProjection(buildSpecRuntimeBarracks.Id)?.Selected == true
            || !buildSpecRuntimeBattlefield.SetBuildingTargetSelected(buildSpecRuntimeBarracks.Id, true)
            || buildSpecRuntimeBattlefield.BuildingProjection(buildSpecRuntimeBarracks.Id)?.Selected != true)
        {
            throw new InvalidOperationException("building selection writes should update EntityWorld selectable state without requiring temporary seed storage");
        }

        if (!buildSpecRuntimeBattlefield.SetRallyPoint(buildSpecRuntimeBarracks.Id, new Vector2(960, 750), out _)
            || buildSpecRuntimeBattlefield.BuildingRallyPoint(buildSpecRuntimeBarracks.Id) != new Vector2(960, 750))
        {
            throw new InvalidOperationException("building direct rally commands should read EntityWorld identity and entity id without requiring temporary seed storage");
        }

        var syncBuildingTargetEntityMethod = typeof(UnitBattlefield).GetMethod("SyncBuildingTargetEntity", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("UnitBattlefield should sync building targets through a private id-based helper");
        var seedlessSyncResult = (bool)syncBuildingTargetEntityMethod.Invoke(
            buildSpecRuntimeBattlefield,
            [buildSpecRuntimeBarracks.Id, null, null, null, null, null])!;
        if (!seedlessSyncResult
            || buildSpecRuntimeBattlefield.BuildingSnapshot(buildSpecRuntimeBarracks.Id) is null)
        {
            throw new InvalidOperationException("building target sync should refresh existing EntityWorld buildings without requiring temporary seed storage");
        }

        var seedlessUpsertedBarracks = buildSpecRuntimeBattlefield.UpsertBuildingTarget(
            buildSpecRuntimeBarracks.Id,
            BuildingDesignIds.PowerPlant,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(970, 760),
            0.25f,
            211,
            powered: true,
            buildProgress: 1,
            rallyPoint: new Vector2(975, 765));
        if (seedlessUpsertedBarracks.Kind != BuildingDesignIds.Barracks
            || seedlessUpsertedBarracks.PlayerSlotId != PlayerSlotId.One
            || seedlessUpsertedBarracks.Faction != UnitFactionId.Dog
            || seedlessUpsertedBarracks.Position != new Vector2(970, 760)
            || seedlessUpsertedBarracks.Hp != 211
            || buildSpecRuntimeBattlefield.BuildingRallyPoint(buildSpecRuntimeBarracks.Id) != new Vector2(975, 765))
        {
            throw new InvalidOperationException("building upsert should preserve EntityWorld identity and refresh runtime state without requiring or repopulating temporary seed storage");
        }

        var adoptConstructedBuildingMethod = typeof(UnitBattlefield).GetMethod("AdoptConstructedBuildingId", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("UnitBattlefield should keep constructed building adoption behind a private id-returning helper");
        buildSpecRuntimeEntity.Components.Remove<BuildingIdentityComponentState>();
        var adoptedExistingId = (int)adoptConstructedBuildingMethod.Invoke(
            buildSpecRuntimeBattlefield,
            [buildSpecRuntimeEntity, BuildingDesignIds.Barracks, PlayerSlotId.One, UnitFactionId.Dog])!;
        if (adoptedExistingId != buildSpecRuntimeBarracks.Id
            || buildSpecRuntimeEntity.Components.TryGet<BuildingIdentityComponentState>(out var restoredIdentity) == false
            || restoredIdentity.BuildingId != buildSpecRuntimeBarracks.Id)
        {
            throw new InvalidOperationException("building adoption should reuse the reverse EntityId index and restore EntityWorld identity without requiring temporary seed storage");
        }

        var buildingReverseIndexField = typeof(UnitBattlefield).GetField("_buildingTargetIdsByEntityId", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("UnitBattlefield should keep a private reverse building EntityId index");
        var buildingReverseIndex = (IDictionary<EntityId, int>)buildingReverseIndexField.GetValue(buildSpecRuntimeBattlefield)!;
        var buildSpecRuntimeEntityId = buildSpecRuntimeEntity.Id;
        if (!buildingReverseIndex.TryGetValue(buildSpecRuntimeEntityId, out var reverseBuildingId)
            || reverseBuildingId != buildSpecRuntimeBarracks.Id)
        {
            throw new InvalidOperationException("building reverse EntityId index should resolve building ids without scanning the forward mapping");
        }

        var nextBuildingTargetIdField = typeof(UnitBattlefield).GetField("_nextBuildingTargetId", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("UnitBattlefield should keep private next building id state");
        var nextBuildingTargetIdMethod = typeof(UnitBattlefield).GetMethod("NextBuildingTargetId", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("UnitBattlefield should allocate building ids through a private helper");
        nextBuildingTargetIdField.SetValue(buildSpecRuntimeBattlefield, buildSpecRuntimeBarracks.Id);
        var seedlessNextBuildingId = (int)nextBuildingTargetIdMethod.Invoke(buildSpecRuntimeBattlefield, [])!;
        if (seedlessNextBuildingId == buildSpecRuntimeBarracks.Id)
        {
            throw new InvalidOperationException("building id allocation should skip EntityWorld building identities without requiring temporary seed storage");
        }

        var runtimeEntityWorldField = typeof(UnitBattlefield).GetField("_entityWorld", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("UnitBattlefield should keep an EntityWorld instance for building adoption");
        var runtimeEntityWorld = (EntityWorld)runtimeEntityWorldField.GetValue(buildSpecRuntimeBattlefield)!;
        var seedlessAdoptSpec = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
        var seedlessAdoptEntity = runtimeEntityWorld.SpawnBuildingTarget(
            new BuildingEntitySeed(
                9904,
                BuildingDesignIds.PowerPlant,
                PlayerSlotId.Two,
                UnitFactionId.Cat,
                new Vector2(1040, 790),
                0.2f,
                seedlessAdoptSpec.MaxHp - 40),
            seedlessAdoptSpec);
        seedlessAdoptEntity.Components.Remove<BuildingIdentityComponentState>();
        var seedlessAdoptedId = (int)adoptConstructedBuildingMethod.Invoke(
            buildSpecRuntimeBattlefield,
            [seedlessAdoptEntity, BuildingDesignIds.PowerPlant, PlayerSlotId.Two, UnitFactionId.Cat])!;
        if (buildSpecRuntimeBattlefield.BuildingEntityIdByTargetId(seedlessAdoptedId) != seedlessAdoptEntity.Id
            || buildSpecRuntimeBattlefield.BuildingSnapshot(seedlessAdoptedId) is not { } seedlessAdoptedSnapshot
            || seedlessAdoptedSnapshot.Kind != BuildingDesignIds.PowerPlant
            || seedlessAdoptedSnapshot.PlayerSlotId != PlayerSlotId.Two)
        {
            throw new InvalidOperationException("building adoption should map constructed EntityWorld buildings without repopulating temporary seed storage");
        }

        buildSpecRuntimeBattlefield.RemoveBuildingTarget(buildSpecRuntimeBarracks.Id);
        if (buildingReverseIndex.ContainsKey(buildSpecRuntimeEntityId)
            || buildSpecRuntimeBattlefield.BuildingEntityIdByTargetId(buildSpecRuntimeBarracks.Id) is not null)
        {
            throw new InvalidOperationException("building reverse EntityId index should clear removed building entity mappings");
        }

        if (buildSpecRuntimeBattlefield.BuildingSnapshots().Any(snapshot => snapshot.Id == buildSpecRuntimeBarracks.Id)
            || buildSpecRuntimeBattlefield.BuildingSnapshot(buildSpecRuntimeBarracks.Id) is not null)
        {
            throw new InvalidOperationException("building removal should not require temporary seed storage");
        }
    }

}
