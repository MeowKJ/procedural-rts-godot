using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private void DrawCommandLines()
    {
        DrawUnitBattlefieldCommandLines();

        var selectedUnits = State.SelectedUnits()
            .Where(unit => unit.CommandVisualTarget is not null || unit.FormationSlot is not null)
            .ToList();
        if (selectedUnits.Count > 0)
        {
            var lineWidth = SelectionMath.ScreenPixelsToWorld(1.2f, Camera.Zoom.X);
            var markerRadius = SelectionMath.ScreenPixelsToWorld(8.5f, Camera.Zoom.X);
            var cross = SelectionMath.ScreenPixelsToWorld(7f, Camera.Zoom.X);
            var targetMarkers = new Dictionary<string, (Vector2 Position, Color Accent, float Pulse)>();

            foreach (var unit in selectedUnits)
            {
                var visualTarget = unit.CommandVisualTarget ?? unit.FormationSlot!.Value;
                var accent = unit.AttackTargetId is not null
                    ? new Color("#ffcf5a")
                    : UnitSpecFeedbackStyleFor(unit).Accent;
                var alpha = unit.MovementState == UnitMovementState.CombatAnchor ? 0.28f : 0.18f;
                if (unit.Position.DistanceTo(visualTarget) > markerRadius * 1.4f)
                {
                    DrawDashedWorldLine(
                        unit.Position,
                        visualTarget,
                        new Color(accent, alpha),
                        lineWidth,
                        SelectionMath.ScreenPixelsToWorld(10, Camera.Zoom.X),
                        SelectionMath.ScreenPixelsToWorld(6, Camera.Zoom.X));
                }

                var key = $"{Mathf.RoundToInt(visualTarget.X / 4f)},{Mathf.RoundToInt(visualTarget.Y / 4f)}";
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
                DrawLine(marker.Position + new Vector2(-cross, 0), marker.Position + new Vector2(cross, 0), new Color("#f7fdff", 0.62f), lineWidth, true);
                DrawLine(marker.Position + new Vector2(0, -cross), marker.Position + new Vector2(0, cross), new Color("#f7fdff", 0.62f), lineWidth, true);
                if (marker.Pulse > 0.02f)
                {
                    DrawArc(marker.Position, pulseRadius, 0, Mathf.Tau, 64, new Color("#d8f7ff", (0.18f + marker.Pulse * 0.42f) * pulseWave), lineWidth * 1.8f, true);
                }
            }
        }

        if (UseUnitBattlefieldInput())
        {
            DrawUnitBattlefieldBuildingRallyLines();
            return;
        }

        foreach (var building in State.SelectedBuildings())
        {
            if (building.RallyPoint is null)
            {
                continue;
            }

            var spec = BuildSpecCatalog.For(building.Kind);
            var accent = State.VisualAccent(building.Owner, building.FactionId, spec.Accent);
            var pulse = 0.45f + building.RallyPulse * 0.55f;
            DrawLine(building.Position, building.RallyPoint.Value, new Color(accent, 0.16f + pulse * 0.26f), 1.4f, true);
            DrawArc(building.RallyPoint.Value, 18 + building.RallyPulse * 18, 0, Mathf.Tau, 72, new Color("#d8f7ff", 0.32f + pulse * 0.42f), 2.2f, true);
            DrawLine(building.RallyPoint.Value + new Vector2(-13, 0), building.RallyPoint.Value + new Vector2(13, 0), new Color(accent, 0.72f), 2.2f, true);
            DrawLine(building.RallyPoint.Value + new Vector2(0, -13), building.RallyPoint.Value + new Vector2(0, 13), new Color(accent, 0.72f), 2.2f, true);
        }
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
        if (!UseUnitBattlefieldInput())
        {
            return;
        }

        var selectedUnits = UnitBattlefield!.SelectedUnits(LocalPlayerSlotId)
            .Where(unit => unit.CommandVisualTarget is not null || unit.FormationSlot is not null)
            .ToList();
        if (selectedUnits.Count == 0)
        {
            return;
        }

        var lineWidth = SelectionMath.ScreenPixelsToWorld(1.2f, Camera.Zoom.X);
        var markerRadius = SelectionMath.ScreenPixelsToWorld(8.5f, Camera.Zoom.X);
        var cross = SelectionMath.ScreenPixelsToWorld(7f, Camera.Zoom.X);
        var targetMarkers = new Dictionary<string, (Vector2 Position, Color Accent, float Pulse)>();

        foreach (var unit in selectedUnits)
        {
            var visualTarget = unit.CommandVisualTarget ?? unit.FormationSlot!.Value;
            var accent = UnitRelationAccent(UnitBattlefield.Relations.Relation(LocalPlayerSlotId, unit.PlayerSlotId));
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

            var key = $"{Mathf.RoundToInt(visualTarget.X / 4f)},{Mathf.RoundToInt(visualTarget.Y / 4f)}";
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
