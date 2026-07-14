namespace ProceduralRts.Core;

public enum UnitStanceAccentRole
{
    CatRoute,
    DogCommand,
    Repair,
    Text,
    Danger,
}

public readonly record struct UnitStancePresentation(
    UnitStance Stance,
    string LabelKey,
    IconGlyph Glyph,
    char Hotkey,
    UnitStanceAccentRole AccentRole)
{
    public string Label => GameText.T(LabelKey);
    public string Tooltip => $"{Hotkey}  {Label}";
}

public static class UnitStancePresentationCatalog
{
    public static readonly UnitStancePresentation[] Definitions =
    [
        new(UnitStance.Hold, "stance.hold", IconGlyph.StanceHold, 'Z', UnitStanceAccentRole.CatRoute),
        new(UnitStance.Aggressive, "stance.aggressive", IconGlyph.StanceAggressive, 'X', UnitStanceAccentRole.DogCommand),
        new(UnitStance.ReturnGuard, "stance.returnGuard", IconGlyph.StanceReturn, 'C', UnitStanceAccentRole.Repair),
        new(UnitStance.PassiveRetaliate, "stance.passive", IconGlyph.StancePassive, 'V', UnitStanceAccentRole.Text),
        new(UnitStance.Ignore, "stance.ignore", IconGlyph.StanceIgnore, 'B', UnitStanceAccentRole.Danger),
    ];

    public static UnitStancePresentation DefinitionFor(UnitStance stance)
    {
        foreach (var definition in Definitions)
        {
            if (definition.Stance == stance)
            {
                return definition;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(stance), stance, "Unknown unit stance.");
    }

    public static bool TryDefinitionForHotkey(char hotkey, out UnitStancePresentation presentation)
    {
        foreach (var definition in Definitions)
        {
            if (definition.Hotkey == char.ToUpperInvariant(hotkey))
            {
                presentation = definition;
                return true;
            }
        }

        presentation = default;
        return false;
    }
}
