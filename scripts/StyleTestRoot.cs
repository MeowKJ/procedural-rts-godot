using Godot;

namespace ProceduralRts;

public partial class StyleTestRoot : Control
{
    private const string CaptureArg = "--capture-style-test";
    private const string CapturePath = "artifacts/style-tests-godot.png";
    private const float RedrawIntervalSeconds = 1f / 20f;
    private static readonly Vector2I CaptureSize = new(1600, 900);
    private static readonly Color Paper = new("#eee7da");
    private static readonly Color Ink = new("#27313a");
    private static readonly Color MutedInk = new("#68717a");

    private float _elapsed;
    private float _redrawTimer;

    public override async void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        FocusMode = FocusModeEnum.All;

        if (!ShouldCapture())
        {
            return;
        }

        DisplayServer.WindowSetSize(CaptureSize);
        await NextFrames(8);
        SaveCapture();
        GetTree().Quit();
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
        if (size.X <= 1 || size.Y <= 1)
        {
            size = new Vector2(CaptureSize.X, CaptureSize.Y);
        }

        DrawRect(new Rect2(Vector2.Zero, size), Paper);
        DrawHeader(size);

        var panels = CreateStyleSpecs();
        for (var index = 0; index < panels.Length; index++)
        {
            DrawPanel(PanelRect(size, index), panels[index]);
        }
    }
}
