using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HotkeyLegendLayer : CanvasLayer
{
    private static readonly Color Ink = new("#d8f7ff");
    private static readonly Color InkMuted = new("#8095aa");
    private static readonly Color Cyan = new("#59f1ff");
    private static readonly Color Mint = new("#8fffe1");
    private static readonly Color Amber = new("#f6c55c");
    private static readonly Color Danger = new("#ff5d75");

    private Control _root = null!;
    private LegendPanel _panel = null!;
    private Label _hint = null!;
    private bool _open;

    public override void _Ready()
    {
        Layer = 35;

        _root = new Control
        {
            Name = "HotkeyLegendRoot",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _panel = new LegendPanel
        {
            Name = "LegendPanel",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _panel.SetAnchorsPreset(Control.LayoutPreset.RightWide);
        _panel.OffsetLeft = -616;
        _panel.OffsetTop = 72;
        _panel.OffsetRight = -318;
        _panel.OffsetBottom = -82;
        _root.AddChild(_panel);

        _hint = MakeLabel(GameText.T("hotkeys.hint.open"), 11, InkMuted);
        _hint.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _hint.OffsetLeft = -382;
        _hint.OffsetTop = 12;
        _hint.OffsetRight = -314;
        _hint.OffsetBottom = 30;
        _hint.HorizontalAlignment = HorizontalAlignment.Right;
        _root.AddChild(_hint);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo || key.Keycode != Key.F1)
        {
            return;
        }

        _open = !_open;
        _panel.Visible = _open;
        _hint.Text = _open ? GameText.T("hotkeys.hint.close") : GameText.T("hotkeys.hint.open");
        GetViewport().SetInputAsHandled();
    }

    private static Label MakeLabel(string text, int fontSize, Color color)
    {
        return new Label
        {
            Text = text,
            ClipText = true,
            LabelSettings = new LabelSettings
            {
                FontSize = fontSize,
                FontColor = color,
                OutlineColor = new Color("#02060a", 0.84f),
                OutlineSize = 1,
            },
        };
    }

    private partial class LegendPanel : Control
    {
        private readonly (string Section, string[] Rows, Color Accent)[] _sections =
        [
            (GameText.T("hotkeys.camera"), [GameText.T("hotkeys.camera.1"), GameText.T("hotkeys.camera.2"), GameText.T("hotkeys.camera.3")], Cyan),
            (GameText.T("hotkeys.select"), [GameText.T("hotkeys.select.1"), GameText.T("hotkeys.select.2"), GameText.T("hotkeys.select.3")], Mint),
            (GameText.T("hotkeys.orders"), [GameText.T("hotkeys.orders.1"), GameText.T("hotkeys.orders.2"), GameText.T("hotkeys.orders.3")], Amber),
            (GameText.T("hotkeys.stance"), [GameText.T("hotkeys.stance.1"), GameText.T("hotkeys.stance.2"), GameText.T("hotkeys.stance.3")], Mint),
            (GameText.T("hotkeys.groups"), [GameText.T("hotkeys.groups.1"), GameText.T("hotkeys.groups.2")], Cyan),
            (GameText.T("hotkeys.build"), [GameText.T("hotkeys.build.1"), GameText.T("hotkeys.build.2"), GameText.T("hotkeys.build.3")], Amber),
            (GameText.T("hotkeys.debug"), [GameText.T("hotkeys.debug.1"), GameText.T("hotkeys.debug.2"), GameText.T("hotkeys.debug.3")], Danger),
        ];

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, Size);
            DrawRect(rect, new Color("#02060a", 0.84f), true);
            DrawRect(rect, new Color("#59f1ff", 0.34f), false, 1.2f);
            DrawHeader();

            var y = 58f;
            foreach (var (section, rows, accent) in _sections)
            {
                DrawSection(section, rows, accent, y);
                y += 52 + rows.Length * 15;
            }
        }

        private void DrawHeader()
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(16, 25), GameText.T("hotkeys.title"), HorizontalAlignment.Left, 180, 18, new Color("#ffffff"));
            DrawString(ThemeDB.FallbackFont, new Vector2(16, 44), GameText.T("hotkeys.subtitle"), HorizontalAlignment.Left, 240, 11, InkMuted);
            DrawLine(new Vector2(14, 52), new Vector2(Size.X - 14, 52), new Color("#59f1ff", 0.24f), 1, true);
        }

        private void DrawSection(string section, string[] rows, Color accent, float y)
        {
            DrawRect(new Rect2(14, y - 14, Size.X - 28, 20 + rows.Length * 15), new Color("#071019", 0.72f), true);
            DrawRect(new Rect2(14, y - 14, 4, 20 + rows.Length * 15), new Color(accent, 0.78f), true);
            DrawString(ThemeDB.FallbackFont, new Vector2(26, y), section, HorizontalAlignment.Left, 92, 11, new Color(accent, 0.96f));

            for (var index = 0; index < rows.Length; index++)
            {
                DrawString(ThemeDB.FallbackFont, new Vector2(116, y + index * 15), rows[index], HorizontalAlignment.Left, Size.X - 132, 11, Ink);
            }
        }
    }
}
