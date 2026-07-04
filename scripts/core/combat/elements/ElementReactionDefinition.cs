namespace ProceduralRts.Core;

public sealed record ElementReactionDefinition
{
    public string ReactionId { get; init; }
    public string Label { get; init; }
    public string PrimerStatusId { get; init; }
    public string TriggerElementId { get; init; }
    public ElementReactionEffectPayload EffectPayload { get; init; }
    public ElementReactionPresentationStyle PresentationStyle { get; init; }
    public bool ConsumesPrimer { get; init; }

    public ElementReactionDefinition(
        string ReactionId,
        string Label,
        string PrimerStatusId,
        string TriggerElementId,
        ElementReactionEffectPayload EffectPayload,
        ElementReactionPresentationStyle PresentationStyle,
        bool ConsumesPrimer = true)
    {
        if (string.IsNullOrWhiteSpace(ReactionId))
        {
            throw new ArgumentException("Element reaction ids must be non-empty.", nameof(ReactionId));
        }

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new ArgumentException("Element reaction labels must be non-empty.", nameof(Label));
        }

        if (string.IsNullOrWhiteSpace(PrimerStatusId))
        {
            throw new ArgumentException("Element reaction primer status ids must be non-empty.", nameof(PrimerStatusId));
        }

        _ = DamageElementCatalog.For(TriggerElementId);

        this.ReactionId = ReactionId;
        this.Label = Label;
        this.PrimerStatusId = PrimerStatusId;
        this.TriggerElementId = TriggerElementId;
        this.EffectPayload = EffectPayload ?? throw new ArgumentNullException(nameof(EffectPayload));
        this.PresentationStyle = PresentationStyle;
        this.ConsumesPrimer = ConsumesPrimer;
    }
}
