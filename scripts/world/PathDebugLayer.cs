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
    private static readonly Color SandboxRuntimePath = new("#64f2ff", 0.88f);
    private static readonly Color SandboxRuntimeRing = new("#ffdd7a", 0.58f);
    private static readonly Color SandboxRuntimeAnchor = new("#ff9aad", 0.86f);

    public required GameState State { get; init; }
    public UnitBattlefield? UnitBattlefield { get; init; }
    public Action<string>? StatusChanged { get; init; }
    public bool Enabled { get; private set; }
    private SandboxDebugOverlayFlag _sandboxOverlayFlags = SandboxDebugOverlayFlag.None;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (State.Options.LaunchMode == LaunchMode.Sandbox)
        {
            return;
        }

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
        if (Enabled || HasSandboxRuntimeOverlays())
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var drawManualDebug = Enabled && State.Options.LaunchMode != LaunchMode.Sandbox;
        var drawSandboxRuntime = HasSandboxRuntimeOverlays();
        if (!drawManualDebug && !drawSandboxRuntime)
        {
            return;
        }

        if (drawManualDebug)
        {
            DrawObstacles();
            DrawTerrainCells();
            DrawUnitPaths();
        }

        if (drawSandboxRuntime)
        {
            DrawSandboxRuntimeOverlays();
        }
    }

    public void SetSandboxOverlayFlags(SandboxDebugOverlayFlag flags)
    {
        var next = flags & SandboxDebugOverlayFlag.All;
        if (_sandboxOverlayFlags == next)
        {
            return;
        }

        _sandboxOverlayFlags = next;
        QueueRedraw();
    }

    private bool HasSandboxRuntimeOverlays()
    {
        return RuntimeSandboxOverlaysVisible(State.Options.LaunchMode, _sandboxOverlayFlags)
            && UnitBattlefield is not null;
    }

    public static bool RuntimeSandboxOverlaysVisible(LaunchMode launchMode, SandboxDebugOverlayFlag flags)
    {
        return launchMode == LaunchMode.Sandbox
            && (flags & (SandboxDebugOverlayFlag.Paths
                | SandboxDebugOverlayFlag.Rings
                | SandboxDebugOverlayFlag.Anchors)) != SandboxDebugOverlayFlag.None;
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

    private void DrawSandboxRuntimeOverlays()
    {
        if (UnitBattlefield is null)
        {
            return;
        }

        foreach (var unit in UnitBattlefield.Units)
        {
            if (!unit.Selected || unit.Hp <= 0 || UnitBattlefield.UnitEntityByInstanceId(unit.Id) is not { } entity)
            {
                continue;
            }

            if (IsSandboxOverlayEnabled(SandboxDebugOverlayFlag.Paths))
            {
                DrawSandboxRuntimePaths(unit, entity);
            }

            if (IsSandboxOverlayEnabled(SandboxDebugOverlayFlag.Rings))
            {
                DrawSandboxRuntimeRings(unit, entity);
            }

            if (IsSandboxOverlayEnabled(SandboxDebugOverlayFlag.Anchors))
            {
                DrawSandboxRuntimeAnchors(entity);
            }
        }
    }

    private void DrawSandboxRuntimePaths(UnitInstance unit, EntityInstance entity)
    {
        var origin = unit.Position;
        var drewSegment = false;

        if (entity.Components.TryGet<PathfindingComponentState>(out var pathfinding)
            && pathfinding.Waypoints.Count > 0)
        {
            var from = origin;
            var firstIndex = Mathf.Clamp(pathfinding.NextWaypointIndex, 0, pathfinding.Waypoints.Count - 1);
            for (var index = firstIndex; index < pathfinding.Waypoints.Count; index++)
            {
                var point = ToVector2(pathfinding.Waypoints[index]);
                DrawSandboxPathSegment(from, point);
                from = point;
                drewSegment = true;
            }
        }

        if (!drewSegment
            && entity.Components.TryGet<MovementComponentState>(out var movement)
            && movement.MoveTarget is { } moveTarget)
        {
            DrawSandboxPathSegment(origin, moveTarget);
            drewSegment = true;
        }

        if (entity.Components.TryGet<CommandableComponentState>(out var commandable))
        {
            if (!drewSegment && commandable.CommandVisualTarget is { } visualTarget)
            {
                DrawSandboxPathSegment(origin, visualTarget);
            }

            if (commandable.PlayerIntentTarget is { } intentTarget)
            {
                DrawSandboxIntentMarker(intentTarget);
            }
        }
    }

    private void DrawSandboxRuntimeRings(UnitInstance unit, EntityInstance entity)
    {
        var radius = entity.Components.TryGet<CollisionComponentState>(out var collision)
            ? collision.Radius
            : unit.Spec.Collision.Radius;
        DrawSandboxRadiusRing(unit.Position, MathF.Max(radius, 6), new Color(SandboxRuntimeRing, 0.48f));

        if (entity.Components.TryGet<GuardOrderComponentState>(out var guard))
        {
            DrawSandboxRadiusRing(GuardAnchor(guard), guard.Radius, SandboxRuntimeRing);
        }
    }

    private void DrawSandboxRuntimeAnchors(EntityInstance entity)
    {
        if (entity.Components.TryGet<GuardOrderComponentState>(out var guard))
        {
            DrawSandboxAnchorMarker(GuardAnchor(guard), 18);
        }

        if (entity.Components.TryGet<AutonomyComponentState>(out var autonomy)
            && autonomy.AnchorPosition is { } autonomyAnchor)
        {
            DrawSandboxAnchorMarker(autonomyAnchor, 13);
        }

        if (entity.Components.TryGet<StanceComponentState>(out var stance)
            && stance.AnchorPosition is { } stanceAnchor)
        {
            DrawSandboxAnchorMarker(stanceAnchor, 10);
        }
    }

    private void DrawSandboxPathSegment(Vector2 from, Vector2 to)
    {
        DrawLine(from, to, new Color(SandboxRuntimePath, 0.22f), 7.5f, true);
        DrawLine(from, to, SandboxRuntimePath, 2.0f, true);
        DrawCircle(to, 7f, new Color(SandboxRuntimePath, 0.24f));
        DrawCircle(to, 2.8f, WaypointFill);
    }

    private void DrawSandboxIntentMarker(Vector2 target)
    {
        DrawArc(target, 20, 0, Mathf.Tau, 56, new Color(IntentColor, 0.72f), 2.1f, true);
        DrawLine(target + new Vector2(-14, 0), target + new Vector2(14, 0), IntentColor, 1.4f, true);
        DrawLine(target + new Vector2(0, -14), target + new Vector2(0, 14), IntentColor, 1.4f, true);
    }

    private void DrawSandboxRadiusRing(Vector2 center, float radius, Color color)
    {
        if (radius <= 0)
        {
            return;
        }

        DrawCircle(center, radius, new Color(color, 0.045f));
        DrawArc(center, radius, 0, Mathf.Tau, 64, color, 1.6f, true);
    }

    private void DrawSandboxAnchorMarker(Vector2 anchor, float radius)
    {
        DrawCircle(anchor, radius * 0.46f, new Color(SandboxRuntimeAnchor, 0.12f));
        DrawLine(anchor + new Vector2(0, -radius), anchor + new Vector2(radius, 0), SandboxRuntimeAnchor, 1.8f, true);
        DrawLine(anchor + new Vector2(radius, 0), anchor + new Vector2(0, radius), SandboxRuntimeAnchor, 1.8f, true);
        DrawLine(anchor + new Vector2(0, radius), anchor + new Vector2(-radius, 0), SandboxRuntimeAnchor, 1.8f, true);
        DrawLine(anchor + new Vector2(-radius, 0), anchor + new Vector2(0, -radius), SandboxRuntimeAnchor, 1.8f, true);
        DrawLine(anchor + new Vector2(-radius * 0.62f, 0), anchor + new Vector2(radius * 0.62f, 0), WaypointFill, 1.1f, true);
        DrawLine(anchor + new Vector2(0, -radius * 0.62f), anchor + new Vector2(0, radius * 0.62f), WaypointFill, 1.1f, true);
    }

    private bool IsSandboxOverlayEnabled(SandboxDebugOverlayFlag flag)
    {
        return (_sandboxOverlayFlags & flag) == flag;
    }

    private Vector2 GuardAnchor(GuardOrderComponentState guard)
    {
        return UnitBattlefield?.EntityWorld.TryGet(guard.TargetEntity, out var target) == true
            ? target.Transform.Position
            : guard.GuardPoint;
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

    private static Vector2 ToVector2(PathPoint point)
    {
        return new Vector2(point.X, point.Y);
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
