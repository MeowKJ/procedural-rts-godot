using Godot;

namespace ProceduralRts.Core;

public sealed record MatchConfig(
    int StartingCredits,
    int MapSeed,
    EnemyDifficulty EnemyDifficulty,
    Vector2 WorldSize,
    FactionId PlayerFaction,
    FactionId AiFaction,
    LaunchMode LaunchMode = LaunchMode.Skirmish,
    MapSpec? AuthoredMap = null)
{
    public static readonly Vector2 DefaultWorldSize = new(3600, 2400);

    public static MatchConfig Default { get; } = new(
        SkirmishOptions.DefaultStartingCredits,
        SkirmishOptions.DefaultMapSeed,
        EnemyDifficulty.Normal,
        DefaultWorldSize,
        FactionId.Dog,
        FactionId.Cat);

    public static MatchConfig Sandbox { get; } = new(
        SkirmishOptions.SandboxStartingCredits,
        SkirmishOptions.SandboxMapSeed,
        EnemyDifficulty.Easy,
        DefaultWorldSize,
        FactionId.Dog,
        FactionId.Cat,
        LaunchMode.Sandbox);

    public static MatchConfig ForAuthoredMap(
        MapSpec map,
        EnemyDifficulty enemyDifficulty = EnemyDifficulty.Normal)
    {
        ArgumentNullException.ThrowIfNull(map);
        MapOwnerTopologyValidator.EnsureValid(map);
        var player = map.StartFor(new OwnerId(1));
        var enemy = map.StartFor(new OwnerId(2));
        return new MatchConfig(
            player.StartingCredits,
            map.Seed,
            enemyDifficulty,
            map.WorldSize.ToVector2(),
            player.Faction,
            enemy.Faction,
            LaunchMode.Skirmish,
            map);
    }

    public FactionId FactionForOwner(Owner owner)
    {
        return owner == Owner.Player ? PlayerFaction : AiFaction;
    }

    public SkirmishOptions ToSkirmishOptions()
    {
        return new SkirmishOptions(
            StartingCredits,
            MapSeed,
            EnemyDifficulty,
            LaunchMode,
            PlayerFaction,
            AiFaction);
    }
}
