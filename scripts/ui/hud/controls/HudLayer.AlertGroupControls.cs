using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private partial class ControlGroupSlot : Panel
    {
        public required int Number { get; init; }
        private Label _numberLabel = null!;
        private Label _countLabel = null!;
        private Label _contentsLabel = null!;
        private ControlGroupSnapshot _snapshot;

        public override void _Ready()
        {
            EnsureLabels();
            SetSnapshot(new ControlGroupSnapshot(Number, 0, 0, 0, false, 0));
        }

        public void SetSnapshot(ControlGroupSnapshot snapshot)
        {
            _snapshot = snapshot;
            EnsureLabels();

            var empty = snapshot.TotalCount == 0;
            _numberLabel.Text = snapshot.Number.ToString();
            _countLabel.Text = empty ? "--" : snapshot.TotalCount.ToString();
            _contentsLabel.Text = empty
                ? GameText.T("ui.group.empty")
                : ContentsText(snapshot);

            var style = UiFactory.GetHudControlGroupSlotStyle(snapshot, CurrentPalette);
            UiFactory.ApplyHudPanelTheme(this, style.Fill, style.Border);
            SetLabelColor(_numberLabel, style.Number);
            SetLabelColor(_countLabel, style.Count);
            SetLabelColor(_contentsLabel, style.Contents);
            QueueRedraw();
        }

        public override void _Draw()
        {
            base._Draw();
            if (_snapshot.FeedbackPulse <= 0.01f)
            {
                return;
            }

            var rect = new Rect2(Vector2.Zero, CustomMinimumSize).Grow(_snapshot.FeedbackPulse * 4);
            var style = UiFactory.GetHudControlGroupSlotStyle(_snapshot, CurrentPalette);
            DrawRect(rect, style.FeedbackPulse, false, 1.4f);
        }

        private void EnsureLabels()
        {
            if (_numberLabel is not null)
            {
                return;
            }

            var style = UiFactory.GetHudControlGroupSlotStyle(new ControlGroupSnapshot(Number, 0, 0, 0, false, 0), CurrentPalette);

            _numberLabel = MakeLabel(Number.ToString(), new Vector2(6, 4), 11, style.Number);
            _numberLabel.CustomMinimumSize = new Vector2(16, 18);
            AddChild(_numberLabel);

            _countLabel = MakeLabel("--", new Vector2(26, 3), 15, style.Count);
            _countLabel.CustomMinimumSize = new Vector2(28, 18);
            AddChild(_countLabel);

            _contentsLabel = MakeLabel(GameText.T("ui.group.empty"), new Vector2(6, 20), 11, style.Contents);
            _contentsLabel.CustomMinimumSize = new Vector2(50, 14);
            AddChild(_contentsLabel);
        }

        private static string ContentsText(ControlGroupSnapshot snapshot)
        {
            var parts = new List<string>(3);
            if (snapshot.InfantryCount > 0)
            {
                parts.Add($"I{snapshot.InfantryCount}");
            }

            if (snapshot.TankCount > 0)
            {
                parts.Add($"T{snapshot.TankCount}");
            }

            if (snapshot.HarvesterCount > 0)
            {
                parts.Add($"H{snapshot.HarvesterCount}");
            }

            return string.Join(" ", parts);
        }
    }

    private partial class AlertRow : Control
    {
        private AlertLine? _alert;
        public AlertLine? Alert => _alert;

        public void SetAlert(AlertLine? alert)
        {
            _alert = alert;
            QueueRedraw();
        }

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, CustomMinimumSize);
            if (_alert is null)
            {
                return;
            }

            var alert = _alert.Value;
            var color = SoftOldCityTheme.AccentForAlert(alert.Kind, CurrentPalette);
            var alpha = Mathf.Lerp(0.3f, 0.9f, Mathf.Clamp(alert.RemainingRatio, 0, 1));
            DrawRect(rect, CurrentPalette.PanelFill, true);
            DrawRect(new Rect2(0, 0, 4, rect.Size.Y), new Color(color, alpha), true);
            DrawRect(new Rect2(6, rect.Size.Y - 2, Mathf.Clamp(alert.RemainingRatio, 0, 1) * (rect.Size.X - 8), 2), new Color(color, 0.58f), true);
            DrawString(UiFontProfile.DrawFont(UiFontRole.Compact), new Vector2(10, 13), AlertPrefix(alert.Kind), HorizontalAlignment.Left, 58, 11, new Color(color, alpha));
            DrawString(UiFontProfile.DrawFont(UiFontRole.Compact), new Vector2(70, 13), alert.Text, HorizontalAlignment.Left, rect.Size.X - 72, 11, new Color(Ink, alpha));
        }

        private static string AlertPrefix(AlertKind kind)
        {
            return kind switch
            {
                AlertKind.Combat => GameText.T("alert.prefix.combat"),
                AlertKind.Production => GameText.T("alert.prefix.production"),
                AlertKind.Economy => GameText.T("alert.prefix.economy"),
                AlertKind.Harvester => GameText.T("alert.prefix.harvester"),
                AlertKind.Building => GameText.T("alert.prefix.building"),
                AlertKind.Power => GameText.T("alert.prefix.power"),
                _ => GameText.T("alert.prefix.system"),
            };
        }

    }
}
