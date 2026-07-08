using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class SignalNetworkLayer : Node2D
{
    private const int RadiusArcSegments = 40;

    private enum SignalOutputMode
    {
        Offline,
        DayControl,
        NightVision,
    }

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
        var outputMode = SignalOutputModeFor(node, State.ResourceAtmosphere);
        var accent = node.Kind switch
        {
            SignalNodeKind.SafeZone => palette.CommandEdge,
            SignalNodeKind.SignalTower => palette.NavigationLine,
            _ => palette.Boundary,
        };
        var position = node.Position;

        DrawCoverage(node, palette, accent, profile, glow, outputMode);

        switch (node.Kind)
        {
            case SignalNodeKind.SafeZone:
                DrawSafeZoneNode(position, accent, profile, glow, outputMode);
                break;
            case SignalNodeKind.SignalTower:
                DrawSignalTower(position, accent, profile, glow, outputMode);
                break;
            default:
                DrawRoadLight(position, accent, profile, glow, outputMode);
                break;
        }

        switch (outputMode)
        {
            case SignalOutputMode.Offline:
                DrawOfflineNodeMarker(position, palette, profile);
                break;
            case SignalOutputMode.DayControl:
                DrawDayControlIndicator(position, palette.CommandEdge, profile);
                break;
            case SignalOutputMode.NightVision:
                DrawNightVisionIndicator(position, accent, profile, glow);
                DrawSignalNoise(node, accent, profile);
                break;
        }
    }

    private void DrawCoverage(
        SignalNetworkNode node,
        WorldThemePalette palette,
        Color accent,
        WorldThemeTacticalProfile profile,
        float glow,
        SignalOutputMode outputMode)
    {
        var dayControl = outputMode == SignalOutputMode.DayControl;
        var nightVision = outputMode == SignalOutputMode.NightVision;
        var offline = outputMode == SignalOutputMode.Offline;
        var dayAlpha = dayControl ? profile.PlanningClarity * 0.26f : (offline ? 0.030f : 0.055f);
        var dayStroke = dayControl ? 1.16f + profile.RebuildingFocus * 0.38f : 0.65f;

        DrawCircle(node.Position, node.DayControlRadius, ScaleAlpha(palette.CommandEdge, dayAlpha * 0.22f));
        DrawArc(node.Position, node.DayControlRadius, 0, Mathf.Tau, RadiusArcSegments, ScaleAlpha(palette.CommandEdge, dayAlpha), dayStroke, true);

        if (node.NightVisionRadius <= 0)
        {
            return;
        }

        var nightAlpha = nightVision
            ? glow * (0.26f + profile.DefensiveCaution * 0.14f)
            : offline ? 0.0f : glow * 0.050f;
        if (nightAlpha <= 0)
        {
            return;
        }

        var nightStroke = nightVision ? 1.45f + profile.LightNetworkSafety * 0.62f : 0.70f;
        DrawCircle(node.Position, node.NightVisionRadius, ScaleAlpha(accent, nightAlpha * 0.34f));
        DrawArc(node.Position, node.NightVisionRadius, 0, Mathf.Tau, RadiusArcSegments, ScaleAlpha(accent, nightAlpha), nightStroke, true);
    }

    private void DrawRoadLight(Vector2 position, Color accent, WorldThemeTacticalProfile profile, float glow, SignalOutputMode outputMode)
    {
        var active = outputMode != SignalOutputMode.Offline;
        var nodeGlow = active ? glow : 0;
        var alpha = active ? 0.34f + nodeGlow * 0.28f : 0.14f;
        DrawLine(position + new Vector2(0, -9), position + new Vector2(0, 9), ScaleAlpha(accent, alpha), 1.1f + profile.LightNetworkSafety * 0.16f, true);
        DrawCircle(position, 4.8f + nodeGlow * 3.2f, ScaleAlpha(accent, active ? 0.10f + nodeGlow * 0.18f : 0.035f));
        DrawCircle(position, 1.9f, new Color("#ffffff", active ? 0.40f + nodeGlow * 0.34f : 0.16f));
    }

    private void DrawSignalTower(Vector2 position, Color accent, WorldThemeTacticalProfile profile, float glow, SignalOutputMode outputMode)
    {
        var active = outputMode != SignalOutputMode.Offline;
        var nodeGlow = active ? glow : 0;
        var structuralAlpha = active ? 0.42f + profile.LightNetworkSafety * 0.08f : 0.16f;
        DrawLine(position + new Vector2(-10, 11), position, ScaleAlpha(accent, structuralAlpha), 1.55f + profile.LightNetworkSafety * 0.20f, true);
        DrawLine(position + new Vector2(10, 11), position, ScaleAlpha(accent, structuralAlpha), 1.55f + profile.LightNetworkSafety * 0.20f, true);
        DrawLine(position + new Vector2(-7, 5), position + new Vector2(7, 5), ScaleAlpha(accent, active ? 0.32f + profile.SignalNoise * 0.08f : 0.12f), 1.1f, true);
        DrawArc(position, 17 + nodeGlow * 8, -Mathf.Pi * 0.85f, -Mathf.Pi * 0.15f, 28, ScaleAlpha(accent, active ? 0.20f + nodeGlow * 0.30f : 0.08f), 1.25f + profile.LightNetworkSafety * 0.25f, true);
        DrawArc(position, 27 + nodeGlow * 12, -Mathf.Pi * 0.82f, -Mathf.Pi * 0.18f, 32, ScaleAlpha(accent, nodeGlow * 0.20f), 1.0f + profile.SignalNoise * 0.28f, true);
    }

    private void DrawSafeZoneNode(Vector2 position, Color accent, WorldThemeTacticalProfile profile, float glow, SignalOutputMode outputMode)
    {
        var active = outputMode != SignalOutputMode.Offline;
        var nodeGlow = active ? glow : 0;
        var radius = 14 + nodeGlow * 5;
        DrawArc(position, radius, 0, Mathf.Tau, 48, ScaleAlpha(accent, active ? 0.46f + nodeGlow * 0.30f : 0.16f), 2.0f + profile.LightNetworkSafety * 0.35f, true);
        DrawLine(position + new Vector2(-radius, 0), position + new Vector2(radius, 0), new Color("#ffffff", active ? 0.38f + nodeGlow * 0.2f : 0.14f), 1.2f, true);
        DrawLine(position + new Vector2(0, -radius), position + new Vector2(0, radius), new Color("#ffffff", active ? 0.38f + nodeGlow * 0.2f : 0.14f), 1.2f, true);
        DrawCircle(position, radius * 0.55f, ScaleAlpha(accent, active ? 0.07f + nodeGlow * 0.14f : 0.030f));
        if (active && profile.DefensiveCaution > 0.6f)
        {
            DrawArc(position, radius + 8, -Mathf.Pi * 0.06f, Mathf.Pi * 0.58f, 32, ScaleAlpha(accent, profile.DefensiveCaution * 0.16f), 1.1f, true);
        }
    }

    private void DrawOfflineNodeMarker(Vector2 position, WorldThemePalette palette, WorldThemeTacticalProfile profile)
    {
        var markerRadius = 18 + profile.SignalNoise * 4;
        var offline = ScaleAlpha(palette.Boundary, 0.70f);
        DrawLine(position + new Vector2(-markerRadius, -markerRadius), position + new Vector2(markerRadius, markerRadius), offline, 1.45f, true);
        DrawLine(position + new Vector2(-markerRadius, markerRadius), position + new Vector2(markerRadius, -markerRadius), offline, 1.15f, true);
        DrawArc(position, markerRadius + 5, -Mathf.Pi * 0.15f, Mathf.Pi * 0.65f, 24, ScaleAlpha(palette.Boundary, 0.28f), 0.9f, true);
    }

    private void DrawDayControlIndicator(Vector2 position, Color accent, WorldThemeTacticalProfile profile)
    {
        var radius = 23 + profile.RebuildingFocus * 4;
        var color = ScaleAlpha(accent, 0.44f + profile.PlanningClarity * 0.12f);
        DrawLine(position + new Vector2(-radius, 0), position + new Vector2(-radius + 8, 0), color, 1.25f, true);
        DrawLine(position + new Vector2(radius - 8, 0), position + new Vector2(radius, 0), color, 1.25f, true);
        DrawLine(position + new Vector2(0, -radius), position + new Vector2(0, -radius + 8), color, 1.25f, true);
        DrawLine(position + new Vector2(0, radius - 8), position + new Vector2(0, radius), color, 1.25f, true);
    }

    private void DrawNightVisionIndicator(Vector2 position, Color accent, WorldThemeTacticalProfile profile, float glow)
    {
        var sweepRadius = 25 + glow * 8;
        var sweep = ScaleAlpha(accent, 0.30f + profile.LightNetworkSafety * 0.18f);
        DrawArc(position, sweepRadius, -Mathf.Pi * 0.08f, Mathf.Pi * 0.48f, 24, sweep, 1.2f + profile.SignalNoise * 0.25f, true);
        DrawLine(position, position + Vector2.Right.Rotated(-Mathf.Pi * 0.08f) * sweepRadius, ScaleAlpha(accent, 0.18f + glow * 0.18f), 0.95f, true);
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

    private static SignalOutputMode SignalOutputModeFor(SignalNetworkNode node, ResourceAtmosphere atmosphere)
    {
        if (!node.Powered)
        {
            return SignalOutputMode.Offline;
        }

        return atmosphere is ResourceAtmosphere.Day or ResourceAtmosphere.Fog
            ? SignalOutputMode.DayControl
            : SignalOutputMode.NightVision;
    }

    private static Color ScaleAlpha(Color color, float scale)
    {
        return new Color(color, Mathf.Clamp(color.A * scale, 0, 1));
    }
}
