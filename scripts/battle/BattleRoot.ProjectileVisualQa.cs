using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public partial class BattleRoot
{
    public Vector2 DebugConfigureProjectileVisualQaScenario()
    {
        var focus = new Vector2(_worldSize.X * 0.58f, _worldSize.Y * 0.38f);
        string[] shooterDesignIds = ["dog.infantry", "dog.guard_tank", "dog.rocket"];
        float[] laneHalfGaps = [90f, 70f, 50f];
        for (var index = 0; index < shooterDesignIds.Length; index++)
        {
            var laneOffset = new Vector2(0, (index - 1) * 190f);
            var halfGap = laneHalfGaps[index];
            var shooter = _unitBattlefield.Spawn(
                shooterDesignIds[index],
                PlayerSlotId.One,
                focus + laneOffset + new Vector2(-halfGap, 0),
                facing: 0);
            var target = _unitBattlefield.Spawn(
                "cat.tank",
                PlayerSlotId.Two,
                focus + laneOffset + new Vector2(halfGap, 0),
                facing: Mathf.Pi);
            target.WeaponMounts.Clear();
            target.MoveMode = MoveCommandMode.Ignore;
            SetUnitInstanceFacing(shooter, 0);
            SetUnitInstanceFacing(target, Mathf.Pi);
            AddUnitInstanceView(shooter);
            AddUnitInstanceView(target);
            _unitBattlefield.CommandAttackUnits(PlayerSlotId.One, [shooter.Id], target);
        }

        _camera.InputEnabled = false;
        _camera.SnapToWorldPoint(focus);
        RefreshViewCulling();
        RefreshMinimap();
        return focus;
    }
}
