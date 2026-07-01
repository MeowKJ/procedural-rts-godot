namespace ProceduralRts.Core;

public readonly record struct SelectionRect(float X, float Y, float Width, float Height)
{
    public float EndX => X + Width;
    public float EndY => Y + Height;
}

public static class SelectionMath
{
    public static SelectionRect RectFromPoints(float ax, float ay, float bx, float by)
    {
        var x = MathF.Min(ax, bx);
        var y = MathF.Min(ay, by);
        return new SelectionRect(x, y, MathF.Abs(ax - bx), MathF.Abs(ay - by));
    }

    public static float ScreenPixelsToWorld(float screenPixels, float cameraZoom)
    {
        return screenPixels / MathF.Max(cameraZoom, 0.001f);
    }

    public static SelectionRect ScreenRectToWorldRect(
        float startX,
        float startY,
        float endX,
        float endY,
        float cameraCenterX,
        float cameraCenterY,
        float viewportWidth,
        float viewportHeight,
        float cameraZoom)
    {
        var a = ScreenToWorld(startX, startY, cameraCenterX, cameraCenterY, viewportWidth, viewportHeight, cameraZoom);
        var b = ScreenToWorld(endX, endY, cameraCenterX, cameraCenterY, viewportWidth, viewportHeight, cameraZoom);
        return RectFromPoints(a.X, a.Y, b.X, b.Y);
    }

    private static (float X, float Y) ScreenToWorld(
        float screenX,
        float screenY,
        float cameraCenterX,
        float cameraCenterY,
        float viewportWidth,
        float viewportHeight,
        float cameraZoom)
    {
        var zoom = MathF.Max(cameraZoom, 0.001f);
        return (
            cameraCenterX + (screenX - viewportWidth / 2f) / zoom,
            cameraCenterY + (screenY - viewportHeight / 2f) / zoom
        );
    }
}
