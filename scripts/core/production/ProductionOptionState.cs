using Godot;

namespace ProceduralRts.Core;

public sealed record ProductionOptionState(
    ProductionKind Kind,
    ProductionCategory Category,
    string ProducerKind,
    string? UnitDesignId,
    string ShortCode,
    IconGlyph Icon,
    IconGlyph RoleGlyph,
    Color Accent,
    int Cost,
    float Duration,
    bool HasProducer,
    bool EnoughCredits,
    int QueuedCount,
    float ActiveProgress,
    string DisabledReasonKey
)
{
    public bool CanQueue => HasProducer && EnoughCredits;
}
