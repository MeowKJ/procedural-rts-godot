using ProceduralRts.Core;

internal static class PlayableMapHandoffScenarios
{
    public static void Run(MapSpec source, List<string> failures)
    {
        var authored = AuthoredFixture(source);
        ValidateLoadedProjection(authored, failures);
        ValidateAtomicStaging(authored, failures);
        MapPreflightAtomicScenarios.Run(authored, failures);
        MapEnvironmentHashScenarios.Run(authored, failures);
        ValidateDefaultHandoff(authored, failures);
    }

    private static void ValidateLoadedProjection(MapSpec map, List<string> failures)
    {
        var world = MapLoader.Load(map);
        var battlefield = UnitBattlefield.AdoptLoadedMap(world, map);

        var expectedBuilding = map.Buildings[0];
        var projectedBuilding = battlefield.BuildingSnapshots().Single(building => building.Id == 77);
        var projectedPresentation = battlefield.BuildingPresentationProjection(projectedBuilding.Id);
        Require(projectedBuilding.Kind == expectedBuilding.Kind
            && projectedBuilding.Position == expectedBuilding.Position.ToVector2()
            && MathF.Abs(projectedBuilding.Facing - expectedBuilding.Facing) < 0.0001f
            && MathF.Abs(projectedBuilding.Hp - 725) < 0.0001f
            && projectedPresentation is { BuildProgress: 0.95f },
            "authored EntityWorld projection should preserve building id, transform, hp, and build progress.", failures);

        Require(battlefield.EntityWorld == world, "UnitBattlefield should adopt the exact EntityWorld produced by MapLoader.", failures);
        Require(battlefield.WorldSize == map.WorldSize.ToVector2(), "UnitBattlefield should use authored world bounds.", failures);
        Require(battlefield.Credits(PlayerSlotId.One) == 1800 && battlefield.Credits(PlayerSlotId.Two) == 2100,
            "UnitBattlefield should observe loaded owner credits.", failures);
        Require(battlefield.Units.Count == map.Units.Count
            && battlefield.BuildingSnapshots().Count == map.Buildings.Count
            && battlefield.ResourceNodeProjections().Count == map.Resources.Count,
            "UnitBattlefield should adopt every loaded gameplay entity without respawning it.", failures);
        Require(battlefield.Units.Zip(map.Units).All(pair =>
                pair.First.Spec.Id == pair.Second.DesignId
                && pair.First.PlayerSlotId == pair.Second.OwnerId.ToPlayerSlot()
                && pair.First.Position == pair.Second.Position.ToVector2()
                && MathF.Abs(pair.First.Facing - pair.Second.Facing) < 0.0001f),
            "UnitBattlefield should preserve authored unit order, owner, position, and facing.", failures);

        var environment = world.MapEnvironment;
        Require(environment.OwnerStarts.Count == map.OwnerStarts.Count
            && environment.TerrainCells.Count == map.TerrainCells.Count
            && environment.StaticObstacles.Count == map.Obstacles.Count
            && environment.Triggers.Count == map.Triggers.Count
            && environment.Objectives.Count == map.Objectives.Count
            && environment.NarrativeNodes.Count == map.NarrativeNodes.Count,
            "loaded runtime environment should preserve starts, terrain, obstacles, triggers, objectives, and narrative metadata.", failures);
        Require(world.OrderedEntities.Any(entity =>
                entity.SpecId == $"map.objective.{map.Objectives[0].Id}"
                && entity.Transform.Position == map.Objectives[0].Position.ToVector2()),
            "authored objective should enter EntityWorld with its exact id and position.", failures);
        Require(MapLoader.Load(map).DeterministicStateHash() == MapLoader.Load(map).DeterministicStateHash(),
            "authored MapLoader handoff should produce a deterministic initial hash.", failures);
    }

