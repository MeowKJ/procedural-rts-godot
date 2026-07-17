using Godot;
using ProceduralRts.MapAuthoring.Nodes;
using AuthoringResource = ProceduralRts.MapAuthoring.Nodes.Resource;

namespace ProceduralRts.MapAuthoring.Projection;

public static class TypedMapTransformValidation
{
    public static bool TryReason(MapRoot root, Node2D node, out string reason)
    {
        var transform = MapSceneProjection.RootLocalTransform(root, node);
        var valid = node switch
        {
            Obstacle or TerrainRegion or Trigger => IsIdentityBasis(transform),
            OwnerStart or Building or Unit or AuthoringResource => IsRotationOnly(transform),
            _ => true,
        };
        reason = valid ? "" : node is Obstacle or TerrainRegion or Trigger
            ? "axis-aligned rectangle basis"
            : "positive orthonormal basis";
        return valid;
    }

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
        if (!IsRotationOnly(transform))
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

    private static bool IsRotationOnly(Transform2D transform)
    {
        return Mathf.IsEqualApprox(transform.X.LengthSquared(), 1)
            && Mathf.IsEqualApprox(transform.Y.LengthSquared(), 1)
            && Mathf.IsZeroApprox(transform.X.Dot(transform.Y))
            && Mathf.IsEqualApprox(transform.Determinant(), 1);
    }
}
