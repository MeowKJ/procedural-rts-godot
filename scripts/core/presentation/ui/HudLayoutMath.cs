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
    public const float RailWidth = 64;
    public const float DrawerWidth = 288;
    public const float RightColumnWidth = 312;
    public const float ProductionPanelTop = 190;
    public const float ProductionPanelHeight = 358;
    public const float MinimumCommandHitTarget = 44;
    public const float CommandRibbonMaxWidth = 720;
    public const float CommandRibbonPreferredWidth = 684;
    public const float CommandRibbonViewportFraction = 0.60f;
    public const float CommandRibbonHeight = 56;
    public const int CommandRibbonSurfaceMaxChars = 14;
    public const int MinimumCompactFontSize = 11;
    public const int MinimumBodyFontSize = 12;
    public const uint ConsoleBaseRgb = 0x111820;
    public const uint ConsoleTextRgb = 0xE9E1D1;
    public const uint ConsoleActionRgb = 0x62C9C4;
    public const uint ConsoleBrassRgb = 0xC99A52;
    public const float MinimumBattlefieldWidth = 920;
    public const float MinimumBattlefieldHeight = 620;
    public const float MaximumPersistentCoverage = 0.16f;

    public static IReadOnlyList<HudRect> CreateRightDeckControls(int viewportHeight, float uiScale = 1)
    {
        var scale = MathF.Max(1, uiScale);
        var railHeight = viewportHeight / scale - ProductionPanelTop - 12;
        var controls = new List<HudRect>
        {
            new("deck-toggle", 6, 4, 52, 44),
        };

        for (var index = 0; index < 8; index++)
        {
            controls.Add(new HudRect($"provider-{index}", 6, 50 + index * 44, 52, 44));
        }

        controls.Add(new HudRect("queue-mini-stack", 6, 406, 52, 58));
        controls.Add(new HudRect("queue-cancel", 10, 468, 44, 44));
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

            if (rect.Width < MinimumCommandHitTarget || rect.Height < MinimumCommandHitTarget)
            {
                issues.Add($"{rect.Name} hit target {rect.Width:0}x{rect.Height:0} is below {MinimumCommandHitTarget:0}px");
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

    public static IReadOnlyList<HudRect> CreateBottomCommandControls(int viewportWidth, float uiScale = 1)
    {
        var scale = MathF.Max(1, uiScale);
        var controls = new List<HudRect>();
        for (var index = 0; index < 5; index++)
        {
            controls.Add(new HudRect($"stance-{index}", 6 + index * 44, 6, 44, 44));
        }

        for (var index = 0; index < 3; index++)
        {
            controls.Add(new HudRect($"move-{index}", 244 + index * 44, 6, 44, 44));
        }

        for (var index = 0; index < 4; index++)
        {
            controls.Add(new HudRect($"context-action-{index}", 494 + index * 44, 6, 44, 44));
        }

        controls.Add(new HudRect("ribbon-bounds", 0, 0, CommandRibbonWidth(viewportWidth, scale) / scale, CommandRibbonHeight));
        return controls;
    }

    public static IReadOnlyList<string> ValidateBottomCommandControls(int viewportWidth, float uiScale = 1)
    {
        var issues = new List<string>();
        var controls = CreateBottomCommandControls(viewportWidth, uiScale);
        var bounds = controls.First(rect => rect.Name == "ribbon-bounds");
        var interactive = controls.Where(rect => rect.Name != "ribbon-bounds").ToArray();
        for (var index = 0; index < interactive.Length; index++)
        {
            var rect = interactive[index];
            if (rect.Width < MinimumCommandHitTarget || rect.Height < MinimumCommandHitTarget)
            {
                issues.Add($"{rect.Name} hit target is below {MinimumCommandHitTarget:0}px");
            }

            if (rect.X < bounds.X || rect.Y < bounds.Y || rect.Right > bounds.Right || rect.Bottom > bounds.Bottom)
            {
                issues.Add($"{rect.Name} is outside bottom command ribbon bounds");
            }

            for (var otherIndex = index + 1; otherIndex < interactive.Length; otherIndex++)
            {
                if (rect.Overlaps(interactive[otherIndex]))
                {
                    issues.Add($"{rect.Name} overlaps {interactive[otherIndex].Name}");
                }
            }
        }

        return issues;
    }

    public static float CommandRibbonWidth(int viewportWidth, float uiScale = 1)
    {
        var scale = MathF.Max(1, uiScale);
        return MathF.Min(
            MathF.Min(CommandRibbonPreferredWidth * scale, CommandRibbonMaxWidth * scale),
            viewportWidth * CommandRibbonViewportFraction);
    }

    public static float ContrastRatio(uint foregroundRgb, uint backgroundRgb)
    {
        var foreground = RelativeLuminance(foregroundRgb);
        var background = RelativeLuminance(backgroundRgb);
        var lighter = MathF.Max(foreground, background);
        var darker = MathF.Min(foreground, background);
        return (lighter + 0.05f) / (darker + 0.05f);
    }

    public static string CompactFieldText(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || maxChars <= 0)
        {
            return "";
        }

        if (text.Length <= maxChars)
        {
            return text;
        }

        var end = Math.Min(maxChars, text.Length);
        if (end > 0 && char.IsHighSurrogate(text[end - 1]))
        {
            end--;
        }

        if (end < text.Length && end > 0 && IsAsciiFieldWord(text[end - 1]) && IsAsciiFieldWord(text[end]))
        {
            while (end > 0 && IsAsciiFieldWord(text[end - 1]))
            {
                end--;
            }
        }

        return end == 0
            ? ""
            : text[..end].TrimEnd(' ', '\t', '/', '|', '-', '_');
    }

    public static string CommandRibbonSurfaceText(string text)
    {
        return CompactFieldText(text.Replace('\n', ' '), CommandRibbonSurfaceMaxChars);
    }

    public static float ProductionDrawerFooterTop(int visibleCardCount)
    {
        var rows = Math.Clamp((Math.Max(0, visibleCardCount) + 2) / 3, 0, 4);
        return rows == 0 ? 104 : MathF.Min(326, 96 + rows * 58 + 4);
    }

    public static float ProductionDrawerHeight(int visibleCardCount)
    {
        return Math.Clamp(ProductionDrawerFooterTop(visibleCardCount) + 38, 172, ProductionPanelHeight);
    }

    public static HudLayoutSnapshot Create(int viewportWidth, int viewportHeight, float uiScale = 1)
    {
        var scale = MathF.Max(1, uiScale);
        var railWidth = RailWidth * scale;
        var rightColumnWidth = RightColumnWidth * scale;
        var resource = new HudRect("resource-strip", viewportWidth / 2f - 210 * scale, 10 * scale, 420 * scale, 46 * scale);
        var minimap = new HudRect("minimap-radar", viewportWidth - rightColumnWidth, 12 * scale, 300 * scale, 166 * scale);
        var globalSkills = new HudRect("global-skills", 12 * scale, 182 * scale, 64 * scale, viewportHeight - 360 * scale);
        var rail = new HudRect("right-rail", viewportWidth - railWidth, ProductionPanelTop * scale, railWidth, viewportHeight - 202 * scale);
        var alerts = new HudRect("alert-chips", 16 * scale, 66 * scale, 280 * scale, 42 * scale);

        var ribbonWidth = CommandRibbonWidth(viewportWidth, scale);
        var commandAreaWidth = viewportWidth - (RightColumnWidth + RailWidth) * scale;
        var ribbon = new HudRect(
            "context-command-ribbon",
            MathF.Max(12 * scale, (commandAreaWidth - ribbonWidth) * 0.5f),
            viewportHeight - 68 * scale,
            ribbonWidth,
            CommandRibbonHeight * scale);
        var drawerX = viewportWidth - (DrawerWidth + RailWidth + 12) * scale;
        var production = new HudRect("context-production-panel", drawerX, ProductionPanelTop * scale, DrawerWidth * scale, ProductionPanelHeight * scale);
        var detail = new HudRect("context-detail-panel", drawerX, viewportHeight - 170 * scale, DrawerWidth * scale, 158 * scale);

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

        if (minimap.Right > snapshot.ViewportWidth - 8 * scale
            || minimap.X < snapshot.ViewportWidth - RightColumnWidth * scale - 2 * scale)
        {
            issues.Add("minimap-radar should remain inside the upper right command column");
        }

        var rail = snapshot.Rects.First(rect => rect.Name == "right-rail");
        var expectedDrawerX = snapshot.ViewportWidth - (DrawerWidth + RailWidth + 12) * scale;
        if (production.Right > rail.X - 8 * scale
            || MathF.Abs(production.X - expectedDrawerX) > 0.5f)
        {
            issues.Add("context-production-panel should open immediately left of the persistent right rail");
        }

        if (unitDetail.Right > rail.X - 8 * scale
            || MathF.Abs(unitDetail.X - production.X) > 0.5f)
        {
            issues.Add("context-detail-panel should share the drawer column left of the persistent right rail");
        }
    }

    private static void CheckBottomCommandStrip(HudLayoutSnapshot snapshot, List<string> issues)
    {
        var scale = snapshot.UiScale;
        var ribbon = snapshot.Rects.First(rect => rect.Name == "context-command-ribbon");
        if (ribbon.Height > 60 * scale)
        {
            issues.Add($"bottom command ribbon height {ribbon.Height:0} should stay narrow");
        }

        if (ribbon.Bottom < snapshot.ViewportHeight - 12 * scale || ribbon.Y < snapshot.ViewportHeight - 78 * scale)
        {
            issues.Add("bottom command ribbon should stay docked to a narrow lower strip");
        }

        var maximumWidth = MathF.Min(CommandRibbonMaxWidth * scale, snapshot.ViewportWidth * CommandRibbonViewportFraction);
        if (ribbon.Width > maximumWidth + 0.5f)
        {
            issues.Add($"bottom command ribbon width {ribbon.Width:0} exceeds {maximumWidth:0}");
        }
    }

    private static float RelativeLuminance(uint rgb)
    {
        var red = LinearChannel((rgb >> 16) & 0xFF);
        var green = LinearChannel((rgb >> 8) & 0xFF);
        var blue = LinearChannel(rgb & 0xFF);
        return 0.2126f * red + 0.7152f * green + 0.0722f * blue;
    }

    private static float LinearChannel(uint channel)
    {
        var value = channel / 255f;
        return value <= 0.04045f
            ? value / 12.92f
            : MathF.Pow((value + 0.055f) / 1.055f, 2.4f);
    }

    private static bool IsAsciiFieldWord(char value)
    {
        return value is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '0' and <= '9';
    }
}
