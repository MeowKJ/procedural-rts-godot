using Godot;
using ProceduralRts.Core;

var failures = new List<string>(BattleCursorCatalog.Validate());
var root = FindRepoRoot();
var requiredTextureStates = Enum.GetValues<BattleCursorState>();

Require(BattleCursorCatalog.StateForPreview(CommandPreviewState.None) == BattleCursorState.DefaultSelect, "None preview should use DefaultSelect.", failures);
Require(StateFor(CommandPreviewKind.Select, true) == BattleCursorState.DefaultSelect, "Select preview should use DefaultSelect.", failures);
Require(StateFor(CommandPreviewKind.TargetHover, true) == BattleCursorState.UiHover, "Target hover preview should use UiHover.", failures);
Require(StateFor(CommandPreviewKind.Move, true) == BattleCursorState.MoveCommand, "Move preview should use MoveCommand.", failures);
Require(StateFor(CommandPreviewKind.Attack, true) == BattleCursorState.AttackCommand, "Attack preview should use AttackCommand.", failures);
Require(StateFor(CommandPreviewKind.Harvest, true) == BattleCursorState.HarvestCommand, "Harvest preview should use HarvestCommand.", failures);
Require(StateFor(CommandPreviewKind.Repair, true) == BattleCursorState.RepairCommand, "Repair preview should use RepairCommand.", failures);
Require(StateFor(CommandPreviewKind.Rally, true) == BattleCursorState.RallyPoint, "Rally preview should use RallyPoint.", failures);
Require(StateFor(CommandPreviewKind.BuildValid, true) == BattleCursorState.BuildValid, "Valid build preview should use BuildValid.", failures);
Require(StateFor(CommandPreviewKind.BuildInvalid, false) == BattleCursorState.BuildInvalid, "Invalid build preview should use BuildInvalid.", failures);
Require(StateFor(CommandPreviewKind.Attack, false) == BattleCursorState.Forbidden, "Invalid non-build previews should use Forbidden.", failures);

foreach (var state in requiredTextureStates)
{
    var definition = BattleCursorCatalog.DefinitionFor(state);
    Require(!string.IsNullOrWhiteSpace(definition.TexturePath), $"{state} should have a Kenney texture path.", failures);
    Require(definition.Source == BattleCursorCatalog.KenneyCursorPackSource, $"{state} should record Kenney CC0 provenance.", failures);

    if (!string.IsNullOrWhiteSpace(definition.TexturePath))
    {
        Require(File.Exists(ToRepoPath(root, definition.TexturePath)), $"{state} texture file should exist: {definition.TexturePath}", failures);
    }
}

Require(File.Exists(Path.Combine(root, "assets", "cursors", "kenney", "LICENSE.kenney-cursor-pack.txt")), "Kenney cursor license/source file should be present.", failures);

if (failures.Count > 0)
{
    Console.Error.WriteLine("CursorCatalogQa FAILED:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    System.Environment.Exit(1);
}

Console.WriteLine($"CursorCatalogQa PASSED: {BattleCursorCatalog.Definitions.Length} cursor states, hotspot bounds, preview mapping, Kenney texture paths, and build valid/invalid hotspot parity.");

static BattleCursorState StateFor(CommandPreviewKind kind, bool valid)
{
    return BattleCursorCatalog.StateForPreview(new CommandPreviewState(kind, kind.ToString(), Vector2.Zero, Vector2.Zero, valid));
}

static void Require(bool condition, string message, List<string> failures)
{
    if (!condition)
    {
        failures.Add(message);
    }
}

static string FindRepoRoot()
{
    var current = Directory.GetCurrentDirectory();
    while (!string.IsNullOrEmpty(current))
    {
        if (File.Exists(Path.Combine(current, "ProceduralRts.csproj")))
        {
            return current;
        }

        current = Directory.GetParent(current)?.FullName ?? string.Empty;
    }

    throw new DirectoryNotFoundException("Could not locate ProceduralRts.csproj from current directory.");
}

static string ToRepoPath(string root, string resourcePath)
{
    const string prefix = "res://";
    if (!resourcePath.StartsWith(prefix, StringComparison.Ordinal))
    {
        return resourcePath;
    }

    return Path.Combine(root, resourcePath[prefix.Length..].Replace('/', Path.DirectorySeparatorChar));
}
