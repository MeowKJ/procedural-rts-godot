using Godot;

namespace ProceduralRts.Core;

public sealed class ResourceFieldModel
{
    internal ResourceFieldModel(
        int id,
        Vector2 position,
        float radius,
        int maxAmount,
        int amount,
        Color accent)
    {
        Id = id;
        Position = position;
        Radius = radius;
        MaxAmount = maxAmount;
        Amount = amount;
        Accent = accent;
    }

    public int Id { get; }
    public Vector2 Position { get; }
    public float Radius { get; }
    public int MaxAmount { get; }
    public int Amount { get; internal set; }
    public Color Accent { get; }
    public float Pulse { get; internal set; }
}
