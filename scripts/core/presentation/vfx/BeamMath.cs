namespace ProceduralRts.Core;

public static class BeamMath
{
    public static float Fade(float age, float duration)
    {
        if (duration <= 0)
        {
            return 0;
        }

        var t = Math.Clamp(age / duration, 0, 1);
        var inverse = 1 - t;
        return inverse * inverse;
    }

    public static float Pulse(float age, float duration)
    {
        if (duration <= 0)
        {
            return 0;
        }

        var t = Math.Clamp(age / duration, 0, 1);
        return MathF.Sin(t * MathF.PI);
    }
}
