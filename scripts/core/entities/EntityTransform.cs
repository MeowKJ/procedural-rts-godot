using Godot;

namespace ProceduralRts.Core;

public readonly record struct EntityTransform(Vector2 Position, float Facing)
{
    public static EntityTransform At(Vector2 position, float facing = 0)
    {
        return new EntityTransform(position, facing);
    }
}
