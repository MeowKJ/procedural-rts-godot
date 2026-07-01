namespace ProceduralRts.Core;

public static class IconLibrary
{
    public const string TablerAttributionPath = "res://assets/icons/tabler/README.md";

    private static readonly IReadOnlyDictionary<IconGlyph, string> TablerPaths = new Dictionary<IconGlyph, string>
    {
        [IconGlyph.Infantry] = "res://assets/icons/tabler/infantry.svg",
        [IconGlyph.Tank] = "res://assets/icons/tabler/tank.svg",
        [IconGlyph.Harvester] = "res://assets/icons/tabler/harvester.svg",
        [IconGlyph.Building] = "res://assets/icons/tabler/building.svg",
        [IconGlyph.Turret] = "res://assets/icons/tabler/attack-move.svg",
        [IconGlyph.Air] = "res://assets/icons/tabler/move.svg",
        [IconGlyph.Naval] = "res://assets/icons/tabler/move.svg",
        [IconGlyph.Group] = "res://assets/icons/tabler/group.svg",
        [IconGlyph.Move] = "res://assets/icons/tabler/move.svg",
        [IconGlyph.AttackMove] = "res://assets/icons/tabler/attack-move.svg",
        [IconGlyph.IgnoreMove] = "res://assets/icons/tabler/ignore-move.svg",
        [IconGlyph.StanceHold] = "res://assets/icons/tabler/group.svg",
        [IconGlyph.StanceAggressive] = "res://assets/icons/tabler/attack-move.svg",
        [IconGlyph.StanceReturn] = "res://assets/icons/tabler/move.svg",
        [IconGlyph.StancePassive] = "res://assets/icons/tabler/group.svg",
        [IconGlyph.StanceIgnore] = "res://assets/icons/tabler/ignore-move.svg",
        [IconGlyph.Cancel] = "res://assets/icons/tabler/cancel.svg",
        [IconGlyph.Credits] = "res://assets/icons/tabler/credits.svg",
        [IconGlyph.Settings] = "res://assets/icons/tabler/group.svg",
    };

    public static bool TryPath(IconGlyph glyph, out string path)
    {
        return TablerPaths.TryGetValue(glyph, out path!);
    }

    public static IReadOnlyDictionary<IconGlyph, string> Paths => TablerPaths;
}
