using Godot;

namespace ProceduralRts;

public partial class StyleTestRoot
{
    private void DrawGrid(Rect2 rect, StyleSpec style)
    {
        for (var x = rect.Position.X + 26; x < rect.End.X; x += 32)
        {
            DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), style.Grid, 0.75f, true);
        }

        for (var y = rect.Position.Y + 20; y < rect.End.Y; y += 32)
        {
            DrawLine(new Vector2(rect.Position.X, y), new Vector2(rect.End.X, y), style.Grid, 0.75f, true);
        }

        for (var x = rect.Position.X + 90; x < rect.End.X; x += 128)
        {
            DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), style.Major, 1.1f, true);
        }

        for (var y = rect.Position.Y + 72; y < rect.End.Y; y += 128)
        {
            DrawLine(new Vector2(rect.Position.X, y), new Vector2(rect.End.X, y), style.Major, 1.1f, true);
        }
    }

    private void DrawOldCityRoutes(Rect2 rect, StyleSpec style)
    {
        var a = rect.Position + new Vector2(34, rect.Size.Y * 0.70f);
        var b = rect.Position + new Vector2(rect.Size.X * 0.45f, rect.Size.Y * 0.58f);
        var c = rect.Position + new Vector2(rect.Size.X - 58, rect.Size.Y * 0.43f);
        var color = new Color(style.Dog, style.Dark ? 0.34f : 0.24f);
        DrawLine(a, b, color, 8, true);
        DrawLine(b, c, color, 8, true);
        DrawLine(a, b, new Color(style.Background, 0.42f), 2.2f, true);
        DrawLine(b, c, new Color(style.Background, 0.42f), 2.2f, true);

        var side = rect.Position + new Vector2(rect.Size.X * 0.28f, rect.Size.Y * 0.34f);
        DrawDashedLine(side, c + new Vector2(-30, 42), new Color(style.Cat, style.Dark ? 0.45f : 0.34f), 2.4f, 10, 7);
    }

    private void DrawCorruption(Rect2 rect, StyleSpec style)
    {
        var alpha = style.CorruptionHeavy ? 0.45f : 0.23f;
        var origin = rect.Position + new Vector2(rect.Size.X * 0.68f, rect.Size.Y * 0.62f);
        var radius = Mathf.Min(rect.Size.X, rect.Size.Y) * (style.CorruptionHeavy ? 0.29f : 0.19f);
        DrawArc(origin, radius, 0, Mathf.Tau, 72, new Color(style.Ai, alpha), 2.4f, true);
        DrawArc(origin, radius * 0.62f, 0, Mathf.Tau, 60, new Color(style.Ai, alpha * 0.84f), 1.4f, true);

        for (var i = 0; i < 10; i++)
        {
            var angle = i * Mathf.Tau / 10f + _elapsed * 0.08f;
            var start = origin + Vector2.FromAngle(angle) * radius * 0.35f;
            var end = origin + Vector2.FromAngle(angle + 0.22f) * radius * (0.82f + (i % 3) * 0.08f);
            DrawLine(start, end, new Color(style.Ai, alpha * 0.72f), i % 2 == 0 ? 2.1f : 1.1f, true);
        }
    }

    private void DrawSafeLights(Rect2 rect, StyleSpec style)
    {
        for (var i = 0; i < 5; i++)
        {
            var pos = rect.Position + new Vector2(70 + i * 76, rect.Size.Y - 58 - Mathf.Sin(_elapsed + i) * 4);
            DrawCircle(pos, 12, new Color(style.Dog, style.Dark ? 0.18f : 0.11f));
            DrawCircle(pos, 4.5f, new Color(style.Dog, style.Dark ? 0.95f : 0.72f));
        }
    }

    private void DrawCommandPreview(Rect2 rect, StyleSpec style)
    {
        var from = rect.Position + new Vector2(rect.Size.X * 0.26f, rect.Size.Y * 0.62f);
        var mid = rect.Position + new Vector2(rect.Size.X * 0.45f, rect.Size.Y * 0.48f);
        var target = rect.Position + new Vector2(rect.Size.X * 0.58f, rect.Size.Y * 0.38f);
        var line = new Color(style.Dog, style.Dark ? 0.72f : 0.56f);
        DrawLine(from, mid, line, 2.6f, true);
        DrawLine(mid, target, line, 2.6f, true);
        DrawCircle(target, 16, new Color(style.Dog, 0.10f), false, 2.2f, true);
        DrawCircle(target, 4.6f, new Color(style.Dog, 0.86f));

        var select = new Rect2(rect.Position + new Vector2(rect.Size.X * 0.20f, rect.Size.Y * 0.50f), new Vector2(160, 86));
        DrawRect(select, new Color(style.Dog, style.Dark ? 0.13f : 0.08f));
        DrawRect(select, new Color(style.Dog, style.Dark ? 0.72f : 0.60f), false, 1.6f);
    }

    private void DrawDashedLine(Vector2 from, Vector2 to, Color color, float width, float dash, float gap)
    {
        var delta = to - from;
        var length = delta.Length();
        if (length <= 0.01f)
        {
            return;
        }

        var dir = delta / length;
        for (var distance = 0f; distance < length; distance += dash + gap)
        {
            var segmentEnd = Mathf.Min(distance + dash, length);
            DrawLine(from + dir * distance, from + dir * segmentEnd, color, width, true);
        }
    }
}
