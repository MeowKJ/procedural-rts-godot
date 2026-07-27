using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public partial class SkirmishFlowQaRunner
{
    private static void AssertAuthoredBattle(BattleRoot battle)
    {
        var map = battle.DebugMatchConfig.AuthoredMap
            ?? throw new InvalidOperationException("authored battle did not preserve its MapSpec handoff");
        if (map.Id != "qa.authored-flow"
            || battle.DebugWorldSize != map.WorldSize.ToVector2()
            || battle.DebugRuntimeCredits(PlayerSlotId.One) != 1800
            || battle.DebugRuntimeCredits(PlayerSlotId.Two) != 2100)
        {
            throw new InvalidOperationException("authored battle did not preserve world bounds and owner credits");
        }

        var projections = battle.DebugRuntimeEntityProjections();
        if (projections.Count != map.Resources.Count + map.Buildings.Count + map.Units.Count + map.Objectives.Count
            || projections.Count(projection => projection.Kind == EntityKind.Unit) != map.Units.Count
            || projections.Count(projection => projection.Kind is EntityKind.Building or EntityKind.Turret) != map.Buildings.Count
            || projections.Count(projection => projection.Kind == EntityKind.Resource) != map.Resources.Count
            || projections.Count(projection => projection.Kind == EntityKind.Objective) != map.Objectives.Count)
        {
            throw new InvalidOperationException("authored battle did not adopt the exact MapLoader entity set");
        }

        var environment = battle.DebugRuntimeMapEnvironment;
        if (environment.WorldSize != map.WorldSize
            || environment.OwnerStarts.Count != map.OwnerStarts.Count
            || environment.TerrainCells.Count != map.TerrainCells.Count
            || environment.StaticObstacles.Count != map.Obstacles.Count
            || environment.Triggers.Count != map.Triggers.Count
            || environment.Objectives.Count != map.Objectives.Count
            || environment.NarrativeNodes.Count != map.NarrativeNodes.Count)
        {
            throw new InvalidOperationException("authored battle did not preserve its loaded runtime environment metadata");
        }

        if (battle.DebugRuntimeStateHash == 0)
        {
            throw new InvalidOperationException("authored battle runtime hash should include the loaded map");
        }

        if (!battle.DebugUsesSingleAuthoredEntityWorld
            || battle.DebugSimClockTick != 0)
        {
            throw new InvalidOperationException("authored battle must use the MapLoader EntityWorld as its runtime authority");
        }
    }

    private static void AssertNormalBattleAfterAuthored(BattleRoot battle)
    {
        var generated = SkirmishMapGenerator.GenerateSpec(MatchConfig.Default);
        if (battle.DebugMatchConfig.AuthoredMap is not null
            || battle.DebugMatchConfig != MatchConfig.Default
            || battle.DebugRuntimeMapSpec.Id != generated.Id
            || battle.DebugRuntimeMapEnvironment.WorldSize != generated.WorldSize)
        {
            throw new InvalidOperationException("normal battle retained stale authored-map state");
        }

        var expectedPlayer = UnitDesignRuntimeLoadouts.StartingUnits(UnitFactionId.Dog).Select(spawn => spawn.DesignId);
        var expectedEnemy = UnitDesignRuntimeLoadouts.StartingUnits(UnitFactionId.Cat).Select(spawn => spawn.DesignId);
        if (!battle.DebugUnitBattlefieldDesignIds(PlayerSlotId.One).SequenceEqual(expectedPlayer)
            || !battle.DebugUnitBattlefieldDesignIds(PlayerSlotId.Two).SequenceEqual(expectedEnemy))
        {
            throw new InvalidOperationException("normal battle loadout changed after authored-map teardown");
        }
    }

    private void LaunchAuthoredBattle()
    {
        SkirmishSetupState.StageAuthoredMap(AuthoredQaMap(), EnemyDifficulty.Hard);
        _startedAuthoredBattle = true;
        _cleanupStarted = false;
        _cleanupFrames = 0;
        var error = GetTree().ChangeSceneToFile("res://scenes/Battle.tscn");
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"Failed to load authored battle for skirmish flow QA: {error}");
        }
    }

    private void LaunchNormalBattleAfterAuthored()
    {
        SkirmishSetupState.PendingOptions = SkirmishOptions.Default;
        _startedPostAuthoredBattle = true;
        _cleanupStarted = false;
        _cleanupFrames = 0;
        var error = GetTree().ChangeSceneToFile("res://scenes/Battle.tscn");
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"Failed to load normal battle after authored teardown: {error}");
        }
    }

    private static MapSpec AuthoredQaMap()
    {
        var generated = SkirmishMapGenerator.GenerateSpec(MatchConfig.Default);
        return generated with
        {
            Id = "qa.authored-flow",
            Seed = 453,
            OwnerStarts =
            [
                generated.OwnerStarts[0] with { StartingCredits = 1800 },
                generated.OwnerStarts[1] with { StartingCredits = 2100 },
            ],
            TerrainCells =
            [
                .. generated.TerrainCells,
                new("qa.authored-road", new MapRect(1400, 1000, 320, 128), "soft-road", 0.85f),
            ],
            Triggers = [new("qa.trigger", new MapRect(1500, 1100, 96, 96), "qa.trigger.enter")],
            Objectives = [new("qa.objective", new MapPoint(1600, 1200), "qa.objective.primary")],
            NarrativeNodes = [new("qa.narrative", new MapPoint(1520, 1080), "qa.narrative.start", "qa.trigger")],
        };
    }
}
