using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class PathDebugLayer : Node2D
{
    private static readonly Color HarvesterPath = new("#f6c55c", 0.82f);
    private static readonly Color ObstacleFill = new("#ff5d75", 0.16f);
    private static readonly Color ObstacleStroke = new("#ff9aad", 0.42f);
    private static readonly Color RawCellFill = new("#f6c55c", 0.16f);
    private static readonly Color RawCellStroke = new("#f6c55c", 0.42f);
    private static readonly Color SlotColor = new("#ffffff", 0.88f);
    private static readonly Color IntentColor = new("#8fffe1", 0.92f);
    private static readonly Color AvoidanceColor = new("#ff9aad", 0.76f);
    private static readonly Color SteeringColor = new("#d8f7ff", 0.78f);
    private static readonly Color WaypointFill = new("#ffffff", 0.92f);

    public required GameState State { get; init; }
    public Action<string>? StatusChanged { get; init; }
    public bool Enabled { get; private set; }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo || key.Keycode != Key.F3)
        {
            return;
        }

        Enabled = !Enabled;
        StatusChanged?.Invoke(Enabled ? GameText.T("debug.path.on") : GameText.T("debug.path.off"));
        QueueRedraw();
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (Enabled)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (!Enabled)
        {
            return;
        }

        DrawObstacles();
        DrawTerrainCells();
        DrawUnitPaths();
    }

    private void DrawObstacles()
    {
        var cellSize = GameState.PathCellSize;
        foreach (var obstacle in State.DebugPathObstacles())
        {
            var rect = new Rect2(
                obstacle.X * cellSize + 3,
                obstacle.Y * cellSize + 3,
                cellSize - 6,
                cellSize - 6);
            DrawRect(rect, ObstacleFill);
            DrawRect(rect, ObstacleStroke, false, 2);
        }
    }

    private void DrawTerrainCells()
    {
        var cellSize = GameState.PathCellSize;
        foreach (var terrain in State.DebugTerrainCells())
        {
            var color = terrain.Layer.HasFlag(TerrainLayer.Water)
                ? new Color("#64f2ff", 0.12f)
                : new Color("#8fffe1", 0.08f);
            DrawRect(
                new Rect2(terrain.X * cellSize + 7, terrain.Y * cellSize + 7, cellSize - 14, cellSize - 14),
                color,
                true);
        }
    }

    private void DrawUnitPaths()
    {
        foreach (var unit in State.Units)
        {
            var color = PathColor(unit);
            DrawRawPathCells(unit);
            DrawCorridor(unit, color);
            DrawFormationSlot(unit);
            DrawPlayerIntent(unit);
            DrawDebugVectors(unit);
        }
    }

    private void DrawRawPathCells(UnitModel unit)
    {
        var cellSize = GameState.PathCellSize;
        foreach (var cell in unit.DebugRawPathCells)
        {
            var rect = new Rect2(
                cell.X * cellSize + cellSize * 0.34f,
                cell.Y * cellSize + cellSize * 0.34f,
                cellSize * 0.32f,
                cellSize * 0.32f);
            DrawRect(rect, RawCellFill, true);
            DrawRect(rect, RawCellStroke, false, 1.1f);
        }
    }

    private void DrawCorridor(UnitModel unit, Color color)
    {
        var points = UnitPathPoints(unit);
        if (points.Count == 0)
        {
            return;
        }

        var from = unit.Position;
        foreach (var point in points)
        {
            DrawLine(from, point, new Color(color, 0.18f), 8.5f, true);
            DrawLine(from, point, color, 2.2f, true);
            DrawCircle(point, 6.5f, new Color(color, 0.24f));
            DrawCircle(point, 2.7f, WaypointFill);
            from = point;
        }

        DrawCircle(points[^1], 11.5f, new Color(color, 0.20f));
        DrawArc(points[^1], 14.5f, 0, Mathf.Tau, 48, new Color(color, 0.70f), 2, true);
    }

    private void DrawFormationSlot(UnitModel unit)
    {
        if (unit.FormationSlot is not { } slot)
        {
            return;
        }

        var radius = unit.MovementState == UnitMovementState.CombatAnchor ? 13f : 9f;
        DrawLine(slot + new Vector2(0, -radius), slot + new Vector2(radius, 0), SlotColor, 1.6f, true);
        DrawLine(slot + new Vector2(radius, 0), slot + new Vector2(0, radius), SlotColor, 1.6f, true);
        DrawLine(slot + new Vector2(0, radius), slot + new Vector2(-radius, 0), SlotColor, 1.6f, true);
        DrawLine(slot + new Vector2(-radius, 0), slot + new Vector2(0, -radius), SlotColor, 1.6f, true);
    }

    private void DrawPlayerIntent(UnitModel unit)
    {
        if (unit.PlayerIntentTarget is not { } intent)
        {
            return;
        }

        DrawArc(intent, 19, 0, Mathf.Tau, 56, new Color(IntentColor, 0.62f), 2.2f, true);
        DrawLine(intent + new Vector2(-13, 0), intent + new Vector2(13, 0), IntentColor, 1.5f, true);
        DrawLine(intent + new Vector2(0, -13), intent + new Vector2(0, 13), IntentColor, 1.5f, true);
    }

    private void DrawDebugVectors(UnitModel unit)
    {
        if (unit.DebugLocalAvoidanceVector.LengthSquared() > 0.0001f)
        {
            DrawVector(unit.Position, unit.DebugLocalAvoidanceVector, AvoidanceColor, 42, 2.1f);
        }

        if (unit.DebugSteeringVector.LengthSquared() > 0.0001f)
        {
            DrawVector(unit.Position + new Vector2(0, -10), unit.DebugSteeringVector, SteeringColor, 34, 1.6f);
        }
    }

    private void DrawVector(Vector2 origin, Vector2 vector, Color color, float scale, float width)
    {
        var direction = vector.Normalized();
        var end = origin + direction * Mathf.Clamp(vector.Length() * scale, 12, scale);
        DrawLine(origin, end, color, width, true);
        var left = direction.Rotated(Mathf.Pi * 0.78f);
        var right = direction.Rotated(-Mathf.Pi * 0.78f);
        DrawLine(end, end + left * 8, color, width, true);
        DrawLine(end, end + right * 8, color, width, true);
    }

    private static List<Vector2> UnitPathPoints(UnitModel unit)
    {
        var points = new List<Vector2>();
        if (unit.MoveTarget is { } target)
        {
            points.Add(target);
        }

        points.AddRange(unit.Path);
        return points;
    }

    private Color PathColor(UnitModel unit)
    {
        var spec = unit.Spec;
        if (spec.RoleTags.Contains(UnitRoleTag.Economy) || spec.RoleTags.Contains(UnitRoleTag.Worker))
        {
            return HarvesterPath;
        }

        var presentation = UnitPresentationCatalog.ForSpec(spec);
        var color = State.VisualAccent(unit.Owner, unit.FactionId, presentation.Accent);
        color.A = 0.78f;
        return color;
    }
}
