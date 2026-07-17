using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring;

public interface IMapSpecSceneProjector
{
    MapSpec Project(Node root);
}
