using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class BuildPlacementController
{
    private void DrawFootprintPreview(Rect2 rect, Color accent, float pulse, bool isValid)
    {
        var pad = 14;
        var footprint = rect.Grow(pad);
        DrawRect(footprint, new Color(accent, isValid ? 0.11f : 0.16f), true);
        DrawRect(footprint, new Color("#ffffff", isValid ? 0.22f + pulse * 0.18f : 0.08f), false, 1.4f);
        DrawRect(footprint, new Color(accent, isValid ? 0.62f : 0.86f), false, 3.4f);

        var step = 32f;
        for (var x = footprint.Position.X + step; x < footprint.End.X; x += step)
        {
            DrawLine(new Vector2(x, footprint.Position.Y), new Vector2(x, footprint.End.Y), new Color(accent, 0.16f), 1, true);
        }

        for (var y = footprint.Position.Y + step; y < footprint.End.Y; y += step)
        {
            DrawLine(new Vector2(footprint.Position.X, y), new Vector2(footprint.End.X, y), new Color(accent, 0.16f), 1, true);
        }
    }

    private void DrawStructurePreview(Rect2 rect, Color accent, float pulse, bool isValid)
    {
        DrawRect(rect, new Color("#07111d", isValid ? 0.44f : 0.3f), true);
        DrawRect(rect, new Color(accent, 0.82f), false, 2.2f);

        var centerGlow = Mathf.Min(rect.Size.X, rect.Size.Y) * (0.2f + pulse * 0.025f);
        DrawCircle(Vector2.Zero, centerGlow, new Color(accent, 0.16f));

        switch (BuildOrder[_selectedIndex])
        {
            case BuildingDesignIds.PowerPlant:
                DrawArc(Vector2.Zero, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.28f, 0, Mathf.Tau, 72, new Color("#ffffff", 0.62f), 2.2f, true);
                break;
            case BuildingDesignIds.Barracks:
                DrawLine(new Vector2(rect.Position.X + 18, 0), new Vector2(rect.End.X - 18, 0), new Color("#ffffff", 0.5f), 2, true);
                break;
            case BuildingDesignIds.VehicleFactory:
                DrawRect(new Rect2(rect.Position + new Vector2(22, rect.Size.Y * 0.5f), new Vector2(rect.Size.X - 44, rect.Size.Y * 0.28f)), new Color("#ffffff", 0.18f), true);
                break;
            case BuildingDesignIds.Refinery:
                DrawCircle(new Vector2(-rect.Size.X * 0.18f, 0), rect.Size.Y * 0.16f, new Color("#ffffff", 0.18f));
                DrawCircle(new Vector2(rect.Size.X * 0.18f, 0), rect.Size.Y * 0.16f, new Color("#ffffff", 0.18f));
                break;
            case BuildingDesignIds.Headquarters:
                DrawArc(Vector2.Zero, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.24f, 0, Mathf.Tau, 72, new Color(accent, 0.9f), 2.6f, true);
                DrawLine(new Vector2(0, rect.Position.Y + 12), new Vector2(0, rect.End.Y - 12), new Color("#ffffff", 0.45f), 1.8f, true);
                break;
        }
    }

    private void DrawPlacementCursor(Rect2 rect, Color accent, bool isValid)
    {
        var size = rect.Size;
        var half = size / 2f;
        var color = isValid ? new Color("#ffffff", 0.72f) : new Color("#ff5d75", 0.88f);
        const float arm = 26;

        DrawLine(new Vector2(-half.X - arm, 0), new Vector2(-half.X - 6, 0), color, 2.4f, true);
        DrawLine(new Vector2(half.X + 6, 0), new Vector2(half.X + arm, 0), color, 2.4f, true);
        DrawLine(new Vector2(0, -half.Y - arm), new Vector2(0, -half.Y - 6), color, 2.4f, true);
        DrawLine(new Vector2(0, half.Y + 6), new Vector2(0, half.Y + arm), color, 2.4f, true);

        if (isValid)
        {
            DrawCircle(Vector2.Zero, 6, new Color(accent, 0.68f));
            return;
        }

        DrawLine(new Vector2(-18, -18), new Vector2(18, 18), color, 3.2f, true);
        DrawLine(new Vector2(-18, 18), new Vector2(18, -18), color, 3.2f, true);
    }
}
