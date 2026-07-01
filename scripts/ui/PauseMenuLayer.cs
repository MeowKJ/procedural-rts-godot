using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class PauseMenuLayer : CanvasLayer
{
    private const string BattleScenePath = "res://scenes/Battle.tscn";
    private const string MainMenuScenePath = "res://scenes/MainMenu.tscn";

    private static readonly Color Ink = new("#d8f7ff");
    private static readonly Color InkMuted = new("#8095aa");
    private static readonly Color Cyan = new("#59f1ff");
    private static readonly Color Mint = new("#8fffe1");
    private static readonly Color Amber = new("#f6c55c");
    private static readonly Color Danger = new("#ff5d75");

    private Control _root = null!;
    private Label _status = null!;
    private PauseBackdrop _backdrop = null!;
    private SettingsOverlayLayer _settings = null!;
    private float _elapsed;
    public bool InputEnabled { get; set; } = true;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 40;

        _root = new Control
        {
            Name = "PauseRoot",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop,
            ProcessMode = ProcessModeEnum.Always,
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _backdrop = new PauseBackdrop { Name = "Backdrop" };
        _backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(_backdrop);

        BuildPausePanel();

        _settings = new SettingsOverlayLayer { Name = "Settings" };
        AddChild(_settings);
    }

    public override void _Process(double delta)
    {
        if (!_root.Visible)
        {
            return;
        }

        _elapsed += (float)delta;
        _backdrop.Elapsed = _elapsed;
        _backdrop.QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!InputEnabled || _settings.IsOpen || @event is not InputEventKey key || !key.Pressed || key.Echo || key.Keycode != Key.Escape)
        {
            return;
        }

        SetPaused(!_root.Visible);
        GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        if (GetTree() is { } tree)
        {
            tree.Paused = false;
        }
    }

    private void BuildPausePanel()
    {
        var panel = UiFactory.MakePanel("Panel", new Color("#071019", 0.96f), new Color("#59f1ff", 0.56f));
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.OffsetLeft = -220;
        panel.OffsetTop = -226;
        panel.OffsetRight = 220;
        panel.OffsetBottom = 226;
        _root.AddChild(panel);

        var title = UiFactory.MakeLabel(GameText.T("pause.title"), 24, Ink);
        title.Position = new Vector2(28, 24);
        title.CustomMinimumSize = new Vector2(330, 34);
        panel.AddChild(title);

        var meta = UiFactory.MakeLabel(GameText.T("pause.meta"), 12, InkMuted);
        meta.Position = new Vector2(30, 62);
        meta.CustomMinimumSize = new Vector2(320, 22);
        panel.AddChild(meta);

        var resume = UiFactory.MakeButton(GameText.T("pause.resume"), Mint);
        resume.Position = new Vector2(28, 112);
        resume.CustomMinimumSize = new Vector2(384, 48);
        resume.Pressed += () => SetPaused(false);
        panel.AddChild(resume);

        var restart = UiFactory.MakeButton(GameText.T("pause.restart"), Cyan);
        restart.Position = new Vector2(28, 172);
        restart.CustomMinimumSize = new Vector2(384, 48);
        restart.Pressed += RestartBattle;
        panel.AddChild(restart);

        var settings = UiFactory.MakeButton(GameText.T("common.settings"), Amber);
        settings.Position = new Vector2(28, 232);
        settings.CustomMinimumSize = new Vector2(384, 48);
        settings.Pressed += OpenSettings;
        panel.AddChild(settings);

        var menu = UiFactory.MakeButton(GameText.T("common.mainMenu"), Amber);
        menu.Position = new Vector2(28, 292);
        menu.CustomMinimumSize = new Vector2(384, 48);
        menu.Pressed += ReturnToMainMenu;
        panel.AddChild(menu);

        var quit = UiFactory.MakeButton(GameText.T("common.quit"), Danger);
        quit.Position = new Vector2(28, 352);
        quit.CustomMinimumSize = new Vector2(384, 44);
        quit.Pressed += QuitGame;
        panel.AddChild(quit);

        _status = UiFactory.MakeLabel(GameText.T("pause.hint"), 12, InkMuted);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.Position = new Vector2(28, 414);
        _status.CustomMinimumSize = new Vector2(384, 22);
        panel.AddChild(_status);
    }

    public void SetPaused(bool paused)
    {
        _root.Visible = paused;
        GetTree().Paused = paused;
        _status.Text = paused ? GameText.T("pause.hint") : GameText.T("pause.resumed");
    }

    private void RestartBattle()
    {
        ChangeScene(BattleScenePath, GameText.T("pause.restarting"));
    }

    public void OpenSettings()
    {
        _settings.Open();
        _status.Text = GameText.T("pause.status.settingsOpen");
    }

    private void ReturnToMainMenu()
    {
        ChangeScene(MainMenuScenePath, GameText.T("pause.returningMenu"));
    }

    private void ChangeScene(string scenePath, string status)
    {
        _status.Text = status;
        GetTree().Paused = false;
        var error = GetTree().ChangeSceneToFile(scenePath);
        if (error != Error.Ok)
        {
            _root.Visible = true;
            GetTree().Paused = true;
            _status.Text = GameText.Format("common.sceneLoadFailed", error);
        }
    }

    private void QuitGame()
    {
        GetTree().Paused = false;
        GetTree().Quit();
    }

    private partial class PauseBackdrop : Control
    {
        public float Elapsed { get; set; }

        public override void _Draw()
        {
            DrawRect(new Rect2(Vector2.Zero, Size), new Color("#02060a", 0.78f));
            DrawScanlines();
            DrawReticle();
        }

        private void DrawScanlines()
        {
            var phase = (Elapsed * 16) % 12;
            for (var y = -12 + phase; y <= Size.Y + 12; y += 12)
            {
                DrawLine(new Vector2(0, y), new Vector2(Size.X, y), new Color("#59f1ff", 0.045f), 1, true);
            }
        }

        private void DrawReticle()
        {
            var center = Size * 0.5f;
            var pulse = 0.5f + Mathf.Sin(Elapsed * 2.4f) * 0.5f;
            DrawArc(center, 194, 0, Mathf.Tau, 128, new Color(Cyan, 0.18f), 2, true);
            DrawArc(center, 236 + pulse * 8, 0, Mathf.Tau, 128, new Color(Mint, 0.12f), 1.4f, true);
            DrawLine(center + new Vector2(-290, 0), center + new Vector2(-236, 0), new Color(Amber, 0.30f), 2, true);
            DrawLine(center + new Vector2(236, 0), center + new Vector2(290, 0), new Color(Amber, 0.30f), 2, true);
            DrawLine(center + new Vector2(0, -254), center + new Vector2(0, -206), new Color(Amber, 0.30f), 2, true);
            DrawLine(center + new Vector2(0, 206), center + new Vector2(0, 254), new Color(Amber, 0.30f), 2, true);
        }
    }
}
