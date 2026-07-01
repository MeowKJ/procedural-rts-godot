namespace ProceduralRts.Core;

public sealed record EconomyTuningConfig(
    float GatherDistance = 24f,
    float DockDistance = 30f,
    float GatherRate = 110f,
    float UnloadRate = 220f,
    float RegenerationRate = 0.8f,
    float RegenerationCapRatio = 1f,
    float CleanRegenerationMultiplier = 1f,
    float TaintedRegenerationMultiplier = 0.45f,
    float HostileRegenerationMultiplier = 0f,
    float SafeAuraRegenerationMultiplier = 1.5f,
    float DayRegenerationMultiplier = 1f,
    float FogRegenerationMultiplier = 0.75f,
    float NightRegenerationMultiplier = 0.5f,
    float CorruptionRegenerationMultiplier = 0.15f)
{
    public static EconomyTuningConfig Default { get; } = new();

    public float GatherRateFor(ResourceNodeComponentState node)
    {
        return MathF.Max(0, GatherRate) * MathF.Max(0, node.GatherRateModifier);
    }

    public float SafeUnloadRate => MathF.Max(0, UnloadRate);

    public int RegenerationCapFor(ResourceNodeComponentState node)
    {
        var ratio = Math.Clamp(RegenerationCapRatio, 0, 1);
        return Math.Min(node.MaxAmount, Math.Max(0, (int)MathF.Floor(node.MaxAmount * ratio)));
    }

    public float RegenerationRateFor(
        ResourceNodeComponentState node,
        ResourceAtmosphere atmosphere,
        float auraMultiplier)
    {
        if (node.DepletionBehavior != ResourceDepletionBehavior.DepleteThenRegrow)
        {
            return 0;
        }

        return MathF.Max(0, RegenerationRate)
            * CorruptionMultiplier(node.CorruptionState)
            * AtmosphereMultiplier(atmosphere)
            * MathF.Max(0, auraMultiplier);
    }

    private float CorruptionMultiplier(ResourceCorruptionState state)
    {
        return state switch
        {
            ResourceCorruptionState.Tainted => MathF.Max(0, TaintedRegenerationMultiplier),
            ResourceCorruptionState.Hostile => MathF.Max(0, HostileRegenerationMultiplier),
            _ => MathF.Max(0, CleanRegenerationMultiplier),
        };
    }

    private float AtmosphereMultiplier(ResourceAtmosphere atmosphere)
    {
        return atmosphere switch
        {
            ResourceAtmosphere.Fog => MathF.Max(0, FogRegenerationMultiplier),
            ResourceAtmosphere.Night => MathF.Max(0, NightRegenerationMultiplier),
            ResourceAtmosphere.Corruption => MathF.Max(0, CorruptionRegenerationMultiplier),
            _ => MathF.Max(0, DayRegenerationMultiplier),
        };
    }
}
