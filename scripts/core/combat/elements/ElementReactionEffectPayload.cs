namespace ProceduralRts.Core;

public sealed record ElementReactionEffectPayload
{
    public float DamageMultiplier { get; init; }
    public float SplashRadius { get; init; }
    public float StatusDurationMultiplier { get; init; }

    public ElementReactionEffectPayload(
        float DamageMultiplier = 1f,
        float SplashRadius = 0f,
        float StatusDurationMultiplier = 1f)
    {
        if (DamageMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(DamageMultiplier), "Reaction damage multipliers must be positive.");
        }

        if (SplashRadius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SplashRadius), "Reaction splash radii cannot be negative.");
        }

        if (StatusDurationMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(StatusDurationMultiplier), "Reaction duration multipliers must be positive.");
        }

        this.DamageMultiplier = DamageMultiplier;
        this.SplashRadius = SplashRadius;
        this.StatusDurationMultiplier = StatusDurationMultiplier;
    }
}
