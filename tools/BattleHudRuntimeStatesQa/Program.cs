using ProceduralRts.Core;

var failures = new List<string>();
var states = BattleHudRuntimeStateCatalog.States;
var resolutions = BattleHudRuntimeStateCatalog.Resolutions;
var config = BattleHudRuntimeStateCatalog.CaptureConfig;

Require(states.Count == 6, "runtime manifest must own exactly the six #604 states", failures);
Require(config == new BattleHudRuntimeCaptureConfig(
        GameLanguage.English,
        2400,
        1729,
        EnemyDifficulty.Normal,
        LaunchMode.Skirmish,
        WorldVisualTheme.DayCommand,
        8),
    "runtime capture config must freeze language, credits, seed, difficulty, launch mode, theme, and settle frames", failures);
Require(resolutions.SequenceEqual(
    [
        new BattleHudCaptureResolution(1280, 720),
        new BattleHudCaptureResolution(1600, 900),
        new BattleHudCaptureResolution(1920, 1080),
    ]), "runtime manifest must own the three accepted desktop resolutions", failures);
Require(states.Select(state => state.Kind).Distinct().Count() == states.Count, "runtime state kinds must be unique", failures);
Require(states.Select(state => state.CaptureId).Distinct(StringComparer.Ordinal).Count() == states.Count, "runtime capture ids must be unique", failures);

var captureFiles = states
    .SelectMany(state => resolutions.Select(state.CaptureFileName))
    .ToArray();
Require(captureFiles.Length == 18, "six states across three resolutions must produce 18 captures", failures);
Require(captureFiles.Distinct(StringComparer.Ordinal).Count() == captureFiles.Length, "runtime capture filenames must be unique", failures);

foreach (var state in states)
{
    Require(state.SourceKind is BattleHudRuntimeSourceKind.ReadOnlyProjection or BattleHudRuntimeSourceKind.CommandIntent,
        $"{state.Kind} must map to a read-only projection or command intent", failures);
    Require(state.SourceKind == BattleHudRuntimeSourceKind.CommandIntent
        ? state.CommandIntent != BattleHudCommandIntentKind.None
        : state.CommandIntent == BattleHudCommandIntentKind.None,
        $"{state.Kind} source and command intent must agree", failures);
    Require(state.Projection.Credits >= 0, $"{state.Kind} credits must be non-negative", failures);
}

var empty = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.Empty).Projection;
Require(empty.Selection.Kind == BattleHudSelectionKind.None && !empty.Production.Visible && empty.Alert is null,
    "empty must be a clean no-selection projection", failures);

var unit = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.UnitSelected).Projection;
Require(unit.Selection.Kind == BattleHudSelectionKind.Unit && !unit.Production.Visible,
    "unit-selected must expose selection detail without production state", failures);

var building = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.ProductionBuildingSelected).Projection;
Require(building.Selection.Kind == BattleHudSelectionKind.ProductionBuilding
    && building.Production.Visible
    && building.Production.EnoughCredits,
    "production-building-selected must expose an available production projection", failures);

var unavailableSpec = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.UnavailableLowResources);
Require(unavailableSpec.SourceKind == BattleHudRuntimeSourceKind.ReadOnlyProjection
    && unavailableSpec.CommandIntent == BattleHudCommandIntentKind.None
    && unavailableSpec.Projection.Production.Visible
    && !unavailableSpec.Projection.Production.EnoughCredits
    && unavailableSpec.Projection.Credits < unavailableSpec.Projection.Production.Cost,
    "unavailable/low-resource must remain an authoritative read-only credit projection", failures);

var queue = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.QueueProgress).Projection.Production;
Require(queue.QueuedCount > 0 && queue.ActiveProgress is > 0 and < 1 && queue.CanCancel,
    "queue-progress must expose active progress and a cancel intent affordance", failures);

var alert = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.Alert).Projection.Alert;
Require(alert is { Kind: AlertKind.Economy, RemainingRatio: > 0 },
    "alert must expose a visible read-only economy alert projection", failures);

var root = FindRoot();
var applicator = Read(root, "scripts", "ui", "hud", "HudLayer.RuntimeStates.cs");
var capture = Read(root, "scripts", "VisualQaCaptureRoot.cs");
var harness = Read(root, "tools", "VisualQaCapture.sh");
var productionBattleRoot = Read(root, "scripts", "BattleRoot.cs")
    + string.Join("\n", Directory.EnumerateFiles(
        Path.Combine(root, "scripts", "battle-root"),
        "*.cs",
        SearchOption.TopDirectoryOnly).Select(File.ReadAllText));
RequireText(applicator, "ApplyBattleHudRuntimeProjection(BattleHudRuntimeProjection projection)",
    "HudLayer must consume the typed read-only runtime projection", failures);
