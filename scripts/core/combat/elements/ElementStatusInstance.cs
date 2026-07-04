namespace ProceduralRts.Core;

public sealed record ElementStatusInstance
{
    public string StatusId { get; init; }
    public string SourceElementId { get; init; }
    public float RemainingDuration { get; init; }
    public int Stacks { get; init; }

    public ElementStatusInstance(
        string StatusId,
        string SourceElementId,
        float RemainingDuration,
        int Stacks = 1)
    {
        if (string.IsNullOrWhiteSpace(StatusId))
        {
            throw new ArgumentException("Element status instance ids must be non-empty.", nameof(StatusId));
        }

        _ = DamageElementCatalog.For(SourceElementId);
        if (RemainingDuration <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RemainingDuration), "Element status remaining duration must be positive.");
        }

        if (Stacks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Stacks), "Element status stacks must be positive.");
        }

        this.StatusId = StatusId;
        this.SourceElementId = SourceElementId;
        this.RemainingDuration = RemainingDuration;
        this.Stacks = Stacks;
    }

    public static ElementStatusInstance FromDefinition(ElementStatusDefinition definition)
    {
        return new ElementStatusInstance(
            definition.Id,
            definition.SourceElementId,
            definition.DurationSeconds);
    }
}
