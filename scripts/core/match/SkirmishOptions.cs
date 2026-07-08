using Godot;

namespace ProceduralRts.Core;

public sealed record SkirmishOptions(
    int StartingCredits,
    int MapSeed,
    EnemyDifficulty EnemyDifficulty,
    LaunchMode LaunchMode = LaunchMode.Skirmish,
    FactionId PlayerFaction = FactionId.Dog,
    FactionId AiFaction = FactionId.Cat)
{
    public const int DefaultStartingCredits = 2400;
    public const int SandboxStartingCredits = 12000;
    public const int DefaultMapSeed = 1729;
    public const int SandboxMapSeed = 424242;

    public static SkirmishOptions Default { get; } = new(
        DefaultStartingCredits,
        DefaultMapSeed,
        EnemyDifficulty.Normal);

    public static SkirmishOptions Sandbox { get; } = new(
        SandboxStartingCredits,
        SandboxMapSeed,
        EnemyDifficulty.Easy,
        LaunchMode.Sandbox);

    public MatchConfig ToMatchConfig(Vector2? worldSize = null)
    {
        return new MatchConfig(
            StartingCredits,
            MapSeed,
            EnemyDifficulty,
            worldSize ?? MatchConfig.DefaultWorldSize,
            PlayerFaction,
            AiFaction,
            LaunchMode);
    }
}

public static class SkirmishSetupState
{
    private static MatchConfig _pendingMatchConfig = SkirmishOptions.Default.ToMatchConfig();

    public static SkirmishOptions PendingOptions
    {
        get => _pendingMatchConfig.ToSkirmishOptions();
        set => _pendingMatchConfig = value.ToMatchConfig();
    }

    public static MatchConfig PendingMatchConfig
    {
        get => _pendingMatchConfig;
        set => _pendingMatchConfig = value;
    }
}