Require(!applicator.Contains("UnitBattlefield", StringComparison.Ordinal),
    "runtime state applicator must not reach into gameplay authority", failures);
Require(!applicator.Contains("SetProcess(false)", StringComparison.Ordinal)
    && !productionBattleRoot.Contains("SetProcess(false)", StringComparison.Ordinal)
    && !productionBattleRoot.Contains("SetPhysicsProcess(false)", StringComparison.Ordinal),
    "capture-only authority freeze must not leak into HudLayer or production BattleRoot sources", failures);
RequireText(capture, "CaptureBattleHudRuntimeStates", "Visual QA must capture the runtime state manifest", failures);
Require(!capture.Contains("SetSandboxDeveloperControlsVisible(false)", StringComparison.Ordinal),
    "runtime capture must not hide sandbox controls and mask the real launch gate", failures);
RequireText(capture, "AssertNormalSkirmishSandboxHidden", "runtime capture must assert the real Skirmish sandbox gate", failures);
RequireText(capture, "RequiredNode<Control>(\"SandboxDeveloperPanel\").Visible",
    "runtime capture must read the actual SandboxDeveloperPanel visibility", failures);
RequireText(capture, "StageDeterministicBattleCapture", "each Battle load must stage the fixed skirmish config", failures);
RequireText(capture, "await LoadScene(BattleScenePath);", "each runtime state must start from a fresh Battle scene", failures);
RequireOrdered(capture, failures,
    "private async Task CaptureBattleHudRuntimeStates(",
    "await LoadScene(BattleScenePath);",
    "AssertNormalSkirmishSandboxHidden();",
    "FreezeBattleHudRuntimeProjectionAuthority();",
    "foreach (var resolution in BattleHudRuntimeStateCatalog.Resolutions)");
RequireText(capture, "battle.SetProcess(false);",
    "runtime fixture must stop live BattleRoot refresh from overwriting the typed projection", failures);
RequireText(capture, "battle.SetPhysicsProcess(false);",
    "runtime fixture must stop live BattleRoot physics while responsive HUD layout settles", failures);
RequireText(capture, "GuiGetFocusOwner()?.ReleaseFocus()", "runtime captures must clear transient UI focus", failures);
RequireText(capture, "NextFrames(config.SettleFrames)", "runtime captures must use the manifest settle-frame contract", failures);
RequireOrdered(capture, failures,
    "private async Task CaptureBattleHudRuntimeResolution(",
    "GetTree().Paused = false;",
    "SetCaptureSize(new Vector2I(resolution.Width, resolution.Height));",
    "hud.ApplyBattleHudRuntimeProjection(state.Projection);",
    "GetViewport().GuiGetFocusOwner()?.ReleaseFocus();",
    "await NextFrames(config.SettleFrames);",
    "AssertNormalSkirmishSandboxHidden();",
    "GetTree().Paused = true;",
    "await Capture(outputPath, state.CaptureFileName(resolution));");
RequireText(harness, "battle_hud_runtime_${state}_${width}x${height}.png",
    "Visual QA harness must validate every state/resolution capture", failures);

Require(BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.ProductionBuildingSelected).Projection.Status == "PROD READY",
    "production-ready status must fit the compact 1280 top strip", failures);
Require(unavailableSpec.Projection.Status == "LOW CREDITS",
    "low-resource status must fit the compact 1280 top strip", failures);
Require(building.Selection.Detail == "SELL REFUND 300",
    "building refund signal must remain complete in the compact 1280 detail drawer", failures);
Require(states.All(state => state.Projection.Status.Length <= 13),
    "runtime top-strip status copy must stay within the compact reference budget", failures);

if (failures.Count > 0)
{
    throw new InvalidOperationException("BattleHudRuntimeStatesQa FAILED:\n" + string.Join("\n", failures));
}

Console.WriteLine("BattleHudRuntimeStatesQa PASSED: six typed projection/intent states produce 18 deterministic normal-skirmish captures with no gameplay authority coupling.");

static void Require(bool condition, string message, List<string> failures)
{
    if (!condition)
    {
        failures.Add(message);
    }
}

static void RequireText(string source, string expected, string message, List<string> failures) =>
    Require(source.Contains(expected, StringComparison.Ordinal), message, failures);

static void RequireOrdered(string source, List<string> failures, params string[] markers)
{
    var cursor = 0;
    foreach (var marker in markers)
    {
        var index = source.IndexOf(marker, cursor, StringComparison.Ordinal);
        if (index < 0)
        {
            failures.Add($"runtime capture order is missing or misplaced: {marker}");
            return;
        }

        cursor = index + marker.Length;
    }
}

static string Read(string root, params string[] parts) =>
    File.ReadAllText(Path.Combine([root, .. parts]));

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
