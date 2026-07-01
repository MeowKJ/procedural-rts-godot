internal sealed record CounterReadabilityCase(string Name, Func<BattleOutcome> Run);

internal sealed record UnitGroup(string SpecId, int Count);

internal enum DuelWinner
{
    Draw,
    Left,
    Right,
}

internal sealed record BattleOutcome(
    string Name,
    DuelWinner Winner,
    int Ticks,
    int LeftAlive,
    int RightAlive,
    float LeftHp,
    float RightHp);
