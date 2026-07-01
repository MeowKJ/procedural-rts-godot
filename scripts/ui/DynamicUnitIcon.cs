using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class DynamicUnitIcon : Control
{
    public string? UnitDesignId { get; set; }
    public IconGlyph FallbackGlyph { get; set; } = IconGlyph.None;
    public Color Accent { get; set; } = new("#8fffe1");
    public bool Animated { get; set; } = true;
    public bool Framed { get; set; }

    public override void _Process(double delta)
    {
        if (Animated && Visible)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Size.X > 0 && Size.Y > 0 ? Size : CustomMinimumSize);
        if (!string.IsNullOrWhiteSpace(UnitDesignId))
        {
            DrawUnitDesignIcon(this, rect, UnitDesignCatalog.Spec(UnitDesignId), Accent, Animated, Framed);
            return;
        }

        DrawFallbackIcon(this, rect, FallbackGlyph, Accent, Framed);
    }

    public static void DrawUnitDesignIcon(CanvasItem canvas, Rect2 rect, UnitSpec spec, Color playerAccent, bool animated, bool framed)
    {
        if (framed)
        {
            canvas.DrawRect(rect, new Color("#050b11", 0.74f), true);
            canvas.DrawRect(rect, new Color(playerAccent, 0.28f), false, 1.1f);
        }

        var pulse = animated
            ? 0.5f + Mathf.Sin(Time.GetTicksMsec() / 360f + spec.Id.GetHashCode() * 0.01f) * 0.5f
            : 0.5f;
        var factionAccent = spec.Faction switch
        {
            UnitFactionId.Dog => new Color("#64c7c7"),
            UnitFactionId.Cat => new Color("#c98293"),
            UnitFactionId.Corruption => new Color("#9d4259"),
            _ => playerAccent,
        };
        var palette = UnitRenderPalette.SoftOldCity(factionAccent, new Color(playerAccent, Mathf.Lerp(0.72f, 1f, pulse)));
        var center = rect.Position + rect.Size * 0.5f;
        var scale = Mathf.Min(rect.Size.X, rect.Size.Y) / 76f;
        var bodyFacing = animated ? Mathf.Sin(Time.GetTicksMsec() / 1600f + spec.Id.Length) * 0.08f : 0;
        var turretFacing = animated ? Mathf.Sin(Time.GetTicksMsec() / 900f + spec.Id.Length * 2.3f) * 0.18f : 0;

        canvas.DrawCircle(center, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.34f, new Color(playerAccent, 0.055f + pulse * 0.035f));
        UnitVisualRenderer.DrawUnitArtRecipe(canvas, spec.Art, palette, center, scale, bodyFacing, new Dictionary<string, float> { ["main"] = turretFacing });
    }

    public static void DrawFallbackIcon(
        CanvasItem canvas,
        Rect2 rect,
        IconGlyph fallbackGlyph,
        Color accent,
        bool framed)
    {
        if (framed)
        {
            canvas.DrawRect(rect, new Color("#050b11", 0.74f), true);
            canvas.DrawRect(rect, new Color(accent, 0.28f), false, 1.1f);
        }

        DrawFallback(canvas, rect, fallbackGlyph, accent);
    }

    private static void DrawFallback(CanvasItem canvas, Rect2 rect, IconGlyph glyph, Color accent)
    {
        var center = rect.Position + rect.Size * 0.5f;
        var radius = Mathf.Min(rect.Size.X, rect.Size.Y) * 0.25f;
        if (glyph == IconGlyph.None)
        {
            canvas.DrawLine(center + new Vector2(-radius, radius), center + new Vector2(radius, -radius), new Color(accent, 0.58f), 2, true);
            return;
        }

        canvas.DrawArc(center, radius, 0, Mathf.Tau, 48, new Color(accent, 0.72f), 2, true);
    }
}
