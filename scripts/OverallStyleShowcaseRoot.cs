using Godot;
using System.IO;

namespace ProceduralRts;

public partial class OverallStyleShowcaseRoot : Control
{
    private const string CapturePath = "artifacts/overall-style-showcase-godot.png";
    private const float RedrawIntervalSeconds = 1f / 20f;
    private static readonly Vector2I CaptureSize = new(1600, 900);
    private static readonly Color Ink = new("#262b2d");
    private static readonly Color Muted = new("#68706e");

    private float _elapsed;
    private float _redrawTimer;

    public override async void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        FocusMode = FocusModeEnum.All;
        DisplayServer.WindowSetSize(CaptureSize);

        if (OS.GetEnvironment("OVERALL_STYLE_CAPTURE") == "1")
        {
            await NextFrames(8);
            SaveCapture();
            GetTree().Quit();
        }
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        _redrawTimer -= (float)delta;
        if (_redrawTimer <= 0)
        {
            _redrawTimer = RedrawIntervalSeconds;
            QueueRedraw();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            GetTree().Quit();
        }
    }

    public override void _Draw()
    {
        var size = Size;
        if (size.X < 10 || size.Y < 10)
        {
            size = new Vector2(CaptureSize.X, CaptureSize.Y);
        }

        var page = new Color("#e6d7c0");
        DrawRect(new Rect2(Vector2.Zero, size), page);
        DrawString(ThemeDB.FallbackFont, new Vector2(34, 46), "Style 1 Variants: Soft Tactical Board", HorizontalAlignment.Left, 780, 30, Ink);
        DrawString(
            ThemeDB.FallbackFont,
            new Vector2(36, 80),
            "Same gameplay language, different atmosphere. The strong candidates are A/B/C for normal play; D/E/F are phase or mission moods.",
            HorizontalAlignment.Left,
            size.X - 72,
            15,
            Muted);

        var specs = new[]
        {
            new StyleSpec("A", "Old City Day", "recommended default", new Color("#eadbc4"), new Color("#c9b89c", 0.32f), new Color("#7a6f60", 0.30f), new Color("#2b3032"), new Color("#c27822"), new Color("#51449c"), new Color("#a33055"), 0.08f, 0.72f, false),
            new StyleSpec("B", "Porcelain Sand", "cleanest low-fatigue", new Color("#f0e7d8"), new Color("#cfbfaa", 0.25f), new Color("#9d8e79", 0.24f), new Color("#303434"), new Color("#bd7626"), new Color("#6251a4"), new Color("#9a3350"), 0.05f, 0.66f, false),
            new StyleSpec("C", "Fog Morning", "soft exploration mood", new Color("#d8d6cb"), new Color("#9da095", 0.22f), new Color("#72786f", 0.24f), new Color("#313837"), new Color("#b87f2f"), new Color("#5f5aa0"), new Color("#93415e"), 0.18f, 0.58f, false),
            new StyleSpec("D", "Warm Gray Board", "best if lines stay thin", new Color("#c9c2b5"), new Color("#696b65", 0.22f), new Color("#454d4e", 0.25f), new Color("#1f2829"), new Color("#c47d20"), new Color("#5144a0"), new Color("#9d2f52"), 0.05f, 0.62f, false),
            new StyleSpec("E", "Dusk Defense", "pressure phase only", new Color("#263134"), new Color("#f0d49d", 0.10f), new Color("#f0aa3d", 0.18f), new Color("#edf0ec"), new Color("#f2a83b"), new Color("#8e80ff"), new Color("#ff5579"), 0.11f, 0.92f, true),
            new StyleSpec("F", "Night Emergency", "alarm / collapse phase", new Color("#11191c"), new Color("#7fb0b7", 0.10f), new Color("#e05b77", 0.18f), new Color("#f1f2ed"), new Color("#ffc35a"), new Color("#9e90ff"), new Color("#ff3f6c"), 0.16f, 0.96f, true),
        };

        var margin = 34f;
        var top = 124f;
        var gap = 18f;
        var columns = 3;
        var rows = 2;
        var panelWidth = (size.X - margin * 2 - gap * (columns - 1)) / columns;
        var panelHeight = (size.Y - top - 30 - gap * (rows - 1)) / rows;

        for (var i = 0; i < specs.Length; i++)
        {
            var col = i % columns;
            var row = i / columns;
            var rect = new Rect2(
                margin + col * (panelWidth + gap),
                top + row * (panelHeight + gap),
                panelWidth,
                panelHeight);
            DrawStylePanel(rect, specs[i]);
        }
    }

    private void DrawStylePanel(Rect2 rect, StyleSpec style)
    {
        DrawRect(rect, style.Background);
        DrawRect(rect, new Color(style.Ink, style.Dark ? 0.38f : 0.20f), false, 1.2f);
        DrawTerrain(rect, style);
        DrawAtmosphere(rect, style);
        DrawUnitsAndCommands(rect, style);
        DrawHud(rect, style);
        DrawTitle(rect, style);
    }

    private void DrawTitle(Rect2 rect, StyleSpec style)
    {
        var tag = new Rect2(rect.Position + new Vector2(14, 13), new Vector2(35, 25));
        DrawRect(tag, new Color(style.Ink, style.Dark ? 0.18f : 0.08f));
        DrawRect(tag, new Color(style.Ink, 0.28f), false, 1f);
        DrawString(ThemeDB.FallbackFont, tag.Position + new Vector2(10, 18), style.Tag, HorizontalAlignment.Left, 22, 14, style.Ink);
        DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(58, 32), style.Name, HorizontalAlignment.Left, rect.Size.X - 82, 17, style.Ink);
        DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(58, 53), style.Note, HorizontalAlignment.Left, rect.Size.X - 82, 11, new Color(style.Ink, style.Dark ? 0.62f : 0.52f));
    }

    private void DrawTerrain(Rect2 rect, StyleSpec style)
    {
        for (var x = rect.Position.X + 18; x < rect.End.X; x += 28)
        {
            DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), style.Grid, 0.8f, true);
        }

        for (var y = rect.Position.Y + 18; y < rect.End.Y; y += 28)
        {
            DrawLine(new Vector2(rect.Position.X, y), new Vector2(rect.End.X, y), style.Grid, 0.8f, true);
        }

        for (var x = rect.Position.X + 74; x < rect.End.X; x += 112)
        {
            DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), style.Major, 1.2f, true);
        }

        var roadA = rect.Position + new Vector2(28, rect.Size.Y * 0.76f);
        var roadB = rect.Position + new Vector2(rect.Size.X * 0.50f, rect.Size.Y * 0.61f);
        var roadC = rect.Position + new Vector2(rect.Size.X - 30, rect.Size.Y * 0.46f);
        DrawLine(roadA, roadB, new Color(style.Dog, style.Dark ? 0.28f : 0.18f), 12f, true);
        DrawLine(roadB, roadC, new Color(style.Dog, style.Dark ? 0.28f : 0.18f), 12f, true);
        DrawLine(roadA, roadB, new Color(style.Background, style.Dark ? 0.28f : 0.36f), 3f, true);
        DrawLine(roadB, roadC, new Color(style.Background, style.Dark ? 0.28f : 0.36f), 3f, true);

        var catMark = rect.Position + new Vector2(rect.Size.X * 0.21f, rect.Size.Y * 0.33f);
        DrawDashedLine(catMark, rect.Position + new Vector2(rect.Size.X * 0.74f, rect.Size.Y * 0.42f), new Color(style.Cat, style.Dark ? 0.46f : 0.28f), 2.2f, 10, 7);

        var ai = rect.Position + new Vector2(rect.Size.X * 0.72f, rect.Size.Y * 0.64f);
        DrawArc(ai, rect.Size.Y * 0.19f, 0, Mathf.Tau, 72, new Color(style.Ai, style.Dark ? 0.46f : 0.25f), 2.2f, true);
        DrawArc(ai, rect.Size.Y * 0.29f, 0, Mathf.Tau, 96, new Color(style.Ai, style.Dark ? 0.28f : 0.16f), 1.6f, true);
        for (var i = 0; i < 8; i++)
        {
            var angle = i * Mathf.Tau / 8f + _elapsed * 0.06f;
            DrawLine(ai, ai + Vector2.FromAngle(angle) * rect.Size.Y * 0.28f, new Color(style.Ai, style.Dark ? 0.30f : 0.16f), 1.2f, true);
        }
    }

    private void DrawAtmosphere(Rect2 rect, StyleSpec style)
    {
        if (style.Haze <= 0.01f)
        {
            return;
        }

        DrawRect(rect, new Color("#f7efe1", style.Haze));
        for (var i = 0; i < 4; i++)
        {
            var y = rect.Position.Y + rect.Size.Y * (0.18f + i * 0.18f);
            DrawLine(
                new Vector2(rect.Position.X + 18, y),
                new Vector2(rect.End.X - 18, y + 26),
                new Color("#fff6e7", style.Haze * 0.55f),
                18,
                true);
        }
    }

    private void DrawUnitsAndCommands(Rect2 rect, StyleSpec style)
    {
        var dogBase = rect.Position + new Vector2(rect.Size.X * 0.28f, rect.Size.Y * 0.61f);
        var catBase = rect.Position + new Vector2(rect.Size.X * 0.46f, rect.Size.Y * 0.36f);
        var target = rect.Position + new Vector2(rect.Size.X * 0.60f, rect.Size.Y * 0.47f);

        DrawSelectionBox(new Rect2(dogBase.X - 72, dogBase.Y - 44, 142, 84), style);
        DrawLine(dogBase + new Vector2(24, -8), target, new Color(style.Dog, style.CommandAlpha), 2.7f, true);
        DrawCircle(target, 16, new Color(style.Dog, 0.12f), false, 2.2f, true);
        DrawCircle(target, 4.5f, new Color(style.Dog, 0.86f));

        DrawDogUnit(dogBase + new Vector2(-34, -8), style);
        DrawDogTank(dogBase + new Vector2(28, 4), style);
        DrawCatUnit(catBase + new Vector2(-18, 0), style);
        DrawCatUnit(catBase + new Vector2(34, 18), style);
        DrawAiNode(rect.Position + new Vector2(rect.Size.X * 0.73f, rect.Size.Y * 0.63f), style);
    }

    private void DrawSelectionBox(Rect2 box, StyleSpec style)
    {
        DrawRect(box, new Color(style.Dog, style.Dark ? 0.10f : 0.07f));
        DrawRect(box, new Color(style.Dog, style.Dark ? 0.76f : 0.55f), false, 1.6f);
    }

    private void DrawDogUnit(Vector2 c, StyleSpec style)
    {
        var pts = Points(c, [new(0, -22), new(19, -6), new(15, 18), new(0, 27), new(-15, 18), new(-19, -6)]);
        DrawToken(pts, style.Dog, style.Ink, style.Background, style.Dark ? 0.44f : 0.28f, 2.4f);
        DrawLine(c + new Vector2(-8, -4), c + new Vector2(8, -4), new Color("#f5f0df", 0.50f), 2.2f, true);
        DrawLine(c + new Vector2(0, -14), c + new Vector2(0, 16), new Color(style.Dog, 0.64f), 2.2f, true);
    }

    private void DrawDogTank(Vector2 c, StyleSpec style)
    {
        var pts = Points(c, [new(-38, -16), new(22, -16), new(38, -4), new(38, 7), new(20, 17), new(-36, 17), new(-48, 6), new(-48, -7)]);
        DrawToken(pts, style.Dog, style.Ink, style.Background, style.Dark ? 0.48f : 0.25f, 2.6f);
        DrawCircle(c, 12, new Color(style.Dog, style.Dark ? 0.20f : 0.16f));
        DrawCircle(c, 12, new Color(style.Dog, 0.84f), false, 2.1f, true);
        DrawLine(c + new Vector2(6, 0), c + new Vector2(52, 0), new Color(style.Dog, 0.90f), 5.4f, true);
        DrawLine(c + new Vector2(8, 0), c + new Vector2(52, 0), new Color("#fff2d9", 0.50f), 1.3f, true);
    }

    private void DrawCatUnit(Vector2 c, StyleSpec style)
    {
        var pts = Points(c, [new(29, 0), new(-13, -23), new(-4, 0), new(-13, 23)]);
        DrawToken(pts, style.Cat, style.Ink, style.Background, style.Dark ? 0.42f : 0.22f, 2.4f);
        DrawLine(c + new Vector2(-4, 0), c + new Vector2(25, 0), new Color(style.Cat, 0.80f), 2.1f, true);
        DrawArc(c, 25, -0.65f, 0.65f, 24, new Color(style.Cat, style.Dark ? 0.48f : 0.32f), 1.7f, true);
    }

    private void DrawAiNode(Vector2 c, StyleSpec style)
    {
        var pts = Points(c, [new(20, -8), new(9, -20), new(-18, -12), new(-22, 8), new(-3, 20), new(22, 8)]);
        DrawToken(pts, style.Ai, style.Ink, style.Background, style.Dark ? 0.44f : 0.22f, 2.3f);
        DrawLine(c + new Vector2(-14, -14), c + new Vector2(15, 15), new Color(style.Ai, 0.72f), 1.7f, true);
        DrawLine(c + new Vector2(-8, 15), c + new Vector2(12, -15), new Color(style.Ai, 0.52f), 1.2f, true);
    }

    private void DrawHud(Rect2 rect, StyleSpec style)
    {
        var hudAlpha = style.Dark ? 0.62f : 0.58f;
        var hud = new Rect2(rect.Position + new Vector2(12, rect.Size.Y - 34), new Vector2(rect.Size.X * 0.58f, 22));
        DrawRect(hud, new Color(style.Background.Lerp(style.Ink, style.Dark ? 0.18f : 0.08f), hudAlpha));
        DrawRect(hud, new Color(style.Ink, style.Dark ? 0.34f : 0.18f), false, 1f);
        DrawString(ThemeDB.FallbackFont, hud.Position + new Vector2(10, 16), "edge HUD / low obstruction", HorizontalAlignment.Left, hud.Size.X - 20, 10, new Color(style.Ink, style.Dark ? 0.76f : 0.58f));

        var mini = new Rect2(rect.End.X - 90, rect.End.Y - 86, 70, 64);
        DrawRect(mini, new Color(style.Ink, style.Dark ? 0.13f : 0.07f));
        DrawRect(mini, new Color(style.Ink, style.Dark ? 0.34f : 0.18f), false, 1f);
        DrawCircle(mini.Position + new Vector2(18, 42), 3, style.Dog);
        DrawCircle(mini.Position + new Vector2(34, 26), 3, style.Cat);
        DrawCircle(mini.Position + new Vector2(54, 18), 3, style.Ai);
    }

    private void DrawToken(Vector2[] points, Color accent, Color ink, Color background, float fill, float width)
    {
        DrawColoredPolygon(Offset(points, new Vector2(3, 5)), new Color("#120d08", 0.13f));
        DrawColoredPolygon(points, new Color(background.Lerp(accent, fill), 0.96f));
        DrawPolyline(Close(points), new Color(ink.Lerp(accent, 0.24f), 0.92f), width, true);
        DrawPolyline(Close(ScalePolygon(points, 0.72f)), new Color("#fff4df", 0.34f), 1.1f, true);
        DrawPolyline(Close(ScalePolygon(points, 0.88f)), new Color(accent, 0.32f), 1.2f, true);
    }

    private void DrawDashedLine(Vector2 from, Vector2 to, Color color, float width, float dash, float gap)
    {
        var delta = to - from;
        var length = delta.Length();
        if (length <= 0.01f)
        {
            return;
        }

        var dir = delta / length;
        for (var distance = 0f; distance < length; distance += dash + gap)
        {
            DrawLine(from + dir * distance, from + dir * Mathf.Min(distance + dash, length), color, width, true);
        }
    }

    private static Vector2[] Points(Vector2 center, Vector2[] local)
    {
        var points = new Vector2[local.Length];
        for (var i = 0; i < local.Length; i++)
        {
            points[i] = center + local[i];
        }

        return points;
    }

    private static Vector2[] Offset(Vector2[] points, Vector2 offset)
    {
        var result = new Vector2[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            result[i] = points[i] + offset;
        }

        return result;
    }

    private static Vector2[] Close(Vector2[] points)
    {
        var closed = new Vector2[points.Length + 1];
        Array.Copy(points, closed, points.Length);
        closed[^1] = points[0];
        return closed;
    }

    private static Vector2[] ScalePolygon(Vector2[] points, float scale)
    {
        var center = Vector2.Zero;
        foreach (var point in points)
        {
            center += point;
        }

        center /= points.Length;
        var scaled = new Vector2[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            scaled[i] = center + (points[i] - center) * scale;
        }

        return scaled;
    }

    private void SaveCapture()
    {
        var image = GetViewport().GetTexture().GetImage();
        var absolutePath = ProjectSettings.GlobalizePath($"res://{CapturePath}");
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        var error = image.SavePng(absolutePath);
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"Failed to save overall style screenshot: {error}");
        }

        GD.Print($"Overall style screenshot saved to {absolutePath}");
    }

    private async Task NextFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private sealed record StyleSpec(
        string Tag,
        string Name,
        string Note,
        Color Background,
        Color Grid,
        Color Major,
        Color Ink,
        Color Dog,
        Color Cat,
        Color Ai,
        float Haze,
        float CommandAlpha,
        bool Dark);
}