    private static void ValidateAtomicStaging(MapSpec valid, List<string> failures)
    {
        var previous = SkirmishSetupState.PendingMatchConfig;
        try
        {
            SkirmishSetupState.StageAuthoredMap(valid, EnemyDifficulty.Hard);
            Require(SkirmishSetupState.PendingMatchConfig.AuthoredMap == valid,
                "StageAuthoredMap should publish a validated MapSpec handoff.", failures);

            var beforeInvalid = SkirmishSetupState.PendingMatchConfig;
            var invalid = valid with
            {
                Id = "qa.authored.invalid",
                Buildings =
                [
                    valid.Buildings[0] with { Position = new MapPoint(valid.WorldSize.Width - 1, valid.WorldSize.Height - 1) },
                    .. valid.Buildings.Skip(1),
                ],
            };
            MapBuildingPlacementValidationException? rejection = null;
            try
            {
                SkirmishSetupState.StageAuthoredMap(invalid);
            }
            catch (MapBuildingPlacementValidationException exception)
            {
                rejection = exception;
            }

            Require(rejection is not null && SkirmishSetupState.PendingMatchConfig == beforeInvalid,
                "invalid authored staging should fail atomically without replacing pending match state.", failures);

            var sandboxAuthored = MatchConfig.ForAuthoredMap(valid) with { LaunchMode = LaunchMode.Sandbox };
            var rejectedSandbox = false;
            try
            {
                SkirmishSetupState.StageMatchConfig(sandboxAuthored);
            }
            catch (InvalidOperationException)
            {
                rejectedSandbox = true;
            }

            Require(rejectedSandbox && SkirmishSetupState.PendingMatchConfig == beforeInvalid,
                "sandbox plus authored map should reject without mutating pending state.", failures);
        }
        finally
        {
            SkirmishSetupState.StageMatchConfig(previous);
        }
    }

    private static void ValidateDefaultHandoff(MapSpec authored, List<string> failures)
    {
        var previous = SkirmishSetupState.PendingMatchConfig;
        try
        {
            SkirmishSetupState.PendingOptions = SkirmishOptions.Default;
            Require(SkirmishSetupState.PendingMatchConfig == MatchConfig.Default,
                "default PendingOptions should retain the existing default MatchConfig.", failures);
            SkirmishSetupState.PendingOptions = SkirmishOptions.Sandbox;
            Require(SkirmishSetupState.PendingMatchConfig == MatchConfig.Sandbox
                && SkirmishSetupState.PendingMatchConfig.AuthoredMap is null,
                "sandbox PendingOptions should clear any authored map handoff.", failures);
            SkirmishSetupState.ClearAuthoredMapHandoff();
            Require(SkirmishSetupState.PendingMatchConfig == MatchConfig.Sandbox,
                "authored-only clear must not reset a sandbox pending config.", failures);
            var custom = MatchConfig.Default with { StartingCredits = 3600, MapSeed = 909 };
            SkirmishSetupState.StageMatchConfig(custom);
            SkirmishSetupState.ClearAuthoredMapHandoff();
            Require(SkirmishSetupState.PendingMatchConfig == custom,
                "authored-only clear must not reset a normal pending config.", failures);
            SkirmishSetupState.StageAuthoredMap(authored);
            var beforeFailedReturn = SkirmishSetupState.PendingMatchConfig;
            Require(SkirmishSetupState.PendingMatchConfig == beforeFailedReturn,
                "failed return-to-menu must preserve authored handoff for restart.", failures);
            Require(SkirmishSetupState.PendingMatchConfig.AuthoredMap == authored,
                "authored restart semantics must survive until MainMenu successfully becomes ready.", failures);
            SkirmishSetupState.ClearAuthoredMapHandoff();
            Require(SkirmishSetupState.PendingMatchConfig == MatchConfig.Default,
                "authored-only clear must reset an authored pending config to default.", failures);
        }
        finally
        {
            SkirmishSetupState.StageMatchConfig(previous);
        }
    }

    private static MapSpec AuthoredFixture(MapSpec source)
    {
        return source with
        {
            Id = "qa.playable-authored-map",
            Seed = 453,
            OwnerStarts =
            [
                source.OwnerStarts[0] with { StartingCredits = 1800 },
                source.OwnerStarts[1] with { StartingCredits = 2100 },
            ],
            Buildings = source.Buildings
                .Select((building, index) => index == 0
                    ? building with { RuntimeId = 77, Hp = 725, BuildProgress = 0.95f }
                    : building)
                .ToArray(),
        };
    }

    private static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }
}
