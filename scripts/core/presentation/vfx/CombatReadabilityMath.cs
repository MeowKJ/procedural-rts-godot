namespace ProceduralRts.Core;

public readonly record struct CombatReadabilityStyle(bool Draw, float AlphaScale, float LineWidthScale);

public static class CombatReadabilityMath
{
    public static CombatReadabilityStyle StyleFor(
        bool visibleToPlayer,
        bool exploredByPlayer,
        int activeEffectCount,
        int commandMarkerCount)
    {
        if (!visibleToPlayer && !exploredByPlayer)
        {
            return new CombatReadabilityStyle(false, 0, 0);
        }

        var alpha = visibleToPlayer ? 0.82f : 0.18f;
        var line = visibleToPlayer ? 0.92f : 0.62f;
        if (commandMarkerCount > 0)
        {
            alpha *= 0.72f;
            line *= 0.86f;
        }

        if (activeEffectCount > 140)
        {
            alpha *= 0.62f;
            line *= 0.82f;
        }
        else if (activeEffectCount > 96)
        {
            alpha *= 0.78f;
            line *= 0.9f;
        }

        return new CombatReadabilityStyle(alpha > 0.02f, alpha, line);
    }
}
