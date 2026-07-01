using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class CommandAcknowledgementLayer : Node2D
{
    private const float Duration = 0.72f;
    private const int SoftMaxRings = 36;
    private const int MaxRings = 48;
    private const int RingPoolLimit = 72;
    private const float UnderLoadFadeSeconds = 0.22f;
    private const int RingArcSegments = 40;
    private const int GlyphArcSegments = 28;

    private readonly List<Ring> _rings = [];
    private readonly List<Ring> _pooledRings = [];
    public Rect2? CullingWorldRect { get; set; }

    public int ActiveRingCount => _rings.Count;

    public void Add(CommandAcknowledgementKind kind, Vector2 position)
    {
        var ring = RentRing();
        ring.Reset(kind, position);
        _rings.Insert(0, ring);
        ApplyRingBudget();
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        for (var index = _rings.Count - 1; index >= 0; index--)
        {
            var ring = _rings[index];
            ring.Age += dt;
            if (ring.Age >= Duration)
            {
                ReturnAndRemoveRing(index);
                continue;
            }
        }

        if (_rings.Count > 0)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        foreach (var ring in _rings)
        {
            if (!IsVisible(ring.Position, BaseRadiusFor(ring.Kind) + 42))
            {
                continue;
            }

            DrawRing(ring);
        }
    }

    private bool IsVisible(Vector2 position, float radius)
    {
        return CullingWorldRect is not { } rect
            || rect.Intersects(new Rect2(position - Vector2.One * radius, Vector2.One * radius * 2f));
    }

    private void DrawRing(Ring ring)
    {
        var progress = Mathf.Clamp(ring.Age / Duration, 0, 1);
        var alpha = 1 - progress;
        var color = ColorFor(ring.Kind);
        var radius = BaseRadiusFor(ring.Kind) + progress * 34;
        var lineWidth = Mathf.Lerp(3.4f, 1.2f, progress);
        var center = ring.Position;

        DrawCircle(center, radius * 0.55f, new Color(color, alpha * 0.06f));
        DrawArc(center, radius, 0, Mathf.Tau, RingArcSegments, new Color(color, alpha * 0.86f), lineWidth, true);
        DrawArc(center, radius + 8, 0, Mathf.Tau, RingArcSegments, new Color("#ffffff", alpha * 0.28f), 1.1f, true);

        switch (ring.Kind)
        {
            case CommandAcknowledgementKind.Move:
                DrawMoveGlyph(center, color, alpha, radius);
                break;
            case CommandAcknowledgementKind.Attack:
                DrawAttackGlyph(center, color, alpha, radius);
                break;
            case CommandAcknowledgementKind.Repair:
                DrawRepairGlyph(center, color, alpha, radius);
                break;
            case CommandAcknowledgementKind.Harvest:
                DrawHarvestGlyph(center, color, alpha, radius);
                break;
            case CommandAcknowledgementKind.Rally:
                DrawRallyGlyph(center, color, alpha, radius);
                break;
            case CommandAcknowledgementKind.Invalid:
                DrawInvalidGlyph(center, color, alpha, radius);
                break;
        }
    }

    private void DrawMoveGlyph(Vector2 center, Color color, float alpha, float radius)
    {
        var arm = Mathf.Max(11, radius * 0.34f);
        DrawLine(center + new Vector2(-arm, 0), center + new Vector2(arm, 0), new Color(color, alpha * 0.82f), 2.2f, true);
        DrawLine(center + new Vector2(0, -arm), center + new Vector2(0, arm), new Color(color, alpha * 0.82f), 2.2f, true);
        DrawCircle(center, 4.2f, new Color("#ffffff", alpha * 0.72f));
    }

    private void DrawAttackGlyph(Vector2 center, Color color, float alpha, float radius)
    {
        var arm = Mathf.Max(13, radius * 0.38f);
        DrawLine(center + new Vector2(-arm, 0), center + new Vector2(-5, 0), new Color(color, alpha * 0.92f), 2.6f, true);
        DrawLine(center + new Vector2(5, 0), center + new Vector2(arm, 0), new Color(color, alpha * 0.92f), 2.6f, true);
        DrawLine(center + new Vector2(0, -arm), center + new Vector2(0, -5), new Color(color, alpha * 0.92f), 2.6f, true);
        DrawLine(center + new Vector2(0, 5), center + new Vector2(0, arm), new Color(color, alpha * 0.92f), 2.6f, true);
        DrawRect(new Rect2(center - new Vector2(5, 5), new Vector2(10, 10)), new Color("#ffffff", alpha * 0.5f), false, 1.4f);
    }

    private void DrawHarvestGlyph(Vector2 center, Color color, float alpha, float radius)
    {
        var hexRadius = Mathf.Max(8, radius * 0.24f);
        var points = new Vector2[6];
        for (var index = 0; index < points.Length; index++)
        {
            points[index] = center + Vector2.FromAngle(Mathf.Pi / 6f + index * Mathf.Tau / points.Length) * hexRadius;
        }

        DrawPolygon(points, [new Color(color, alpha * 0.18f)]);
        for (var index = 0; index < points.Length; index++)
        {
            DrawLine(points[index], points[(index + 1) % points.Length], new Color(color, alpha * 0.86f), 2, true);
        }

        DrawLine(center + new Vector2(-hexRadius * 0.55f, 0), center + new Vector2(hexRadius * 0.55f, 0), new Color("#ffffff", alpha * 0.62f), 1.6f, true);
    }

    private void DrawRepairGlyph(Vector2 center, Color color, float alpha, float radius)
    {
        var arm = Mathf.Max(9, radius * 0.26f);
        DrawArc(center, radius * 0.52f, 0, Mathf.Tau, GlyphArcSegments, new Color(color, alpha * 0.72f), 1.8f, true);
        DrawLine(center + new Vector2(-arm, 0), center + new Vector2(arm, 0), new Color(color, alpha * 0.92f), 2.8f, true);
        DrawLine(center + new Vector2(0, -arm), center + new Vector2(0, arm), new Color(color, alpha * 0.92f), 2.8f, true);
        DrawCircle(center, 3.8f, new Color("#ffffff", alpha * 0.54f));
    }

    private void DrawRallyGlyph(Vector2 center, Color color, float alpha, float radius)
    {
        var pole = Mathf.Max(14, radius * 0.4f);
        DrawLine(center + new Vector2(-8, pole * 0.55f), center + new Vector2(-8, -pole * 0.55f), new Color(color, alpha * 0.86f), 2.4f, true);
        var flag =
            new[]
            {
                center + new Vector2(-7, -pole * 0.52f),
                center + new Vector2(pole * 0.48f, -pole * 0.34f),
                center + new Vector2(-7, -pole * 0.12f),
            };
        DrawPolygon(flag, [new Color(color, alpha * 0.2f)]);
        DrawPolyline(flag, new Color("#ffffff", alpha * 0.58f), 1.6f, true);
    }

    private void DrawInvalidGlyph(Vector2 center, Color color, float alpha, float radius)
    {
        var arm = Mathf.Max(12, radius * 0.34f);
        DrawLine(center + new Vector2(-arm, -arm), center + new Vector2(arm, arm), new Color(color, alpha * 0.9f), 3.2f, true);
        DrawLine(center + new Vector2(-arm, arm), center + new Vector2(arm, -arm), new Color(color, alpha * 0.9f), 3.2f, true);
        DrawArc(center, radius * 0.58f, 0, Mathf.Tau, GlyphArcSegments, new Color("#ffffff", alpha * 0.24f), 1.2f, true);
    }

    private static Color ColorFor(CommandAcknowledgementKind kind)
    {
        return kind switch
        {
            CommandAcknowledgementKind.Attack => new Color("#ff5d75"),
            CommandAcknowledgementKind.Repair => new Color("#66c49a"),
            CommandAcknowledgementKind.Harvest => new Color("#f6c55c"),
            CommandAcknowledgementKind.Rally => new Color("#8fffe1"),
            CommandAcknowledgementKind.Invalid => new Color("#ff5d75"),
            _ => new Color("#59f1ff"),
        };
    }

    private static float BaseRadiusFor(CommandAcknowledgementKind kind)
    {
        return kind switch
        {
            CommandAcknowledgementKind.Attack => 24,
            CommandAcknowledgementKind.Repair => 24,
            CommandAcknowledgementKind.Harvest => 26,
            CommandAcknowledgementKind.Rally => 24,
            CommandAcknowledgementKind.Invalid => 22,
            _ => 20,
        };
    }

    private Ring RentRing()
    {
        if (_pooledRings.Count == 0)
        {
            return new Ring();
        }

        var last = _pooledRings.Count - 1;
        var ring = _pooledRings[last];
        _pooledRings.RemoveAt(last);
        return ring;
    }

    private void ApplyRingBudget()
    {
        if (_rings.Count > SoftMaxRings)
        {
            for (var index = SoftMaxRings; index < _rings.Count; index++)
            {
                _rings[index].FadeOutSoon(UnderLoadFadeSeconds);
            }
        }

        while (_rings.Count > MaxRings)
        {
            ReturnAndRemoveRing(_rings.Count - 1);
        }
    }

    private void ReturnAndRemoveRing(int index)
    {
        var ring = _rings[index];
        _rings.RemoveAt(index);
        if (_pooledRings.Count < RingPoolLimit)
        {
            _pooledRings.Add(ring);
        }
    }

    private sealed class Ring
    {
        public CommandAcknowledgementKind Kind { get; private set; }
        public Vector2 Position { get; private set; }
        public float Age { get; set; }

        public void Reset(CommandAcknowledgementKind kind, Vector2 position)
        {
            Kind = kind;
            Position = position;
            Age = 0;
        }

        public void FadeOutSoon(float remainingSeconds)
        {
            Age = Mathf.Max(Age, Duration - remainingSeconds);
        }
    }
}
