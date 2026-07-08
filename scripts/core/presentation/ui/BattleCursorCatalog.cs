namespace ProceduralRts.Core;

public enum BattleCursorState
{
    DefaultSelect,
    DragSelect,
    UiHover,
    MoveCommand,
    AttackCommand,
    BuildValid,
    BuildInvalid,
    Forbidden,
    HarvestCommand,
    RepairCommand,
    RallyPoint,
}

public enum BattleCursorShape
{
    Arrow,
    PointingHand,
    Cross,
    Move,
    CanDrop,
    Forbidden,
    Drag,
    Help,
}

public readonly record struct BattleCursorDefinition(
    BattleCursorState State,
    BattleCursorShape Shape,
    int HotspotX,
    int HotspotY,
    int CanvasWidth,
    int CanvasHeight,
    string? TexturePath = null,
    string? Source = null)
{
    public bool HotspotInBounds =>
        HotspotX >= 0
        && HotspotY >= 0
        && HotspotX < CanvasWidth
        && HotspotY < CanvasHeight;
}

public static class BattleCursorCatalog
{
    public const int MaximumBitmapCursorSize = 128;

    public static readonly BattleCursorDefinition[] Definitions =
    [
        new(BattleCursorState.DefaultSelect, BattleCursorShape.Arrow, 0, 0, 32, 32, "res://assets/cursors/kenney/default_select.png", KenneyCursorPackSource),
        new(BattleCursorState.DragSelect, BattleCursorShape.Drag, 16, 16, 32, 32, "res://assets/cursors/kenney/move_command.png", KenneyCursorPackSource),
        new(BattleCursorState.UiHover, BattleCursorShape.PointingHand, 8, 2, 32, 32, "res://assets/cursors/kenney/ui_hover.png", KenneyCursorPackSource),
        new(BattleCursorState.MoveCommand, BattleCursorShape.Move, 16, 16, 32, 32, "res://assets/cursors/kenney/move_command.png", KenneyCursorPackSource),
        new(BattleCursorState.AttackCommand, BattleCursorShape.Cross, 16, 16, 32, 32, "res://assets/cursors/kenney/attack_command.png", KenneyCursorPackSource),
        new(BattleCursorState.BuildValid, BattleCursorShape.CanDrop, 16, 16, 32, 32, "res://assets/cursors/kenney/build_valid.png", KenneyCursorPackSource),
        new(BattleCursorState.BuildInvalid, BattleCursorShape.Forbidden, 16, 16, 32, 32, "res://assets/cursors/kenney/build_invalid.png", KenneyCursorPackSource),
        new(BattleCursorState.Forbidden, BattleCursorShape.Forbidden, 16, 16, 32, 32, "res://assets/cursors/kenney/forbidden.png", KenneyCursorPackSource),
        new(BattleCursorState.HarvestCommand, BattleCursorShape.Drag, 16, 16, 32, 32, "res://assets/cursors/kenney/harvest_command.png", KenneyCursorPackSource),
        new(BattleCursorState.RepairCommand, BattleCursorShape.Help, 16, 16, 32, 32, "res://assets/cursors/kenney/repair_command.png", KenneyCursorPackSource),
        new(BattleCursorState.RallyPoint, BattleCursorShape.Cross, 16, 16, 32, 32, "res://assets/cursors/kenney/rally_point.png", KenneyCursorPackSource),
    ];

    public const string KenneyCursorPackSource = "Kenney Cursor Pack CC0: https://kenney.nl/assets/cursor-pack";

    public static BattleCursorDefinition DefinitionFor(BattleCursorState state)
    {
        foreach (var definition in Definitions)
        {
            if (definition.State == state)
            {
                return definition;
            }
        }

        return Definitions[0];
    }

    public static BattleCursorState StateForPreview(CommandPreviewState preview)
    {
        if (!preview.IsValid && preview.Kind != CommandPreviewKind.None)
        {
            return preview.Kind == CommandPreviewKind.BuildInvalid
                ? BattleCursorState.BuildInvalid
                : BattleCursorState.Forbidden;
        }

        return preview.Kind switch
        {
            CommandPreviewKind.Move => BattleCursorState.MoveCommand,
            CommandPreviewKind.DragSelect => BattleCursorState.DragSelect,
            CommandPreviewKind.Attack => BattleCursorState.AttackCommand,
            CommandPreviewKind.Repair => BattleCursorState.RepairCommand,
            CommandPreviewKind.Rally => BattleCursorState.RallyPoint,
            CommandPreviewKind.Harvest => BattleCursorState.HarvestCommand,
            CommandPreviewKind.BuildValid => BattleCursorState.BuildValid,
            CommandPreviewKind.BuildInvalid => BattleCursorState.BuildInvalid,
            CommandPreviewKind.TargetHover => BattleCursorState.UiHover,
            _ => BattleCursorState.DefaultSelect,
        };
    }

    public static IReadOnlyList<string> Validate()
    {
        var issues = new List<string>();
        var seen = new HashSet<BattleCursorState>();
        foreach (var definition in Definitions)
        {
            if (!seen.Add(definition.State))
            {
                issues.Add($"{definition.State} is defined more than once");
            }

            if (definition.CanvasWidth <= 0 || definition.CanvasHeight <= 0)
            {
                issues.Add($"{definition.State} has non-positive bitmap canvas");
            }

            if (definition.CanvasWidth > MaximumBitmapCursorSize || definition.CanvasHeight > MaximumBitmapCursorSize)
            {
                issues.Add($"{definition.State} exceeds {MaximumBitmapCursorSize}px cursor bitmap budget");
            }

            if (!definition.HotspotInBounds)
            {
                issues.Add($"{definition.State} hotspot {definition.HotspotX},{definition.HotspotY} is outside {definition.CanvasWidth}x{definition.CanvasHeight}");
            }

            if (definition.TexturePath is { Length: > 0 } path && !path.StartsWith("res://", StringComparison.Ordinal))
            {
                issues.Add($"{definition.State} texture path must be res:// rooted");
            }

            if (definition.TexturePath is { Length: > 0 } && string.IsNullOrWhiteSpace(definition.Source))
            {
                issues.Add($"{definition.State} texture source/license provenance is missing");
            }
        }

        foreach (BattleCursorState state in Enum.GetValues<BattleCursorState>())
        {
            if (!seen.Contains(state))
            {
                issues.Add($"{state} is missing from the cursor catalog");
            }
        }

        var valid = DefinitionFor(BattleCursorState.BuildValid);
        var invalid = DefinitionFor(BattleCursorState.BuildInvalid);
        if (valid.HotspotX != invalid.HotspotX || valid.HotspotY != invalid.HotspotY)
        {
            issues.Add("BuildValid and BuildInvalid must share a hotspot so placement feedback does not jump");
        }

        var select = DefinitionFor(BattleCursorState.DefaultSelect);
        var dragSelect = DefinitionFor(BattleCursorState.DragSelect);
        if (select.Shape == dragSelect.Shape && select.TexturePath == dragSelect.TexturePath)
        {
            issues.Add("DragSelect must not reuse the default select cursor shape and texture together");
        }

        return issues;
    }
}
