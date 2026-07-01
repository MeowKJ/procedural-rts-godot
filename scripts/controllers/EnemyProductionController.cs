using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class EnemyProductionController : Node
{
    public required GameState State { get; init; }
    public EnemyDifficultyProfile DifficultyProfile { get; init; } = EnemyDifficultyProfile.Normal;

    private EnemyProductionAi? _ai;

    public override void _Process(double delta)
    {
        _ai ??= new EnemyProductionAi(DifficultyProfile);
        _ai.Update(State, delta);
    }
}
