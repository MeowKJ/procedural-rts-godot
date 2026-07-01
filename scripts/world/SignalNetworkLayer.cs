using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class SignalNetworkLayer : Node2D
{
    private const int RadiusArcSegments = 40;

    public required GameState State { get; init; }

    public override void _Ready()
    {
        State.VisualThemeChanged += _ => QueueRedraw();
        State.SignalNetworkChanged += QueueRedraw;
    }

    public override void _Draw()
    {
        var palette = WorldThemeMath.Palette(State.VisualTheme);
        var profile = WorldThemeMath.Profile(State.VisualTheme);
        var glow = SignalNetworkMath.ThemeGlowStrength(State.VisualTheme);
        foreach (var node in State.SignalNodes)
        {
            DrawNode(node, palette, profile, glow);
        }
    }

    private void DrawNode(SignalNetworkNode node, WorldThemePalette palette, WorldThemeTacticalProfile profile, float glow)
    {
        var accent = node.Kind switch
        {
            SignalNodeKind.SafeZone => palette.CommandEdge,
            SignalNodeKind.SignalTower => palette.NavigationLine,
            _ => palette.Boundary,
        };
        var position = node.Position;
        var dayAlpha = profile.PlanningClarity * 0.20f;
        var nightAlpha = glow * (0.20f + profile.DefensiveCaution * 0.10f);

        DrawCircle(position, node.DayControlRadius, ScaleAlpha(palette.CommandEdge, dayAlpha * 0.22f));
        DrawArc(position, node.DayControlRadius, 0, Mathf.Tau, RadiusArcSegments, ScaleAlpha(palette.CommandEdge, dayAlpha), 0.95f + profile.RebuildingFocus * 0.28f, true);

        if (node.Powered)
        {
            DrawCircle(position, node.NightVisionRadius, ScaleAlpha(accent, nightAlpha * 0.34f));
            DrawArc(position, node.NightVisionRadius, 0, Mathf.Tau, RadiusArcSegments, ScaleAlpha(accent, nightAlpha), 1.25f + profile.LightNetworkSafety * 0.48f, true);
            DrawSignalNoise(node, accent, profile);
        }

        switch (node.Kind)
        {
            case SignalNodeKind.SafeZone:
                DrawSafeZoneNode(position, accent, profile, glow);
                break;
            case SignalNodeKind.SignalTower:
                DrawSignalTower(position, accent, profile, glow);
                break;
            default:
                DrawRoadLight(position, accent, profile, glow);
                break;
        }
    }

    private void DrawRoadLight(Vector2 position, Color accent, WorldThemeTacticalProfile profile, float glow)
    {
        DrawLine(position + new Vector2(0, -9), position + new Vector2(0, 9), ScaleAlpha(accent, 0.34f + glow * 0.28f), 1.1f + profile.LightNetworkSafety * 0.16f, true);
        DrawCircle(position, 4.8f + glow * 3.2f, ScaleAlpha(accent, 0.10f + glow * 0.18f));
        DrawCircle(position, 1.9f, new Color("#ffffff", 0.40f + glow * 0.34f));
    }

    private void DrawSignalTower(Vector2 position, Color accent, WorldThemeTacticalProfile profile, float glow)
    {
        DrawLine(position + new Vector2(-10, 11), position, ScaleAlpha(accent, 0.42f + profile.LightNetworkSafety * 0.08f), 1.55f + profile.LightNetworkSafety * 0.20f, true);
        DrawLine(position + new Vector2(10, 11), position, ScaleAlpha(accent, 0.42f + profile.LightNetworkSafety * 0.08f), 1.55f + profile.LightNetworkSafety * 0.20f, true);
        DrawLine(position + new Vector2(-7, 5), position + new Vector2(7, 5), ScaleAlpha(accent, 0.32f + profile.SignalNoise * 0.08f), 1.1f, true);
        DrawArc(position, 17 + glow * 8, -Mathf.Pi * 0.85f, -Mathf.Pi * 0.15f, 28, ScaleAlpha(accent, 0.20f + glow * 0.30f), 1.25f + profile.LightNetworkSafety * 0.25f, true);
        DrawArc(position, 27 + glow * 12, -Mathf.Pi * 0.82f, -Mathf.Pi * 0.18f, 32, ScaleAlpha(accent, glow * 0.20f), 1.0f + profile.SignalNoise * 0.28f, true);
    }

    private void DrawSafeZoneNode(Vector2 position, Color accent, WorldThemeTacticalProfile profile, float glow)
    {
        var radius = 14 + glow * 5;
        DrawArc(position, radius, 0, Mathf.Tau, 48, ScaleAlpha(accent, 0.46f + glow * 0.30f), 2.0f + profile.LightNetworkSafety * 0.35f, true);
        DrawLine(position + new Vector2(-radius, 0), position + new Vector2(radius, 0), new Color("#ffffff", 0.38f + glow * 0.2f), 1.2f, true);
        DrawLine(position + new Vector2(0, -radius), position + new Vector2(0, radius), new Color("#ffffff", 0.38f + glow * 0.2f), 1.2f, true);
        DrawCircle(position, radius * 0.55f, ScaleAlpha(accent, 0.07f + glow * 0.14f));
        if (profile.DefensiveCaution > 0.6f)
        {
            DrawArc(position, radius + 8, -Mathf.Pi * 0.06f, Mathf.Pi * 0.58f, 32, ScaleAlpha(accent, profile.DefensiveCaution * 0.16f), 1.1f, true);
        }
    }

    private void DrawSignalNoise(SignalNetworkNode node, Color accent, WorldThemeTacticalProfile profile)
    {
        if (profile.SignalNoise < 0.2f)
        {
            return;
        }

        var baseAngle = (node.Id * 0.71f) % Mathf.Tau;
        var radius = node.NightVisionRadius * (0.36f + node.Id % 3 * 0.08f);
        var alpha = profile.SignalNoise * profile.Pressure * 0.10f;
        DrawArc(node.Position, radius, baseAngle, baseAngle + 0.62f, 18, ScaleAlpha(accent, alpha), 0.8f + profile.SignalNoise * 0.35f, true);
        DrawArc(node.Position, radius + 18, baseAngle + 1.4f, baseAngle + 1.82f, 16, ScaleAlpha(accent, alpha * 0.72f), 0.7f, true);
    }

    private static Color ScaleAlpha(Color color, float scale)
    {
        return new Color(color, Mathf.Clamp(color.A * scale, 0, 1));
    }
}
