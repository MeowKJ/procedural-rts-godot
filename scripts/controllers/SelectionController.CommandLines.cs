using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private void DrawCommandLines()
    {
        DrawUnitBattlefieldCommandLines();
        DrawUnitBattlefieldBuildingRallyLines();
    }

    private void DrawUnitBattlefieldBuildingRallyLines()
    {
        var rallyLines = UnitBattlefield!.SelectedBuildingRallyProjections(LocalPlayerSlotId);
        if (rallyLines.Count == 0)
        {
            return;
        }

        var accent = UnitRelationAccent(PlayerRelation.Self);
        foreach (var rally in rallyLines)
        {
            var pulse = 0.45f + rally.RallyPulse * 0.55f;
            DrawLine(rally.Position, rally.RallyPoint, new Color(accent, 0.16f + pulse * 0.26f), 1.4f, true);
            DrawArc(rally.RallyPoint, 18 + rally.RallyPulse * 18, 0, Mathf.Tau, 72, new Color("#d8f7ff", 0.32f + pulse * 0.42f), 2.2f, true);
            DrawLine(rally.RallyPoint + new Vector2(-13, 0), rally.RallyPoint + new Vector2(13, 0), new Color(accent, 0.72f), 2.2f, true);
            DrawLine(rally.RallyPoint + new Vector2(0, -13), rally.RallyPoint + new Vector2(0, 13), new Color(accent, 0.72f), 2.2f, true);
        }
    }

    private void DrawUnitBattlefieldCommandLines()
    {
        CollectRuntimeCommandLineUnits(_runtimeCommandLineUnitBuffer);
        var selectedUnits = _runtimeCommandLineUnitBuffer;
        if (selectedUnits.Count == 0)
        {
            return;
        }

        var lineWidth = SelectionMath.ScreenPixelsToWorld(1.2f, Camera.Zoom.X);
        var markerRadius = SelectionMath.ScreenPixelsToWorld(8.5f, Camera.Zoom.X);
        var cross = SelectionMath.ScreenPixelsToWorld(7f, Camera.Zoom.X);
        var targetMarkers = _commandLineTargetMarkers;
        var battlefield = UnitBattlefield!;
        targetMarkers.Clear();

        foreach (var unit in selectedUnits)
        {
            var visualTarget = unit.CommandVisualTarget ?? unit.FormationSlot!.Value;
            var accent = UnitRelationAccent(battlefield.Relations.Relation(LocalPlayerSlotId, unit.PlayerSlotId));
            if (unit.Position.DistanceTo(visualTarget) > markerRadius * 1.4f)
            {
                DrawDashedWorldLine(
                    unit.Position,
                    visualTarget,
                    new Color(accent, 0.20f),
                    lineWidth,
                    SelectionMath.ScreenPixelsToWorld(10, Camera.Zoom.X),
                    SelectionMath.ScreenPixelsToWorld(6, Camera.Zoom.X));
            }

            var key = CommandLineTargetKey(visualTarget);
            if (!targetMarkers.TryGetValue(key, out var marker) || marker.Pulse < unit.CommandPulse)
            {
                targetMarkers[key] = (visualTarget, accent, unit.CommandPulse);
            }
        }

        foreach (var marker in targetMarkers.Values)
        {
            var pulseWave = 0.55f + Mathf.Sin(Time.GetTicksMsec() / 1000f * 9f) * 0.15f;
            var pulseRadius = markerRadius + SelectionMath.ScreenPixelsToWorld(marker.Pulse * 18, Camera.Zoom.X);
            DrawArc(marker.Position, markerRadius, 0, Mathf.Tau, 32, new Color(marker.Accent, 0.72f), lineWidth * 1.35f, true);
            DrawLine(marker.Position + new Vector2(-cross, 0), marker.Position + new Vector2(cross, 0), new Color("#3f4a4c", 0.62f), lineWidth, true);
            DrawLine(marker.Position + new Vector2(0, -cross), marker.Position + new Vector2(0, cross), new Color("#3f4a4c", 0.62f), lineWidth, true);
            if (marker.Pulse > 0.02f)
            {
                DrawArc(marker.Position, pulseRadius, 0, Mathf.Tau, 64, new Color(marker.Accent, (0.18f + marker.Pulse * 0.42f) * pulseWave), lineWidth * 1.8f, true);
            }
        }
    }

    private void DrawDashedWorldLine(Vector2 start, Vector2 end, Color color, float width, float dashLength, float gapLength)
    {
        var delta = end - start;
        var length = delta.Length();
        if (length <= 0.01f)
        {
            return;
        }

        var direction = delta / length;
        var cursor = 0f;
        while (cursor < length)
        {
            var segmentEnd = Mathf.Min(cursor + dashLength, length);
            DrawLine(start + direction * cursor, start + direction * segmentEnd, color, width, true);
            cursor = segmentEnd + gapLength;
        }
    }
}
