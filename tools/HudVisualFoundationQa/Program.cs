using ProceduralRts.Core;
using ProceduralRts.Ui;

var palettes = new[]
{
    SoftOldCityTheme.For(WorldVisualTheme.DayCommand),
    SoftOldCityTheme.For(WorldVisualTheme.FogMorning),
    SoftOldCityTheme.For(WorldVisualTheme.DuskDefense),
    SoftOldCityTheme.For(WorldVisualTheme.NightRadar),
};
var primitives = Enum.GetValues<HudVisualPrimitive>();
var failures = new List<string>();

foreach (var palette in palettes)
{
    foreach (var primitive in primitives)
    {
        var metrics = HudVisualFoundation.MetricsFor(primitive);
        var normal = HudVisualFoundation.For(palette, primitive, HudVisualState.Normal, palette.Repair);
        var selected = HudVisualFoundation.For(palette, primitive, HudVisualState.Selected, palette.Repair);
        var selectedFocused = HudVisualFoundation.For(
            palette,
            primitive,
            HudVisualState.Selected | HudVisualState.Focused,
            palette.Repair);
        var disabled = HudVisualFoundation.For(palette, primitive, HudVisualState.Disabled, palette.Repair);
        var warning = HudVisualFoundation.For(palette, primitive, HudVisualState.Warning, palette.Repair);

        Require(selected.BorderWidth == 2 && selected.Border.A > normal.Border.A, $"{primitive} selected state must be visually stronger", failures);
        Require(selectedFocused.Fill == selected.Fill, $"{primitive} focus must preserve the selected fill", failures);
        Require(selectedFocused.BorderWidth >= selected.BorderWidth && selectedFocused.Border.A > selected.Border.A, $"{primitive} selected+focused state must compose both signals", failures);
        Require(disabled.Text.A < normal.Text.A && disabled.Border.A < normal.Border.A, $"{primitive} disabled state must be quieter", failures);
        Require(warning.Accent == palette.Danger && warning.Border.A > normal.Border.A, $"{primitive} warning state must use the danger token", failures);
        Require(metrics.CornerRadius > 0, $"{primitive} must own a positive radius metric", failures);
        Require(metrics.ContentPadding > 0 && metrics.ItemSpacing > 0, $"{primitive} must own positive spacing metrics", failures);
        Require(metrics.FontSize > 0 && metrics.DetailFontSize > 0, $"{primitive} must own typography sizes", failures);
    }
}

Require(HudVisualFoundation.StateFor(HudStatusBadgeRole.Neutral) == HudVisualState.Normal, "neutral badge role must map explicitly to normal", failures);
Require(HudVisualFoundation.StateFor(HudStatusBadgeRole.Warning) == HudVisualState.Warning, "warning badge role must map explicitly to warning", failures);

var root = FindRoot();
var uiFactory = Read(root, "scripts", "ui", "UiFactory.cs");
var commandCard = Read(root, "scripts", "ui", "hud", "HudLayer.CommandControls.cs");
var providerLane = Read(root, "scripts", "ui", "hud", "HudLayer.ProviderLaneControls.cs");
var modeStrip = Read(root, "scripts", "ui", "hud", "controls", "HudLayer.CatalogControls.cs");
var upgradeCards = Read(root, "scripts", "ui", "hud", "HudLayer.UpgradeCards.cs");
var upgradeCardControl = Read(root, "scripts", "ui", "hud", "HudLayer.UpgradeCardControls.cs");
var visualCapture = Read(root, "scripts", "VisualQaCaptureRoot.cs");
var visualCaptureHarness = Read(root, "tools", "VisualQaCapture.sh");

RequireText(uiFactory, "var metrics = HudVisualFoundation.MetricsFor(primitive);", "UiFactory must consume foundation metrics", failures);
RequireText(uiFactory, "CreateHudFoundationStyleBox", "UiFactory must apply foundation radius and padding", failures);
RequireText(commandCard, "MetricsFor(HudVisualPrimitive.CommandCard)", "Command Card must consume foundation metrics", failures);
RequireText(providerLane, "MetricsFor(HudVisualPrimitive.QueueRow)", "ProviderLane Queue Row must consume foundation metrics", failures);
RequireText(providerLane, "HudVisualState.Selected", "ProviderLane must reuse foundation selected state", failures);
RequireText(modeStrip, "HudVisualState.Selected", "Mode Strip must preserve selected state", failures);
RequireText(modeStrip, "HudVisualState.Focused", "Mode Strip must compose focused state", failures);
RequireText(modeStrip, "new Vector2(13, rect.Size.Y * 0.5f)", "Mode Strip must reserve a left icon lane for its compact grammar copy", failures);
RequireText(modeStrip, "Label,", "Mode Strip must visibly draw its localized mode label", failures);
RequireText(modeStrip, "HudVisualPrimitive.StatusBadge", "Mode Strip must render its interaction grammar as a foundation status badge", failures);
RequireText(modeStrip, "Detail,", "Mode Strip must visibly draw its localized interaction grammar", failures);
RequireText(upgradeCards, "HudStatusBadgeRole BadgeRole", "upgrade status data must expose an explicit badge role", failures);
RequireText(upgradeCardControl, "StateFor(_state.BadgeRole)", "Status Badge must consume its explicit visual role", failures);
Require(!upgradeCardControl.Contains("StatusKey.Contains", StringComparison.Ordinal), "Status Badge role must not be inferred from localization-key substrings", failures);

foreach (var fileName in new[]
{
    "battle_hud_style1d_night.png",
    "battle_hud_theme_transition.png",
    "battle_hud_foundation_states.png",
})
{
    RequireText(visualCapture, fileName, $"Visual QA capture must produce {fileName}", failures);
    RequireText(visualCaptureHarness, fileName, $"Visual QA harness must assert {fileName}", failures);
}

RequireText(visualCapture, "SetBattleThemeTransition(", "Visual QA must exercise a real theme transition", failures);
RequireText(visualCapture, "DebugConfigureHudVisualFoundationQa", "Visual QA must stage selected+focused Mode Strip and warning Status Badge", failures);

if (failures.Count > 0)
{
    throw new InvalidOperationException("HudVisualFoundationQa FAILED:\n" + string.Join("\n", failures));
}

Console.WriteLine("HudVisualFoundationQa PASSED: composed states, Godot-owned metrics, consumer wiring, explicit badge roles, and focused visual evidence are covered across every world theme.");

static void Require(bool condition, string message, List<string> failures)
{
    if (!condition)
    {
        failures.Add(message);
    }
}

static string FindRoot()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "ProceduralRts.csproj")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not locate ProceduralRts.csproj.");
}

static string Read(string root, params string[] parts)
{
    return File.ReadAllText(Path.Combine([root, .. parts]));
}

static void RequireText(string source, string expected, string message, List<string> failures)
{
    Require(source.Contains(expected, StringComparison.Ordinal), message, failures);
}
