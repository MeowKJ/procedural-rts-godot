namespace ProceduralRts.Core;

public sealed record ElementReactionResolution(
    bool Triggered,
    ElementReactionDefinition? Reaction,
    ElementStatusInstance? ConsumedStatus,
    IReadOnlyList<ElementStatusInstance> ActiveStatuses);
