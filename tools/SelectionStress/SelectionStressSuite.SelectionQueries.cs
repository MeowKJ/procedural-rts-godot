using ProceduralRts.Core;

internal static partial class SelectionStressSuite
{
    private static int RunSelectionQueries()
    {
        var zooms = new[] { 0.42f, 0.65f, 0.82f, 1.0f, 1.45f };
        var cameras = new[]
        {
            (X: 900f, Y: 820f),
            (X: 1280f, Y: 720f),
            (X: 2400f, Y: 1500f),
        };
        var drags = new[]
        {
            (A: (X: 100f, Y: 100f), B: (X: 400f, Y: 300f)),
            (A: (X: 400f, Y: 300f), B: (X: 100f, Y: 100f)),
            (A: (X: 900f, Y: 620f), B: (X: 320f, Y: 160f)),
            (A: (X: 320f, Y: 620f), B: (X: 900f, Y: 160f)),
        };

        foreach (var zoom in zooms)
        {
            var padding = SelectionMath.ScreenPixelsToWorld(13, zoom);
            AssertClose(padding * zoom, 13, $"padding zoom {zoom}");

            foreach (var camera in cameras)
            {
                foreach (var drag in drags)
                {
                    var rect = SelectionMath.ScreenRectToWorldRect(
                        drag.A.X,
                        drag.A.Y,
                        drag.B.X,
                        drag.B.Y,
                        camera.X,
                        camera.Y,
                        1280,
                        720,
                        zoom);

                    var reverse = SelectionMath.ScreenRectToWorldRect(
                        drag.B.X,
                        drag.B.Y,
                        drag.A.X,
                        drag.A.Y,
                        camera.X,
                        camera.Y,
                        1280,
                        720,
                        zoom);

                    AssertRect(rect, reverse, $"reverse zoom {zoom} camera {camera} drag {drag}");

                    if (rect.Width <= 0 || rect.Height <= 0)
                    {
                        throw new InvalidOperationException($"degenerate rect at zoom {zoom}");
                    }
                }
            }
        }

        var defaultCamera = (X: 900f, Y: 820f);
        var defaultZoom = 0.82f;
        var playerTank = (X: 720f, Y: 760f);
        var tankScreen = WorldToScreen(playerTank.X, playerTank.Y, defaultCamera.X, defaultCamera.Y, 1280, 720, defaultZoom);
        var tankSelection = SelectionMath.ScreenRectToWorldRect(
            tankScreen.X - 35,
            tankScreen.Y - 35,
            tankScreen.X + 35,
            tankScreen.Y + 35,
            defaultCamera.X,
            defaultCamera.Y,
            1280,
            720,
            defaultZoom);

        if (!Contains(tankSelection, playerTank.X, playerTank.Y))
        {
            throw new InvalidOperationException("default spawn drag should contain the first player tank");
        }

        if (!SelectionGestureMath.IsLeftSelectionDrag(8) || SelectionGestureMath.IsLeftSelectionDrag(7.9f))
        {
            throw new InvalidOperationException("left selection should keep the precise 8px drag threshold");
        }

        if (SelectionGestureMath.IsRightSelectionDrag(20, 0.08))
        {
            throw new InvalidOperationException("fast right-click jitter should remain a command and must not clear selection as a box select");
        }

        if (SelectionGestureMath.IsRightSelectionDrag(31, 0.17))
        {
            throw new InvalidOperationException("rapid right-click movement inside the grace window should not become a selection drag");
        }

        if (!SelectionGestureMath.IsRightSelectionDrag(20, 0.24) || !SelectionGestureMath.IsRightSelectionDrag(36, 0.05))
        {
            throw new InvalidOperationException("intentional right-drag box selection should still work after the grace window or with a clear drag distance");
        }

        return zooms.Length * cameras.Length * drags.Length;
    }

    private static bool Contains(SelectionRect rect, float x, float y)
    {
        return x >= rect.X && x <= rect.EndX && y >= rect.Y && y <= rect.EndY;
    }

    private static (float X, float Y) WorldToScreen(
        float worldX,
        float worldY,
        float cameraCenterX,
        float cameraCenterY,
        float viewportWidth,
        float viewportHeight,
        float cameraZoom)
    {
        return (
            (worldX - cameraCenterX) * cameraZoom + viewportWidth / 2f,
            (worldY - cameraCenterY) * cameraZoom + viewportHeight / 2f
        );
    }
}
