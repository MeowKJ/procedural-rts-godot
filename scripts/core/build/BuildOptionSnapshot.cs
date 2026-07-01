using Godot;

namespace ProceduralRts.Core;

public sealed record BuildOptionSnapshot(
    string Kind,
    BuildCategory Category,
    IconGlyph Icon,
    int Cost,
    float BuildTime,
    Vector2 Footprint,
    bool CanAfford,
    bool HasPrerequisites,
    string DisabledReasonKey,
    int PowerProvided,
    int PowerUsed,
    float BuildRadius
)
{
    public bool CanStart => CanAfford && HasPrerequisites;
}
