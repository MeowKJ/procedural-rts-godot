namespace ProceduralRts.Core;

public sealed record ElementStatusDefinition
{
    public string Id { get; init; }
    public string Label { get; init; }
    public string SourceElementId { get; init; }
    public float DurationSeconds { get; init; }
    public ElementStatusStackingMode StackingMode { get; init; }
    public int MaxStacks { get; init; }
    public ElementStatusVisibility Visibility { get; init; }
    public ElementStatusModifierPayload? ModifierPayload { get; init; }

    public ElementStatusDefinition(
        string Id,
        string Label,
        string SourceElementId,
        float DurationSeconds,
        ElementStatusStackingMode StackingMode = ElementStatusStackingMode.RefreshDuration,
        int MaxStacks = 1,
        ElementStatusVisibility Visibility = ElementStatusVisibility.Visible,
        ElementStatusModifierPayload? ModifierPayload = null)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new ArgumentException("Element status ids must be non-empty.", nameof(Id));
        }

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new ArgumentException("Element status labels must be non-empty.", nameof(Label));
        }

        _ = DamageElementCatalog.For(SourceElementId);
        if (DurationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(DurationSeconds), "Element status duration must be positive.");
        }

        if (MaxStacks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxStacks), "Element status max stacks must be positive.");
        }

        this.Id = Id;
        this.Label = Label;
        this.SourceElementId = SourceElementId;
        this.DurationSeconds = DurationSeconds;
        this.StackingMode = StackingMode;
        this.MaxStacks = MaxStacks;
        this.Visibility = Visibility;
        this.ModifierPayload = ModifierPayload;
    }
}
