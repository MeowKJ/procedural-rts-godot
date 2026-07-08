static partial class Program
{
    private static void AssertSkirmishAiAndSetup()
    {
        var enemyProductionState = new GameState();
        var enemyAi = new EnemyProductionAi();
        var enemyStartCredits = enemyProductionState.Credits(Owner.Enemy);
        var enemyStartUnits = enemyProductionState.Units.Count(unit => unit.Owner == Owner.Enemy);
        Advance(enemyProductionState, 0.1f);
        enemyAi.Update(enemyProductionState, 2.0);

        var enemyQueued = enemyProductionState.Buildings
            .Where(building => building.Owner == Owner.Enemy)
            .SelectMany(building => building.ProductionQueue)
            .ToList();

        if (enemyQueued.Count == 0)
        {
            throw new InvalidOperationException("enemy production AI should queue a unit when producers and credits exist");
        }

        if (enemyProductionState.Credits(Owner.Enemy) >= enemyStartCredits)
        {
            throw new InvalidOperationException("enemy production AI should spend enemy credits when queueing");
        }

        var playableProductionProducerKinds = ProductionKindDesignBridge.PlayableProductionSpecs(UnitFactionId.Dog, UnitFactionId.Cat)
            .Select(spec => spec.Production!.ProducerKind)
            .ToHashSet();
        if (!enemyProductionState.Buildings
            .Where(building => building.Owner == Owner.Enemy)
            .Where(building => playableProductionProducerKinds.Contains(building.Kind))
            .All(building => building.RallyPoint is not null))
        {
            throw new InvalidOperationException("enemy production AI should set rally points on enemy production buildings");
        }

        Advance(enemyProductionState, 12.0f);

        if (enemyProductionState.Units.Count(unit => unit.Owner == Owner.Enemy) <= enemyStartUnits)
        {
            throw new InvalidOperationException("enemy production queue should complete into enemy units");
        }

        if (EnemyDifficultyProfile.For(EnemyDifficulty.Normal).ProductionInitialDelay != 1.4f
            || EnemyDifficultyProfile.For(EnemyDifficulty.Normal).AttackInitialDelay != 8f
            || EnemyDifficultyProfile.For(EnemyDifficulty.Normal).MaximumWaveUnits != 8)
        {
            throw new InvalidOperationException("normal enemy difficulty should preserve the original AI timing and wave size");
        }

        if (EnemyDifficultyProfile.Hard.ProductionDecisionInterval >= EnemyDifficultyProfile.Normal.ProductionDecisionInterval
            || EnemyDifficultyProfile.Hard.MaximumWaveUnits <= EnemyDifficultyProfile.Easy.MaximumWaveUnits
            || EnemyDifficultyProfile.Easy.AggressionRadius >= EnemyDifficultyProfile.Hard.AggressionRadius)
        {
            throw new InvalidOperationException("enemy difficulty profiles should scale production pace, wave size, and aggression radius");
        }

        var customSkirmish = new SkirmishOptions(StartingCredits: 3600, MapSeed: 424242, EnemyDifficulty: EnemyDifficulty.Hard);
        SkirmishSetupState.PendingOptions = customSkirmish;
        var customSkirmishState = new GameState(SkirmishSetupState.PendingOptions);
        if (customSkirmishState.Options != customSkirmish
            || customSkirmishState.MatchConfig != customSkirmish.ToMatchConfig()
            || customSkirmishState.Credits(Owner.Player) != 3600
            || customSkirmishState.Credits(Owner.Enemy) != 3600
            || customSkirmishState.Options.EnemyDifficulty != EnemyDifficulty.Hard)
        {
            throw new InvalidOperationException("skirmish options should configure starting resources and enemy difficulty");
        }

        var mirroredMatchConfig = new MatchConfig(
            StartingCredits: 4100,
            MapSeed: 515151,
            EnemyDifficulty: EnemyDifficulty.Hard,
            WorldSize: new Vector2(4200, 2800),
            PlayerFaction: FactionId.Cat,
            AiFaction: FactionId.Dog);
        var mirroredMatchA = new GameState(mirroredMatchConfig);
        var mirroredMatchB = new GameState(mirroredMatchConfig);
        if (mirroredMatchA.MatchConfig != mirroredMatchConfig
            || mirroredMatchA.Options != mirroredMatchConfig.ToSkirmishOptions()
            || mirroredMatchA.WorldSize != mirroredMatchConfig.WorldSize
            || mirroredMatchA.Credits(Owner.Player) != mirroredMatchConfig.StartingCredits
            || mirroredMatchA.Credits(Owner.Enemy) != mirroredMatchConfig.StartingCredits
            || mirroredMatchA.MatchConfig.FactionForOwner(Owner.Player) != FactionId.Cat
            || mirroredMatchA.MatchConfig.FactionForOwner(Owner.Enemy) != FactionId.Dog)
        {
            throw new InvalidOperationException("MatchConfig should be the immutable source for factions, world size, credits, seed, and difficulty");
        }

        if (mirroredMatchA.Buildings.Any(building => building.Owner == Owner.Player && building.FactionId != FactionId.Cat)
            || mirroredMatchA.Buildings.Any(building => building.Owner == Owner.Enemy && building.FactionId != FactionId.Dog))
        {
            throw new InvalidOperationException("MatchConfig factions should seed owner building factions");
        }

        var mirroredResourcesA = mirroredMatchA.ResourceFields.Select(field => field.Position).ToList();
        var mirroredResourcesB = mirroredMatchB.ResourceFields.Select(field => field.Position).ToList();
        if (mirroredResourcesA.Where((position, index) => position.DistanceTo(mirroredResourcesB[index]) > 0.01f).Any())
        {
            throw new InvalidOperationException("same MatchConfig should produce stable resource positions");
        }

        var generatedLayout = SkirmishMapGenerator.Generate(mirroredMatchConfig);
        if (generatedLayout.Resources.Count < 6
            || generatedLayout.Resources.Count % 2 != 0
            || generatedLayout.Obstacles.Count < 4
            || generatedLayout.Obstacles.Count % 2 != 0)
        {
            throw new InvalidOperationException("skirmish map generator should create paired resources and paired choke obstacles");
        }

        if ((generatedLayout.PlayerStart + generatedLayout.EnemyStart).DistanceTo(mirroredMatchConfig.WorldSize) > 0.01f)
        {
            throw new InvalidOperationException("skirmish map starts should be mirrored around the world center");
        }

        var playerHqStart = mirroredMatchA.Buildings.First(building => building.Owner == Owner.Player && building.Kind == BuildingDesignIds.Headquarters);
        var enemyHqStart = mirroredMatchA.Buildings.First(building => building.Owner == Owner.Enemy && building.Kind == BuildingDesignIds.Headquarters);
        if ((playerHqStart.Position + enemyHqStart.Position).DistanceTo(mirroredMatchConfig.WorldSize) > 0.01f)
        {
            throw new InvalidOperationException("skirmish map should seed mirrored HQ start positions");
        }

        if (mirroredMatchA.ResourceFields.Count != generatedLayout.Resources.Count)
        {
            throw new InvalidOperationException("GameState should seed resources from SkirmishMapGenerator");
        }

        for (var index = 0; index < mirroredMatchA.ResourceFields.Count; index += 2)
        {
            var left = mirroredMatchA.ResourceFields[index];
            var right = mirroredMatchA.ResourceFields[index + 1];
            if ((left.Position + right.Position).DistanceTo(mirroredMatchConfig.WorldSize) > 0.01f
                || left.Amount != right.Amount
                || left.MaxAmount != right.MaxAmount
                || MathF.Abs(left.Radius - right.Radius) > 0.01f)
            {
                throw new InvalidOperationException("skirmish map should seed mirrored, equal-value resource pairs");
            }
        }

        if (mirroredMatchA.MapObstacles.Count != generatedLayout.Obstacles.Count
            || mirroredMatchA.DebugPathObstacles().Count == 0)
        {
            throw new InvalidOperationException("skirmish map choke obstacles should be stored and included in path obstacles");
        }

        for (var index = 0; index < mirroredMatchA.MapObstacles.Count; index += 2)
        {
            var left = mirroredMatchA.MapObstacles[index];
            var right = mirroredMatchA.MapObstacles[index + 1];
            var leftCenter = new Vector2(left.X + left.Width * 0.5f, left.Y + left.Height * 0.5f);
            var rightCenter = new Vector2(right.X + right.Width * 0.5f, right.Y + right.Height * 0.5f);
            if ((leftCenter + rightCenter).DistanceTo(mirroredMatchConfig.WorldSize) > 0.01f
                || MathF.Abs(left.Width - right.Width) > 0.01f
                || MathF.Abs(left.Height - right.Height) > 0.01f)
            {
                throw new InvalidOperationException("skirmish map should seed mirrored choke obstacles");
            }
        }

        var mirroredBuildingsA = mirroredMatchA.Buildings.Select(building => (building.Owner, building.FactionId, building.Kind, building.Position)).ToList();
        var mirroredBuildingsB = mirroredMatchB.Buildings.Select(building => (building.Owner, building.FactionId, building.Kind, building.Position)).ToList();
        if (mirroredBuildingsA.Count != mirroredBuildingsB.Count
            || mirroredBuildingsA.Where((building, index) =>
                building.Owner != mirroredBuildingsB[index].Owner
                || building.FactionId != mirroredBuildingsB[index].FactionId
                || building.Kind != mirroredBuildingsB[index].Kind
                || building.Position.DistanceTo(mirroredBuildingsB[index].Position) > 0.01f).Any())
        {
            throw new InvalidOperationException("same MatchConfig should produce stable starting buildings");
        }

        var dogStartLoadout = MatchStartLoadouts.For(Owner.Player, FactionId.Dog);
        var catStartLoadout = MatchStartLoadouts.For(Owner.Enemy, FactionId.Cat);
        if (!dogStartLoadout.Buildings.Select(building => building.Kind).SequenceEqual(MatchStartLoadouts.StartingBuildings(FactionId.Dog))
            || !catStartLoadout.Buildings.Select(building => building.Kind).SequenceEqual(MatchStartLoadouts.StartingBuildings(FactionId.Cat))
            || !dogStartLoadout.Units.Select(unit => unit.DesignId).SequenceEqual(StartingDesignIds(UnitFactionId.Dog))
            || !catStartLoadout.Units.Select(unit => unit.DesignId).SequenceEqual(StartingDesignIds(UnitFactionId.Cat))
            || dogStartLoadout.Buildings.All(building => building.Kind != BuildingDesignIds.Headquarters)
            || dogStartLoadout.Buildings.All(building => building.Kind != BuildingDesignIds.Refinery)
            || dogStartLoadout.Units.Count(unit => UnitDesignCatalog.Spec(unit.DesignId).RoleTags.Contains(UnitRoleTag.Economy)) is < 1 or > 2
            || catStartLoadout.Units.Count(unit => UnitDesignCatalog.Spec(unit.DesignId).RoleTags.Contains(UnitRoleTag.Economy)) is < 1 or > 2)
        {
            throw new InvalidOperationException("MatchStartLoadouts should source unit starts from UnitDesignRuntimeLoadouts instead of FactionCatalog StartingUnits");
        }

        var catPlayerDogAi = new GameState(new MatchConfig(
            StartingCredits: 2400,
            MapSeed: 111111,
            EnemyDifficulty: EnemyDifficulty.Normal,
            WorldSize: MatchConfig.DefaultWorldSize,
            PlayerFaction: FactionId.Cat,
            AiFaction: FactionId.Dog));
        if (!StartingDesignIds(UnitFactionId.Cat).All(designId => catPlayerDogAi.Units.Any(unit => unit.Owner == Owner.Player && unit.FactionId == FactionId.Cat && unit.DesignId == designId))
            || !StartingDesignIds(UnitFactionId.Dog).All(designId => catPlayerDogAi.Units.Any(unit => unit.Owner == Owner.Enemy && unit.FactionId == FactionId.Dog && unit.DesignId == designId))
            || !MatchStartLoadouts.StartingBuildings(FactionId.Cat).All(kind => catPlayerDogAi.Buildings.Any(building => building.Owner == Owner.Player && building.FactionId == FactionId.Cat && building.Kind == kind))
            || !MatchStartLoadouts.StartingBuildings(FactionId.Dog).All(kind => catPlayerDogAi.Buildings.Any(building => building.Owner == Owner.Enemy && building.FactionId == FactionId.Dog && building.Kind == kind)))
        {
            throw new InvalidOperationException("MatchConfig factions should choose MatchStartLoadouts-owned starting buildings and UnitDesign-owned starting units per owner");
        }

        var uiFactionSkirmish = new SkirmishOptions(
            StartingCredits: 2600,
            MapSeed: 222222,
            EnemyDifficulty: EnemyDifficulty.Easy,
            PlayerFaction: FactionId.Cat,
            AiFaction: FactionId.Dog);
        var uiFactionState = new GameState(uiFactionSkirmish);
        if (uiFactionState.Options != uiFactionSkirmish
            || uiFactionState.MatchConfig.PlayerFaction != FactionId.Cat
            || uiFactionState.MatchConfig.AiFaction != FactionId.Dog
            || !StartingDesignIds(UnitFactionId.Cat).All(designId => uiFactionState.Units.Any(unit => unit.Owner == Owner.Player && unit.FactionId == FactionId.Cat && unit.DesignId == designId))
            || !StartingDesignIds(UnitFactionId.Dog).All(designId => uiFactionState.Units.Any(unit => unit.Owner == Owner.Enemy && unit.FactionId == FactionId.Dog && unit.DesignId == designId)))
        {
            throw new InvalidOperationException("SkirmishOptions factions should configure player and AI starting loadouts");
        }

        var mirrorFactionSkirmish = new GameState(new SkirmishOptions(
            StartingCredits: 2600,
            MapSeed: 222333,
            EnemyDifficulty: EnemyDifficulty.Normal,
            PlayerFaction: FactionId.Dog,
            AiFaction: FactionId.Dog));
        var mirrorEnemyUnit = mirrorFactionSkirmish.Units.First(unit => unit.Owner == Owner.Enemy);
        if (mirrorFactionSkirmish.MatchConfig.PlayerFaction != mirrorFactionSkirmish.MatchConfig.AiFaction
            || !mirrorFactionSkirmish.CanOwnerAttack(Owner.Player, mirrorEnemyUnit.Owner))
        {
            throw new InvalidOperationException("same-faction mirror skirmish should remain owner-hostile");
        }

        var defaultSandboxComparison = new GameState(SkirmishOptions.Default);
        var sandboxState = new GameState(SkirmishOptions.Sandbox);
        if (SkirmishOptions.Sandbox.LaunchMode != LaunchMode.Sandbox
            || sandboxState.Options.LaunchMode != LaunchMode.Sandbox
            || sandboxState.Credits(Owner.Player) != SkirmishOptions.SandboxStartingCredits
            || sandboxState.VisualTheme.Current != WorldVisualTheme.DayCommand
            || sandboxState.ResourceAtmosphere != ResourceAtmosphere.Day
            || sandboxState.VisualTheme.Driver != "developer-sandbox")
        {
            throw new InvalidOperationException("developer sandbox launch options should initialize a high-resource daytime test battle");
        }

        if (sandboxState.Units.Count <= defaultSandboxComparison.Units.Count
            || sandboxState.Buildings.Count <= defaultSandboxComparison.Buildings.Count
            || sandboxState.ResourceFields.Count <= defaultSandboxComparison.ResourceFields.Count
            || !sandboxState.Buildings.Any(building => building.Owner == Owner.Player && building.Kind == BuildingDesignIds.VehicleFactory))
        {
            throw new InvalidOperationException("developer sandbox should add extra units, resources, and a player vehicle factory for current system testing");
        }

        var dogPlayableDesignIds = PlayableDesignIds(UnitFactionId.Dog);
        var catPlayableDesignIds = PlayableDesignIds(UnitFactionId.Cat);
        if (!dogPlayableDesignIds.All(designId => sandboxState.Units.Any(unit => unit.Owner == Owner.Player && unit.FactionId == FactionId.Dog && unit.DesignId == designId))
            || !catPlayableDesignIds.All(designId => sandboxState.Units.Any(unit => unit.Owner == Owner.Player && unit.FactionId == FactionId.Cat && unit.DesignId == designId)))
        {
            throw new InvalidOperationException("developer sandbox should spawn every Dog/Cat UnitDesign playable roster unit for presentation and gameplay inspection");
        }

        var sandboxEnemyDog = sandboxState.Units.FirstOrDefault(unit => unit.Owner == Owner.Enemy && unit.FactionId == FactionId.Dog && unit.DesignId == "dog.guard_tank");
        var sandboxEnemyCat = sandboxState.Units.FirstOrDefault(unit => unit.Owner == Owner.Enemy && unit.FactionId == FactionId.Cat && unit.DesignId == "cat.tank");
        var sandboxPlayerDog = sandboxState.Units.FirstOrDefault(unit => unit.Owner == Owner.Player && unit.FactionId == FactionId.Dog && unit.DesignId == "dog.guard_tank");
        var sandboxPlayerCat = sandboxState.Units.FirstOrDefault(unit => unit.Owner == Owner.Player && unit.FactionId == FactionId.Cat && unit.DesignId == "cat.tank");
        if (sandboxEnemyDog is null
            || sandboxEnemyCat is null
            || sandboxPlayerDog is null
            || sandboxPlayerCat is null
            || !sandboxState.IsTargetableHostile(sandboxPlayerDog.Owner, sandboxEnemyDog)
            || !sandboxState.IsTargetableHostile(sandboxPlayerCat.Owner, sandboxEnemyCat)
            || !sandboxState.IsAlliedWithPlayer(sandboxPlayerCat))
        {
            throw new InvalidOperationException("developer sandbox should include dog-vs-dog, cat-vs-cat, and allied mixed-faction owner test cases");
        }

        sandboxState.ApplySandboxAtmosphere(SandboxAtmospherePreset.Dusk);
        if (sandboxState.VisualTheme.Current != WorldVisualTheme.FogMorning
            || sandboxState.VisualTheme.Target != WorldVisualTheme.FogMorning
            || sandboxState.VisualTheme.Driver != "sandbox-fog-morning"
            || sandboxState.ResourceAtmosphere != ResourceAtmosphere.Fog
            || sandboxState.SignalNodes.Any(node => !node.Powered))
        {
            throw new InvalidOperationException("sandbox dusk control should switch to fog-morning exploration while keeping the signal network powered");
        }

        sandboxState.ApplySandboxAtmosphere(SandboxAtmospherePreset.Corruption);
        if (sandboxState.VisualTheme.Driver != "sandbox-corruption"
            || sandboxState.VisualTheme.Target != WorldVisualTheme.DuskDefense
            || sandboxState.ResourceAtmosphere != ResourceAtmosphere.Corruption
            || sandboxState.SignalNodes.Any(node => node.Powered))
        {
            throw new InvalidOperationException("sandbox corruption control should start a dusk-defense transition and depower signal nodes");
        }

        sandboxState.AdvanceVisualThemeTransition(10f);
        if (sandboxState.ResourceAtmosphere != ResourceAtmosphere.Corruption)
        {
            throw new InvalidOperationException("sandbox corruption atmosphere should stay authoritative for sim economy/signal rules until another atmosphere preset replaces it");
        }

        var sandboxSignalProbe = sandboxState.SignalNodes.First(node => node.Kind == SignalNodeKind.SignalTower);
        if (sandboxState.FogOfWar.IsVisible(sandboxSignalProbe.Position))
        {
            throw new InvalidOperationException("depowered sandbox signal network should not contribute fog vision during corruption tests");
        }

        sandboxState.ApplySandboxAtmosphere(SandboxAtmospherePreset.SignalRestoration);
        if (sandboxState.VisualTheme.Driver != "sandbox-signal-restoration"
            || sandboxState.VisualTheme.Current != WorldVisualTheme.FogMorning
            || sandboxState.VisualTheme.Target != WorldVisualTheme.DayCommand
            || sandboxState.ResourceAtmosphere != ResourceAtmosphere.Fog
            || sandboxState.SignalNodes.Any(node => !node.Powered))
        {
            throw new InvalidOperationException("sandbox signal restoration control should repower signal nodes and transition fog morning back to day");
        }

        sandboxState.ApplySandboxAtmosphere(SandboxAtmospherePreset.Daytime);
        if (sandboxState.VisualTheme.Current != WorldVisualTheme.DayCommand
            || sandboxState.VisualTheme.Driver != "sandbox-daytime"
            || sandboxState.ResourceAtmosphere != ResourceAtmosphere.Day
            || sandboxState.FogOfWar.IsVisible(sandboxSignalProbe.Position))
        {
            throw new InvalidOperationException("sandbox daytime control should return to planning mode without night signal-vision contribution");
        }

        var defaultSeedState = new GameState(SkirmishOptions.Default);
        var seededStateA = new GameState(new SkirmishOptions(2400, 111111, EnemyDifficulty.Normal));
        var seededStateB = new GameState(new SkirmishOptions(2400, 111111, EnemyDifficulty.Normal));
        var seededStateC = new GameState(new SkirmishOptions(2400, 222222, EnemyDifficulty.Normal));
        var defaultResourcePositions = defaultSeedState.ResourceFields.Select(field => field.Position).ToList();
        var seededPositionsA = seededStateA.ResourceFields.Select(field => field.Position).ToList();
        var seededPositionsB = seededStateB.ResourceFields.Select(field => field.Position).ToList();
        var seededPositionsC = seededStateC.ResourceFields.Select(field => field.Position).ToList();

        if (seededPositionsA.Where((position, index) => position.DistanceTo(seededPositionsB[index]) > 0.01f).Any())
        {
            throw new InvalidOperationException("same skirmish map seed should produce stable resource positions");
        }

        if (!seededPositionsA.Where((position, index) => position.DistanceTo(defaultResourcePositions[index]) > 0.01f).Any()
            || !seededPositionsA.Where((position, index) => position.DistanceTo(seededPositionsC[index]) > 0.01f).Any())
        {
            throw new InvalidOperationException("different skirmish map seeds should alter resource positions");
        }

        SkirmishSetupState.PendingOptions = SkirmishOptions.Default;

        var easyProductionState = new GameState();
        var easyProductionAi = new EnemyProductionAi(EnemyDifficultyProfile.Easy);
        Advance(easyProductionState, 0.1f);
        easyProductionAi.Update(easyProductionState, 2.0);

        if (easyProductionState.Buildings
            .Where(building => building.Owner == Owner.Enemy)
            .SelectMany(building => building.ProductionQueue)
            .Any())
        {
            throw new InvalidOperationException("easy enemy production should wait longer before its first queue decision");
        }

        var hardProductionState = new GameState();
        var hardProductionAi = new EnemyProductionAi(EnemyDifficultyProfile.Hard);
        Advance(hardProductionState, 0.1f);
        hardProductionAi.Update(hardProductionState, 1.0);

        if (!hardProductionState.Buildings
            .Where(building => building.Owner == Owner.Enemy)
            .SelectMany(building => building.ProductionQueue)
            .Any())
        {
            throw new InvalidOperationException("hard enemy production should queue quickly from its shorter opening delay");
        }

        var enemyWaveState = new GameState();
        var waveAi = new EnemyAttackWaveAi();
        var playerHq = enemyWaveState.Buildings.First(building => building.Owner == Owner.Player && building.Kind == BuildingDesignIds.Headquarters);
        waveAi.Update(enemyWaveState, 8.1);

        if (waveAi.WavesLaunched != 1)
        {
            throw new InvalidOperationException("enemy attack wave AI should launch when enough combat units exist");
        }

        var waveCombatUnits = enemyWaveState.Units
            .Where(unit => unit.Owner == Owner.Enemy && IsCombatUnit(unit))
            .ToList();

        if (waveCombatUnits.Count == 0 || waveCombatUnits.Any(unit =>
            unit.AttackTargetKind != CombatTargetKind.Building
            || unit.AttackTargetId != playerHq.Id
            || !unit.AttackTargetIsManual
            || !unit.AttackTargetAllowsPursuit))
        {
            throw new InvalidOperationException("enemy attack wave should order combat units to pursue the player HQ");
        }

        if (enemyWaveState.Units
            .Where(unit => unit.Owner == Owner.Enemy && IsHarvesterUnit(unit))
            .Any(unit => unit.AttackTargetId is not null))
        {
            throw new InvalidOperationException("enemy attack wave should not assign harvesters to combat waves");
        }
    }
}
