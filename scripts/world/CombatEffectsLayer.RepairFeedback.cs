using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class CombatEffectsLayer : Node2D
{
    private void DrawActiveRepairFeedback()
    {
        if (UnitBattlefield is null)
        {
            _activeRepairFeedbackEffectCount = 0;
            return;
        }

        UnitBattlefield.ActiveRepairFeedbackProjections(_activeRepairFeedbackProjections);
        _activeRepairFeedbackEffectCount = _activeRepairFeedbackProjections.Count;
        foreach (var repair in _activeRepairFeedbackProjections)
        {
            DrawActiveRepairFeedback(repair);
        }
    }

    private void DrawActiveRepairFeedback(ActiveRepairFeedbackProjection repair)
    {
        var padding = MathF.Max(20, repair.TargetRadius + 12);
        if (!IsSegmentVisible(repair.RepairerPosition, repair.TargetPosition, padding))
        {
            return;
        }

        var readability = ReadabilityForSegment(repair.RepairerPosition, repair.TargetPosition);
        if (!readability.Draw)
        {
            return;
        }

        var pulse = RepairFeedbackPulse(repair);
        var ringRadius = repair.TargetRadius + 8 + pulse * 5;
        var lineWidth = ReadableWidth(1.2f + pulse * 0.9f, readability);
        var accent = repair.Accent;
        var white = new Color("#ffffff");

        DrawLine(
            repair.RepairerPosition,
            repair.TargetPosition,
            Readable(accent, 0.18f + pulse * 0.14f, readability),
            ReadableWidth(5.2f, readability),
            true);
        DrawLine(
            repair.RepairerPosition,
            repair.TargetPosition,
            Readable(white, 0.30f + pulse * 0.20f, readability),
            lineWidth,
            true);
        DrawArc(
            repair.TargetPosition,
            ringRadius,
            0,
            Mathf.Tau,
            MediumEffectArcSegments,
            Readable(accent, 0.48f + pulse * 0.18f, readability),
            ReadableWidth(1.8f + pulse, readability),
            true);
        DrawCircle(
            repair.TargetPosition,
            MathF.Max(3.5f, repair.TargetRadius * 0.18f + pulse * 2),
            Readable(white, 0.20f + pulse * 0.12f, readability));
    }

    private static float RepairFeedbackPulse(ActiveRepairFeedbackProjection repair)
    {
        var seed = repair.RepairerId.Value * 31 + repair.TargetId.Value * 17;
        var phase = repair.ProgressCarry * 0.45f + repair.WorkRate * 0.09f + Noise01(seed, 3);
        return 0.5f + 0.5f * Mathf.Sin(phase * Mathf.Tau);
    }
}
