using ProceduralRts.Core;

internal static partial class SelectionStressSuite
{
    private static void RunCameraCommandScenarios()
    {
        var leftEdge = CameraInputMath.EdgeScrollDirection(2, 360, 1280, 720, 28);
        AssertClose(leftEdge.X, -1, "left edge scroll x");
        AssertClose(leftEdge.Y, 0, "left edge scroll y");

        var rightBottomCorner = CameraInputMath.EdgeScrollDirection(1278, 718, 1280, 720, 28);
        AssertClose(MathF.Round(rightBottomCorner.X, 3), 0.707f, "corner edge scroll normalized x");
        AssertClose(MathF.Round(rightBottomCorner.Y, 3), 0.707f, "corner edge scroll normalized y");

        var centerNoScroll = CameraInputMath.EdgeScrollDirection(640, 360, 1280, 720, 28);
        AssertClose(centerNoScroll.LengthSquared, 0, "center should not edge scroll");

        const float cameraSmoothingTolerance = 0.01f;
        var smooth30 = SimulateCameraSmooth(0, 1000, 18, 30, 1);
        var smooth60 = SimulateCameraSmooth(0, 1000, 18, 60, 1);
        var smooth144 = SimulateCameraSmooth(0, 1000, 18, 144, 1);
        AssertCloseWithin(smooth30, smooth60, cameraSmoothingTolerance, "camera smoothing 30hz vs 60hz");
        AssertCloseWithin(smooth144, smooth60, cameraSmoothingTolerance, "camera smoothing 144hz vs 60hz");

        var pan30 = SimulateCameraSmooth2D((0, 0), (900, 620), 18, 30, 0.65f);
        var pan60 = SimulateCameraSmooth2D((0, 0), (900, 620), 18, 60, 0.65f);
        var pan144 = SimulateCameraSmooth2D((0, 0), (900, 620), 18, 144, 0.65f);
        AssertCloseWithin(pan30.X, pan60.X, cameraSmoothingTolerance, "camera pan smoothing x 30hz vs 60hz");
        AssertCloseWithin(pan30.Y, pan60.Y, cameraSmoothingTolerance, "camera pan smoothing y 30hz vs 60hz");
        AssertCloseWithin(pan144.X, pan60.X, cameraSmoothingTolerance, "camera pan smoothing x 144hz vs 60hz");
        AssertCloseWithin(pan144.Y, pan60.Y, cameraSmoothingTolerance, "camera pan smoothing y 144hz vs 60hz");
    }

    private static float SimulateCameraSmooth(float start, float target, float responsiveness, int fps, float seconds)
    {
        var value = start;
        var dt = 1f / fps;
        var steps = MathF.Round(seconds * fps);
        for (var i = 0; i < steps; i++)
        {
            value = CameraInputMath.SmoothToward(value, target, responsiveness, dt);
        }

        return value;
    }

    private static (float X, float Y) SimulateCameraSmooth2D((float X, float Y) start, (float X, float Y) target, float responsiveness, int fps, float seconds)
    {
        var value = start;
        var dt = 1f / fps;
        var steps = MathF.Round(seconds * fps);
        for (var i = 0; i < steps; i++)
        {
            value = CameraInputMath.SmoothToward(value.X, value.Y, target.X, target.Y, responsiveness, dt);
        }

        return value;
    }
}
