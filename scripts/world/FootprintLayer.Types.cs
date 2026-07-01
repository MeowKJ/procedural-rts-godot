using Godot;
using ProceduralRts.Core;
using CoreOwner = ProceduralRts.Core.Owner;

namespace ProceduralRts.World;

public partial class FootprintLayer
{
    private readonly record struct TrailState(Vector2 LastPosition, float AccumulatedDistance, bool Alternate);

    private readonly record struct FootprintSpecStyle(
        FootprintStyle Footprint,
        MovementDomain MovementDomain,
        float Radius);

    private readonly record struct FootprintMark(
        FootprintMarkKind Kind,
        CoreOwner Owner,
        Vector2 Position,
        Vector2 Direction,
        Vector2 Side,
        Color Color,
        float Width,
        float Length,
        float LateralOffset,
        float Lifetime,
        float Age,
        bool Alternate);
}
