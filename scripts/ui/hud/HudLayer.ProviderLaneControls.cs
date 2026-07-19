using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private partial class ProductionProviderLaneButton : Button
    {
        public required int Index { get; init; }
        public ProductionProviderLaneState State { get; private set; } = new(
            ProductionProviderLaneScope.Auto,
            0,
            "",
            "",
            "",
            0,
            0,
            0,
            false,
            "");
        private Color _accent = Cyan;
        private bool _selected;
        public Color Accent => _accent;
        public string FixedHoverText { get; private set; } = "";

        public void SetState(ProductionProviderLaneState state, bool selected, bool enabled, bool constructionMode)
        {
            State = state;
            _selected = selected;
            _accent = state.Scope switch
            {
                ProductionProviderLaneScope.Auto => Cyan,
                ProductionProviderLaneScope.All => Mint,
                _ => Amber,
            };
            Text = "";
            Disabled = !enabled;
            UiFactory.ApplyHudQueueRowTheme(this, CurrentPalette, _accent);
            FixedHoverText = enabled
                ? GameText.Format(
                    constructionMode ? "ui.constructionProviderLane.tooltip" : "ui.providerLane.tooltip",
                    state.Label,
                    state.ProviderCount,
                    state.QueueCount) + RepeatTooltipSuffix(state)
                : LocalizedDisabledReason(state.DisabledReasonKey, 0);
            QueueRedraw();
        }

        public override void _Draw()
        {
            base._Draw();
            var metrics = HudVisualFoundation.MetricsFor(HudVisualPrimitive.QueueRow);
            var alpha = Disabled ? 0.32f : 0.86f;
            if (!Disabled && (_selected || HasFocus()))
            {
                var state = (_selected ? HudVisualState.Selected : HudVisualState.Normal)
                    | (HasFocus() ? HudVisualState.Focused : HudVisualState.Normal);
                var style = HudVisualFoundation.For(CurrentPalette, HudVisualPrimitive.QueueRow, state, _accent);
                DrawStyleBox(
                    UiFactory.CreateHudFoundationStyleBox(style, metrics),
                    new Rect2(Vector2.Zero, Size).Grow(-metrics.ItemSpacing));
            }

            DrawIconGlyph(
                this,
                ScopeGlyph(State.Scope),
                new Vector2(16, Size.Y * 0.5f),
                24,
                new Color(_accent, Disabled ? 0.42f : 0.92f));

            var count = State.Scope == ProductionProviderLaneScope.Specific
                ? Math.Max(1, Index - 1)
                : State.ProviderCount;
            var countText = Math.Min(99, Math.Max(0, count)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            DrawString(
                UiFontProfile.DrawFont(metrics.DetailFontRole),
                new Vector2(31, Size.Y * 0.5f + 4),
                countText,
                HorizontalAlignment.Center,
                15,
                metrics.DetailFontSize,
                new Color(_accent, Disabled ? 0.52f : 0.94f));

            if (State.ActiveProgress > 0)
            {
                DrawRect(
                    new Rect2(
                        metrics.ContentPadding,
                        Size.Y - metrics.ContentPadding - metrics.ItemSpacing,
                        (Size.X - metrics.ContentPadding * 2) * State.ActiveProgress,
                        metrics.ItemSpacing),
                    new Color(_accent, alpha),
                    true);
            }

            if (State.QueueCount > 0)
            {
                DrawCircle(new Vector2(Size.X - 6, 6), 3.2f, new Color(_accent, alpha));
            }

            if (!string.IsNullOrWhiteSpace(State.RepeatOutputSpecId))
            {
                DrawCircle(new Vector2(7, Size.Y - 7), 3.6f, new Color(Mint, Disabled ? 0.36f : 0.9f));
                DrawArc(new Vector2(7, Size.Y - 7), 6.2f, -Mathf.Pi * 0.8f, Mathf.Pi * 1.15f, 24, new Color(Mint, Disabled ? 0.28f : 0.74f), 1.2f, true);
            }
        }

        private static IconGlyph ScopeGlyph(ProductionProviderLaneScope scope)
        {
            return scope switch
            {
                ProductionProviderLaneScope.All => IconGlyph.Group,
                ProductionProviderLaneScope.Specific => IconGlyph.Building,
                _ => IconGlyph.Ability,
            };
        }

        private static string RepeatTooltipSuffix(ProductionProviderLaneState state)
        {
            if (string.IsNullOrWhiteSpace(state.RepeatOutputSpecId))
            {
                return "";
            }

            var label = state.RepeatOutputSpecId;
            try
            {
                label = UnitDesignCatalog.Spec(state.RepeatOutputSpecId).ShortCode;
            }
            catch (InvalidOperationException)
            {
                // Keep the raw spec id visible if authoring changes remove the unit.
            }

            return " - " + GameText.Format("ui.repeat.laneTooltip", label);
        }
    }
}
