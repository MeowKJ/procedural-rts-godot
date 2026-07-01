using Godot;

namespace ProceduralRts;

public partial class UnitShowcaseRoot : Control
{
    private const string CapturePath = "artifacts/unit-showcase-godot.png";
    private const float RedrawIntervalSeconds = 1f / 20f;
    private static readonly Vector2I ShowcaseSize = new(1600, 900);
    private static readonly Color Paper = new("#eadfce");
    private static readonly Color Ink = new("#242b30");
    private static readonly Color InkSoft = new("#687071");
    private static readonly Color Dog = new("#c47719");
    private static readonly Color DogDark = new("#704217");
    private static readonly Color Cat = new("#4f409b");
    private static readonly Color CatDark = new("#2f2a67");
    private static readonly Color Blue = new("#3d7184");
    private static readonly Color Red = new("#9b284c");

    private float _elapsed;
    private float _redrawTimer;

    public override async void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        FocusMode = FocusModeEnum.All;
        DisplayServer.WindowSetSize(ShowcaseSize);

        if (OS.GetEnvironment("UNIT_SHOWCASE_CAPTURE") == "1")
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
        HandleUnhandledInput(@event);
    }

    public override void _Draw()
    {
        var size = Size;
        if (size.X < 10 || size.Y < 10)
        {
            size = new Vector2(ShowcaseSize.X, ShowcaseSize.Y);
        }

        DrawBackground(size);
        DrawHeader(size);

        var left = new Rect2(36, 120, (size.X - 108) * 0.5f, size.Y - 156);
        var right = new Rect2(left.End.X + 36, 120, left.Size.X, left.Size.Y);
        DrawFaction(left, DogFaction(), Dog, DogDark);
        DrawFaction(right, CatFaction(), Cat, CatDark);
    }
}
