using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring;

public static class MapSceneProjection
{
    public static IEnumerable<Node> SceneOrder(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren())
        {
            foreach (var descendant in SceneOrder(child))
            {
                yield return descendant;
            }
        }
    }

    public static Transform2D RootLocalTransform(Node2D root, Node2D contributor)
    {
        return root.GlobalTransform.AffineInverse() * contributor.GlobalTransform;
    }

    public static MapPoint RootLocalPoint(Node2D root, Node2D contributor)
    {
        var origin = RootLocalTransform(root, contributor).Origin;
        return new MapPoint(origin.X, origin.Y);
    }
}
