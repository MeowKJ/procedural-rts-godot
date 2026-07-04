static partial class Program
{
    private static void AssertMixedTurnModes(
        IReadOnlyList<UnitInstance> subjects,
        int commandTick,
        Vector2 target,
        UnitBattlefield battlefield)
    {
        var hasPivot = false;
        var hasArc = false;
        foreach (var subject in subjects)
        {
            hasPivot |= subject.Spec.Movement.TurnMode == TurnMode.PivotInPlace;
            hasArc |= subject.Spec.Movement.TurnMode == TurnMode.ArcTurn;
        }

        if (!hasPivot || !hasArc)
        {
            Fail($"movement-feel replacement requires mixed pivot/arc turn subjects: pivot={hasPivot}, arc={hasArc}, trace={MovementFeelTrace(battlefield, subjects, commandTick, 0, target, [])}");
        }
    }

    private static void UpdateTurnModeEvidence(
        IReadOnlyList<UnitInstance> subjects,
        ref bool sawPivotOffFacing,
        ref bool sawArcFacingLocked)
    {
        foreach (var subject in subjects)
        {
            if (subject.Velocity.LengthSquared() <= 4f)
            {
                continue;
            }

            var velocityFacingDelta = MathF.Abs(Mathf.AngleDifference(subject.Facing, subject.Velocity.Angle()));
            if (subject.Spec.Movement.TurnMode == TurnMode.PivotInPlace && velocityFacingDelta > 0.12f)
            {
                sawPivotOffFacing = true;
            }

            if (subject.Spec.Movement.TurnMode == TurnMode.ArcTurn && velocityFacingDelta <= 0.05f)
            {
                sawArcFacingLocked = true;
            }
        }
    }
}
