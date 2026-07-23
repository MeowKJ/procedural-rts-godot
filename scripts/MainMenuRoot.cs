using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts;

public partial class MainMenuRoot : Control
{
    private const string BattleScenePath = "res://scenes/Battle.tscn";
    private const float BackdropRedrawIntervalSeconds = 1f / 20f;
    private static readonly Color Ink = new("#d8f7ff");
    private static readonly Color InkMuted = new("#8095aa");
    private static readonly Color Cyan = new("#59f1ff");
    private static readonly Color Mint = new("#8fffe1");
    private static readonly Color Amber = new("#f6c55c");
    private static readonly Color Danger = new("#ff5d75");

    private Label _status = null!;
    private Label _setupSummary = null!;
    private OptionButton _playerFaction = null!;
    private OptionButton _aiFaction = null!;
    private OptionButton _difficulty = null!;
    private SpinBox _startingCredits = null!;
    private SpinBox _mapSeed = null!;
    private MenuBackdrop _backdrop = null!;
    private SettingsOverlayLayer _settings = null!;
    private float _elapsed;
    private float _backdropRedrawTimer;

    public override void _Ready()
    {
        SkirmishSetupState.ClearAuthoredMapHandoff();
        if (TryStartAuthoredMapPreviewFromCommandLine())
            return;
        DisplayAudioSettings.LoadAndApply();

        SetAnchorsPreset(LayoutPreset.FullRect);
        FocusMode = FocusModeEnum.All;

        _backdrop = new MenuBackdrop { Name = "Backdrop" };
        _backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_backdrop);

        BuildHeader();
        BuildMissionPanel();
        BuildCommandSidebar();

        _settings = new SettingsOverlayLayer { Name = "Settings" };
        AddChild(_settings);
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        _backdrop.Elapsed = _elapsed;
        _backdropRedrawTimer -= (float)delta;
        if (_backdropRedrawTimer <= 0)
        {
            _backdropRedrawTimer = BackdropRedrawIntervalSeconds;
            _backdrop.QueueRedraw();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_settings.IsOpen || @event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        if (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter)
        {
            StartSkirmish();
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Escape)
        {
            QuitGame();
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.F5)
        {
            StartSandbox();
            GetViewport().SetInputAsHandled();
        }
    }

}
