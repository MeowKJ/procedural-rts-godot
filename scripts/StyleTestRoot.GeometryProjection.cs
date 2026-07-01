using Godot;

namespace ProceduralRts;

public partial class StyleTestRoot
{
    private static Vector2[] TransformPoints(Vector2 center, float rotation, float scale, Vector2[] points)
    {
        var transformed = new Vector2[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            transformed[i] = Rotate(center, points[i] * scale, rotation);
        }

        return transformed;
    }

    private static Vector2 Rotate(Vector2 center, Vector2 local, float rotation)
    {
        var cos = Mathf.Cos(rotation);
        var sin = Mathf.Sin(rotation);
        return center + new Vector2(local.X * cos - local.Y * sin, local.X * sin + local.Y * cos);
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

    private static Color FillFor(Color accent, StyleSpec style, float amount)
    {
        return new Color(style.Background.Lerp(accent, amount), style.Dark ? 0.78f : 0.92f);
    }
}
