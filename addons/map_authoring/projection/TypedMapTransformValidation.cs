using Godot;
using ProceduralRts.MapAuthoring.Nodes;

namespace ProceduralRts.MapAuthoring.Projection;

static class TypedMapTransformValidation
{
    public static Transform2D Rect(MapRoot root, Node2D node)
    {
        var transform = MapSceneProjection.RootLocalTransform(root, node);
        if (!IsIdentityBasis(transform))
        {
            throw new MapAuthoringTransformException(node, "identity for an axis-aligned rectangle");
        }
        return transform;
    }

    public static Transform2D Circle(MapRoot root, Node2D node)
    {
        return RotationOnly(root, node, "positive orthonormal rotation for a circle");
    }

    public static Transform2D Entity(MapRoot root, Node2D node)
    {
        return RotationOnly(root, node, "positive orthonormal rotation for an entity");
    }

    private static Transform2D RotationOnly(MapRoot root, Node2D node, string expected)
    {
        var transform = MapSceneProjection.RootLocalTransform(root, node);
        if (!Mathf.IsEqualApprox(transform.X.LengthSquared(), 1)
            || !Mathf.IsEqualApprox(transform.Y.LengthSquared(), 1)
            || !Mathf.IsZeroApprox(transform.X.Dot(transform.Y))
            || !Mathf.IsEqualApprox(transform.Determinant(), 1))
        {
            throw new MapAuthoringTransformException(node, expected);
        }
        return transform;
    }

    private static bool IsIdentityBasis(Transform2D transform)
    {
        return transform.X.IsEqualApprox(Vector2.Right)
            && transform.Y.IsEqualApprox(Vector2.Down);
    }
}
