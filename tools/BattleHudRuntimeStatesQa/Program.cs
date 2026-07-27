using System.Diagnostics;
using ProceduralRts.Core;

var failures = new List<string>();
var states = BattleHudRuntimeStateCatalog.States;
var resolutions = BattleHudRuntimeStateCatalog.Resolutions;
var config = BattleHudRuntimeStateCatalog.CaptureConfig;
var visualGateCases = new List<BattleHudVisualGateCase>();

Require(states.Count == 6, "runtime manifest must own exactly the six #604 states", failures);
Require(config == new BattleHudRuntimeCaptureConfig(
        GameLanguage.English,
        2400,
        1729,
        EnemyDifficulty.Normal,
        LaunchMode.Skirmish,
        WorldVisualTheme.DayCommand,
        8,
        6),
    "runtime capture config must freeze language, credits, seed, difficulty, launch mode, theme, settle frames, and render flush frames", failures);
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
    foreach (var resolution in resolutions)
    {
        visualGateCases.Add(BattleHudVisualGate.Validate(state, resolution, failures));
    }
}
Require(visualGateCases.Count == 18, "visual gate catalog must cover all 18 state-resolution captures", failures);

var empty = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.Empty).Projection;
Require(empty.Selection.Kind == BattleHudSelectionKind.None && !empty.Production.Visible && empty.Alert is null,
    "empty must be a clean no-selection projection", failures);
Require(empty.StanceStrip == UnitStanceStripProjection.None,
    "empty must keep the stance strip on its zero-selection projection", failures);

var unit = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.UnitSelected).Projection;
Require(unit.Selection.Kind == BattleHudSelectionKind.Unit && !unit.Production.Visible,
    "unit-selected must expose selection detail without production state", failures);
Require(unit.StanceStrip.State == UnitStanceStripSelectionState.Uniform
    && unit.StanceStrip.IsSelected(UnitStance.Hold),
    "unit-selected must source a uniform Hold stance projection for the pilot screenshot", failures);

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
var capture = Read(root, "scripts", "VisualQaCaptureRoot.cs")
    + Read(root, "scripts", "VisualQaCaptureRoot.BattleHudRuntime.cs");
var harness = Read(root, "tools", "VisualQaCapture.sh");
var productionBattleRoot = Read(root, "scripts", "BattleRoot.cs")
    + string.Join("\n", Directory.EnumerateFiles(
        Path.Combine(root, "scripts", "battle-root"),
        "*.cs",
        SearchOption.TopDirectoryOnly).Select(File.ReadAllText));
Require(!applicator.Contains("UnitBattlefield", StringComparison.Ordinal),
    "runtime state applicator must not reach into gameplay authority", failures);
Require(!applicator.Contains("SetProcess(false)", StringComparison.Ordinal)
    && !productionBattleRoot.Contains("SetProcess(false)", StringComparison.Ordinal)
    && !productionBattleRoot.Contains("SetPhysicsProcess(false)", StringComparison.Ordinal),
    "capture-only authority freeze must not leak into HudLayer or production BattleRoot sources", failures);
Require(!capture.Contains("SetSandboxDeveloperControlsVisible(false)", StringComparison.Ordinal),
    "runtime capture must not hide sandbox controls and mask the real launch gate", failures);
var rootLookup = capture.IndexOf("_activeScene is T root && root.Name == name", StringComparison.Ordinal);
var descendantLookup = capture.IndexOf("_activeScene?.FindChild(name", StringComparison.Ordinal);
Require(rootLookup >= 0 && descendantLookup > rootLookup,
    "visual capture node lookup must match the loaded scene root before searching descendants", failures);
Require(capture.Contains("RequiredNode<BattleRoot>(\"Battle\")", StringComparison.Ordinal),
    "visual capture theme controls must resolve the actual Battle scene root name", failures);
Require(!harness.Contains("for state in", StringComparison.Ordinal),
    "Visual QA harness must not duplicate the typed runtime state catalog in bash", failures);
RequireText(harness, "git status --porcelain=v1 --untracked-files=all",
    "Visual QA harness must reject untracked capture inputs", failures);

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

if (args is [
    "--write-artifact-manifest",
    var manifestPath,
    var structuralEvidencePath,
    var exactCommit,
    var captureRunNonce])
{
    BattleHudVisualArtifactManifestWriter.Write(
        manifestPath,
        structuralEvidencePath,
        exactCommit,
        captureRunNonce,
        ReadRepositoryHead(root),
        visualGateCases);
    Console.WriteLine($"Battle HUD artifact manifest written: {manifestPath} ({visualGateCases.Count} captures, {exactCommit})");
}
else if (args.Length == 0)
{
    Console.WriteLine("BattleHudRuntimeStatesQa PASSED: one typed catalog defines 18 deterministic captures and their runtime probe contracts.");
}
else
{
    throw new ArgumentException(
        "Usage: BattleHudRuntimeStatesQa [--write-artifact-manifest <manifest-path> <structural-evidence-path> <exact-commit> <capture-run-nonce>]");
}

static void Require(bool condition, string message, List<string> failures)
{
    if (!condition)
    {
        failures.Add(message);
    }
}

static void RequireText(string source, string expected, string message, List<string> failures) =>
    Require(source.Contains(expected, StringComparison.Ordinal), message, failures);

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

static string ReadRepositoryHead(string root)
{
    using var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        },
    };
    process.StartInfo.ArgumentList.Add("rev-parse");
    process.StartInfo.ArgumentList.Add("HEAD");
    process.Start();
    var output = process.StandardOutput.ReadToEnd().Trim();
    var error = process.StandardError.ReadToEnd().Trim();
    process.WaitForExit();
    if (process.ExitCode != 0 || output.Length != 40 || !output.All(Uri.IsHexDigit))
    {
        throw new InvalidOperationException($"Could not resolve repository HEAD: {error}");
    }

    return output.ToLowerInvariant();
}
