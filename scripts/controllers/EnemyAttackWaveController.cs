using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class EnemyAttackWaveController : Node
{
    public required GameState State { get; init; }
    public EnemyDifficultyProfile DifficultyProfile { get; init; } = EnemyDifficultyProfile.Normal;

    private EnemyAttackWaveAi? _ai;

    public override void _Process(double delta)
    {
        _ai ??= new EnemyAttackWaveAi(DifficultyProfile);
        _ai.Update(State, delta);
    }
}
