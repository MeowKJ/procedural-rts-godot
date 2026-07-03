using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class OutcomeScreenLayer : CanvasLayer
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
    private OutcomeBackdrop _backdrop = null!;
    private Label _title = null!;
    private Label _subtitle = null!;
    private Label _detail = null!;
    private Label _status = null!;
    private Button _restart = null!;
    private float _elapsed;
    private GameOutcome _outcome = GameOutcome.InProgress;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 55;

        _root = new Control
        {
            Name = "OutcomeRoot",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop,
            ProcessMode = ProcessModeEnum.Always,
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _backdrop = new OutcomeBackdrop { Name = "Backdrop" };
        _backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(_backdrop);

        BuildOutcomePanel();
    }

    public override void _Process(double delta)
    {
        if (!_root.Visible)
        {
            return;
        }

        _elapsed += (float)delta;
        _backdrop.Elapsed = _elapsed;
        _backdrop.Outcome = _outcome;
        _backdrop.QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_root.Visible || @event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        if (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter)
        {
            RestartBattle();
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Escape)
        {
            ReturnToMainMenu();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _ExitTree()
    {
        if (GetTree() is { } tree)
        {
            tree.Paused = false;
        }
    }

    public void ShowOutcome(GameOutcome outcome, string detail)
    {
        if (outcome == GameOutcome.InProgress)
        {
            return;
        }

        _outcome = outcome;
        _root.Visible = true;
        GetTree().Paused = true;

        var victory = outcome == GameOutcome.Victory;
        _title.Text = victory ? GameText.T("ui.outcome.victory") : GameText.T("ui.outcome.defeat");
        _title.LabelSettings.FontColor = victory ? Mint : Danger;
        _subtitle.Text = victory ? GameText.T("outcome.subtitle.victory") : GameText.T("outcome.subtitle.defeat");
        _detail.Text = detail;
        _restart.Text = victory ? GameText.T("outcome.playAgain") : GameText.T("outcome.retry");
        _status.Text = GameText.T("outcome.hint");
    }

    private void BuildOutcomePanel()
    {
        var panel = UiFactory.MakePanel("Panel", new Color("#071019", 0.97f), new Color("#59f1ff", 0.58f));
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.OffsetLeft = -278;
        panel.OffsetTop = -218;
        panel.OffsetRight = 278;
        panel.OffsetBottom = 218;
        _root.AddChild(panel);

        _title = UiFactory.MakeLabel(GameText.T("ui.outcome.victory"), 40, Mint, 0.80f);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        _title.Position = new Vector2(28, 30);
        _title.CustomMinimumSize = new Vector2(500, 54);
        panel.AddChild(_title);

        _subtitle = UiFactory.MakeLabel(GameText.T("outcome.subtitle.victory"), 13, InkMuted, 0.80f);
        _subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        _subtitle.Position = new Vector2(28, 88);
        _subtitle.CustomMinimumSize = new Vector2(500, 22);
        panel.AddChild(_subtitle);

        _detail = UiFactory.MakeLabel(GameText.T("ui.outcome.enemyHqDestroyed"), 15, Ink, 0.80f);
        _detail.HorizontalAlignment = HorizontalAlignment.Center;
        _detail.Position = new Vector2(28, 130);
        _detail.CustomMinimumSize = new Vector2(500, 28);
        panel.AddChild(_detail);

        var telemetry = new OutcomeTelemetry { Name = "Telemetry" };
        telemetry.Position = new Vector2(48, 176);
        telemetry.CustomMinimumSize = new Vector2(460, 68);
        panel.AddChild(telemetry);

        _restart = UiFactory.MakeButton(GameText.T("outcome.playAgain"), Cyan);
        _restart.Position = new Vector2(86, 276);
        _restart.CustomMinimumSize = new Vector2(384, 46);
        _restart.Pressed += RestartBattle;
        panel.AddChild(_restart);

        var menu = UiFactory.MakeButton(GameText.T("common.mainMenu"), Amber);
        menu.Position = new Vector2(86, 332);
        menu.CustomMinimumSize = new Vector2(184, 42);
        menu.Pressed += ReturnToMainMenu;
        panel.AddChild(menu);

        var quit = UiFactory.MakeButton(GameText.T("common.quit"), Danger);
        quit.Position = new Vector2(286, 332);
        quit.CustomMinimumSize = new Vector2(184, 42);
        quit.Pressed += QuitGame;
        panel.AddChild(quit);

        _status = UiFactory.MakeLabel(GameText.T("outcome.hint"), 11, InkMuted, 0.80f);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.Position = new Vector2(28, 394);
        _status.CustomMinimumSize = new Vector2(500, 20);
        panel.AddChild(_status);
    }

    private void RestartBattle()
    {
        ChangeScene(BattleScenePath, GameText.T("outcome.restarting"));
    }

    private void ReturnToMainMenu()
    {
        ChangeScene(MainMenuScenePath, GameText.T("outcome.returningMenu"));
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

    private partial class OutcomeBackdrop : Control
    {
        public float Elapsed { get; set; }
        public GameOutcome Outcome { get; set; } = GameOutcome.InProgress;

        public override void _Draw()
        {
            var accent = Outcome == GameOutcome.Defeat ? Danger : Mint;
            DrawRect(new Rect2(Vector2.Zero, Size), new Color("#02060a", 0.84f));
            DrawGrid(accent);
            DrawPulse(accent);
        }

        private void DrawGrid(Color accent)
        {
            var offset = (Elapsed * 12) % 48;
            for (var x = -48 + offset; x <= Size.X + 48; x += 48)
            {
                DrawLine(new Vector2(x, 0), new Vector2(x, Size.Y), new Color(accent, 0.055f), 1, true);
            }

            for (var y = -48 + offset * 0.5f; y <= Size.Y + 48; y += 48)
            {
                DrawLine(new Vector2(0, y), new Vector2(Size.X, y), new Color("#59f1ff", 0.045f), 1, true);
            }
        }

        private void DrawPulse(Color accent)
        {
            var center = Size * 0.5f;
            var pulse = 0.5f + Mathf.Sin(Elapsed * 2.6f) * 0.5f;
            for (var index = 0; index < 5; index++)
            {
                var radius = 150 + index * 62 + pulse * 12;
                DrawArc(center, radius, 0, Mathf.Tau, 144, new Color(accent, 0.18f - index * 0.024f), 2, true);
            }

            DrawLine(center + new Vector2(-350, -210), center + new Vector2(-260, -150), new Color(Amber, 0.28f), 2, true);
            DrawLine(center + new Vector2(350, 210), center + new Vector2(260, 150), new Color(Amber, 0.28f), 2, true);
            DrawLine(center + new Vector2(350, -210), center + new Vector2(260, -150), new Color(Cyan, 0.25f), 2, true);
            DrawLine(center + new Vector2(-350, 210), center + new Vector2(-260, 150), new Color(Cyan, 0.25f), 2, true);
        }
    }

    private partial class OutcomeTelemetry : Control
    {
        public override void _Draw()
        {
            var labels = new[]
            {
                GameText.T("outcome.telemetry.command"),
                GameText.T("outcome.telemetry.economy"),
                GameText.T("outcome.telemetry.tactics"),
            };
            var colors = new[] { Cyan, Amber, Mint };
            var segmentWidth = Size.X / labels.Length;
            for (var index = 0; index < labels.Length; index++)
            {
                var rect = new Rect2(index * segmentWidth + 4, 0, segmentWidth - 8, Size.Y);
                DrawRect(rect, new Color("#02060a", 0.55f));
                DrawRect(rect, new Color(colors[index], 0.36f), false, 1);
                DrawString(UiFontProfile.DrawFont(UiFontRole.Body), rect.Position + new Vector2(12, 22), labels[index], HorizontalAlignment.Left, rect.Size.X - 24, 12, new Color(colors[index], 0.95f));
                DrawString(UiFontProfile.DrawFont(UiFontRole.Compact), rect.Position + new Vector2(12, 46), GameText.T("outcome.telemetry.resolved"), HorizontalAlignment.Left, rect.Size.X - 24, 11, InkMuted);
            }
        }
    }
}
