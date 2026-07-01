namespace ProceduralRts.Core;

public readonly record struct PanDirection(float X, float Y)
{
    public float LengthSquared => X * X + Y * Y;
}

public static class CameraInputMath
{
    public const float MaxVisualDeltaSeconds = 1f / 30f;

    public static float StableVisualDelta(float deltaSeconds, float maxDeltaSeconds = MaxVisualDeltaSeconds)
    {
        if (deltaSeconds <= 0)
        {
            return 0;
        }

        return maxDeltaSeconds <= 0
            ? deltaSeconds
            : MathF.Min(deltaSeconds, maxDeltaSeconds);
    }

    public static float ExponentialSmoothingFactor(float responsiveness, float deltaSeconds)
    {
        if (deltaSeconds <= 0)
        {
            return 0;
        }

        if (responsiveness <= 0)
        {
            return 1;
        }

        return 1 - MathF.Exp(-responsiveness * deltaSeconds);
    }

    public static float SmoothToward(float current, float target, float responsiveness, float deltaSeconds)
    {
        return current + ((target - current) * ExponentialSmoothingFactor(responsiveness, deltaSeconds));
    }

    public static (float X, float Y) SmoothToward(
        float currentX,
        float currentY,
        float targetX,
        float targetY,
        float responsiveness,
        float deltaSeconds)
    {
        var factor = ExponentialSmoothingFactor(responsiveness, deltaSeconds);
        return (
            currentX + ((targetX - currentX) * factor),
            currentY + ((targetY - currentY) * factor));
    }

    public static PanDirection EdgeScrollDirection(
        float mouseX,
        float mouseY,
        float viewportWidth,
        float viewportHeight,
        float edgeSize)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0 || edgeSize <= 0)
        {
            return new PanDirection(0, 0);
        }

        var x = 0f;
        var y = 0f;
        var edge = MathF.Min(edgeSize, MathF.Min(viewportWidth, viewportHeight) * 0.35f);

        if (mouseX <= edge)
        {
            x -= 1;
        }
        else if (mouseX >= viewportWidth - edge)
        {
            x += 1;
        }

        if (mouseY <= edge)
        {
            y -= 1;
        }
        else if (mouseY >= viewportHeight - edge)
        {
            y += 1;
        }

        var lengthSquared = x * x + y * y;
        if (lengthSquared <= 1)
        {
            return new PanDirection(x, y);
        }

        var inverseLength = 1 / MathF.Sqrt(lengthSquared);
        return new PanDirection(x * inverseLength, y * inverseLength);
    }
}
