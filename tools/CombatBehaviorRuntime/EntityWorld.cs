static partial class Program
{
    private static void AssertEntityWorldAndPalette()
    {
        var dogGuardDesign = UnitDesignCatalog.Spec<DogGuardTank>();

        var entityWorld = new EntityWorld();
        var dogGuardEntity = entityWorld.SpawnUnit(
            dogGuardDesign,
            OwnerId.FromPlayerSlot(PlayerSlotId.One),
            new Vector2(90, 110),
            0.35f);
        var dogMirrorEntity = entityWorld.SpawnUnit(
            dogGuardDesign,
            OwnerId.FromPlayerSlot(PlayerSlotId.Two),
            new Vector2(130, 110),
            0.35f);
        var dogHarvesterEntity = entityWorld.SpawnUnit(
            UnitDesignCatalog.Spec<DogHarvester>(),
            OwnerId.FromPlayerSlot(PlayerSlotId.One),
            new Vector2(90, 150),
            0);
        var guardHealth = dogGuardEntity.Components.Require<HealthComponentState>();
        var guardWeapons = dogGuardEntity.Components.Require<WeaponUserComponentState>();
        var harvesterCargo = dogHarvesterEntity.Components.Require<ResourceCargoComponentState>();
        var moveEntityCommand = new MoveEntityCommand(
            OwnerId.FromPlayerSlot(PlayerSlotId.One),
            [dogGuardEntity.Id, dogHarvesterEntity.Id],
            12,
            new Vector2(300, 320),
            MoveCommandMode.Attack);
        if (!dogGuardEntity.Id.IsValid
            || dogGuardEntity.Id == dogMirrorEntity.Id
            || dogGuardEntity.SpecId != dogGuardDesign.Id
            || dogGuardEntity.OwnerId != OwnerId.FromPlayerSlot(PlayerSlotId.One)
            || dogMirrorEntity.OwnerId != OwnerId.FromPlayerSlot(PlayerSlotId.Two)
            || dogGuardEntity.Transform.Position != new Vector2(90, 110)
            || guardHealth.Hp != dogGuardDesign.Stats.MaxHp
            || guardWeapons.Mounts.Count != dogGuardDesign.Weapons.Count
            || !dogGuardEntity.Components.Has<SelectableComponentState>()
            || !dogGuardEntity.Components.Has<CommandableComponentState>()
            || !dogGuardEntity.Components.Has<MovementComponentState>()
            || !dogGuardEntity.Components.Has<CollisionComponentState>()
            || !dogGuardEntity.Components.Has<VisionComponentState>()
            || harvesterCargo.Capacity <= 0
            || !dogHarvesterEntity.Components.Has<HarvesterComponentState>()
            || entityWorld.StableEntities.Select(entity => entity.Id.Value).SequenceEqual(entityWorld.StableEntities.Select(entity => entity.Id.Value).OrderBy(id => id)) == false
            || moveEntityCommand.Kind != EntityCommandKind.Move
            || moveEntityCommand.Subjects.Count != 2
            || moveEntityCommand.Mode != MoveCommandMode.Attack)
        {
            throw new InvalidOperationException("entity framework skeleton should spawn stable thin entities with explicit component state and command objects");
        }

        var repeatedEntityWorld = new EntityWorld();
        repeatedEntityWorld.SpawnUnit(
            dogGuardDesign,
            OwnerId.FromPlayerSlot(PlayerSlotId.One),
            new Vector2(90, 110),
            0.35f);
        repeatedEntityWorld.SpawnUnit(
            dogGuardDesign,
            OwnerId.FromPlayerSlot(PlayerSlotId.Two),
            new Vector2(130, 110),
            0.35f);
        repeatedEntityWorld.SpawnUnit(
            UnitDesignCatalog.Spec<DogHarvester>(),
            OwnerId.FromPlayerSlot(PlayerSlotId.One),
            new Vector2(90, 150),
            0);
        var initialEntityHash = entityWorld.DeterministicStateHash();
        var repeatedEntityHash = repeatedEntityWorld.DeterministicStateHash();
        dogMirrorEntity.Transform = EntityTransform.At(new Vector2(134, 110), dogMirrorEntity.Transform.Facing);
        var movedEntityHash = entityWorld.DeterministicStateHash();
        if (entityWorld.StableSpecs.Count != 2
            || !entityWorld.TryGetSpec(dogGuardDesign.Id, out var registeredDogGuardSpec)
            || registeredDogGuardSpec.Kind != EntityKind.Unit
            || initialEntityHash == 0
            || initialEntityHash != repeatedEntityHash
            || movedEntityHash == initialEntityHash)
        {
            throw new InvalidOperationException("entity worlds should register specs and produce deterministic state hashes for replay/sync verification");
        }

        var commandBuffer = new EntityCommandBuffer();
        commandBuffer.Enqueue(new MoveEntityCommand(
            OwnerId.FromPlayerSlot(PlayerSlotId.Two),
            [dogMirrorEntity.Id],
            14,
            new Vector2(500, 500),
            MoveCommandMode.Direct));
        commandBuffer.Enqueue(moveEntityCommand);
        commandBuffer.Enqueue(new HarvestEntityCommand(
            OwnerId.FromPlayerSlot(PlayerSlotId.One),
            [dogHarvesterEntity.Id],
            12,
            dogGuardEntity.Id));
        var commandSnapshot = commandBuffer.Snapshot();
        var drainedCommands = commandBuffer.DrainUpToTick(12);
        if (commandSnapshot.Count != 3
            || commandSnapshot[0].Command.Tick != 12
            || commandSnapshot[0].Command.Issuer != OwnerId.FromPlayerSlot(PlayerSlotId.One)
            || drainedCommands.Count != 2
            || commandBuffer.Count != 1
            || commandBuffer.DrainUpToTick(99).Single().Command.Tick != 14)
        {
            throw new InvalidOperationException("entity command buffer should keep a stable command log order and deterministic tick draining");
        }

        DisplayAudioSettings.ApplyOwnerColorPalette(OwnerColorPaletteMode.Standard, persist: false);
        var standardOwnerOne = SoftOldCityPalette.PlayerColor(PlayerSlotId.One);
        var standardOwnerTwo = SoftOldCityPalette.PlayerColor(PlayerSlotId.Two);
        DisplayAudioSettings.ApplyOwnerColorPalette(OwnerColorPaletteMode.ColorblindSafe, persist: false);
        var safeOwnerOne = SoftOldCityPalette.PlayerColor(PlayerSlotId.One);
        var safeOwnerTwo = SoftOldCityPalette.PlayerColor(PlayerSlotId.Two);
        var safeOwnerThree = SoftOldCityPalette.PlayerColor(PlayerSlotId.Three);
        var safeOwnerFour = SoftOldCityPalette.PlayerColor(PlayerSlotId.Four);
        if (DisplayAudioSettings.OwnerColors != OwnerColorPaletteMode.ColorblindSafe
            || DisplayAudioSettings.OwnerColorPaletteLabel(OwnerColorPaletteMode.ColorblindSafe) == DisplayAudioSettings.OwnerColorPaletteLabel(OwnerColorPaletteMode.Standard)
            || safeOwnerOne == standardOwnerOne
            || safeOwnerTwo == standardOwnerTwo
            || ColorDistance(safeOwnerOne, safeOwnerTwo) < 0.34f
            || ColorDistance(safeOwnerOne, safeOwnerThree) < 0.34f
            || ColorDistance(safeOwnerTwo, safeOwnerFour) < 0.34f
            || ColorDistance(safeOwnerThree, safeOwnerFour) < 0.34f)
        {
            throw new InvalidOperationException("owner colors should provide a selectable colorblind-safe palette with separated player colors");
        }

        DisplayAudioSettings.ApplyOwnerColorPalette(OwnerColorPaletteMode.Standard, persist: false);
        var ownerColor = SoftOldCityPalette.PlayerColor(PlayerSlotId.Two);
        var entityRenderPalette = EntityRenderPalette.SoftOldCity(ownerColor);
        var ownerDay = entityRenderPalette.Resolve(ColorRole.Owner, EnvironmentTone.Day, EnvironmentResponse.OwnerProtected);
        var ownerNight = entityRenderPalette.Resolve(ColorRole.Owner, EnvironmentTone.Night, EnvironmentResponse.OwnerProtected);
        var ownerCorruption = entityRenderPalette.Resolve(ColorRole.Owner, EnvironmentTone.Corruption, EnvironmentResponse.OwnerProtected);
        var transitionTone = EnvironmentTone.Lerp(EnvironmentTone.FogMorning, EnvironmentTone.Night, 0.5f);
        var ownerTransition = entityRenderPalette.Resolve(ColorRole.Owner, transitionTone, EnvironmentResponse.OwnerProtected);
        var bodyDay = entityRenderPalette.Resolve(ColorRole.Body, EnvironmentTone.Day);
        var bodyFog = entityRenderPalette.Resolve(ColorRole.Body, EnvironmentTone.FogMorning);
        var inkDay = entityRenderPalette.Resolve(ColorRole.Ink, EnvironmentTone.Day);
        var inkDusk = entityRenderPalette.Resolve(ColorRole.Ink, EnvironmentTone.Dusk);
        var shadowDay = entityRenderPalette.Resolve(ColorRole.Shadow, EnvironmentTone.Day);
        var shadowNight = entityRenderPalette.Resolve(ColorRole.Shadow, EnvironmentTone.Night);
        var effectDay = entityRenderPalette.Resolve(ColorRole.Effect, EnvironmentTone.Day);
        var effectCorruption = entityRenderPalette.Resolve(ColorRole.Effect, EnvironmentTone.Corruption);
        var visualThemeTone = EnvironmentTonePalette.For(new WorldVisualThemeState(WorldVisualTheme.DuskDefense, WorldVisualTheme.DuskDefense, 1, "combat-test"));
        var corruptionThemeTone = EnvironmentTonePalette.For(new WorldVisualThemeState(WorldVisualTheme.DayCommand, WorldVisualTheme.DuskDefense, 0.24f, "sandbox-corruption"));
        var hostileRelationColor = SoftOldCityPalette.RelationColor(PlayerRelation.Hostile);
        var dogFactionColor = SoftOldCityPalette.FactionColor(UnitFactionId.Dog);
        var dogGuardOwnerLayers = dogGuardDesign.Art.PlayerColorZones.ToList();
        var environmentToneFailures = new List<string>();
        if (dogGuardOwnerLayers.Count == 0)
        {
            environmentToneFailures.Add("missing owner layers");
        }

        if (dogGuardOwnerLayers.Any(layer => layer.ColorRole != ColorRole.Owner))
        {
            environmentToneFailures.Add("owner layers not ColorRole.Owner");
        }

        if (dogGuardOwnerLayers.Any(layer => layer.EnvironmentResponse != EnvironmentResponse.Normal))
        {
            environmentToneFailures.Add("owner layers should not require per-layer response overrides");
        }

        if (dogGuardDesign.Art.Layers.Any(layer => layer.Zone == ArtLayerZone.FactionMark && layer.ColorRole == ColorRole.Owner))
        {
            environmentToneFailures.Add("faction marks use owner color");
        }

        if (ownerDay.A <= 0)
        {
            environmentToneFailures.Add("owner day alpha");
        }

        if (Mathf.Max(ownerNight.R, Mathf.Max(ownerNight.G, ownerNight.B)) < EnvironmentTone.Night.OwnerMinimumChannel)
        {
            environmentToneFailures.Add("owner night readability");
        }

        if (Mathf.Max(ownerCorruption.R, Mathf.Max(ownerCorruption.G, ownerCorruption.B)) < EnvironmentTone.Corruption.OwnerMinimumChannel)
        {
            environmentToneFailures.Add("owner corruption readability");
        }

        if (Mathf.Max(ownerTransition.R, Mathf.Max(ownerTransition.G, ownerTransition.B)) < transitionTone.OwnerMinimumChannel)
        {
            environmentToneFailures.Add("owner transition readability");
        }

        if (ColorDistance(bodyDay, bodyFog) < 0.04f)
        {
            environmentToneFailures.Add("body tone delta");
        }

        if (ColorDistance(inkDay, inkDusk) < 0.04f)
        {
            environmentToneFailures.Add("ink tone delta");
        }

        if (ColorDistance(shadowDay, shadowNight) < 0.04f)
        {
            environmentToneFailures.Add("shadow tone delta");
        }

        if (ColorDistance(effectDay, effectCorruption) < 0.04f)
        {
            environmentToneFailures.Add("effect tone delta");
        }

        if (visualThemeTone != EnvironmentTone.Dusk)
        {
            environmentToneFailures.Add("dusk theme mapping");
        }

        if (ColorDistance(corruptionThemeTone.Apply(effectDay, ColorRole.Effect), effectDay) < 0.04f)
        {
            environmentToneFailures.Add("corruption driver tone delta");
        }

        if (ColorDistance(ownerNight, ownerColor) > ColorDistance(ownerNight, hostileRelationColor))
        {
            environmentToneFailures.Add("owner night closer to relation than owner");
        }

        if (ColorDistance(ownerCorruption, ownerColor) > ColorDistance(ownerCorruption, dogFactionColor))
        {
            environmentToneFailures.Add("owner corruption closer to faction than owner");
        }

        if (environmentToneFailures.Count > 0)
        {
            throw new InvalidOperationException($"environment tone art profile should affect body/ink/shadow/effect while preserving owner color as the only ownership signal and keeping relation colors overlay-only: {string.Join(", ", environmentToneFailures)}");
        }
    }
}
