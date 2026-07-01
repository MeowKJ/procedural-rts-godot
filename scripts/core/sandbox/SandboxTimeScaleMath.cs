namespace ProceduralRts.Core;

public static class SandboxTimeScaleMath
{
    public const float DefaultScale = 1f;
    public static readonly IReadOnlyList<float> Presets = [0.25f, 0.5f, 1f, 2f, 4f];

    public static float Adjust(float currentScale, int direction)
    {
        var index = NearestIndex(currentScale);
        return Presets[Math.Clamp(index + Math.Sign(direction), 0, Presets.Count - 1)];
    }

    public static double ScaledGameplayDelta(double delta, LaunchMode launchMode, float scale)
    {
        if (launchMode != LaunchMode.Sandbox)
        {
            return delta;
        }

        return delta * Math.Max(0, scale);
    }

    public static string Format(float scale)
    {
        return $"Sandbox time x{scale:0.##}";
    }

    private static int NearestIndex(float scale)
    {
        var bestIndex = 0;
        var bestDistance = float.MaxValue;
        for (var index = 0; index < Presets.Count; index++)
        {
            var distance = MathF.Abs(Presets[index] - scale);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = index;
            }
        }

        return bestIndex;
    }
}
