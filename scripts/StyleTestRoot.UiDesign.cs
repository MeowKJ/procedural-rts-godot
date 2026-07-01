using Godot;

namespace ProceduralRts;

public partial class StyleTestRoot
{
    private static Rect2 PanelRect(Vector2 size, int index)
    {
        var margin = 28f;
        var gap = 18f;
        var top = 112f;
        var panelWidth = (size.X - margin * 2 - gap) * 0.5f;
        var panelHeight = (size.Y - top - margin - gap) * 0.5f;
        var col = index % 2;
        var row = index / 2;
        return new Rect2(
            new Vector2(margin + col * (panelWidth + gap), top + row * (panelHeight + gap)),
            new Vector2(panelWidth, panelHeight));
    }

    private void DrawHeader(Vector2 size)
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(30, 42), "Godot RTS Style Test", HorizontalAlignment.Left, 520, 30, Ink);
        DrawString(
            ThemeDB.FallbackFont,
            new Vector2(32, 74),
            "Engine-rendered samples: terrain, units, command lines, selection, and edge HUD. Light themes need filled unit masses; pure line units prefer a mid board.",
            HorizontalAlignment.Left,
            size.X - 64,
            15,
            MutedInk);

        DrawLegend(new Vector2(size.X - 454, 34));
    }

    private void DrawLegend(Vector2 origin)
    {
        var items = new[]
        {
            ("Dog ownership", new Color("#d9a441")),
            ("Cat ownership", new Color("#7569b9")),
            ("AI pressure", new Color("#be315d")),
        };

        for (var i = 0; i < items.Length; i++)
        {
            var pos = origin + new Vector2(i * 138, 0);
            DrawCircle(pos + new Vector2(8, 8), 7, items[i].Item2);
            DrawString(ThemeDB.FallbackFont, pos + new Vector2(22, 13), items[i].Item1, HorizontalAlignment.Left, 110, 12, MutedInk);
        }
    }

    private void DrawPanel(Rect2 rect, StyleSpec style)
    {
        DrawRect(rect, style.Background);
        DrawRect(rect, new Color(style.Ink, style.Dark ? 0.42f : 0.20f), false, 1.2f);
        DrawGrid(rect, style);
        DrawOldCityRoutes(rect, style);
        DrawCorruption(rect, style);
        DrawSafeLights(rect, style);
        DrawCommandPreview(rect, style);
        DrawUnits(rect, style);
        DrawEdgeHud(rect, style);
        DrawPanelTitle(rect, style);
    }

    private void DrawPanelTitle(Rect2 rect, StyleSpec style)
    {
        var tagRect = new Rect2(rect.Position + new Vector2(14, 12), new Vector2(34, 24));
        DrawRect(tagRect, new Color(style.Ink, style.Dark ? 0.18f : 0.10f));
        DrawRect(tagRect, new Color(style.Ink, 0.30f), false, 1f);
        DrawString(ThemeDB.FallbackFont, tagRect.Position + new Vector2(9, 17), style.Tag, HorizontalAlignment.Left, 26, 15, style.Ink);
        DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(56, 30), style.Title, HorizontalAlignment.Left, rect.Size.X - 80, 17, style.Ink);
        DrawString(
            ThemeDB.FallbackFont,
            rect.Position + new Vector2(56, 51),
            style.Verdict,
            HorizontalAlignment.Left,
            rect.Size.X - 80,
            12,
            new Color(style.Ink, style.Dark ? 0.62f : 0.58f));
    }

    private void DrawEdgeHud(Rect2 rect, StyleSpec style)
    {
        var hudFill = new Color(style.Background.Lerp(style.Ink, style.Dark ? 0.16f : 0.06f), style.Dark ? 0.72f : 0.76f);
        var hudStroke = new Color(style.Ink, style.Dark ? 0.34f : 0.18f);
        var top = new Rect2(rect.Position + new Vector2(12, rect.Size.Y - 34), new Vector2(rect.Size.X * 0.55f, 22));
        DrawRect(top, hudFill);
        DrawRect(top, hudStroke, false, 1f);
        DrawString(ThemeDB.FallbackFont, top.Position + new Vector2(10, 16), "edge HUD: resources / stance / time", HorizontalAlignment.Left, top.Size.X - 20, 11, new Color(style.Ink, 0.62f));

        var mini = new Rect2(rect.Position + new Vector2(rect.Size.X - 112, rect.Size.Y - 108), new Vector2(92, 86));
        DrawRect(mini, new Color(style.Ink, style.Dark ? 0.16f : 0.08f));
        DrawRect(mini, hudStroke, false, 1f);
        DrawCircle(mini.Position + new Vector2(24, 56), 3, style.Dog);
        DrawCircle(mini.Position + new Vector2(46, 42), 3, style.Cat);
        DrawCircle(mini.Position + new Vector2(68, 30), 3, style.Ai);
        DrawRect(new Rect2(mini.Position + new Vector2(14, 18), new Vector2(44, 34)), new Color(style.Dog, 0.20f), false, 1f);
    }
}
