using Godot;

namespace ProceduralRts.MapAuthoring.Projection;

public sealed class MapAuthoringTransformException : InvalidOperationException
{
    public MapAuthoringTransformException(Node2D node, string representableBasis)
        : base($"Typed map node '{node.Name}' has an unrepresentable root-local basis; expected {representableBasis}.")
    {
    }
}
