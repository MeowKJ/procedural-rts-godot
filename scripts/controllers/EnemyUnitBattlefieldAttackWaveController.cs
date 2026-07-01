using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class EnemyUnitBattlefieldAttackWaveController : Node
{
    public required UnitBattlefield Battlefield { get; init; }
    public required PlayerSlotId EnemyPlayerSlotId { get; init; }
    public EnemyDifficultyProfile DifficultyProfile { get; init; } = EnemyDifficultyProfile.Normal;

    private UnitBattlefieldEnemyAttackWaveAi? _ai;

    public override void _Process(double delta)
    {
        _ai ??= new UnitBattlefieldEnemyAttackWaveAi(DifficultyProfile);
        _ai.Update(Battlefield, EnemyPlayerSlotId, delta);
    }
}
