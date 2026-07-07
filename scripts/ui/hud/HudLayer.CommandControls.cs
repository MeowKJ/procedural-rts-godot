using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private partial class CommandPreviewOverlay : Control
    {
        public CommandPreviewState Preview { get; set; } = CommandPreviewState.None;

        public override void _Draw()
        {
            if (Preview.Kind == CommandPreviewKind.None)
            {
                return;
            }

            var position = AvoidHud(Preview.ScreenPosition + new Vector2(18, 18));
            var color = PreviewColor(Preview.Kind, Preview.IsValid);
            DrawGlyph(position, color);
            DrawLabel(position + new Vector2(18, -5), color);
        }

        private Vector2 AvoidHud(Vector2 position)
        {
            var adjusted = position;
            if (adjusted.X < 112 && adjusted.Y is > 150 and < 460)
            {
                adjusted.X = 112;
            }

            if (adjusted.Y < 128 && adjusted.X < 520)
            {
                adjusted.Y = 128;
            }

            return adjusted;
        }

        private void DrawGlyph(Vector2 position, Color color)
        {
            switch (Preview.Kind)
            {
                case CommandPreviewKind.Move:
                    DrawLine(position + new Vector2(-10, 0), position + new Vector2(10, 0), color, 2.4f, true);
                    DrawLine(position + new Vector2(0, -10), position + new Vector2(0, 10), color, 2.4f, true);
                    DrawArc(position, 13, 0, Mathf.Tau, 64, new Color(color, 0.44f), 1.8f, true);
                    break;
                case CommandPreviewKind.Attack:
                    DrawArc(position, 14, 0, Mathf.Tau, 64, color, 2.3f, true);
                    DrawLine(position + new Vector2(-17, 0), position + new Vector2(-5, 0), color, 2.3f, true);
                    DrawLine(position + new Vector2(5, 0), position + new Vector2(17, 0), color, 2.3f, true);
                    DrawLine(position + new Vector2(0, -17), position + new Vector2(0, -5), color, 2.3f, true);
                    DrawLine(position + new Vector2(0, 5), position + new Vector2(0, 17), color, 2.3f, true);
                    break;
                case CommandPreviewKind.Repair:
                    DrawArc(position, 13, 0, Mathf.Tau, 56, color, 1.9f, true);
                    DrawLine(position + new Vector2(-10, 0), position + new Vector2(10, 0), color, 2.6f, true);
                    DrawLine(position + new Vector2(0, -10), position + new Vector2(0, 10), color, 2.6f, true);
                    break;
                case CommandPreviewKind.Harvest:
                    DrawArc(position, 13, -Mathf.Pi * 0.2f, Mathf.Pi * 1.25f, 48, color, 2.5f, true);
                    DrawLine(position + new Vector2(-10, 8), position + new Vector2(0, 15), color, 2.3f, true);
                    DrawLine(position + new Vector2(0, 15), position + new Vector2(11, 7), color, 2.3f, true);
                    break;
                case CommandPreviewKind.Rally:
                    DrawLine(position + new Vector2(-12, 12), position + new Vector2(0, -12), color, 2.4f, true);
                    DrawLine(position + new Vector2(0, -12), position + new Vector2(12, 12), color, 2.4f, true);
                    DrawLine(position + new Vector2(-7, 2), position + new Vector2(7, 2), color, 2.4f, true);
                    break;
                case CommandPreviewKind.BuildValid:
                case CommandPreviewKind.BuildInvalid:
                    DrawRect(new Rect2(position - new Vector2(11, 9), new Vector2(22, 18)), new Color(color, 0.16f), true);
                    DrawRect(new Rect2(position - new Vector2(11, 9), new Vector2(22, 18)), color, false, 2);
                    if (!Preview.IsValid)
                    {
                        DrawLine(position + new Vector2(-9, -9), position + new Vector2(9, 9), color, 2.4f, true);
                        DrawLine(position + new Vector2(-9, 9), position + new Vector2(9, -9), color, 2.4f, true);
                    }
                    break;
                case CommandPreviewKind.TargetHover:
                    DrawArc(position, 12, 0, Mathf.Tau, 48, color, 1.8f, true);
                    DrawCircle(position, 3.5f, new Color(color, 0.56f));
                    break;
                default:
                    DrawLine(position + new Vector2(-8, -8), position + new Vector2(8, 8), color, 2, true);
                    DrawLine(position + new Vector2(-8, 8), position + new Vector2(8, -8), color, 2, true);
                    break;
            }
        }

        private void DrawLabel(Vector2 position, Color color)
        {
            var text = Preview.Label;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var font = UiFontProfile.DrawFont(UiFontRole.Compact);
            var textSize = font.GetStringSize(text, HorizontalAlignment.Left, -1, 11);
            var rect = new Rect2(position + new Vector2(-5, -14), textSize + new Vector2(12, 18));
            DrawRect(rect, CurrentPalette.PanelStrongFill, true);
            DrawRect(rect, new Color(color, 0.5f), false, 1);
            DrawString(font, position, text, HorizontalAlignment.Left, textSize.X + 4, 11, new Color(Ink, 0.9f));
        }

        private static Color PreviewColor(CommandPreviewKind kind, bool isValid)
        {
            if (!isValid || kind == CommandPreviewKind.BuildInvalid)
            {
                return new Color(Danger, 0.96f);
            }

            return kind switch
            {
                CommandPreviewKind.Attack => new Color(Danger, 0.96f),
                CommandPreviewKind.Repair => new Color(Mint, 0.94f),
                CommandPreviewKind.Harvest => new Color(Amber, 0.94f),
                CommandPreviewKind.Rally => new Color(Mint, 0.94f),
                CommandPreviewKind.BuildValid => new Color(Cyan, 0.96f),
                CommandPreviewKind.TargetHover => new Color(Ink, 0.78f),
                _ => new Color(Cyan, 0.9f),
            };
        }
    }

    private partial class CommandButton : Button
    {
        public required string OptionId { get; init; }
        public ProductionKind Kind { get; set; }
        public required string Hotkey { get; set; }
        public required string ShortLabel { get; set; }
        public required IconGlyph Glyph { get; set; }
        public required Color Accent { get; set; }
        public required int Cost { get; set; }
        public string? UnitDesignId { get; set; }
        public string? BuildKind { get; set; }
        public string ProducerShortCode { get; private set; } = "";
        public string ProducerLabel { get; private set; } = "";
        public IconGlyph RoleGlyph { get; private set; }
        public float Duration { get; private set; }
        private int _queued;
        private float _progress;
        private string _disabledReason = "";
        private float _feedbackPulse;
        private bool _hasState;

        public void SetState(ProductionOptionState state, string disabledReason)
        {
            BuildKind = null;
            UnitDesignId = state.UnitDesignId;
            ShortLabel = state.ShortCode;
            Glyph = state.Icon;
            Accent = state.Accent;
            Cost = state.Cost;
            ProducerShortCode = BuildSpecCatalog.For(state.ProducerKind).ShortCode;
            ProducerLabel = BuildSpecCatalog.For(state.ProducerKind).Label;
            RoleGlyph = state.RoleGlyph == IconGlyph.None ? state.Icon : state.RoleGlyph;
            Duration = state.Duration;
            SetState(state.CanQueue, state.QueuedCount, state.ActiveProgress, disabledReason);
        }

        public void SetBuildState(BuildOptionSnapshot state, string disabledReason)
        {
            var spec = BuildSpecCatalog.For(state.Kind);
            UnitDesignId = null;
            BuildKind = state.Kind;
            ProducerShortCode = "";
            ProducerLabel = "";
            RoleGlyph = IconGlyph.None;
            Duration = 0;
            ShortLabel = spec.ShortCode;
            Glyph = state.Icon;
            Accent = spec.Accent;
            Cost = state.Cost;
            SetState(state.CanStart, 0, 0, disabledReason);
        }

        public void SetState(bool enabled, int queued, float progress, string disabledReason)
        {
            var wasEnabled = !Disabled;
            if (_hasState
                && (queued > _queued
                    || (queued == 0 && _queued > 0)
                    || wasEnabled != enabled))
            {
                _feedbackPulse = 1f;
            }

            _hasState = true;
            Disabled = !enabled;
            _queued = queued;
            _progress = Mathf.Clamp(progress, 0, 1);
            _disabledReason = disabledReason;
            Text = queued > 0
                ? $"{Hotkey}\n\n{Cost}  x{queued}"
                : $"{Hotkey}\n\n{Cost}";
            var label = !string.IsNullOrWhiteSpace(BuildKind)
                ? BuildSpecCatalog.For(BuildKind).Label
                : !string.IsNullOrWhiteSpace(UnitDesignId)
                ? UnitDesignCatalog.Spec(UnitDesignId).Label
                : UnitPresentationCatalog.Production[Kind].ShortCode;
            TooltipText = enabled
                ? CommandCardTooltip(label, Cost, ProducerLabel, Duration)
                : CommandCardTooltip(label, Cost, ProducerLabel, Duration, disabledReason);
            QueueRedraw();
        }

        public override void _Process(double delta)
        {
            if (_feedbackPulse <= 0)
            {
                return;
            }

            _feedbackPulse = Mathf.Max(0, _feedbackPulse - (float)delta * 2.6f);
            QueueRedraw();
        }

        public override void _Draw()
        {
            base._Draw();
            var size = Size;
            var style = UiFactory.GetHudCommandButtonOverlayStyle(Disabled, CurrentPalette);
            var iconRect = new Rect2(new Vector2(14, 6), new Vector2(size.X - 28, 32));
            var iconAccent = Disabled ? new Color(Accent, 0.42f) : new Color(Accent, 0.96f);
            if (!string.IsNullOrWhiteSpace(UnitDesignId))
            {
                DynamicUnitIcon.DrawUnitDesignIcon(this, iconRect, UnitDesignCatalog.Spec(UnitDesignId), iconAccent, animated: true, framed: false);
            }
            else
            {
                DynamicUnitIcon.DrawFallbackIcon(
                    this,
                    iconRect,
                    Glyph,
                    iconAccent,
                    framed: false);
            }

            if (IsTrainCard)
            {
                DrawTrainCardMetadata(size);
            }

            DrawString(UiFontProfile.DrawFont(UiFontRole.Compact), new Vector2(8, size.Y - 12), ShortLabel, HorizontalAlignment.Left, size.X - 16, 9, style.ShortLabel);
            if (_progress > 0)
            {
                DrawRect(new Rect2(0, size.Y - 5, size.X * _progress, 5), style.Progress, true);
            }

            if (_queued > 0)
            {
                DrawCircle(new Vector2(size.X - 10, 10), 8, style.QueueBadge);
                DrawCircle(new Vector2(size.X - 10, 10), 4, style.QueueBadgeCutout);
            }

            if (_feedbackPulse > 0)
            {
                var pulse = Mathf.Clamp(_feedbackPulse, 0, 1);
                DrawRect(new Rect2(Vector2.Zero, size).Grow(pulse * 5f), new Color(Accent, pulse * 0.42f), false, 1.2f + pulse * 1.6f);
            }

            if (Disabled)
            {
                DrawRect(new Rect2(Vector2.Zero, size), style.DisabledFill, true);
                DrawLine(new Vector2(8, size.Y - 8), new Vector2(size.X - 8, 8), style.DisabledStrike, 2, true);
                if (IsTrainCard && !string.IsNullOrWhiteSpace(_disabledReason))
                {
                    var font = UiFontProfile.DrawFont(UiFontRole.Compact);
                    DrawString(
                        font,
                        new Vector2(8, size.Y - 24),
                        CompactCardText(_disabledReason, 11),
                        HorizontalAlignment.Left,
                        size.X - 16,
                        8,
                        new Color(CurrentPalette.Danger, 0.9f));
                }
            }
        }

        private bool IsTrainCard => !string.IsNullOrWhiteSpace(UnitDesignId);

        private void DrawTrainCardMetadata(Vector2 size)
        {
            var sourceCode = string.IsNullOrWhiteSpace(ProducerShortCode) ? "--" : ProducerShortCode;
            var font = UiFontProfile.DrawFont(UiFontRole.Compact);
            var chip = new Rect2(new Vector2(5, 5), new Vector2(28, 13));
            DrawRect(chip, new Color(CurrentPalette.PanelStrongFill, 0.74f), true);
            DrawRect(chip, new Color(Accent, Disabled ? 0.22f : 0.42f), false, 1);
            DrawString(
                font,
                chip.Position + new Vector2(3, 10),
                CompactCardText(sourceCode, 4),
                HorizontalAlignment.Left,
                chip.Size.X - 5,
                8,
                new Color(Accent, Disabled ? 0.48f : 0.82f));

            if (RoleGlyph != IconGlyph.None)
            {
                DrawIconGlyph(
                    this,
                    RoleGlyph,
                    new Vector2(size.X - 14, 13),
                    13,
                    new Color(Accent, Disabled ? 0.38f : 0.84f));
            }

            if (Duration > 0)
            {
                var seconds = $"{Mathf.CeilToInt(Duration)}s";
                DrawString(
                    font,
                    new Vector2(size.X - 30, size.Y - 12),
                    seconds,
                    HorizontalAlignment.Right,
                    24,
                    8,
                    new Color(CurrentPalette.TextMuted, Disabled ? 0.46f : 0.68f));
            }
        }

        private static string CommandCardTooltip(string label, int cost, string producerLabel, float duration, string disabledReason = "")
        {
            var source = string.IsNullOrWhiteSpace(producerLabel) ? "" : $" - {producerLabel}";
            var time = duration > 0 ? $" - {Mathf.CeilToInt(duration)}s" : "";
            var disabled = string.IsNullOrWhiteSpace(disabledReason) ? "" : $" - {disabledReason}";
            return $"{label} - {cost}{source}{time}{disabled}";
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
