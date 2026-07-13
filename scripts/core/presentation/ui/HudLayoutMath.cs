namespace ProceduralRts.Core;

public readonly record struct HudRect(string Name, float X, float Y, float Width, float Height)
{
    public float Right => X + Width;
    public float Bottom => Y + Height;
    public float Area => Width * Height;

    public bool Overlaps(HudRect other)
    {
        return X < other.Right
            && Right > other.X
            && Y < other.Bottom
            && Bottom > other.Y;
    }
}

public sealed record HudLayoutSnapshot(
    int ViewportWidth,
    int ViewportHeight,
    float UiScale,
    IReadOnlyList<HudRect> Rects,
    HudRect BattleViewport)
{
    public float PersistentCoverageRatio =>
        Rects.Where(rect => !rect.Name.StartsWith("context-", StringComparison.Ordinal))
            .Sum(rect => rect.Area) / MathF.Max(1, ViewportWidth * ViewportHeight);
}

public static class HudLayoutMath
{
    public const float RailWidth = 72;
    public const float DrawerWidth = 300;
    public const float RightColumnWidth = 312;
    public const float ProductionPanelTop = 190;
    public const float ProductionPanelHeight = 358;
    public const float MinimumBattlefieldWidth = 920;
    public const float MinimumBattlefieldHeight = 620;
    public const float MaximumPersistentCoverage = 0.16f;

    public static IReadOnlyList<HudRect> CreateRightDeckControls(int viewportHeight, float uiScale = 1)
    {
        var scale = MathF.Max(1, uiScale);
        var railHeight = viewportHeight / scale - ProductionPanelTop - 12;
        var controls = new List<HudRect>
        {
            new("deck-toggle", 8, 6, 56, 38),
        };

        for (var index = 0; index < 8; index++)
        {
            controls.Add(new HudRect($"provider-{index}", 8, 52 + index * 28, 56, 24));
        }

        controls.Add(new HudRect("queue-mini-stack", 8, 282, 56, 84));
        controls.Add(new HudRect("queue-cancel", 18, 374, 36, 32));
        controls.Add(new HudRect("rail-bounds", 0, 0, RailWidth, railHeight));
        return controls;
    }

    public static IReadOnlyList<string> ValidateRightDeckControls(int viewportHeight, float uiScale = 1)
    {
        var issues = new List<string>();
        var controls = CreateRightDeckControls(viewportHeight, uiScale);
        var bounds = controls.First(rect => rect.Name == "rail-bounds");
        var visible = controls.Where(rect => rect.Name != "rail-bounds").ToArray();
        for (var index = 0; index < visible.Length; index++)
        {
            var rect = visible[index];
            if (rect.X < bounds.X || rect.Y < bounds.Y || rect.Right > bounds.Right || rect.Bottom > bounds.Bottom)
            {
                issues.Add($"{rect.Name} is outside right command deck bounds");
            }

            for (var otherIndex = index + 1; otherIndex < visible.Length; otherIndex++)
            {
                if (rect.Overlaps(visible[otherIndex]))
                {
                    issues.Add($"{rect.Name} overlaps {visible[otherIndex].Name}");
                }
            }
        }

        return issues;
    }

    public static HudLayoutSnapshot Create(int viewportWidth, int viewportHeight, float uiScale = 1)
    {
        var scale = MathF.Max(1, uiScale);
        var railWidth = RailWidth * scale;
        var rightColumnWidth = RightColumnWidth * scale;
        var resource = new HudRect("resource-strip", viewportWidth / 2f - 224 * scale, 10 * scale, 448 * scale, 46 * scale);
        var minimap = new HudRect("minimap-radar", viewportWidth - rightColumnWidth, 12 * scale, 300 * scale, 166 * scale);
        var globalSkills = new HudRect("global-skills", 12 * scale, 182 * scale, 64 * scale, viewportHeight - 360 * scale);
        var rail = new HudRect("right-rail", viewportWidth - railWidth, ProductionPanelTop * scale, railWidth, viewportHeight - 202 * scale);
        var alerts = new HudRect("alert-chips", 16 * scale, 66 * scale, 280 * scale, 42 * scale);

        var ribbon = new HudRect("context-command-ribbon", 96 * scale, viewportHeight - 58 * scale, viewportWidth - rightColumnWidth - 108 * scale, 46 * scale);
        var production = new HudRect("context-production-panel", viewportWidth - rightColumnWidth, ProductionPanelTop * scale, 300 * scale, ProductionPanelHeight * scale);
        var detail = new HudRect("context-detail-panel", viewportWidth - rightColumnWidth, viewportHeight - 170 * scale, 300 * scale, 158 * scale);

        var rects = new List<HudRect>
        {
            resource,
            minimap,
            globalSkills,
            rail,
            alerts,
            ribbon,
            production,
            detail,
        };

        var battleViewport = new HudRect(
            "battlefield",
            0,
            0,
            viewportWidth - railWidth,
            viewportHeight);

        return new HudLayoutSnapshot(viewportWidth, viewportHeight, scale, rects, battleViewport);
    }

