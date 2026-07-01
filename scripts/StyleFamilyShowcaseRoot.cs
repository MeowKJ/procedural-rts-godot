using Godot;
using System.IO;

namespace ProceduralRts;

public partial class StyleFamilyShowcaseRoot : Control
{
    private const string CapturePath = "artifacts/style-family-showcase-godot.png";
    private const float RedrawIntervalSeconds = 1f / 20f;
    private static readonly Vector2I CaptureSize = new(1600, 900);
    private static readonly Color Page = new("#d9c4a4");
    private static readonly Color Ink = new("#25282a");
    private static readonly Color Muted = new("#626763");

    private float _elapsed;
    private float _redrawTimer;

    public override async void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        FocusMode = FocusModeEnum.All;
        DisplayServer.WindowSetSize(CaptureSize);

        if (OS.GetEnvironment("STYLE_FAMILY_CAPTURE") == "1")
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

        DrawRect(new Rect2(Vector2.Zero, size), Page);
        DrawHeader(size);

        var styles = BuildStyles();
        var margin = 34f;
        var top = 124f;
        var gap = 14f;
        var columns = 3;
        var rows = 4;
        var cellWidth = (size.X - margin * 2 - gap * (columns - 1)) / columns;
        var cellHeight = (size.Y - top - 28 - gap * (rows - 1)) / rows;

