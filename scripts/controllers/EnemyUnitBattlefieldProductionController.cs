using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class EnemyUnitBattlefieldProductionController : Node
{
    public required UnitBattlefield Battlefield { get; init; }
    public required PlayerSlotId EnemyPlayerSlotId { get; init; }
    public EnemyDifficultyProfile DifficultyProfile { get; init; } = EnemyDifficultyProfile.Normal;

    private UnitBattlefieldEnemyProductionAi? _ai;

    public override void _Process(double delta)
    {
        _ai ??= new UnitBattlefieldEnemyProductionAi(DifficultyProfile);
        _ai.Update(Battlefield, EnemyPlayerSlotId, delta);
    }
}