    public static IReadOnlyList<string> Validate(HudLayoutSnapshot snapshot)
    {
        var issues = new List<string>();
        foreach (var rect in snapshot.Rects)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                issues.Add($"{rect.Name} has non-positive size");
            }

            if (rect.X < 0 || rect.Y < 0 || rect.Right > snapshot.ViewportWidth || rect.Bottom > snapshot.ViewportHeight)
            {
                issues.Add($"{rect.Name} is outside viewport {snapshot.ViewportWidth}x{snapshot.ViewportHeight}: {rect}");
            }
        }

        CheckNoOverlap(snapshot, issues, "resource-strip", "minimap-radar");
        CheckNoOverlap(snapshot, issues, "resource-strip", "global-skills");
        CheckNoOverlap(snapshot, issues, "minimap-radar", "right-rail");
        CheckNoOverlap(snapshot, issues, "minimap-radar", "context-production-panel");
        CheckNoOverlap(snapshot, issues, "context-production-panel", "context-detail-panel");
        CheckNoOverlap(snapshot, issues, "context-command-ribbon", "context-detail-panel");
        CheckRightCommandStack(snapshot, issues);
        CheckBottomCommandStrip(snapshot, issues);

        var lowerMiddle = new HudRect(
            "lower-middle-playfield",
            snapshot.ViewportWidth * 0.34f,
            snapshot.ViewportHeight * 0.64f,
            snapshot.ViewportWidth * 0.32f,
            snapshot.ViewportHeight * 0.26f);
        foreach (var rect in snapshot.Rects.Where(rect => !rect.Name.StartsWith("context-", StringComparison.Ordinal)))
        {
            if (rect.Overlaps(lowerMiddle))
            {
                issues.Add($"{rect.Name} obstructs lower-middle playfield");
            }
        }

        if (snapshot.PersistentCoverageRatio > MaximumPersistentCoverage)
        {
            issues.Add($"persistent HUD coverage {snapshot.PersistentCoverageRatio:P1} exceeds {MaximumPersistentCoverage:P0}");
        }

        if (snapshot.BattleViewport.Width < MinimumBattlefieldWidth * snapshot.UiScale)
        {
            issues.Add($"battlefield width {snapshot.BattleViewport.Width:0} is below minimum");
        }

        if (snapshot.BattleViewport.Height < MinimumBattlefieldHeight * snapshot.UiScale)
        {
            issues.Add($"battlefield height {snapshot.BattleViewport.Height:0} is below minimum");
        }

        return issues;
    }

    private static void CheckNoOverlap(HudLayoutSnapshot snapshot, List<string> issues, string aName, string bName)
    {
        var a = snapshot.Rects.First(rect => rect.Name == aName);
        var b = snapshot.Rects.First(rect => rect.Name == bName);
        if (a.Overlaps(b))
        {
            issues.Add($"{aName} overlaps {bName}");
        }
    }

    private static void CheckRightCommandStack(HudLayoutSnapshot snapshot, List<string> issues)
    {
        var scale = snapshot.UiScale;
        var minimap = snapshot.Rects.First(rect => rect.Name == "minimap-radar");
        var production = snapshot.Rects.First(rect => rect.Name == "context-production-panel");
        var unitDetail = snapshot.Rects.First(rect => rect.Name == "context-detail-panel");

        if (minimap.Bottom > production.Y)
        {
            issues.Add("right sidebar minimap should sit above the production/build grid");
        }

        if (!(minimap.Bottom < production.Y && production.Bottom < unitDetail.Y && unitDetail.Bottom <= snapshot.ViewportHeight - 8 * scale))
        {
            issues.Add("right sidebar stack should order minimap, production/build grid, then unit or building details");
        }

        foreach (var section in new[] { minimap, production, unitDetail })
        {
            if (section.Right > snapshot.ViewportWidth - 8 * scale || section.X < snapshot.ViewportWidth - RightColumnWidth * scale - 2 * scale)
            {
                issues.Add($"{section.Name} should remain inside the right command column");
            }
        }
    }

    private static void CheckBottomCommandStrip(HudLayoutSnapshot snapshot, List<string> issues)
    {
        var scale = snapshot.UiScale;
        var ribbon = snapshot.Rects.First(rect => rect.Name == "context-command-ribbon");
        if (ribbon.Height > 52 * scale)
        {
            issues.Add($"bottom command ribbon height {ribbon.Height:0} should stay narrow");
        }

        if (ribbon.Bottom < snapshot.ViewportHeight - 12 * scale || ribbon.Y < snapshot.ViewportHeight - 68 * scale)
        {
            issues.Add("bottom command ribbon should stay docked to a narrow lower strip");
        }
    }
}
