using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class BuildingView : Node2D
{
    private void DrawDamageReadability(
        Rect2 rect,
        BuildingArtColors art,
        float pulse,
        BuildingDamageReadabilityLevel damageSeverity,
        float missingHealthFraction)
    {
        if (damageSeverity == BuildingDamageReadabilityLevel.None)
        {
            return;
        }

        var missing = Mathf.Clamp(missingHealthFraction, 0, 1);
        var crackAlpha = Mathf.Lerp(0.34f, 0.68f, missing);
        var crackColor = new Color(art.Ink, crackAlpha);
        var warningColor = DamageReadabilityColor(damageSeverity);

        DrawDamageGap(
            rect.Position + new Vector2(rect.Size.X * 0.20f, 0),
            rect.Position + new Vector2(rect.Size.X * 0.34f, 0),
            art);
        DrawLine(
            rect.Position + new Vector2(rect.Size.X * 0.27f, 3),
            rect.Position + new Vector2(rect.Size.X * 0.32f, 16),
            crackColor,
            1.7f,
            true);
        DrawLine(
            rect.Position + new Vector2(rect.Size.X * 0.32f, 16),
            rect.Position + new Vector2(rect.Size.X * 0.27f, 27),
            new Color(art.Ink, crackAlpha * 0.72f),
            1.2f,
            true);

        if (damageSeverity >= BuildingDamageReadabilityLevel.Moderate)
        {
            DrawDamageGap(
                new Vector2(rect.End.X, rect.Position.Y + rect.Size.Y * 0.58f),
                new Vector2(rect.End.X, rect.Position.Y + rect.Size.Y * 0.74f),
                art);
            DrawLine(
                new Vector2(rect.End.X - 4, rect.Position.Y + rect.Size.Y * 0.62f),
                new Vector2(rect.End.X - 18, rect.Position.Y + rect.Size.Y * 0.68f),
                crackColor,
                1.6f,
                true);
            DrawLine(
                new Vector2(rect.End.X - 18, rect.Position.Y + rect.Size.Y * 0.68f),
                new Vector2(rect.End.X - 12, rect.Position.Y + rect.Size.Y * 0.78f),
                new Color(art.Ink, crackAlpha * 0.68f),
                1.1f,
                true);
        }

        if (damageSeverity >= BuildingDamageReadabilityLevel.Heavy)
        {
            DrawDamageGap(
                rect.End - new Vector2(rect.Size.X * 0.32f, 0),
                rect.End - new Vector2(rect.Size.X * 0.18f, 0),
                art);
            DrawLine(
                rect.End - new Vector2(rect.Size.X * 0.26f, 4),
                rect.End - new Vector2(rect.Size.X * 0.31f, 21),
                crackColor,
                1.8f,
                true);
            DrawDamageSpark(
                rect.Position + new Vector2(rect.Size.X * 0.30f, 12),
                warningColor,
                pulse,
                0);
            DrawDamageSpark(
                new Vector2(rect.End.X - 14, rect.Position.Y + rect.Size.Y * 0.66f),
                warningColor,
                pulse,
                1);
        }

        if (damageSeverity >= BuildingDamageReadabilityLevel.Critical)
        {
            DrawDamageSpark(
                rect.End - new Vector2(rect.Size.X * 0.28f, 12),
                warningColor,
                pulse,
                2);
            DrawArc(
                rect.Position + rect.Size * 0.5f,
                Mathf.Min(rect.Size.X, rect.Size.Y) * 0.42f,
                -Mathf.Pi * 0.08f,
                Mathf.Pi * 0.22f,
                28,
                new Color(warningColor, 0.22f + pulse * 0.12f),
                1.1f,
                true);
        }
    }

    private void DrawDamageGap(Vector2 from, Vector2 to, BuildingArtColors art)
    {
        DrawLine(from, to, new Color(art.Body, 0.92f), 6.0f, true);
        DrawLine(from, to, new Color(art.Shadow, 0.36f), 2.0f, true);
    }

    private void DrawDamageSpark(Vector2 origin, Color warningColor, float pulse, int phase)
    {
        var localPulse = 0.55f + Mathf.Sin((float)Time.GetTicksMsec() / 270f + phase * 1.9f) * 0.35f;
        var alpha = 0.32f + Mathf.Max(pulse, localPulse) * 0.28f;
        DrawCircle(origin, 2.4f + localPulse * 1.2f, new Color(warningColor, alpha * 0.46f));
        DrawLine(origin + new Vector2(-5, -1), origin + new Vector2(5, 1), new Color(warningColor, alpha), 1.2f, true);
        DrawLine(origin + new Vector2(0, -5), origin + new Vector2(0, 5), new Color(SoftOldCityPalette.InnerLight, alpha * 0.76f), 0.9f, true);
    }

    private static Color DamageReadabilityColor(BuildingDamageReadabilityLevel damageSeverity)
    {
        return damageSeverity >= BuildingDamageReadabilityLevel.Heavy
            ? SoftOldCityPalette.Danger
            : SoftOldCityPalette.Cargo;
    }

    private void DrawPausedConstructionProgress(Rect2 rect, BuildingArtColors art, float buildProgress, ConstructionPauseReason pauseReason)
    {
        var clamped = Mathf.Clamp(buildProgress, 0, 1);
        var statusColor = pauseReason == ConstructionPauseReason.Unpowered
            ? SoftOldCityPalette.Cargo
            : SoftOldCityPalette.Danger;
        var bar = new Rect2(
            new Vector2(rect.Position.X + 14, rect.End.Y - 24),
            new Vector2(rect.Size.X - 28, 8));
        var fill = new Rect2(bar.Position, new Vector2(bar.Size.X * clamped, bar.Size.Y));
        DrawRect(bar, new Color(art.Ink, 0.42f), true);
        DrawRect(fill, new Color(statusColor, 0.58f), true);
        DrawRect(bar, new Color(SoftOldCityPalette.InnerLight, 0.36f), false, 1.0f);

        var hatchCount = Mathf.Clamp(Mathf.CeilToInt(fill.Size.X / 24f), 1, 4);
        for (var index = 0; index < hatchCount; index++)
        {
            var x = fill.Position.X + 7 + index * 18;
            if (x > fill.End.X)
            {
                break;
            }

            DrawLine(
                new Vector2(x, bar.End.Y + 3),
                new Vector2(Mathf.Min(x + 9, fill.End.X), bar.Position.Y - 3),
                new Color(SoftOldCityPalette.InnerLight, 0.62f),
                1.2f,
                true);
        }
    }

    private void DrawOfflineStatusBadge(
        Rect2 rect,
        BuildingArtColors art,
        float pulse,
        bool powered,
        bool constructionPaused,
        ConstructionPauseReason pauseReason)
    {
        var offline = !powered || pauseReason == ConstructionPauseReason.Unpowered;
        var statusColor = offline ? SoftOldCityPalette.Cargo : SoftOldCityPalette.Danger;
        var center = new Vector2(rect.End.X + 12, rect.Position.Y + 12);
        var radius = 13f + pulse * 1.4f;
        DrawCircle(center, radius + 3, new Color(art.Ink, 0.34f));
        DrawCircle(center, radius, new Color(art.Body, 0.74f));
        DrawArc(center, radius, 0, Mathf.Tau, MediumArcSegments, new Color(statusColor, 0.86f), 2.0f, true);
        DrawArc(center, radius + 4, -Mathf.Pi * 0.18f, Mathf.Pi * 1.16f, SmallArcSegments, new Color(statusColor, 0.28f), 1.0f, true);

        if (offline)
        {
            DrawLowPowerGlyph(center, statusColor);
        }
        else if (constructionPaused)
        {
            DrawPauseGlyph(center, statusColor);
        }
    }

    private void DrawLowPowerGlyph(Vector2 center, Color statusColor)
    {
        var points = new[]
        {
            center + new Vector2(-2, -8),
            center + new Vector2(-7, 0),
            center + new Vector2(-1, 0),
            center + new Vector2(-5, 8),
            center + new Vector2(7, -3),
            center + new Vector2(1, -3),
        };

        for (var index = 0; index < points.Length - 1; index++)
        {
            DrawLine(points[index], points[index + 1], new Color(statusColor, 0.94f), 2.0f, true);
        }

        DrawLine(center + new Vector2(6, 6), center + new Vector2(9, 9), new Color(SoftOldCityPalette.InnerLight, 0.78f), 1.5f, true);
    }

    private void DrawPauseGlyph(Vector2 center, Color statusColor)
    {
        var left = new Rect2(center + new Vector2(-5, -7), new Vector2(3, 14));
        var right = new Rect2(center + new Vector2(2, -7), new Vector2(3, 14));
        DrawRect(left, new Color(statusColor, 0.86f), true);
        DrawRect(right, new Color(statusColor, 0.86f), true);
    }
}
