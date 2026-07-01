using Godot;

namespace ProceduralRts.Core;

public static class SignalNetworkMath
{
    public static IReadOnlyList<SignalNetworkNode> CreateDefaultNetwork(Vector2 worldSize)
    {
        var nodes = new List<SignalNetworkNode>();
        var id = 1;

        foreach (var point in NavigationLights(worldSize))
        {
            nodes.Add(new SignalNetworkNode(id++, SignalNodeKind.RoadLight, point, 135, 70, Powered: true));
        }

        nodes.Add(new SignalNetworkNode(id++, SignalNodeKind.SafeZone, new Vector2(worldSize.X * 0.16f, worldSize.Y * 0.30f), 310, 340, Powered: true));
        nodes.Add(new SignalNetworkNode(id++, SignalNodeKind.SafeZone, new Vector2(worldSize.X * 0.76f, worldSize.Y * 0.54f), 300, 330, Powered: true));
        nodes.Add(new SignalNetworkNode(id++, SignalNodeKind.SignalTower, new Vector2(worldSize.X * 0.48f, worldSize.Y * 0.57f), 245, 300, Powered: true));
        nodes.Add(new SignalNetworkNode(id++, SignalNodeKind.SignalTower, new Vector2(worldSize.X * 0.34f, worldSize.Y * 0.71f), 210, 260, Powered: true));
        nodes.Add(new SignalNetworkNode(id++, SignalNodeKind.SignalTower, new Vector2(worldSize.X * 0.66f, worldSize.Y * 0.41f), 210, 260, Powered: true));

        return nodes;
    }

    public static bool EmitsNightVision(SignalNetworkNode node, WorldVisualThemeState theme)
    {
        return node.Powered
            && node.NightVisionRadius > 0
            && (theme.Current == WorldVisualTheme.NightRadar
                || theme.Target == WorldVisualTheme.NightRadar
                || theme.Current == WorldVisualTheme.DuskDefense
                || theme.Target == WorldVisualTheme.DuskDefense);
    }

    public static float ThemeGlowStrength(WorldVisualThemeState theme)
    {
        return WorldThemeMath.Profile(theme).LightNetworkSafety;
    }

    private static IEnumerable<Vector2> NavigationLights(Vector2 worldSize)
    {
        for (var t = 0.12f; t <= 0.88f; t += 0.12f)
        {
            yield return new Vector2(worldSize.X * t, worldSize.Y * 0.82f - worldSize.X * t * 0.23f);
        }

        for (var t = 0.18f; t <= 0.84f; t += 0.16f)
        {
            yield return new Vector2(worldSize.X * t, worldSize.Y * 0.57f);
        }

        for (var t = 0.2f; t <= 0.76f; t += 0.14f)
        {
            yield return new Vector2(worldSize.X * 0.48f, worldSize.Y * t);
        }
    }
}
