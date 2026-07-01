using Godot;

namespace ProceduralRts;

public partial class UnitShowcaseRoot
{
    private void DrawBackground(Vector2 size)
    {
        DrawRect(new Rect2(Vector2.Zero, size), Paper);
        for (var x = 0f; x < size.X; x += 40)
        {
            DrawLine(new Vector2(x, 0), new Vector2(x, size.Y), new Color("#8b7c68", 0.13f), 1f, true);
        }

        for (var y = 0f; y < size.Y; y += 40)
        {
            DrawLine(new Vector2(0, y), new Vector2(size.X, y), new Color("#8b7c68", 0.13f), 1f, true);
        }

        DrawLine(new Vector2(0, size.Y * 0.78f), new Vector2(size.X, size.Y * 0.55f), new Color(Dog, 0.10f), 18f, true);
        DrawLine(new Vector2(size.X * 0.16f, size.Y * 0.20f), new Vector2(size.X, size.Y * 0.42f), new Color(Cat, 0.10f), 5f, true);
    }

    private void DrawHeader(Vector2 size)
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(36, 48), "Two-Faction Unit Showcase", HorizontalAlignment.Left, 560, 30, Ink);
        DrawString(
            ThemeDB.FallbackFont,
            new Vector2(38, 82),
            "Godot-rendered T1-T3 unit sheet. Faction identity is shape language; team ownership can still be recolored later.",
            HorizontalAlignment.Left,
            size.X - 76,
            15,
            InkSoft);

        DrawPill(new Rect2(size.X - 408, 34, 174, 32), "DOG: heavy / loyal", Dog, DogDark);
        DrawPill(new Rect2(size.X - 218, 34, 174, 32), "CAT: sharp / hidden", Cat, CatDark);
    }

    private void DrawFaction(Rect2 rect, UnitSpec[] units, Color accent, Color dark)
    {
        var factionName = accent == Dog ? "DOG GUARD" : "CAT DRIFT";
        var subtitle = accent == Dog ? "warm shields, repaired lights, thick armor" : "thin silhouettes, moon marks, evasive vectors";

        DrawRect(rect, new Color("#f5ead9", 0.42f));
        DrawRect(rect, new Color(dark, 0.22f), false, 1.3f);
        DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(20, 31), factionName, HorizontalAlignment.Left, rect.Size.X - 40, 22, dark);
        DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(22, 56), subtitle, HorizontalAlignment.Left, rect.Size.X - 40, 12, new Color(Ink, 0.56f));

        const int columns = 3;
        var rows = Mathf.CeilToInt(units.Length / (float)columns);
        var top = rect.Position.Y + 82;
        var gap = 12f;
        var cellWidth = (rect.Size.X - 40 - gap * (columns - 1)) / columns;
        var cellHeight = (rect.End.Y - top - 24 - gap * (rows - 1)) / rows;

        for (var i = 0; i < units.Length; i++)
        {
            var col = i % columns;
            var row = i / columns;
            var cell = new Rect2(
                rect.Position.X + 20 + col * (cellWidth + gap),
                top + row * (cellHeight + gap),
                cellWidth,
                cellHeight);
            DrawUnitCell(cell, units[i], accent, dark);
        }
    }

    private void DrawUnitCell(Rect2 cell, UnitSpec unit, Color accent, Color dark)
    {
        DrawRect(cell, new Color("#fff8ed", 0.34f));
        DrawRect(cell, new Color(dark, 0.18f), false, 1f);

        var platform = new Rect2(cell.Position + new Vector2(12, 10), new Vector2(cell.Size.X - 24, cell.Size.Y * 0.52f));
        DrawRect(platform, new Color(accent, 0.055f));
        DrawRect(platform, new Color(accent, 0.15f), false, 1f);
        DrawGridInside(platform, accent);

        var center = platform.GetCenter() + new Vector2(0, 2 + Mathf.Sin(_elapsed * 1.1f + unit.Code.GetHashCode() * 0.01f) * 1.2f);
        DrawUnit(unit.Shape, center, Mathf.Min(platform.Size.X, platform.Size.Y) / 98f, accent, dark, unit.Role);

        var roleColor = RoleColor(unit.Role, accent);
        DrawPill(new Rect2(cell.Position.X + 12, cell.End.Y - 44, 44, 19), unit.Code, roleColor, dark);
        DrawString(ThemeDB.FallbackFont, new Vector2(cell.Position.X + 64, cell.End.Y - 29), unit.Name, HorizontalAlignment.Left, cell.Size.X - 76, 12, Ink);
        DrawString(ThemeDB.FallbackFont, new Vector2(cell.Position.X + 14, cell.End.Y - 10), unit.Description, HorizontalAlignment.Left, cell.Size.X - 28, 9, new Color(Ink, 0.56f));
    }

    private void DrawGridInside(Rect2 rect, Color accent)
    {
        for (var x = rect.Position.X + 24; x < rect.End.X; x += 24)
        {
            DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), new Color(accent, 0.055f), 1f, true);
        }

        for (var y = rect.Position.Y + 18; y < rect.End.Y; y += 18)
        {
            DrawLine(new Vector2(rect.Position.X, y), new Vector2(rect.End.X, y), new Color(accent, 0.055f), 1f, true);
        }
    }
}
