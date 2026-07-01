using Godot;
using System;

namespace ProceduralRts;

public partial class UnitShowcaseRoot
{
    private void DrawToken(Vector2[] points, Color accent, Color dark, float fill, float width)
    {
        var shadow = Offset(points, new Vector2(4, 6));
        DrawColoredPolygon(shadow, new Color("#1a130b", 0.13f));
        DrawColoredPolygon(points, new Color(Paper.Lerp(accent, fill), 0.96f));
        DrawPolyline(Close(points), new Color(dark, 0.92f), width, true);
        DrawPolyline(Close(ScalePolygon(points, 0.72f)), new Color("#fff7e8", 0.34f), 1.2f, true);
        DrawPolyline(Close(ScalePolygon(points, 0.88f)), new Color(accent, 0.40f), 1.4f, true);
    }

    private void DrawPill(Rect2 rect, string text, Color accent, Color dark)
    {
        DrawRect(rect, new Color(Paper.Lerp(accent, 0.16f), 0.94f));
        DrawRect(rect, new Color(dark, 0.34f), false, 1f);
        DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(10, rect.Size.Y * 0.64f), text, HorizontalAlignment.Left, rect.Size.X - 20, 11, dark);
    }

    private void DrawDashedCircle(Vector2 center, float radius, Color color, float width)
    {
        const int segments = 36;
        for (var i = 0; i < segments; i += 2)
        {
            var a = i * Mathf.Tau / segments;
            var b = (i + 1) * Mathf.Tau / segments;
            DrawArc(center, radius, a, b, 5, color, width, true);
        }
    }

    private static Color RoleColor(Role role, Color faction)
    {
        return role switch
        {
            Role.Assault => faction,
            Role.Repair => new Color("#3f8a6f"),
            Role.Defense => new Color("#3d7184"),
            Role.Bombard => new Color("#9b284c"),
            Role.Harvest => new Color("#b68a2c"),
            Role.Scout => new Color("#6c619f"),
            _ => faction,
        };
    }

    private static Vector2[] Points(Vector2 center, float scale, Vector2[] local)
    {
        var points = new Vector2[local.Length];
        for (var i = 0; i < local.Length; i++)
        {
            points[i] = center + local[i] * scale;
        }

        return points;
    }

    private static Vector2[] Close(Vector2[] points)
    {
        var closed = new Vector2[points.Length + 1];
        Array.Copy(points, closed, points.Length);
        closed[^1] = points[0];
        return closed;
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
