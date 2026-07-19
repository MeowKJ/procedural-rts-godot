using Godot;

namespace ProceduralRts;

public partial class StyleTestRoot
{
    private void DrawUnits(Rect2 rect, StyleSpec style)
    {
        var dogBase = rect.Position + new Vector2(rect.Size.X * 0.29f, rect.Size.Y * 0.61f);
        var catBase = rect.Position + new Vector2(rect.Size.X * 0.43f, rect.Size.Y * 0.35f);
        var aiBase = rect.Position + new Vector2(rect.Size.X * 0.70f, rect.Size.Y * 0.58f);

        DrawDogUnit(dogBase + new Vector2(-30, -12), 0.05f, style);
        DrawDogUnit(dogBase + new Vector2(24, 2), -0.08f, style);
        DrawVehicle(dogBase + new Vector2(84, -22), 0.15f, style.Dog, style, "repair");

        DrawCatUnit(catBase + new Vector2(-18, 0), -0.22f, style);
        DrawCatUnit(catBase + new Vector2(34, 20), 0.18f, style);

        DrawAiUnit(aiBase + new Vector2(-22, -8), 0.1f, style);
        DrawAiUnit(aiBase + new Vector2(38, 22), -0.18f, style);
    }

    private void DrawDogUnit(Vector2 center, float facing, StyleSpec style)
    {
        var points = TransformPoints(
            center,
            facing,
            1,
            new[]
            {
                new Vector2(22, 0),
                new Vector2(10, -16),
                new Vector2(-18, -14),
                new Vector2(-26, 0),
                new Vector2(-18, 14),
                new Vector2(10, 16),
            });
        DrawTokenPolygon(points, style.Dog, style, filledScale: 0.23f);
        DrawLine(Rotate(center, new Vector2(-8, -7), facing), Rotate(center, new Vector2(12, -7), facing), new Color(style.Dog, 0.72f), 2.1f, true);
        DrawLine(Rotate(center, new Vector2(-8, 7), facing), Rotate(center, new Vector2(12, 7), facing), new Color(style.Dog, 0.72f), 2.1f, true);
    }

    private void DrawCatUnit(Vector2 center, float facing, StyleSpec style)
    {
        var points = TransformPoints(
            center,
            facing,
            1,
            new[]
            {
                new Vector2(24, 0),
                new Vector2(-11, -18),
                new Vector2(-4, 0),
                new Vector2(-11, 18),
            });
        DrawTokenPolygon(points, style.Cat, style, filledScale: 0.18f);
        DrawArc(center, 17, facing - 0.76f, facing + 0.76f, 24, new Color(style.Cat, 0.74f), 1.6f, true);
        DrawLine(Rotate(center, new Vector2(-4, 0), facing), Rotate(center, new Vector2(17, 0), facing), new Color(style.Cat, 0.68f), 1.6f, true);
    }

    private void DrawAiUnit(Vector2 center, float facing, StyleSpec style)
    {
        var points = TransformPoints(
            center,
            facing,
            1,
            new[]
            {
                new Vector2(20, -6),
                new Vector2(10, -18),
                new Vector2(-18, -12),
                new Vector2(-24, 6),
                new Vector2(-5, 18),
                new Vector2(22, 8),
            });
        DrawTokenPolygon(points, style.Ai, style, filledScale: 0.20f);
        DrawLine(Rotate(center, new Vector2(-14, -15), facing), Rotate(center, new Vector2(16, 16), facing), new Color(style.Ai, 0.76f), 1.8f, true);
        DrawLine(Rotate(center, new Vector2(-8, 16), facing), Rotate(center, new Vector2(10, -14), facing), new Color(style.Ai, 0.54f), 1.3f, true);
    }

    private void DrawVehicle(Vector2 center, float facing, Color accent, StyleSpec style, string role)
    {
        var body = TransformPoints(
            center,
            facing,
            1,
            new[]
            {
                new Vector2(-32, -15),
                new Vector2(20, -15),
                new Vector2(32, -5),
                new Vector2(32, 7),
                new Vector2(18, 15),
                new Vector2(-30, 15),
                new Vector2(-38, 5),
                new Vector2(-38, -7),
            });
        DrawTokenPolygon(body, accent, style, filledScale: 0.20f);
        if (style.FilledTokens)
        {
            DrawCircle(center, 10, FillFor(accent, style, 0.26f));
        }

        DrawCircle(center, 10, new Color(accent, 0.72f), false, 2f, true);

        if (role == "repair")
        {
            DrawArc(center, 26, 0, Mathf.Tau, 64, new Color(accent, 0.42f), 2.2f, true);
            DrawLine(center + new Vector2(-9, 0), center + new Vector2(9, 0), new Color(accent, 0.84f), 2.4f, true);
            DrawLine(center + new Vector2(0, -9), center + new Vector2(0, 9), new Color(accent, 0.84f), 2.4f, true);
        }
    }

    private void DrawTokenPolygon(Vector2[] points, Color accent, StyleSpec style, float filledScale)
    {
        if (style.FilledTokens)
        {
            DrawColoredPolygon(points, FillFor(accent, style, filledScale));
        }
        else if (style.Dark)
        {
            DrawColoredPolygon(points, new Color("#0d1418", 0.70f));
        }

        DrawPolyline(Close(points), new Color(accent, style.Dark ? 0.94f : 0.82f), style.FilledTokens ? 2.1f : 2.7f, true);
        DrawPolyline(Close(ScalePolygon(points, 0.72f)), new Color(style.Ink, style.Dark ? 0.24f : 0.18f), 1.1f, true);
    }
}
