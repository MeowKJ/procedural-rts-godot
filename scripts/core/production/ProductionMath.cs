namespace ProceduralRts.Core;

public readonly record struct ProductionAdvance(float Progress, bool IsComplete);

public static class ProductionMath
{
    public static ProductionAdvance Advance(float currentProgress, float delta, float duration)
    {
        if (duration <= 0)
        {
            return new ProductionAdvance(duration, true);
        }

        var progress = currentProgress + MathF.Max(0, delta);
        return new ProductionAdvance(MathF.Min(progress, duration), progress >= duration);
    }
}
