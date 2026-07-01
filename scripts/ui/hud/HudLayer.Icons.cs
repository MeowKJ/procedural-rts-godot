using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private static void DrawIconGlyph(CanvasItem canvas, IconGlyph glyph, Vector2 center, float size, Color color)
    {
        if (TryDrawIconTexture(canvas, glyph, center, size, color))
        {
            return;
        }

        var r = size * 0.5f;
        switch (glyph)
        {
            case IconGlyph.Infantry:
                canvas.DrawArc(center, r * 0.62f, 0, Mathf.Tau, 48, color, 2, true);
                canvas.DrawLine(center + new Vector2(-r * 0.75f, r * 0.74f), center + new Vector2(r * 0.76f, -r * 0.48f), color, 2.4f, true);
                canvas.DrawLine(center + new Vector2(-r * 0.34f, -r * 0.56f), center + new Vector2(r * 0.72f, r * 0.54f), new Color("#ffffff", color.A * 0.64f), 1.5f, true);
                break;
            case IconGlyph.Tank:
                var tank = new Rect2(center - new Vector2(r * 0.82f, r * 0.42f), new Vector2(r * 1.36f, r * 0.84f));
                canvas.DrawRect(tank, new Color(color, 0.2f), true);
                canvas.DrawRect(tank, color, false, 2, true);
                canvas.DrawCircle(center, r * 0.24f, color);
                canvas.DrawLine(center + new Vector2(r * 0.1f, 0), center + new Vector2(r * 0.94f, 0), new Color("#ffffff", color.A * 0.76f), 2.2f, true);
                break;
            case IconGlyph.Harvester:
                Vector2[] harvester =
                [
                    center + new Vector2(r * 0.86f, 0),
                    center + new Vector2(r * 0.36f, r * 0.72f),
                    center + new Vector2(-r * 0.72f, r * 0.56f),
                    center + new Vector2(-r * 0.92f, 0),
                    center + new Vector2(-r * 0.72f, -r * 0.56f),
                    center + new Vector2(r * 0.36f, -r * 0.72f),
                ];
                canvas.DrawColoredPolygon(harvester, new Color(color, 0.15f));
                canvas.DrawPolyline([.. harvester, harvester[0]], color, 2, true);
                canvas.DrawLine(center + new Vector2(-r * 0.42f, -r * 0.22f), center + new Vector2(r * 0.34f, -r * 0.22f), new Color("#ffffff", color.A * 0.62f), 1.8f, true);
                canvas.DrawLine(center + new Vector2(-r * 0.42f, r * 0.22f), center + new Vector2(r * 0.34f, r * 0.22f), new Color("#ffffff", color.A * 0.62f), 1.8f, true);
                break;
            case IconGlyph.Building:
                var building = new Rect2(center - new Vector2(r * 0.66f, r * 0.54f), new Vector2(r * 1.32f, r * 1.08f));
                canvas.DrawRect(building, new Color(color, 0.16f), true);
                canvas.DrawRect(building, color, false, 2, true);
                canvas.DrawLine(center + new Vector2(-r * 0.66f, -r * 0.12f), center + new Vector2(r * 0.66f, -r * 0.12f), new Color("#ffffff", color.A * 0.54f), 1.4f, true);
                canvas.DrawLine(center + new Vector2(-r * 0.24f, -r * 0.54f), center + new Vector2(-r * 0.24f, r * 0.54f), new Color("#ffffff", color.A * 0.42f), 1.2f, true);
                canvas.DrawLine(center + new Vector2(r * 0.24f, -r * 0.54f), center + new Vector2(r * 0.24f, r * 0.54f), new Color("#ffffff", color.A * 0.42f), 1.2f, true);
                break;
            case IconGlyph.Turret:
                var turretBase = new Rect2(center - new Vector2(r * 0.58f, r * 0.2f), new Vector2(r * 1.16f, r * 0.5f));
                canvas.DrawRect(turretBase, new Color(color, 0.18f), true);
                canvas.DrawRect(turretBase, color, false, 1.8f, true);
                canvas.DrawCircle(center + new Vector2(-r * 0.08f, -r * 0.1f), r * 0.3f, new Color(color, 0.28f));
                canvas.DrawLine(center + new Vector2(r * 0.08f, -r * 0.12f), center + new Vector2(r * 0.88f, -r * 0.48f), new Color("#ffffff", color.A * 0.78f), 2.6f, true);
                canvas.DrawArc(center, r * 0.78f, Mathf.Pi * 0.08f, Mathf.Pi * 0.92f, 36, new Color(color, 0.34f), 1.4f, true);
                break;
            case IconGlyph.Air:
                Vector2[] aircraft =
                [
                    center + new Vector2(r * 0.88f, 0),
                    center + new Vector2(-r * 0.34f, r * 0.28f),
                    center + new Vector2(-r * 0.82f, r * 0.7f),
                    center + new Vector2(-r * 0.58f, r * 0.08f),
                    center + new Vector2(-r * 0.58f, -r * 0.08f),
                    center + new Vector2(-r * 0.82f, -r * 0.7f),
                    center + new Vector2(-r * 0.34f, -r * 0.28f),
                ];
                canvas.DrawColoredPolygon(aircraft, new Color(color, 0.16f));
                canvas.DrawPolyline([.. aircraft, aircraft[0]], color, 2, true);
                canvas.DrawLine(center + new Vector2(-r * 0.52f, 0), center + new Vector2(r * 0.56f, 0), new Color("#ffffff", color.A * 0.55f), 1.3f, true);
                break;
            case IconGlyph.Naval:
                Vector2[] hull =
                [
                    center + new Vector2(-r * 0.82f, -r * 0.08f),
                    center + new Vector2(r * 0.72f, -r * 0.08f),
                    center + new Vector2(r * 0.46f, r * 0.42f),
                    center + new Vector2(-r * 0.58f, r * 0.42f),
                ];
                canvas.DrawColoredPolygon(hull, new Color(color, 0.16f));
                canvas.DrawPolyline([.. hull, hull[0]], color, 2, true);
                canvas.DrawLine(center + new Vector2(-r * 0.18f, -r * 0.1f), center + new Vector2(-r * 0.18f, -r * 0.62f), new Color("#ffffff", color.A * 0.58f), 1.5f, true);
                canvas.DrawLine(center + new Vector2(-r * 0.18f, -r * 0.62f), center + new Vector2(r * 0.38f, -r * 0.28f), new Color("#ffffff", color.A * 0.58f), 1.5f, true);
                canvas.DrawArc(center + new Vector2(-r * 0.28f, r * 0.58f), r * 0.36f, 0, Mathf.Pi, 20, new Color(color, 0.42f), 1.3f, true);
                canvas.DrawArc(center + new Vector2(r * 0.4f, r * 0.58f), r * 0.36f, 0, Mathf.Pi, 20, new Color(color, 0.42f), 1.3f, true);
                break;
            case IconGlyph.Move:
                canvas.DrawLine(center + new Vector2(-r, 0), center + new Vector2(r, 0), color, 2.2f, true);
                canvas.DrawLine(center + new Vector2(0, -r), center + new Vector2(0, r), color, 2.2f, true);
                canvas.DrawArc(center, r * 0.82f, 0, Mathf.Tau, 48, new Color(color, 0.44f), 1.5f, true);
                break;
            case IconGlyph.AttackMove:
                canvas.DrawArc(center, r * 0.72f, 0, Mathf.Tau, 48, color, 2.1f, true);
                canvas.DrawLine(center + new Vector2(-r, 0), center + new Vector2(-r * 0.34f, 0), color, 2.1f, true);
                canvas.DrawLine(center + new Vector2(r * 0.34f, 0), center + new Vector2(r, 0), color, 2.1f, true);
                canvas.DrawLine(center + new Vector2(0, -r), center + new Vector2(0, -r * 0.34f), color, 2.1f, true);
                canvas.DrawLine(center + new Vector2(0, r * 0.34f), center + new Vector2(0, r), color, 2.1f, true);
                break;
            case IconGlyph.IgnoreMove:
                canvas.DrawLine(center + new Vector2(-r * 0.82f, -r * 0.82f), center + new Vector2(r * 0.82f, r * 0.82f), color, 2.4f, true);
                canvas.DrawLine(center + new Vector2(-r * 0.82f, r * 0.82f), center + new Vector2(r * 0.82f, -r * 0.82f), color, 2.4f, true);
                canvas.DrawArc(center, r * 0.9f, 0, Mathf.Tau, 48, new Color(color, 0.42f), 1.5f, true);
                break;
            case IconGlyph.StanceHold:
                canvas.DrawRect(new Rect2(center - new Vector2(r * 0.62f, r * 0.62f), new Vector2(r * 1.24f, r * 1.24f)), new Color(color, 0.12f), true);
                canvas.DrawRect(new Rect2(center - new Vector2(r * 0.62f, r * 0.62f), new Vector2(r * 1.24f, r * 1.24f)), color, false, 2, true);
                canvas.DrawCircle(center, r * 0.25f, new Color("#ffffff", color.A * 0.66f));
                break;
            case IconGlyph.StanceAggressive:
                canvas.DrawLine(center + new Vector2(-r * 0.78f, r * 0.62f), center + new Vector2(r * 0.74f, -r * 0.58f), color, 2.5f, true);
                canvas.DrawLine(center + new Vector2(r * 0.16f, -r * 0.7f), center + new Vector2(r * 0.76f, -r * 0.58f), color, 2.5f, true);
                canvas.DrawLine(center + new Vector2(r * 0.62f, 0), center + new Vector2(r * 0.76f, -r * 0.58f), color, 2.5f, true);
                canvas.DrawArc(center, r * 0.72f, 0.25f, Mathf.Tau * 0.82f, 48, new Color(color, 0.42f), 1.5f, true);
                break;
            case IconGlyph.StanceReturn:
                canvas.DrawArc(center, r * 0.76f, -Mathf.Pi * 0.2f, Mathf.Pi * 1.35f, 56, color, 2.2f, true);
                canvas.DrawLine(center + new Vector2(-r * 0.42f, -r * 0.64f), center + new Vector2(-r * 0.78f, -r * 0.22f), color, 2.2f, true);
                canvas.DrawLine(center + new Vector2(-r * 0.2f, -r * 0.2f), center + new Vector2(-r * 0.78f, -r * 0.22f), color, 2.2f, true);
                canvas.DrawCircle(center, r * 0.24f, new Color("#ffffff", color.A * 0.58f));
                break;
            case IconGlyph.StancePassive:
                canvas.DrawArc(center, r * 0.78f, 0, Mathf.Tau, 48, color, 2.1f, true);
                canvas.DrawLine(center + new Vector2(-r * 0.58f, 0), center + new Vector2(r * 0.58f, 0), color, 2.1f, true);
                canvas.DrawCircle(center, r * 0.16f, new Color("#ffffff", color.A * 0.58f));
                break;
            case IconGlyph.StanceIgnore:
                canvas.DrawArc(center, r * 0.78f, 0, Mathf.Tau, 48, color, 2.1f, true);
                canvas.DrawLine(center + new Vector2(-r * 0.62f, -r * 0.62f), center + new Vector2(r * 0.62f, r * 0.62f), color, 2.5f, true);
                canvas.DrawLine(center + new Vector2(-r * 0.62f, r * 0.62f), center + new Vector2(r * 0.62f, -r * 0.62f), color, 2.5f, true);
                break;
            case IconGlyph.Group:
                canvas.DrawCircle(center + new Vector2(-r * 0.38f, -r * 0.12f), r * 0.34f, new Color(color, 0.42f));
                canvas.DrawCircle(center + new Vector2(r * 0.36f, r * 0.18f), r * 0.34f, new Color(color, 0.32f));
                canvas.DrawCircle(center + new Vector2(0, -r * 0.54f), r * 0.28f, new Color("#ffffff", color.A * 0.28f));
                break;
            case IconGlyph.Cancel:
                canvas.DrawLine(center + new Vector2(-r * 0.74f, -r * 0.74f), center + new Vector2(r * 0.74f, r * 0.74f), color, 2.4f, true);
                canvas.DrawLine(center + new Vector2(-r * 0.74f, r * 0.74f), center + new Vector2(r * 0.74f, -r * 0.74f), color, 2.4f, true);
                break;
            case IconGlyph.Settings:
                canvas.DrawArc(center, r * 0.72f, 0, Mathf.Tau, 48, color, 2.1f, true);
                canvas.DrawCircle(center, r * 0.24f, new Color(color, 0.55f), false, 1.8f, true);
                for (var index = 0; index < 8; index++)
                {
                    var angle = index * Mathf.Tau / 8f;
                    var from = center + Vector2.FromAngle(angle) * r * 0.82f;
                    var to = center + Vector2.FromAngle(angle) * r * 1.04f;
                    canvas.DrawLine(from, to, color, 1.7f, true);
                }
                break;
            default:
                canvas.DrawLine(center + new Vector2(-r * 0.72f, r * 0.72f), center + new Vector2(r * 0.72f, -r * 0.72f), new Color(color, 0.54f), 2, true);
                break;
        }
    }

    private static bool TryDrawIconTexture(CanvasItem canvas, IconGlyph glyph, Vector2 center, float size, Color color)
    {
        if (!IconLibrary.TryPath(glyph, out var path))
        {
            return false;
        }

        if (!IconTextureCache.TryGetValue(glyph, out var texture))
        {
            texture = LoadSvgIconTexture(path);
            IconTextureCache[glyph] = texture;
        }

        if (texture is null)
        {
            return false;
        }

        var rect = new Rect2(center - new Vector2(size, size) / 2f, new Vector2(size, size));
        canvas.DrawTextureRect(texture, rect, false, color);
        return true;
    }

    private static Texture2D? LoadSvgIconTexture(string path)
    {
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file is null)
        {
            return null;
        }

        var svg = file.GetAsText();
        if (string.IsNullOrWhiteSpace(svg))
        {
            return null;
        }

        using var image = new Image();
        if (image.LoadSvgFromString(svg, 4) != Error.Ok)
        {
            return null;
        }

        return ImageTexture.CreateFromImage(image);
    }
}
