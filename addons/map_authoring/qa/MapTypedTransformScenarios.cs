using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Nodes;
using ProceduralRts.MapAuthoring.Projection;
using AuthoringResource = ProceduralRts.MapAuthoring.Nodes.Resource;

namespace ProceduralRts.MapAuthoring.Qa;

static class MapTypedTransformScenarios
{
    public static void Run(MapTypedProjectionFixture fixture)
    {
        AllowNestedResourceRotation(fixture.Root);
        Reject(fixture.Root, Parent(scale: new Vector2(1.5f, 1)), Resource("ScaledResource"), "resource scale");
        Reject(fixture.Root, Parent(scale: new Vector2(-1, 1)), Resource("ReflectedResource"), "resource reflection");
        Reject(fixture.Root, Parent(rotation: 0.2f), new Obstacle { Id = "RotatedObstacle" }, "obstacle rotation");
        Reject(fixture.Root, Parent(scale: new Vector2(1, 0.8f)), new TerrainRegion { Id = "ScaledTerrain" }, "terrain scale");
        Reject(fixture.Root, Parent(skew: 0.15f), new Trigger { Id = "SkewedTrigger" }, "trigger skew");
        Reject(fixture.Root, Parent(scale: new Vector2(1.2f, 1)), new OwnerStart { OwnerId = 3 }, "owner scale");
        Reject(fixture.Root, Parent(scale: new Vector2(0.9f, 1)), new Building
        {
            BuildingId = BuildingDesignIds.Barracks, OwnerId = 1, FactionId = "dog",
        }, "building scale");
        Reject(fixture.Root, Parent(scale: new Vector2(-1, 1)), new Unit
        {
            DesignId = "dog.infantry", OwnerId = 1,
        }, "unit reflection");
    }

    private static void AllowNestedResourceRotation(MapRoot root)
    {
        var parent = Parent(rotation: 0.4f);
        var resource = Resource("RotatedCircle");
        Attach(root, parent, resource);
        try
        {
            var projected = TypedMapSceneProjector.Instance.Project(root);
            Require(projected.Resources.Any(item => item.Id == resource.Id),
                "Pure nested rotation must remain representable for a circular Resource.");
        }
        finally
        {
            Detach(root, parent);
        }
    }

    private static void Reject(MapRoot root, Node2D parent, Node2D contributor, string label)
    {
        contributor.Name = label.Replace(' ', '_');
        Attach(root, parent, contributor);
        try
        {
            var exception = Capture<MapAuthoringTransformException>(() => TypedMapSceneProjector.Instance.Project(root));
            Require(exception.Message.Contains(contributor.Name.ToString(), StringComparison.Ordinal),
                $"Rejected {label} diagnostic must name its contributor.");
        }
        finally
        {
            Detach(root, parent);
        }
    }

    private static Node2D Parent(float rotation = 0, Vector2? scale = null, float skew = 0)
    {
        return new Node2D
        {
            Name = "NestedTransform",
            Rotation = rotation,
            Scale = scale ?? Vector2.One,
            Skew = skew,
        };
    }

    private static AuthoringResource Resource(string id)
    {
        return new AuthoringResource { Id = id, Position = new Vector2(400, 300), Radius = 40, Amount = 100 };
    }

    private static void Attach(MapRoot root, Node2D parent, Node2D contributor)
    {
        root.AddChild(parent);
        parent.AddChild(contributor);
    }

    private static void Detach(MapRoot root, Node2D parent)
    {
        root.RemoveChild(parent);
        parent.Free();
    }

    private static T Capture<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T exception) { return exception; }
        catch (Exception exception) { throw new InvalidOperationException($"Expected {typeof(T).Name}, got {exception.GetType().Name}.", exception); }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