        for (var i = 0; i < styles.Length; i++)
        {
            var col = i % columns;
            var row = i / columns;
            var rect = new Rect2(
                margin + col * (cellWidth + gap),
                top + row * (cellHeight + gap),
                cellWidth,
                cellHeight);
            DrawStyleCell(rect, styles[i]);
        }
    }

    private static StyleSpec[] BuildStyles()
    {
        return
        [
            new("STYLE 1A", "Soft Old City", "default day", new Color("#eadbc4"), new Color("#bca98d", 0.28f), new Color("#786d5e", 0.26f), new Color("#2b3032"), new Color("#c47719"), new Color("#50439c"), new Color("#a83255"), 0.02f, 0.68f, false, true),
            new("STYLE 1B", "Soft Old City", "fog exploration", new Color("#d9d6c9"), new Color("#8d9488", 0.23f), new Color("#687067", 0.22f), new Color("#303938"), new Color("#b77d2d"), new Color("#5d58a2"), new Color("#93415e"), 0.16f, 0.56f, false, true),
            new("STYLE 1C", "Soft Old City", "dusk defense", new Color("#293234"), new Color("#e7c982", 0.10f), new Color("#d8912f", 0.18f), new Color("#eef1ec"), new Color("#f0a33c"), new Color("#8f82ff"), new Color("#ff5578"), 0.05f, 0.90f, true, true),

            new("STYLE 2A", "Porcelain Table", "clean default", new Color("#f2eee7"), new Color("#c8c6c1", 0.30f), new Color("#9a9690", 0.22f), new Color("#252b2d"), new Color("#bc7424"), new Color("#5b4aaa"), new Color("#a03450"), 0.00f, 0.58f, false, false),
            new("STYLE 2B", "Porcelain Table", "cool command", new Color("#e8edf0"), new Color("#aab6bb", 0.24f), new Color("#76898f", 0.22f), new Color("#243039"), new Color("#b8732b"), new Color("#4c5cb2"), new Color("#9e3659"), 0.06f, 0.62f, false, false),
            new("STYLE 2C", "Porcelain Table", "alarm overlay", new Color("#ebe7df"), new Color("#bdb7ab", 0.22f), new Color("#a75c6d", 0.20f), new Color("#2d2d2e"), new Color("#c47a24"), new Color("#604fb0"), new Color("#c63358"), 0.02f, 0.80f, false, false),

            new("STYLE 3A", "Archive Map", "paper campaign", new Color("#dfccaa"), new Color("#8b7355", 0.24f), new Color("#684f33", 0.20f), new Color("#352920"), new Color("#a75d19"), new Color("#4b3d7e"), new Color("#8d2941"), 0.04f, 0.56f, false, true),
            new("STYLE 3B", "Archive Map", "ink scouting", new Color("#d4c4a6"), new Color("#5e5140", 0.20f), new Color("#473a2d", 0.18f), new Color("#28231f"), new Color("#9c651e"), new Color("#3d427d"), new Color("#813846"), 0.12f, 0.52f, false, true),
            new("STYLE 3C", "Archive Map", "burnt front", new Color("#b9a285"), new Color("#4d4034", 0.18f), new Color("#7f3428", 0.20f), new Color("#241f1c"), new Color("#bd6a1c"), new Color("#554185"), new Color("#ae2f45"), 0.06f, 0.72f, false, true),

            new("STYLE 4A", "Repair Blueprint", "base building", new Color("#dce6e4"), new Color("#71949a", 0.24f), new Color("#3c6973", 0.22f), new Color("#1f3135"), new Color("#bc7b2a"), new Color("#4c57a8"), new Color("#a52d55"), 0.03f, 0.64f, false, false),
            new("STYLE 4B", "Repair Blueprint", "signal restored", new Color("#d6e2dc"), new Color("#759b8b", 0.24f), new Color("#487866", 0.20f), new Color("#22342f"), new Color("#d08c2b"), new Color("#5160aa"), new Color("#9b3353"), 0.04f, 0.76f, false, false),
            new("STYLE 4C", "Repair Blueprint", "system crisis", new Color("#16242a"), new Color("#78b6c0", 0.11f), new Color("#e14b70", 0.18f), new Color("#edf4f2"), new Color("#ffbd55"), new Color("#8d8cff"), new Color("#ff3f69"), 0.07f, 0.96f, true, false),
        ];
    }

    private void DrawHeader(Vector2 size)
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(34, 46), "RTS Visual Style Families", HorizontalAlignment.Left, 620, 30, Ink);
        DrawString(
            ThemeDB.FallbackFont,
            new Vector2(36, 80),
            "Pick one main family first. Each family includes default, exploration/support, and pressure/crisis variants using the same unit and UI language.",
            HorizontalAlignment.Left,
            size.X - 72,
            15,
            Muted);
    }

    private void DrawStyleCell(Rect2 rect, StyleSpec style)
    {
        DrawRect(rect, style.Background);
        DrawRect(rect, new Color(style.Ink, style.Dark ? 0.42f : 0.22f), false, 1.2f);
        DrawTerrain(rect, style);
        DrawBattleSample(rect, style);
        DrawHud(rect, style);
        DrawLabels(rect, style);
    }

    private void DrawLabels(Rect2 rect, StyleSpec style)
    {
        var tag = new Rect2(rect.Position + new Vector2(12, 10), new Vector2(74, 24));
        DrawRect(tag, new Color(style.Ink, style.Dark ? 0.18f : 0.08f));
        DrawRect(tag, new Color(style.Ink, 0.28f), false, 1f);
        DrawString(ThemeDB.FallbackFont, tag.Position + new Vector2(8, 17), style.Code, HorizontalAlignment.Left, 62, 12, style.Ink);
        DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(98, 28), style.Family, HorizontalAlignment.Left, rect.Size.X - 116, 16, style.Ink);
        DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(98, 48), style.Variant, HorizontalAlignment.Left, rect.Size.X - 116, 11, new Color(style.Ink, style.Dark ? 0.64f : 0.56f));
    }

    private void DrawTerrain(Rect2 rect, StyleSpec style)
    {
        for (var x = rect.Position.X + 18; x < rect.End.X; x += 26)
        {
            DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), style.Grid, 0.75f, true);
        }

        for (var y = rect.Position.Y + 16; y < rect.End.Y; y += 26)
        {
            DrawLine(new Vector2(rect.Position.X, y), new Vector2(rect.End.X, y), style.Grid, 0.75f, true);
        }

        for (var x = rect.Position.X + 70; x < rect.End.X; x += 104)
        {
            DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), style.Major, 1.05f, true);
        }

        var roadWidth = style.PaperTexture ? 10f : 7f;
        var a = rect.Position + new Vector2(24, rect.Size.Y * 0.75f);
        var b = rect.Position + new Vector2(rect.Size.X * 0.47f, rect.Size.Y * 0.62f);
        var c = rect.Position + new Vector2(rect.Size.X - 32, rect.Size.Y * 0.49f);
        DrawLine(a, b, new Color(style.Dog, style.Dark ? 0.28f : 0.17f), roadWidth, true);
        DrawLine(b, c, new Color(style.Dog, style.Dark ? 0.28f : 0.17f), roadWidth, true);

        if (style.PaperTexture)
        {
            for (var i = 0; i < 5; i++)
            {
                var y = rect.Position.Y + 34 + i * 28;
                DrawLine(new Vector2(rect.Position.X + 18, y), new Vector2(rect.End.X - 20, y + 12), new Color(style.Ink, style.Dark ? 0.06f : 0.045f), 4f, true);
            }
        }

        if (style.Haze > 0)
        {
            DrawRect(rect, new Color("#fff6e6", style.Haze));
        }
    }

    private void DrawBattleSample(Rect2 rect, StyleSpec style)
    {
        var dog = rect.Position + new Vector2(rect.Size.X * 0.30f, rect.Size.Y * 0.64f);
        var cat = rect.Position + new Vector2(rect.Size.X * 0.50f, rect.Size.Y * 0.40f);
        var ai = rect.Position + new Vector2(rect.Size.X * 0.73f, rect.Size.Y * 0.63f);
        var target = rect.Position + new Vector2(rect.Size.X * 0.60f, rect.Size.Y * 0.48f);

        DrawRect(new Rect2(dog.X - 66, dog.Y - 38, 130, 76), new Color(style.Dog, style.Dark ? 0.10f : 0.06f));
        DrawRect(new Rect2(dog.X - 66, dog.Y - 38, 130, 76), new Color(style.Dog, style.Dark ? 0.78f : 0.54f), false, 1.5f);
        DrawLine(dog + new Vector2(18, -4), target, new Color(style.Dog, style.CommandAlpha), 2.4f, true);
        DrawCircle(target, 13, new Color(style.Dog, 0.12f), false, 2f, true);
        DrawCircle(target, 4, new Color(style.Dog, 0.85f));

        DrawDogUnit(dog + new Vector2(-30, -6), style);
        DrawTank(dog + new Vector2(26, 6), style);
        DrawCatUnit(cat + new Vector2(-16, -2), style);
        DrawCatUnit(cat + new Vector2(34, 14), style);
        DrawAiNode(ai, style);

        DrawArc(ai, rect.Size.Y * 0.22f, 0, Mathf.Tau, 72, new Color(style.Ai, style.Dark ? 0.42f : 0.22f), 1.9f, true);
        DrawArc(ai, rect.Size.Y * 0.32f, 0, Mathf.Tau, 96, new Color(style.Ai, style.Dark ? 0.24f : 0.12f), 1.25f, true);
        DrawDashedLine(
            rect.Position + new Vector2(rect.Size.X * 0.22f, rect.Size.Y * 0.33f),
            rect.Position + new Vector2(rect.Size.X * 0.75f, rect.Size.Y * 0.42f),
            new Color(style.Cat, style.Dark ? 0.46f : 0.28f),
            2f,
            9,
            7);
    }

    private void DrawDogUnit(Vector2 c, StyleSpec style)
    {
        var points = Points(c, [new(0, -19), new(17, -5), new(13, 16), new(0, 23), new(-13, 16), new(-17, -5)]);
        DrawToken(points, style.Dog, style);
        DrawLine(c + new Vector2(-7, -4), c + new Vector2(7, -4), new Color("#fff2d8", 0.48f), 2f, true);
        DrawLine(c + new Vector2(0, -13), c + new Vector2(0, 14), new Color(style.Dog, 0.66f), 2f, true);
    }

    private void DrawTank(Vector2 c, StyleSpec style)
    {
        var points = Points(c, [new(-34, -14), new(21, -14), new(35, -4), new(35, 7), new(18, 15), new(-33, 15), new(-43, 5), new(-43, -6)]);
        DrawToken(points, style.Dog, style);
        DrawCircle(c, 10, new Color(style.Dog, style.Dark ? 0.22f : 0.15f));
        DrawCircle(c, 10, new Color(style.Dog, 0.82f), false, 1.9f, true);
        DrawLine(c + new Vector2(5, 0), c + new Vector2(47, 0), new Color(style.Dog, 0.88f), 4.7f, true);
    }

    private void DrawCatUnit(Vector2 c, StyleSpec style)
    {
        var points = Points(c, [new(26, 0), new(-12, -20), new(-4, 0), new(-12, 20)]);
        DrawToken(points, style.Cat, style);
        DrawLine(c + new Vector2(-4, 0), c + new Vector2(24, 0), new Color(style.Cat, 0.78f), 2f, true);
        DrawArc(c, 22, -0.62f, 0.62f, 20, new Color(style.Cat, style.Dark ? 0.46f : 0.30f), 1.6f, true);
    }

    private void DrawAiNode(Vector2 c, StyleSpec style)
    {
        var points = Points(c, [new(19, -7), new(8, -18), new(-17, -11), new(-21, 7), new(-3, 18), new(21, 7)]);
        DrawToken(points, style.Ai, style);
        DrawLine(c + new Vector2(-13, -13), c + new Vector2(14, 14), new Color(style.Ai, 0.70f), 1.5f, true);
        DrawLine(c + new Vector2(-7, 14), c + new Vector2(11, -14), new Color(style.Ai, 0.50f), 1.1f, true);
    }

    private void DrawHud(Rect2 rect, StyleSpec style)
    {
        var hud = new Rect2(rect.Position + new Vector2(10, rect.Size.Y - 27), new Vector2(rect.Size.X * 0.54f, 18));
        DrawRect(hud, new Color(style.Background.Lerp(style.Ink, style.Dark ? 0.18f : 0.08f), style.Dark ? 0.64f : 0.58f));
        DrawRect(hud, new Color(style.Ink, style.Dark ? 0.34f : 0.16f), false, 1f);
        DrawString(ThemeDB.FallbackFont, hud.Position + new Vector2(8, 13), "edge HUD", HorizontalAlignment.Left, hud.Size.X - 16, 9, new Color(style.Ink, style.Dark ? 0.78f : 0.58f));

        var mini = new Rect2(rect.End.X - 72, rect.End.Y - 64, 54, 48);
        DrawRect(mini, new Color(style.Ink, style.Dark ? 0.12f : 0.065f));
        DrawRect(mini, new Color(style.Ink, style.Dark ? 0.32f : 0.16f), false, 1f);
        DrawCircle(mini.Position + new Vector2(14, 32), 2.5f, style.Dog);
        DrawCircle(mini.Position + new Vector2(28, 20), 2.5f, style.Cat);
        DrawCircle(mini.Position + new Vector2(42, 15), 2.5f, style.Ai);
    }

    private void DrawToken(Vector2[] points, Color accent, StyleSpec style)
    {
        var fill = style.Dark ? 0.44f : 0.24f;
        DrawColoredPolygon(Offset(points, new Vector2(3, 5)), new Color("#120d08", style.Dark ? 0.24f : 0.13f));
        DrawColoredPolygon(points, new Color(style.Background.Lerp(accent, fill), 0.96f));
        DrawPolyline(Close(points), new Color(style.Ink.Lerp(accent, 0.20f), 0.92f), 2.2f, true);
        DrawPolyline(Close(ScalePolygon(points, 0.72f)), new Color("#fff2dd", style.Dark ? 0.20f : 0.34f), 1f, true);
        DrawPolyline(Close(ScalePolygon(points, 0.88f)), new Color(accent, 0.30f), 1.1f, true);
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
            throw new InvalidOperationException($"Failed to save style family screenshot: {error}");
        }

        GD.Print($"Style family screenshot saved to {absolutePath}");
    }

    private async Task NextFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private sealed record StyleSpec(
        string Code,
        string Family,
        string Variant,
        Color Background,
        Color Grid,
        Color Major,
        Color Ink,
        Color Dog,
        Color Cat,
        Color Ai,
        float Haze,
        float CommandAlpha,
        bool Dark,
        bool PaperTexture);
}
