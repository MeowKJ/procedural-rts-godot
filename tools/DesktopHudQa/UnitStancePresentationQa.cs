using ProceduralRts.Core;

static class UnitStancePresentationQa
{
    public static void AssertCatalog()
    {
        var expected = new[]
        {
            (UnitStance.Hold, "stance.hold", IconGlyph.StanceHold, 'Z', UnitStanceAccentRole.CatRoute, "HOLD", "守卫"),
            (UnitStance.Aggressive, "stance.aggressive", IconGlyph.StanceAggressive, 'X', UnitStanceAccentRole.DogCommand, "AGGRESSIVE", "侵略"),
            (UnitStance.ReturnGuard, "stance.returnGuard", IconGlyph.StanceReturn, 'C', UnitStanceAccentRole.Repair, "RETURN", "积极守卫"),
            (UnitStance.PassiveRetaliate, "stance.passive", IconGlyph.StancePassive, 'V', UnitStanceAccentRole.Text, "PASSIVE", "被动"),
            (UnitStance.Ignore, "stance.ignore", IconGlyph.StanceIgnore, 'B', UnitStanceAccentRole.Danger, "IGNORE", "无视"),
        };
        Require(UnitStancePresentationCatalog.Definitions.Length == expected.Length, "Stance catalog must define exactly five stances.");
        var previousLanguage = GameText.CurrentLanguage;
        try
        {
            for (var index = 0; index < expected.Length; index++)
            {
                var definition = UnitStancePresentationCatalog.Definitions[index];
                var item = expected[index];
                Require((definition.Stance, definition.LabelKey, definition.Glyph, definition.Hotkey, definition.AccentRole)
                    == (item.Item1, item.Item2, item.Item3, item.Item4, item.Item5), $"Stance catalog entry {index} drifted.");
                Require(UnitStancePresentationCatalog.DefinitionFor(item.Item1) == definition, $"Stance lookup failed for {item.Item1}.");
                Require(UnitStancePresentationCatalog.TryDefinitionForHotkey(item.Item4, out var byHotkey) && byHotkey == definition, $"Hotkey lookup failed for {item.Item4}.");
                GameText.CurrentLanguage = GameLanguage.English;
                Require(definition.Label == item.Item6, $"English stance label drifted for {item.Item1}.");
                GameText.CurrentLanguage = GameLanguage.ChineseSimplified;
                Require(definition.Label == item.Item7, $"Chinese stance label drifted for {item.Item1}.");
            }
        }
        finally
        {
            GameText.CurrentLanguage = previousLanguage;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
