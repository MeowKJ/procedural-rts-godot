using ProceduralRts.Core;

var cases = new (int Width, int Height, float UiScale, string Name)[]
{
    (1280, 720, 1.0f, "desktop minimum"),
    (1600, 900, 1.0f, "desktop standard"),
    (1920, 1080, 1.0f, "desktop full hd"),
    (1600, 900, 1.25f, "high dpi 125"),
    (1920, 1080, 1.5f, "high dpi 150"),
};

var failures = new List<string>();
foreach (var testCase in cases)
{
    var snapshot = HudLayoutMath.Create(testCase.Width, testCase.Height, testCase.UiScale);
    var issues = HudLayoutMath.Validate(snapshot);
    if (issues.Count > 0)
    {
        failures.Add($"{testCase.Name} {testCase.Width}x{testCase.Height} scale {testCase.UiScale:0.##}: {string.Join("; ", issues)}");
    }
}

if (failures.Count > 0)
{
    throw new InvalidOperationException("HUD desktop QA failed:\n" + string.Join("\n", failures));
}

var repoRoot = FindRepoRoot();
AssertHudFactoryExtraction(repoRoot);

Console.WriteLine("Desktop HUD QA passed: 1280x720, 1600x900, 1920x1080, high-DPI layout constraints, and HUD UiFactory extraction");

static string FindRepoRoot()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "ProceduralRts.csproj"))
            && File.Exists(Path.Combine(current.FullName, "scripts", "ui", "HudLayer.cs")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not find procedural-rts-godot repository root for HUD source checks.");
}

static void AssertHudFactoryExtraction(string root)
{
    var hudLayer = ReadSourceWithPartials(Path.Combine(root, "scripts", "ui", "HudLayer.cs"));
    var uiFactory = File.ReadAllText(Path.Combine(root, "scripts", "ui", "UiFactory.cs"));

    RequireText(hudLayer, "UiFactory.MakeHudPanel", "HudLayer panel creation must use UiFactory.MakeHudPanel.");
    RequireText(hudLayer, "UiFactory.MakeHudSizedLabel", "HudLayer sized labels must use UiFactory.MakeHudSizedLabel.");
    RequireText(hudLayer, "UiFactory.ApplyNamedHudPanelTheme", "HudLayer panel refresh must use UiFactory.ApplyNamedHudPanelTheme.");
    RequireText(hudLayer, "UiFactory.ApplyHudActionButtonTheme", "HudLayer icon actions must use UiFactory.ApplyHudActionButtonTheme.");
    RequireText(hudLayer, "UiFactory.ApplyHudCommandButtonTheme", "HudLayer command buttons must use UiFactory.ApplyHudCommandButtonTheme.");
    RequireText(hudLayer, "UiFactory.GetHudControlGroupSlotStyle", "HudLayer control-group slot style must come from UiFactory.");
    ForbidText(hudLayer, "BuildGlobalSkillPanel", "Normal HUD must not build placeholder global-skill controls.");
    ForbidText(hudLayer, "GlobalSkillPanel", "Normal HUD must not include an unwired global-skill panel.");
    ForbidText(hudLayer, "PlaceholderBuildSlot", "Normal HUD must not keep placeholder production slot controls.");
    ForbidText(hudLayer, "_selectionCluster", "Selection detail must be owned by the right detail drawer, not a permanently hidden duplicate panel.");

    RequireText(uiFactory, "ApplyHudLabelStyle", "UiFactory must own HUD label color, outline, and shadow styling.");
    RequireText(uiFactory, "ApplyHudMoveModeButtonTheme", "UiFactory must own HUD move-mode button styling.");
    RequireText(uiFactory, "ApplyHudStanceButtonTheme", "UiFactory must own HUD stance button styling.");

    if (hudLayer.Contains("AddThemeStyleboxOverride(\"panel\"", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("HudLayer must not directly override panel styleboxes; use UiFactory panel helpers.");
    }
}

static void ForbidText(string source, string forbidden, string message)
{
    if (source.Contains(forbidden, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(message);
    }
}

static string ReadSourceWithPartials(string sourcePath)
{
    var parts = new List<string>();
    var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (File.Exists(sourcePath))
    {
        parts.Add(File.ReadAllText(sourcePath));
        addedPaths.Add(sourcePath);
    }

    var directory = Path.GetDirectoryName(sourcePath);
    var sourceName = Path.GetFileNameWithoutExtension(sourcePath);
    if (directory is not null && Directory.Exists(directory))
    {
        foreach (var partialPath in Directory.EnumerateFiles(directory, $"{sourceName}.*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path))
        {
            if (addedPaths.Add(partialPath))
            {
                parts.Add(File.ReadAllText(partialPath));
            }
        }
    }

    return string.Join("\n\n", parts);
}

static void RequireText(string source, string required, string message)
{
    if (!source.Contains(required, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(message);
    }
}
