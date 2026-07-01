using Godot;

namespace ProceduralRts;

public partial class StyleCandidateDeckRoot
{
    private void DrawVariantTerrain(Rect2 rect, VariantSpec style)
    {
        for (var x = rect.Position.X + 18; x < rect.End.X; x += 30)
        {
            DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), style.Grid, 0.8f, true);
        }

        for (var y = rect.Position.Y + 18; y < rect.End.Y; y += 30)
        {
            DrawLine(new Vector2(rect.Position.X, y), new Vector2(rect.End.X, y), style.Grid, 0.8f, true);
        }

        for (var x = rect.Position.X + 78; x < rect.End.X; x += 120)
        {
            DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), style.Major, 1.1f, true);
        }

        var roadAlpha = style.Dark ? 0.28f : 0.18f;
        var roadA = rect.Position + new Vector2(26, rect.Size.Y * 0.73f);
        var roadB = rect.Position + new Vector2(rect.Size.X * 0.48f, rect.Size.Y * 0.60f);
        var roadC = rect.Position + new Vector2(rect.Size.X - 30, rect.Size.Y * 0.47f);
        DrawLine(roadA, roadB, new Color(style.Dog, roadAlpha), 12, true);
        DrawLine(roadB, roadC, new Color(style.Dog, roadAlpha), 12, true);

        if (style.FamilyMood is 2 or 4)
        {
            for (var i = 0; i < 8; i++)
            {
                var y = rect.Position.Y + 48 + i * 42;
                DrawLine(new Vector2(rect.Position.X + 18, y), new Vector2(rect.End.X - 22, y + 14), new Color(style.Ink, style.Dark ? 0.055f : 0.045f), 4f, true);
            }
        }

        if (style.FamilyMood is 3 or 5)
        {
            for (var i = 0; i < 5; i++)
            {
                var x = rect.Position.X + rect.Size.X * (0.18f + i * 0.16f);
                DrawArc(new Vector2(x, rect.Position.Y + rect.Size.Y * 0.36f), 42 + i * 9, 0, Mathf.Tau, 48, new Color(style.Cat, style.Dark ? 0.10f : 0.065f), 1f, true);
            }
        }

        if (style.Haze > 0)
        {
            DrawRect(rect, new Color("#fff6e8", style.Haze));
        }
    }

    private void DrawBattleSample(Rect2 rect, VariantSpec style)
    {
        var dog = rect.Position + new Vector2(rect.Size.X * 0.30f, rect.Size.Y * 0.62f);
        var cat = rect.Position + new Vector2(rect.Size.X * 0.50f, rect.Size.Y * 0.36f);
        var ai = rect.Position + new Vector2(rect.Size.X * 0.72f, rect.Size.Y * 0.61f);
        var target = rect.Position + new Vector2(rect.Size.X * 0.60f, rect.Size.Y * 0.45f);

        var selection = new Rect2(dog.X - 72, dog.Y - 46, 146, 88);
        DrawRect(selection, new Color(style.Dog, style.Dark ? 0.10f : 0.06f));
        DrawRect(selection, new Color(style.Dog, style.Dark ? 0.78f : 0.54f), false, 1.5f);
        DrawLine(dog + new Vector2(26, -6), target, new Color(style.Dog, style.CommandAlpha), 2.7f, true);
        DrawCircle(target, 16, new Color(style.Dog, 0.12f), false, 2.2f, true);
        DrawCircle(target, 4.5f, new Color(style.Dog, 0.85f));

        DrawDogUnit(dog + new Vector2(-34, -6), style);
        DrawDogTank(dog + new Vector2(30, 4), style);
        DrawCatUnit(cat + new Vector2(-18, -2), style);
        DrawCatUnit(cat + new Vector2(38, 15), style);
        DrawAiNode(ai, style);

        var aiAlpha = style.Dark ? 0.42f : 0.22f;
        DrawArc(ai, rect.Size.Y * 0.19f, 0, Mathf.Tau, 72, new Color(style.Ai, aiAlpha), 2.1f, true);
        DrawArc(ai, rect.Size.Y * 0.30f, 0, Mathf.Tau, 96, new Color(style.Ai, aiAlpha * 0.55f), 1.4f, true);
        DrawDashedLine(
            rect.Position + new Vector2(rect.Size.X * 0.22f, rect.Size.Y * 0.32f),
            rect.Position + new Vector2(rect.Size.X * 0.76f, rect.Size.Y * 0.42f),
            new Color(style.Cat, style.Dark ? 0.46f : 0.28f),
            2.1f,
            10,
            7);
    }

    private void DrawDogUnit(Vector2 c, VariantSpec style)
    {
        var points = Points(c, [new(0, -23), new(20, -7), new(16, 19), new(0, 28), new(-16, 19), new(-20, -7)]);
        DrawToken(points, style.Dog, style);
        DrawLine(c + new Vector2(-9, -4), c + new Vector2(9, -4), new Color("#fff2d8", 0.48f), 2.3f, true);
        DrawLine(c + new Vector2(0, -15), c + new Vector2(0, 17), new Color(style.Dog, 0.66f), 2.3f, true);
    }

    private void DrawDogTank(Vector2 c, VariantSpec style)
    {
        var points = Points(c, [new(-40, -17), new(24, -17), new(40, -5), new(40, 8), new(20, 18), new(-38, 18), new(-50, 6), new(-50, -7)]);
        DrawToken(points, style.Dog, style);
        DrawCircle(c, 12, new Color(style.Dog, style.Dark ? 0.22f : 0.15f));
        DrawCircle(c, 12, new Color(style.Dog, 0.82f), false, 2f, true);
        DrawLine(c + new Vector2(6, 0), c + new Vector2(54, 0), new Color(style.Dog, 0.88f), 5.4f, true);
        DrawLine(c + new Vector2(8, 0), c + new Vector2(54, 0), new Color("#fff2d9", 0.44f), 1.2f, true);
    }

    private void DrawCatUnit(Vector2 c, VariantSpec style)
    {
        var points = Points(c, [new(30, 0), new(-13, -23), new(-4, 0), new(-13, 23)]);
        DrawToken(points, style.Cat, style);
        DrawLine(c + new Vector2(-4, 0), c + new Vector2(26, 0), new Color(style.Cat, 0.78f), 2.1f, true);
        DrawArc(c, 25, -0.62f, 0.62f, 24, new Color(style.Cat, style.Dark ? 0.46f : 0.30f), 1.7f, true);
    }

    private void DrawAiNode(Vector2 c, VariantSpec style)
    {
        var points = Points(c, [new(20, -8), new(9, -20), new(-18, -12), new(-22, 8), new(-3, 20), new(22, 8)]);
        DrawToken(points, style.Ai, style);
        DrawLine(c + new Vector2(-14, -14), c + new Vector2(15, 15), new Color(style.Ai, 0.70f), 1.7f, true);
        DrawLine(c + new Vector2(-8, 15), c + new Vector2(12, -15), new Color(style.Ai, 0.50f), 1.1f, true);
    }

    private void DrawToken(Vector2[] points, Color accent, VariantSpec style)
    {
        var fill = style.Dark ? 0.44f : 0.24f;
        DrawColoredPolygon(Offset(points, new Vector2(3, 5)), new Color("#120d08", style.Dark ? 0.24f : 0.13f));
        DrawColoredPolygon(points, new Color(style.Background.Lerp(accent, fill), 0.96f));
        DrawPolyline(Close(points), new Color(style.Ink.Lerp(accent, 0.20f), 0.92f), 2.3f, true);
        DrawPolyline(Close(ScalePolygon(points, 0.72f)), new Color("#fff2dd", style.Dark ? 0.20f : 0.34f), 1.1f, true);
        DrawPolyline(Close(ScalePolygon(points, 0.88f)), new Color(accent, 0.32f), 1.2f, true);
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
            DrawLine(from + dir * distance, from + dir * Mathf.Min(distance + dash, length), color, width, true);
        }
    }

    private static Vector2[] Points(Vector2 center, Vector2[] local)
    {
        var points = new Vector2[local.Length];
        for (var i = 0; i < local.Length; i++)
        {
            points[i] = center + local[i];
        }

        return points;
    }

    private static Vector2[] Offset(Vector2[] points, Vector2 offset)
    {
        var result = new Vector2[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            result[i] = points[i] + offset;
        }

        return result;
    }

    private static Vector2[] Close(Vector2[] points)
    {
        var closed = new Vector2[points.Length + 1];
        Array.Copy(points, closed, points.Length);
        closed[^1] = points[0];
        return closed;
    }

    private static Vector2[] ScalePolygon(Vector2[] points, float scale)
    {
        var center = Vector2.Zero;
        foreach (var point in points)
        {
            center += point;
        }

        center /= points.Length;
        var scaled = new Vector2[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            scaled[i] = center + (points[i] - center) * scale;
        }

        return scaled;
    }
}
