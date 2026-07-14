using Godot;
using ProceduralRts.Core;

var failures = new List<string>(BattleCursorCatalog.Validate());
var root = FindRepoRoot();
var requiredTextureStates = Enum.GetValues<BattleCursorState>();

Require(BattleCursorCatalog.StateForPreview(CommandPreviewState.None) == BattleCursorState.DefaultSelect, "None preview should use DefaultSelect.", failures);
Require(StateFor(CommandPreviewKind.Select, true) == BattleCursorState.DefaultSelect, "Select preview should use DefaultSelect.", failures);
Require(StateFor(CommandPreviewKind.DragSelect, true) == BattleCursorState.DragSelect, "Drag-select preview should use DragSelect.", failures);
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

var hudCursor = File.ReadAllText(Path.Combine(root, "scripts", "ui", "hud", "HudLayer.Cursor.cs"));
var hudLayer = File.ReadAllText(Path.Combine(root, "scripts", "ui", "HudLayer.cs"));
var battleLifecycle = File.ReadAllText(Path.Combine(root, "scripts", "BattleRoot.Lifecycle.cs"));
RequireText(hudCursor, "HashSet<Texture2D> _ownedCursorTextures", "HudLayer should distinguish owned ImageTexture cursors from ResourceLoader textures.", failures);
RequireText(hudCursor, "HashSet<Input.CursorShape> _customCursorShapes", "HudLayer should track every Input shape that receives a custom cursor texture.", failures);
RequireText(hudCursor, "_ownedCursorTextures.Add(texture)", "Source-PNG cursor textures should be registered for teardown.", failures);
RequireText(hudCursor, "_customCursorShapes.Add(shape)", "Custom cursor assignment should register the affected Input shape.", failures);
RequireText(hudCursor, "foreach (var shape in _customCursorShapes)", "Cursor teardown should visit every registered custom Input shape.", failures);
RequireText(hudCursor, "Input.SetCustomMouseCursor(null, shape)", "Cursor teardown should clear each registered Input shape before disposing textures.", failures);
RequireText(hudCursor, "_customCursorShapes.Clear()", "Cursor teardown should clear the registered Input shape set.", failures);
RequireText(hudCursor, "Input.SetCustomMouseCursor(null, Input.CursorShape.Arrow)", "Cursor teardown should clear Input's custom cursor reference before disposing textures.", failures);
RequireText(hudCursor, "ManagedGodotResourceCleanup.DisposeGodotObject(texture)", "Cursor teardown should dispose HUD-owned textures explicitly.", failures);
RequireText(hudCursor, "_cursorTextureCache.Clear()", "Cursor teardown should clear cached references.", failures);
RequireText(hudLayer, "public override void _ExitTree()", "HudLayer should own its texture teardown lifecycle.", failures);
RequireText(hudLayer, "ReleaseCursorTextures();", "HudLayer managed-resource release should include cursor textures.", failures);
Require(!battleLifecycle.Contains("_hud?.ReleaseManagedResources()", StringComparison.Ordinal), "BattleRoot should not duplicate HudLayer-owned texture teardown.", failures);

var select = BattleCursorCatalog.DefinitionFor(BattleCursorState.DefaultSelect);
var dragSelect = BattleCursorCatalog.DefinitionFor(BattleCursorState.DragSelect);
Require(dragSelect.Shape == BattleCursorShape.Drag, "DragSelect should keep Godot drag-shape fallback.", failures);
Require(dragSelect.TexturePath != select.TexturePath, "DragSelect should not reuse the default select texture.", failures);

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

static void RequireText(string source, string required, string message, List<string> failures)
{
    Require(source.Contains(required, StringComparison.Ordinal), message, failures);
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
