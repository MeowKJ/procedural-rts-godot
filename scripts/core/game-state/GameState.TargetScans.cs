using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    private UnitModel? BestUnitTargetForWeapon(
        Owner viewerOwner,
        WeaponDefinition weapon,
        Vector2 sourcePosition,
        float range,
        bool requirePositiveHp)
    {
        UnitModel? best = null;
        var bestScore = float.NegativeInfinity;
        foreach (var candidate in Units)
        {
            if ((requirePositiveHp && candidate.Hp <= 0)
                || !IsTargetableHostile(viewerOwner, candidate)
                || !WeaponCanTarget(weapon, candidate.RuntimeDescriptor)
                || candidate.Position.DistanceTo(sourcePosition) > range)
            {
                continue;
            }

            var score = TargetScore(weapon, sourcePosition, CombatTargetKind.Unit, candidate.Id, range);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }
}
