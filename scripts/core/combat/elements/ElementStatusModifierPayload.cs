namespace ProceduralRts.Core;

public sealed record ElementStatusModifierPayload
{
    public static ElementStatusModifierPayload Neutral { get; } = new();

    public float IncomingDamageMultiplier { get; init; }
    public float OutgoingDamageMultiplier { get; init; }
    public float MovementSpeedMultiplier { get; init; }

    public ElementStatusModifierPayload(
        float IncomingDamageMultiplier = 1f,
        float OutgoingDamageMultiplier = 1f,
        float MovementSpeedMultiplier = 1f)
    {
        if (IncomingDamageMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(IncomingDamageMultiplier), "Status incoming damage multipliers must be positive.");
        }

        if (OutgoingDamageMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(OutgoingDamageMultiplier), "Status outgoing damage multipliers must be positive.");
        }

        if (MovementSpeedMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MovementSpeedMultiplier), "Status movement multipliers must be positive.");
        }

        this.IncomingDamageMultiplier = IncomingDamageMultiplier;
        this.OutgoingDamageMultiplier = OutgoingDamageMultiplier;
        this.MovementSpeedMultiplier = MovementSpeedMultiplier;
    }
}
