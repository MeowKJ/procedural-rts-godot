using Godot;

namespace ProceduralRts.Core;

public sealed record UnitShapeLayer(
    UnitShapeKind Kind,
    UnitShapeRole Role,
    Vector2[] Points,
    Vector2 From,
    Vector2 To,
    float Radius,
    float Width,
    bool Filled
)
{
    public static UnitShapeLayer Polygon(UnitShapeRole role, Vector2[] points, bool filled, float width = 2)
    {
        return new UnitShapeLayer(UnitShapeKind.Polygon, role, points, Vector2.Zero, Vector2.Zero, 0, width, filled);
    }

    public static UnitShapeLayer Line(UnitShapeRole role, Vector2 from, Vector2 to, float width)
    {
        return new UnitShapeLayer(UnitShapeKind.Line, role, [], from, to, 0, width, false);
    }

    public static UnitShapeLayer Circle(UnitShapeRole role, Vector2 center, float radius, bool filled, float width = 2)
    {
        return new UnitShapeLayer(UnitShapeKind.Circle, role, [], center, Vector2.Zero, radius, width, filled);
    }

    public static UnitShapeLayer Arc(UnitShapeRole role, Vector2 center, float radius, float width)
    {
        return new UnitShapeLayer(UnitShapeKind.Arc, role, [], center, Vector2.Zero, radius, width, false);
    }
}
