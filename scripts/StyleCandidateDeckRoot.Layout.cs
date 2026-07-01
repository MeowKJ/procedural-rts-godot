using Godot;

namespace ProceduralRts;

public partial class StyleCandidateDeckRoot
{
    private void DrawHeader(Vector2 size, StyleFamily[] families, StyleFamily selected)
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(34, 44), "Choose One Visual Family", HorizontalAlignment.Left, 520, 30, Ink);
        DrawString(
            ThemeDB.FallbackFont,
            new Vector2(36, 78),
            "Each family has two light variants and one dark crisis variant. Pick the family first; variants become day/night/story states.",
            HorizontalAlignment.Left,
            size.X - 72,
            15,
            Muted);

        var tabTop = 114f;
        var tabWidth = (size.X - 68 - 10 * (families.Length - 1)) / families.Length;
        for (var i = 0; i < families.Length; i++)
        {
            var rect = new Rect2(34 + i * (tabWidth + 10), tabTop, tabWidth, 36);
            var active = families[i] == selected;
            DrawRect(rect, active ? new Color("#fff2dc", 0.72f) : new Color("#ead8bd", 0.42f));
            DrawRect(rect, new Color(active ? "#2b2b2b" : "#7d705e", active ? 0.42f : 0.24f), false, active ? 1.6f : 1f);
            DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(10, 24), families[i].Code, HorizontalAlignment.Left, rect.Size.X - 20, 12, Ink);
        }
    }

    private void DrawFamily(StyleFamily family, Rect2 area)
    {
        DrawString(ThemeDB.FallbackFont, area.Position + new Vector2(0, 28), $"{family.Code}: {family.Name}", HorizontalAlignment.Left, 520, 24, Ink);
        DrawString(ThemeDB.FallbackFont, area.Position + new Vector2(0, 55), family.ChineseName, HorizontalAlignment.Left, 240, 14, new Color(Ink, 0.72f));
        DrawString(ThemeDB.FallbackFont, area.Position + new Vector2(258, 55), family.Note, HorizontalAlignment.Left, area.Size.X - 258, 13, new Color(Ink, 0.58f));

        var top = area.Position.Y + 82;
        var gap = 18f;
        var panelWidth = (area.Size.X - gap * 2) / 3f;
        var panelHeight = area.End.Y - top;

        for (var i = 0; i < family.Variants.Length; i++)
        {
            var rect = new Rect2(area.Position.X + i * (panelWidth + gap), top, panelWidth, panelHeight);
            DrawVariant(rect, family.Variants[i]);
        }
    }

    private void DrawVariant(Rect2 rect, VariantSpec style)
    {
        DrawRect(rect, style.Background);
        DrawRect(rect, new Color(style.Ink, style.Dark ? 0.42f : 0.22f), false, 1.4f);
        DrawVariantTerrain(rect, style);
        DrawBattleSample(rect, style);
        DrawEdgeHud(rect, style);
        DrawVariantTitle(rect, style);
    }

    private void DrawVariantTitle(Rect2 rect, VariantSpec style)
    {
        var tag = new Rect2(rect.Position + new Vector2(14, 14), new Vector2(46, 28));
        DrawRect(tag, new Color(style.Ink, style.Dark ? 0.18f : 0.08f));
        DrawRect(tag, new Color(style.Ink, 0.30f), false, 1f);
        DrawString(ThemeDB.FallbackFont, tag.Position + new Vector2(9, 20), style.Code, HorizontalAlignment.Left, 32, 14, style.Ink);
        DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(72, 36), style.Name, HorizontalAlignment.Left, rect.Size.X - 94, 18, style.Ink);
        DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(72, 58), style.Role, HorizontalAlignment.Left, rect.Size.X - 94, 12, new Color(style.Ink, style.Dark ? 0.64f : 0.54f));
    }

    private void DrawEdgeHud(Rect2 rect, VariantSpec style)
    {
        var hud = new Rect2(rect.Position + new Vector2(14, rect.Size.Y - 36), new Vector2(rect.Size.X * 0.55f, 22));
        DrawRect(hud, new Color(style.Background.Lerp(style.Ink, style.Dark ? 0.18f : 0.08f), style.Dark ? 0.64f : 0.58f));
        DrawRect(hud, new Color(style.Ink, style.Dark ? 0.34f : 0.16f), false, 1f);
        DrawString(ThemeDB.FallbackFont, hud.Position + new Vector2(10, 16), "edge HUD", HorizontalAlignment.Left, hud.Size.X - 20, 10, new Color(style.Ink, style.Dark ? 0.78f : 0.58f));

        var mini = new Rect2(rect.End.X - 88, rect.End.Y - 82, 66, 60);
        DrawRect(mini, new Color(style.Ink, style.Dark ? 0.12f : 0.065f));
        DrawRect(mini, new Color(style.Ink, style.Dark ? 0.32f : 0.16f), false, 1f);
        DrawCircle(mini.Position + new Vector2(18, 40), 3f, style.Dog);
        DrawCircle(mini.Position + new Vector2(34, 25), 3f, style.Cat);
        DrawCircle(mini.Position + new Vector2(50, 18), 3f, style.Ai);
    }
}
