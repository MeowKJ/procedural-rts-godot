using Godot;

namespace ProceduralRts.Core;

static class TurnModeMath
{
    private const float ArcTurnMinSpeedScale = 0.35f;
    private const float ArcTurnLargeAngleThreshold = 0.65f;

    public static float NextFacing(float current, float desired, float turnRate, float dt, TurnMode turnMode)
    {
        return turnMode == TurnMode.FixedFacing || turnRate <= 0
            ? current
            : WeaponEngagementMath.RotateToward(current, desired, turnRate * dt);
    }

    public static Vector2 MovementDirection(TurnMode turnMode, Vector2 desiredDirection, float facing)
    {
        return turnMode == TurnMode.ArcTurn && IsLargeArcTurn(facing, desiredDirection.Angle())
            ? Vector2.FromAngle(facing)
            : desiredDirection;
    }

    public static float SpeedScale(TurnMode turnMode, float facing, float desired)
    {
        if (turnMode != TurnMode.ArcTurn || !IsLargeArcTurn(facing, desired))
        {
            return 1;
        }

        var alignment = (MathF.Cos(Mathf.AngleDifference(facing, desired)) + 1f) * 0.5f;
        return Mathf.Clamp(alignment, ArcTurnMinSpeedScale, 1f);
    }

    private static bool IsLargeArcTurn(float facing, float desired)
    {
        return MathF.Abs(Mathf.AngleDifference(facing, desired)) > ArcTurnLargeAngleThreshold;
    }
}
