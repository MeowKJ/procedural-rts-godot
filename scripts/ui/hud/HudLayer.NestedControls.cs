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
            DrawIconGlyph(this, Glyph, rect.Size / 2f, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.58f, style.Icon);
        }
    }

    private partial class QueueMiniStack : Control
    {
        private IconGlyph _glyph = IconGlyph.Infantry;
        private Color _accent = Mint;
        private int _queued;
        private float _progress;
        private bool _available;

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
                var card = new Rect2(new Vector2(10 + index * 3, 10 + index * 3), new Vector2(36, 36));
                DrawRect(card, new Color(CurrentPalette.PanelSubtleFill, 0.94f), true);
                DrawRect(card, new Color(accent, 0.32f), false, 1, true);
            }

            var active = new Rect2(new Vector2(8, 8), new Vector2(40, 40));
            DrawRect(active, new Color(accent, _queued > 0 ? 0.15f : 0.06f), true);
            DrawRect(active, new Color(accent, 0.72f), false, 1.4f, true);
            DrawIconGlyph(this, _glyph, active.GetCenter(), 24, accent);

            if (_progress > 0)
            {
                DrawArc(active.GetCenter(), 23, -Mathf.Pi * 0.5f, -Mathf.Pi * 0.5f + Mathf.Tau * _progress, 48, new Color(accent, 0.96f), 3, true);
            }

            if (_queued > 0)
            {
                var badgeCenter = new Vector2(45, 13);
                DrawCircle(badgeCenter, 10, new Color(CurrentPalette.PanelStrongFill, 0.98f));
                DrawCircle(badgeCenter, 9, new Color(accent, 0.9f));
                var text = Math.Min(99, _queued).ToString(System.Globalization.CultureInfo.InvariantCulture);
                DrawString(UiFontProfile.DrawFont(UiFontRole.Compact), badgeCenter + new Vector2(-8, 4), text, HorizontalAlignment.Center, 16, 9, new Color(CurrentPalette.PanelStrongFill));
            }

            var remaining = Math.Max(0, _queued - 1);
            for (var index = 0; index < 3; index++)
            {
                var filled = index < remaining;
                var slot = new Rect2(new Vector2(8 + index * 15, 58), new Vector2(11, 12));
                DrawRect(slot, new Color(filled ? accent : CurrentPalette.PanelSubtleFill, filled ? 0.72f : 0.56f), true);
                DrawRect(slot, new Color(accent, filled ? 0.76f : 0.24f), false, 1, true);
            }

            DrawRect(new Rect2(new Vector2(8, 76), new Vector2(40, 3)), new Color(CurrentPalette.PanelSubtleFill, 0.88f), true);
            DrawRect(new Rect2(new Vector2(8, 76), new Vector2(40 * _progress, 3)), new Color(accent, 0.94f), true);
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
            var borderAlpha = focused ? 0.95f : _selected ? 0.84f : 0.42f;
            var borderWidth = focused ? 2.2f : _selected ? 1.8f : 1.2f;
            var fill = _selected
                ? new Color(accent, focused ? 0.24f : 0.16f)
                : new Color(focused ? accent : CurrentPalette.PanelSubtleFill, focused ? 0.12f : 0.82f);
            DrawRect(rect, fill, true);
            DrawRect(rect.Grow(-1), new Color(accent, borderAlpha), false, borderWidth, true);
            if (focused)
            {
                DrawRect(rect.Grow(-4), new Color(Ink, 0.24f), false, 1, true);
                DrawRect(new Rect2(new Vector2(2, 5), new Vector2(2, rect.Size.Y - 10)), new Color(accent, 0.95f), true);
            }

            DrawRect(new Rect2(new Vector2(4, rect.Size.Y - 4), new Vector2(rect.Size.X - 8, 1)), new Color(accent, _selected || focused ? 0.78f : 0.46f), true);

            var glyph = CatalogModeGlyph(Mode);
            DrawIconGlyph(this, glyph, rect.GetCenter(), 20, new Color(accent, _selected || focused ? 1f : 0.72f));
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
            DrawIconGlyph(this, Glyph, rect.Size / 2f, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.58f, style.Icon);
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
            DrawIconGlyph(this, Glyph, rect.Size / 2f, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.58f, style.Icon);
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
            DrawIconGlyph(this, Glyph, rect.Size / 2f, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.54f, new Color(IconColor, 0.88f));
        }
    }

    private partial class MoveModeButton : Button
    {
        public required MoveCommandMode Mode { get; init; }
        public required IconGlyph Glyph { get; init; }
        private bool _selected;

        public void SetSelected(bool selected)
        {
            _selected = selected;
            QueueRedraw();
        }

        public override void _Draw()
        {
            base._Draw();
            var rect = new Rect2(Vector2.Zero, Size);
            var accent = UiFactory.HudMoveModeAccent(Mode, CurrentPalette);
            var style = UiFactory.GetHudModeButtonDrawStyle(accent, _selected);
            DrawRect(rect.Grow(-2), style.Fill, true);
            DrawRect(rect.Grow(-1), style.Border, false, style.BorderWidth);
            DrawIconGlyph(this, Glyph, rect.Size / 2f, 22, style.Icon);
        }
    }

    private partial class StanceModeButton : Button
    {
        public required UnitStancePresentation Presentation { get; init; }
        public UnitStance Stance => Presentation.Stance;
        private bool _selected;

        public void SetSelected(bool selected)
        {
            _selected = selected;
            QueueRedraw();
        }

        public override void _Draw()
        {
            base._Draw();
            var rect = new Rect2(Vector2.Zero, Size);
            var accent = UiFactory.HudStanceAccent(Presentation.AccentRole, CurrentPalette);
            var style = UiFactory.GetHudModeButtonDrawStyle(accent, _selected);
            DrawRect(rect.Grow(-2), style.Fill, true);
            DrawRect(rect.Grow(-1), style.Border, false, style.BorderWidth);
            DrawIconGlyph(this, Presentation.Glyph, rect.Size / 2f, 22, style.Icon);
        }
    }

    private partial class PortraitGlyph : Control
    {
        public string Mode { get; set; } = "none";
        public IconGlyph Icon { get; set; } = IconGlyph.None;
        public string? UnitDesignId { get; set; }
        public Color Accent { get; set; } = Mint;

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, CustomMinimumSize);
            DrawRect(rect, CurrentPalette.PanelSubtleFill, true);
            DrawRect(rect, new Color(Cyan, 0.36f), false, 1.4f);
            var center = rect.Size / 2f;
            DrawArc(center, 30, 0, Mathf.Tau, 72, new Color(Cyan, 0.72f), 2.4f, true);

            switch (Mode)
            {
                case "building":
                    DrawRect(new Rect2(center - new Vector2(22, 18), new Vector2(44, 36)), CurrentPalette.PanelFill, true);
                    DrawRect(new Rect2(center - new Vector2(22, 18), new Vector2(44, 36)), new Color(Mint, 0.72f), false, 2, true);
                    DrawLine(center + new Vector2(-20, 0), center + new Vector2(20, 0), new Color(Ink, 0.55f), 1.6f, true);
                    break;
                case "multi":
                    DrawCircle(center + new Vector2(-13, -4), 12, new Color(Cyan, 0.38f));
                    DrawCircle(center + new Vector2(12, 7), 12, new Color(Mint, 0.38f));
                    DrawCircle(center + new Vector2(0, -17), 10, new Color(Amber, 0.34f));
                    break;
                case "unit":
                    if (!string.IsNullOrWhiteSpace(UnitDesignId))
                    {
                        DynamicUnitIcon.DrawUnitDesignIcon(this, rect.Grow(-8), UnitDesignCatalog.Spec(UnitDesignId), Accent, animated: true, framed: false);
                    }
                    else
                    {
                        DynamicUnitIcon.DrawFallbackIcon(this, rect.Grow(-8), Icon, Accent, framed: false);
                    }

                    break;
                default:
                    DrawLine(new Vector2(18, rect.Size.Y - 22), new Vector2(rect.Size.X - 18, 22), new Color(Mint, 0.32f), 2.2f, true);
                    DrawLine(new Vector2(24, 26), new Vector2(rect.Size.X - 22, rect.Size.Y - 24), new Color(Ink, 0.24f), 1.4f, true);
                    break;
            }
        }
    }

    private partial class SelectionIconSummary : Control
    {
        public IReadOnlyList<SelectionIconItem> Items { get; set; } = [];

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, CustomMinimumSize);
            DrawRect(rect, CurrentPalette.PanelSubtleFill, true);
            DrawRect(rect, new Color(Cyan, 0.24f), false, 1.1f);

            var maxItems = Math.Min(Items.Count, 4);
            for (var index = 0; index < maxItems; index++)
            {
                var item = Items[index];
                var column = index % 2;
                var row = index / 2;
                var origin = new Vector2(8 + column * 39, 8 + row * 42);
                var slot = new Rect2(origin, new Vector2(32, 34));
                DrawRect(slot, new Color(item.Accent, 0.08f), true);
                DrawRect(slot, new Color(item.Accent, 0.36f), false, 1.1f);
                if (!string.IsNullOrWhiteSpace(item.UnitDesignId))
                {
                    DynamicUnitIcon.DrawUnitDesignIcon(this, new Rect2(slot.Position + new Vector2(3, 1), new Vector2(26, 24)), UnitDesignCatalog.Spec(item.UnitDesignId), item.Accent, animated: true, framed: false);
                }
                else
                {
                    DynamicUnitIcon.DrawFallbackIcon(this, new Rect2(slot.Position + new Vector2(3, 1), new Vector2(26, 24)), item.Glyph, item.Accent, framed: false);
                }

                DrawString(UiFontProfile.DrawFont(UiFontRole.Compact), slot.Position + new Vector2(3, 31), item.Label, HorizontalAlignment.Left, 30, 8, new Color(item.Accent, 0.78f));
                DrawString(UiFontProfile.DrawFont(UiFontRole.Numeric), slot.Position + new Vector2(20, 31), item.Count.ToString(), HorizontalAlignment.Right, 10, 9, new Color(Ink, 0.88f));
            }
        }
    }

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

            _contentsLabel = MakeLabel(GameText.T("ui.group.empty"), new Vector2(6, 20), 9, style.Contents);
            _contentsLabel.CustomMinimumSize = new Vector2(50, 12);
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
            DrawString(UiFontProfile.DrawFont(UiFontRole.Compact), new Vector2(10, 12), AlertPrefix(alert.Kind), HorizontalAlignment.Left, 58, 10, new Color(color, alpha));
            DrawString(UiFontProfile.DrawFont(UiFontRole.Compact), new Vector2(70, 12), alert.Text, HorizontalAlignment.Left, rect.Size.X - 72, 10, new Color(Ink, alpha));
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
