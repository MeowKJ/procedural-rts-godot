using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class UnitStanceStrip : Control
{
    private const int ButtonSize = 44;
    private static readonly Vector2 MinimumStripSize = new(220, 44);

    private readonly List<StanceButton> _buttons = [];
    private UnitStanceStripProjection _projection = UnitStanceStripProjection.None;
    private SoftOldCityHudPalette _palette = SoftOldCityTheme.Day;
    private int _fontSize = HudLayoutMath.MinimumCompactFontSize;

    public Action<UnitStance>? IntentRequested { get; init; }
    public Action<UnitStancePresentation>? HoverStarted { get; init; }
    public Action<UnitStancePresentation>? HoverEnded { get; init; }
    public UnitStanceStripProjection Projection => _projection;

    public UnitStanceStrip()
    {
        CustomMinimumSize = MinimumStripSize;
        Size = MinimumStripSize;
    }

    public override void _Ready()
    {
        BuildButtons();
        ApplyTheme(_palette, _fontSize);
        ApplyProjection(_projection);
    }

    public void ApplyProjection(UnitStanceStripProjection projection)
    {
        _projection = projection;
        foreach (var button in _buttons)
        {
            button.SetSelected(projection.IsSelected(button.Presentation.Stance));
        }
    }

    public void ApplyTheme(SoftOldCityHudPalette palette, int fontSize)
    {
        _palette = palette;
        _fontSize = fontSize;
        foreach (var button in _buttons)
        {
            button.Palette = palette;
            UiFactory.ApplyHudStanceButtonTheme(button, palette, button.Presentation, fontSize);
            button.QueueRedraw();
        }
    }

    private void BuildButtons()
    {
        if (_buttons.Count > 0)
        {
            return;
        }

        for (var index = 0; index < UnitStancePresentationCatalog.Definitions.Length; index++)
        {
            var presentation = UnitStancePresentationCatalog.Definitions[index];
            var button = new StanceButton
            {
                Name = $"Stance{presentation.Stance}",
                Presentation = presentation,
                Palette = _palette,
                Position = new Vector2(index * ButtonSize, 0),
                CustomMinimumSize = new Vector2(ButtonSize, ButtonSize),
                FocusMode = FocusModeEnum.Click,
                MouseFilter = MouseFilterEnum.Stop,
            };
            button.Size = button.CustomMinimumSize;
            button.Pressed += () => IntentRequested?.Invoke(presentation.Stance);
            button.MouseEntered += () => HoverStarted?.Invoke(presentation);
            button.MouseExited += () => HoverEnded?.Invoke(presentation);
            button.FocusEntered += () => HoverStarted?.Invoke(presentation);
            button.FocusExited += () => HoverEnded?.Invoke(presentation);
            _buttons.Add(button);
            AddChild(button);
        }
    }

    private partial class StanceButton : Button
    {
        private bool _selected;

        public required UnitStancePresentation Presentation { get; init; }
        public required SoftOldCityHudPalette Palette { get; set; }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            QueueRedraw();
        }

        public override void _Draw()
        {
            base._Draw();
            var rect = new Rect2(Vector2.Zero, Size);
            var accent = UiFactory.HudStanceAccent(Presentation.AccentRole, Palette);
            var style = UiFactory.GetHudModeButtonDrawStyle(accent, _selected);
            DrawRect(rect.Grow(-2), style.Fill, true);
            DrawRect(rect.Grow(-1), style.Border, false, style.BorderWidth);
            HudIconRenderer.Draw(this, Presentation.Glyph, rect.Size / 2f, 28, style.Icon);
        }
    }
}
