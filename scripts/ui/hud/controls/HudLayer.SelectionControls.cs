using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
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
            HudIconRenderer.Draw(this, Glyph, rect.Size / 2f, 28, style.Icon);
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

                DrawString(UiFontProfile.DrawFont(UiFontRole.Numeric), slot.Position + new Vector2(6, 32), item.Count.ToString(), HorizontalAlignment.Right, 22, 11, new Color(Ink, 0.88f));
            }
        }
    }
}
