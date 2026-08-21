using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private enum CatalogModeKind
    {
        Build,
        Train,
        Upgrades,
        Abilities,
    }

    private static Color CatalogModeAccent(CatalogModeKind mode)
    {
        return mode switch
        {
            CatalogModeKind.Build => Cyan,
            CatalogModeKind.Train => Mint,
            CatalogModeKind.Upgrades => Amber,
            CatalogModeKind.Abilities => Danger,
            _ => InkMuted,
        };
    }

    private static IconGlyph CatalogModeGlyph(CatalogModeKind mode)
    {
        return mode switch
        {
            CatalogModeKind.Build => IconGlyph.Building,
            CatalogModeKind.Train => IconGlyph.Infantry,
            CatalogModeKind.Upgrades => IconGlyph.Settings,
            CatalogModeKind.Abilities => IconGlyph.Ability,
            _ => IconGlyph.None,
        };
    }

    private partial class SeparatorLine : Control
    {
        public override void _Draw()
        {
            DrawLine(new Vector2(0, 0), new Vector2(0, CustomMinimumSize.Y), new Color(CurrentPalette.PanelBorderStrong, 0.46f), 1, true);
        }
    }

    private partial class IconActionButton : Button
    {
        public IconGlyph Glyph { get; set; }
        public Color Accent { get; set; } = Mint;
        public string FixedHoverText { get; set; } = "";

        public override void _Draw()
        {
            base._Draw();
            var rect = new Rect2(Vector2.Zero, Size);
            var style = UiFactory.GetHudIconButtonDrawStyle(Accent, ButtonPressed, HasFocus(), Disabled);
            DrawRect(rect.Grow(-2), style.Fill, true);
            DrawRect(rect.Grow(-1), style.Border, false, style.BorderWidth);
            var glyphSize = Mathf.Clamp(Mathf.Min(rect.Size.X, rect.Size.Y) * 0.64f, 28, 32);
            HudIconRenderer.Draw(this, Glyph, rect.Size / 2f, glyphSize, style.Icon);
        }
    }

    private partial class QueueMiniStack : Control
    {
        private IconGlyph _glyph = IconGlyph.Infantry;
        private Color _accent = Mint;
        private int _queued;
        private float _progress;
        private bool _available;
        public int QueuedCount => _queued;
        public float ActiveProgress => _progress;
        public bool Available => _available;

        public void SetState(IconGlyph glyph, Color accent, int queued, float progress, bool available)
        {
            _glyph = glyph;
            _accent = accent;
            _queued = Math.Max(0, queued);
            _progress = Mathf.Clamp(progress, 0, 1);
            _available = available;
            QueueRedraw();
        }

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, Size);
            var accent = new Color(_accent, _available ? 0.92f : 0.42f);
            DrawRect(rect, new Color(CurrentPalette.PanelStrongFill, 0.82f), true);
            DrawRect(rect.Grow(-1), new Color(accent, 0.62f), false, 1.2f, true);

            var stackDepth = Math.Min(3, Math.Max(0, _queued - 1));
            for (var index = stackDepth; index > 0; index--)
            {
                var card = new Rect2(new Vector2(8 + index * 2, 8 + index * 2), new Vector2(32, 32));
                DrawRect(card, new Color(CurrentPalette.PanelSubtleFill, 0.94f), true);
                DrawRect(card, new Color(accent, 0.32f), false, 1, true);
            }

            var active = new Rect2(new Vector2(6, 6), new Vector2(36, 36));
            DrawRect(active, new Color(accent, _queued > 0 ? 0.15f : 0.06f), true);
            DrawRect(active, new Color(accent, 0.72f), false, 1.4f, true);
            HudIconRenderer.Draw(this, _glyph, active.GetCenter(), 26, accent);

            if (_progress > 0)
            {
                DrawArc(active.GetCenter(), 20, -Mathf.Pi * 0.5f, -Mathf.Pi * 0.5f + Mathf.Tau * _progress, 48, new Color(accent, 0.96f), 3, true);
            }

            if (_queued > 0)
            {
                var badgeCenter = new Vector2(42, 10);
                DrawCircle(badgeCenter, 9, new Color(CurrentPalette.PanelStrongFill, 0.98f));
                DrawCircle(badgeCenter, 8, new Color(accent, 0.9f));
                var text = Math.Min(99, _queued).ToString(System.Globalization.CultureInfo.InvariantCulture);
                DrawString(UiFontProfile.DrawFont(UiFontRole.Compact), badgeCenter + new Vector2(-7, 4), text, HorizontalAlignment.Center, 14, 11, new Color(CurrentPalette.PanelStrongFill));
            }

            var remaining = Math.Max(0, _queued - 1);
            for (var index = 0; index < 3; index++)
            {
                var filled = index < remaining;
                var slot = new Rect2(new Vector2(6 + index * 14, 45), new Vector2(10, 7));
                DrawRect(slot, new Color(filled ? accent : CurrentPalette.PanelSubtleFill, filled ? 0.72f : 0.56f), true);
                DrawRect(slot, new Color(accent, filled ? 0.76f : 0.24f), false, 1, true);
            }

            DrawRect(new Rect2(new Vector2(6, 55), new Vector2(40, 2)), new Color(CurrentPalette.PanelSubtleFill, 0.88f), true);
            DrawRect(new Rect2(new Vector2(6, 55), new Vector2(40 * _progress, 2)), new Color(accent, 0.94f), true);
        }
    }

    private partial class CatalogModeButton : Button
    {
        public required CatalogModeKind Mode { get; init; }
        public required string Label { get; init; }
        public required string Detail { get; init; }
        public required string HelpText { get; init; }
        private bool _selected;

        public void SetSelected(bool selected)
        {
            _selected = selected;
            QueueRedraw();
        }

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, Size);
            var accent = CatalogModeAccent(Mode);
            var focused = HasFocus();
            var metrics = HudVisualFoundation.MetricsFor(HudVisualPrimitive.ModeStrip);
            var state = (_selected ? HudVisualState.Selected : HudVisualState.Normal)
                | (focused ? HudVisualState.Focused : HudVisualState.Normal);
            var style = HudVisualFoundation.For(CurrentPalette, HudVisualPrimitive.ModeStrip, state, accent);
            DrawStyleBox(UiFactory.CreateHudFoundationStyleBox(style, metrics), rect);
            if (focused)
            {
                DrawRect(rect.Grow(-metrics.ContentPadding), new Color(Ink, 0.24f), false, 1, true);
                DrawRect(
                    new Rect2(
                        new Vector2(metrics.ItemSpacing, metrics.ContentPadding + 1),
                        new Vector2(metrics.ItemSpacing, rect.Size.Y - (metrics.ContentPadding + 1) * 2)),
                    new Color(style.Accent, 0.95f),
                    true);
            }

            DrawRect(
                new Rect2(
                    new Vector2(metrics.ContentPadding, rect.Size.Y - metrics.ContentPadding),
                    new Vector2(rect.Size.X - metrics.ContentPadding * 2, 1)),
                new Color(style.Accent, _selected || focused ? 0.78f : 0.46f),
                true);

            var glyph = CatalogModeGlyph(Mode);
            var signalStrength = _selected || focused ? 1f : 0.72f;
            HudIconRenderer.Draw(this, glyph, new Vector2(13, rect.Size.Y * 0.5f), 16, new Color(style.Accent, signalStrength));

            var labelBounds = new Rect2(new Vector2(23, 3), new Vector2(rect.Size.X - 27, 14));
            DrawString(
                UiFontProfile.DrawFont(metrics.DetailFontRole),
                labelBounds.Position + new Vector2(0, metrics.DetailFontSize),
                Label,
                HorizontalAlignment.Left,
                labelBounds.Size.X,
                metrics.DetailFontSize,
                new Color(style.Text, _selected || focused ? 0.98f : 0.82f));

            var chipMetrics = HudVisualFoundation.MetricsFor(HudVisualPrimitive.StatusBadge);
            var chipBounds = new Rect2(new Vector2(22, 19), new Vector2(rect.Size.X - 26, 10));
            var chipStyle = HudVisualFoundation.For(CurrentPalette, HudVisualPrimitive.StatusBadge, state, style.Accent);
            DrawStyleBox(UiFactory.CreateHudFoundationStyleBox(chipStyle, chipMetrics), chipBounds);
            DrawString(
                UiFontProfile.DrawFont(chipMetrics.DetailFontRole),
                chipBounds.Position + new Vector2(chipMetrics.ContentPadding, chipBounds.Size.Y - chipMetrics.ItemSpacing),
                Detail,
                HorizontalAlignment.Center,
                chipBounds.Size.X - chipMetrics.ContentPadding * 2,
                chipMetrics.DetailFontSize,
                chipStyle.Text);
        }
    }

    private partial class ProductionTab : Button
    {
        public required IconGlyph Glyph { get; init; }
        public required BuildCategory Category { get; init; }
        public bool Active { get; init; }
        private bool _selected;

        public void SetSelected(bool selected)
        {
            _selected = selected;
            QueueRedraw();
        }

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, CustomMinimumSize);
            var style = UiFactory.GetHudProductionTabDrawStyle(Glyph, Active, _selected, CurrentPalette);
            DrawRect(rect, style.Fill, true);
            DrawRect(rect.Grow(-2), style.AccentFill, true);
            DrawRect(rect.Grow(-1), style.AccentBorder, false, style.BorderWidth);
            HudIconRenderer.Draw(this, Glyph, rect.Size / 2f, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.58f, style.Icon);
        }
    }

    private partial class ProductionCategoryTab : Button
    {
        public required IconGlyph Glyph { get; init; }
        public required ProductionCategory Category { get; init; }
        public bool Active { get; init; }
        private bool _selected;

        public void SetSelected(bool selected)
        {
            _selected = selected;
            QueueRedraw();
        }

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, CustomMinimumSize);
            var style = UiFactory.GetHudProductionTabDrawStyle(Glyph, Active, _selected, CurrentPalette);
            DrawRect(rect, style.Fill, true);
            DrawRect(rect.Grow(-2), style.AccentFill, true);
            DrawRect(rect.Grow(-1), style.AccentBorder, false, style.BorderWidth);
            HudIconRenderer.Draw(this, Glyph, rect.Size / 2f, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.58f, style.Icon);
        }
    }

    private partial class UpgradeCategoryTab : Button
    {
        public required IconGlyph Glyph { get; init; }
        public required UpgradeProjectAccentKind Category { get; init; }
        private bool _selected;

        public void SetSelected(bool selected)
        {
            _selected = selected;
            QueueRedraw();
        }

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, CustomMinimumSize);
            var style = UiFactory.GetHudProductionTabDrawStyle(Glyph, active: true, _selected, CurrentPalette);
            DrawRect(rect, style.Fill, true);
            DrawRect(rect.Grow(-2), style.AccentFill, true);
            DrawRect(rect.Grow(-1), style.AccentBorder, false, style.BorderWidth);
            HudIconRenderer.Draw(this, Glyph, rect.Size / 2f, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.58f, style.Icon);
        }
    }

    private partial class IconOnlyButton : Button
    {
        public required IconGlyph Glyph { get; init; }
        public required Color IconColor { get; init; }

        public override void _Draw()
        {
            base._Draw();
            var rect = new Rect2(Vector2.Zero, Size);
            DrawRect(rect.Grow(-2), new Color(IconColor, 0.08f), true);
            HudIconRenderer.Draw(this, Glyph, rect.Size / 2f, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.54f, new Color(IconColor, 0.88f));
        }
    }
}
