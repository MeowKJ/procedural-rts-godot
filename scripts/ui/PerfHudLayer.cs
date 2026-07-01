using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class PerfHudLayer : CanvasLayer
{
    private const float RefreshIntervalSeconds = 0.12f;
    private static readonly Color Ink = new("#d8f7ff");
    private static readonly Color InkMuted = new("#8095aa");
    private static readonly Color Mint = new("#8fffe1");
    private static readonly Color PanelFill = new("#071019", 0.86f);
    private static readonly Color PanelStroke = new("#59f1ff", 0.34f);

    private Control _root = null!;
    private PerfPanel _panel = null!;
    private Label _label = null!;
    private float _refreshTimer;
    private bool _open;

    public Func<PresentationMetricsSnapshot> SnapshotProvider { get; init; } = () => default;
    public Func<PerfHudCounts> CountsProvider { get; init; } = () => default;

    public override void _Ready()
    {
        Layer = 55;
        _open = System.Environment.GetEnvironmentVariable("PROCEDURAL_RTS_PERF_HUD") == "1";

        _root = new Control
        {
            Name = "PerfHudRoot",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _panel = new PerfPanel
        {
            Name = "PerfHudPanel",
            Visible = _open,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _panel.OffsetLeft = 14;
        _panel.OffsetTop = 46;
        _panel.OffsetRight = 334;
        _panel.OffsetBottom = 184;
        _root.AddChild(_panel);

        _label = new Label
        {
            Name = "PerfHudLabel",
            Position = new Vector2(12, 10),
            CustomMinimumSize = new Vector2(294, 116),
            ClipText = true,
            LabelSettings = new LabelSettings
            {
                FontSize = 11,
                FontColor = Ink,
                OutlineColor = new Color("#02060a", 0.92f),
                OutlineSize = 1,
            },
        };
        _panel.AddChild(_label);
        RefreshText();
    }

    public override void _Process(double delta)
    {
        if (!_open)
        {
            return;
        }

        _refreshTimer -= (float)delta;
        if (_refreshTimer > 0)
        {
            return;
        }

        _refreshTimer = RefreshIntervalSeconds;
        RefreshText();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.F3 })
        {
            return;
        }

        _open = !_open;
        _panel.Visible = _open;
        _refreshTimer = 0;
        RefreshText();
        GetViewport().SetInputAsHandled();
    }

    private void RefreshText()
    {
        if (_label is null)
        {
            return;
        }

        var snapshot = SnapshotProvider();
        var counts = CountsProvider();
        var fps = Engine.GetFramesPerSecond();
        _label.Text =
            $"FPS {fps:0}  frame {snapshot.LastFrameMs:0.0}ms  avg {snapshot.AverageFrameMs:0.0}ms\n" +
            $"1% low {snapshot.OnePercentLowFrameMs:0.0}ms / {snapshot.OnePercentLowFps:0} fps\n" +
            $"process {snapshot.LastProcessMs:0.0}ms  render* {snapshot.LastRenderEstimateMs:0.0}ms\n" +
            $"sim {snapshot.LastSimStepMs:0.0}ms  sim avg {snapshot.AverageSimStepMs:0.0}ms\n" +
            $"entities {counts.LiveEntityCount}  units {counts.LiveUnitCount}  visible {counts.VisibleUnitCount}\n" +
            $"projectiles {counts.ProjectileCount}  fx {counts.EffectCount}  fog {counts.LastFogUpdateMs:0.0}ms/{counts.FogTextureUploads}";
    }

    private partial class PerfPanel : Control
    {
        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, Size);
            DrawRect(rect, PanelFill, true);
            DrawRect(rect, PanelStroke, false, 1.1f);
            DrawLine(new Vector2(10, 31), new Vector2(Size.X - 10, 31), new Color(Mint, 0.26f), 1, true);
            DrawString(ThemeDB.FallbackFont, new Vector2(12, 24), "PERF HUD   F3", HorizontalAlignment.Left, 160, 11, InkMuted);
        }
    }
}
