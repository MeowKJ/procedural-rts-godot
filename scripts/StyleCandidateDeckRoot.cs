using Godot;
using System.IO;

namespace ProceduralRts;

public partial class StyleCandidateDeckRoot : Control
{
    private const string CapturePath = "artifacts/style-candidate-deck-godot.png";
    private const float RedrawIntervalSeconds = 1f / 20f;
    private static readonly Vector2I CaptureSize = new(1600, 900);
    private static readonly Color Page = new("#d7c1a0");
    private static readonly Color Ink = new("#24282a");
    private static readonly Color Muted = new("#666963");

    private int _selected;
    private float _elapsed;
    private float _redrawTimer;

    public override async void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        FocusMode = FocusModeEnum.All;
        DisplayServer.WindowSetSize(CaptureSize);

        var selected = OS.GetEnvironment("STYLE_CANDIDATE_INDEX");
        if (int.TryParse(selected, out var index))
        {
            _selected = Mathf.Clamp(index, 0, Families().Length - 1);
        }

        if (OS.GetEnvironment("STYLE_CANDIDATE_CAPTURE") == "1")
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
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        HandleNavigationKey(key.Keycode);
    }

    public override void _Draw()
    {
        var size = Size;
        if (size.X < 10 || size.Y < 10)
        {
            size = new Vector2(CaptureSize.X, CaptureSize.Y);
        }

        var families = Families();
        var family = families[_selected];

        DrawRect(new Rect2(Vector2.Zero, size), Page);
        DrawHeader(size, families, family);
        DrawFamily(family, new Rect2(34, 172, size.X - 68, size.Y - 206));
    }

    private void SaveCapture()
    {
        var image = GetViewport().GetTexture().GetImage();
        var absolutePath = ProjectSettings.GlobalizePath($"res://{CapturePath}");
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        var error = image.SavePng(absolutePath);
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"Failed to save style candidate screenshot: {error}");
        }

        GD.Print($"Style candidate screenshot saved to {absolutePath}");
    }

    private async Task NextFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
}
