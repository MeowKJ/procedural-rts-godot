namespace ProceduralRts.Core;

public static class SelectionGestureMath
{
    public const float LeftDragSelectPixels = 8;
    public const float RightDragSelectPixels = 18;
    public const float RightQuickClickMaxPixels = 32;
    public const double RightQuickClickSeconds = 0.18;

    public static bool IsLeftSelectionDrag(float distancePixels)
    {
        return distancePixels >= LeftDragSelectPixels;
    }

    public static bool IsRightSelectionDrag(float distancePixels, double elapsedSeconds)
    {
        if (elapsedSeconds <= RightQuickClickSeconds && distancePixels <= RightQuickClickMaxPixels)
        {
            return false;
        }

        return distancePixels >= RightDragSelectPixels;
    }
}
