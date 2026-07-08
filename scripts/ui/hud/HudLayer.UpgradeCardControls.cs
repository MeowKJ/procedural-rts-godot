using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private partial class UpgradeProjectCard : Button
    {
        private UpgradeProjectCardState _state;
        private string _label = "";
        private string _shortLabel = "";
        private string _target = "";
        private string _source = "";
        private string _effect = "";
        private string _status = "";
        private string _statusBadge = "";
        public string InspectorText { get; private set; } = "";

        public void SetState(UpgradeProjectCardState state)
        {
            _state = state;
            _label = GameText.T(state.LabelKey);
            _shortLabel = GameText.T(state.ShortKey);
            _target = GameText.T(state.TargetKey);
            _source = GameText.T(state.SourceKey);
            _effect = GameText.T(state.EffectKey);
            _status = GameText.T(state.StatusKey);
            _statusBadge = GameText.T(state.StatusBadgeKey);
            InspectorText = GameText.Format(
                "ui.catalog.inspectUpgrade",
                _label,
                _target,
                _source,
                state.Cost,
                state.DurationSeconds,
                _effect,
                _status);
            Text = $"\n\n{state.Cost}  {state.DurationSeconds}s";
            TooltipText = $"{_label} - {_target} - {_source} - {_effect} - {_status}";
            QueueRedraw();
        }

        public override void _Draw()
        {
            base._Draw();
            var size = Size;
            var accentColor = UpgradeProjectAccent(_state.Accent);
            var accent = new Color(accentColor, 0.92f);
            var iconRect = new Rect2(new Vector2(14, 6), new Vector2(size.X - 28, 30));
            DynamicUnitIcon.DrawFallbackIcon(this, iconRect, _state.Icon, accent, framed: false);

            var font = UiFontProfile.DrawFont(UiFontRole.Compact);
            DrawString(font, new Vector2(8, size.Y - 12), CompactCardText(_shortLabel, 9), HorizontalAlignment.Left, size.X - 16, 9, new Color(CurrentPalette.Text, 0.92f));
            DrawString(font, new Vector2(8, 17), CompactCardText(_target, 11), HorizontalAlignment.Left, size.X - 16, 8, new Color(CurrentPalette.TextMuted, 0.72f));

            var badge = new Rect2(new Vector2(size.X - 42, 20), new Vector2(36, 12));
            DrawRect(badge, new Color(accentColor, 0.16f), true);
            DrawRect(badge, new Color(accentColor, 0.68f), false, 1);
            DrawString(font, badge.Position + new Vector2(3, 9), CompactCardText(_statusBadge, 6), HorizontalAlignment.Center, badge.Size.X - 6, 8, new Color(accentColor, 0.96f));
            DrawRect(new Rect2(0, size.Y - 5, size.X, 5), new Color(accentColor, 0.28f), true);
        }

        private static string CompactCardText(string text, int maxChars)
        {
            if (text.Length <= maxChars)
            {
                return text;
            }

            return maxChars <= 1 ? text[..maxChars] : text[..(maxChars - 1)] + ".";
        }
    }
}
