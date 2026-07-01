namespace ProceduralRts.Core;

public sealed record EnemyDifficultyProfile(
    EnemyDifficulty Difficulty,
    float ProductionInitialDelay,
    float ProductionDecisionInterval,
    int DesiredHarvesters,
    int MaxQueuedItems,
    float AttackInitialDelay,
    float AttackWaveInterval,
    int MinimumWaveUnits,
    int MaximumWaveUnits,
    float AggressionRadius)
{
    public static EnemyDifficultyProfile Easy { get; } = new(
        EnemyDifficulty.Easy,
        ProductionInitialDelay: 4.4f,
        ProductionDecisionInterval: 5.2f,
        DesiredHarvesters: 1,
        MaxQueuedItems: 2,
        AttackInitialDelay: 18f,
        AttackWaveInterval: 34f,
        MinimumWaveUnits: 3,
        MaximumWaveUnits: 5,
        AggressionRadius: 1450);

    public static EnemyDifficultyProfile Normal { get; } = new(
        EnemyDifficulty.Normal,
        ProductionInitialDelay: 1.4f,
        ProductionDecisionInterval: 3.2f,
        DesiredHarvesters: 2,
        MaxQueuedItems: 3,
        AttackInitialDelay: 8f,
        AttackWaveInterval: 24f,
        MinimumWaveUnits: 3,
        MaximumWaveUnits: 8,
        AggressionRadius: float.PositiveInfinity);

    public static EnemyDifficultyProfile Hard { get; } = new(
        EnemyDifficulty.Hard,
        ProductionInitialDelay: 0.7f,
        ProductionDecisionInterval: 2.1f,
        DesiredHarvesters: 3,
        MaxQueuedItems: 5,
        AttackInitialDelay: 6.5f,
        AttackWaveInterval: 15f,
        MinimumWaveUnits: 5,
        MaximumWaveUnits: 12,
        AggressionRadius: 4200);

    public static EnemyDifficultyProfile For(EnemyDifficulty difficulty)
    {
        return difficulty switch
        {
            EnemyDifficulty.Easy => Easy,
            EnemyDifficulty.Hard => Hard,
            _ => Normal,
        };
    }
}
