using ProceduralRts.Core;

static class CommandRibbonContextQa
{
    public static void AssertResolver()
    {
        var passive = Resolve(CommandPreviewPhase.PassiveHover, "ATTACK", true, null, 0, MoveCommandMode.Direct);
        Require(passive.Kind == CommandRibbonContextKind.IdleMoveMode, "Passive hover must not masquerade as an armed command.");

        var uniform = Resolve(CommandPreviewPhase.PassiveHover, "ENEMY", true, UnitStance.Hold, 3, MoveCommandMode.Attack);
        Require(uniform.Kind == CommandRibbonContextKind.UniformStance
            && uniform.StanceAccentRole == UnitStanceAccentRole.CatRoute, "Uniform stance must outrank passive hover.");

        var mixed = Resolve(CommandPreviewPhase.None, "", false, null, 4, MoveCommandMode.Ignore);
        Require(mixed.Kind == CommandRibbonContextKind.MixedStance, "Mixed selection must outrank idle move mode.");

        var rally = Resolve(CommandPreviewPhase.ArmedCommand, "RALLY POINT", true, UnitStance.Hold, 3, MoveCommandMode.Direct);
        Require(rally.Kind == CommandRibbonContextKind.ActiveCommand, "Armed rally must outrank selected stance.");
        var repairBlocked = Resolve(CommandPreviewPhase.ArmedCommand, "NO REPAIR TARGET", false, UnitStance.Hold, 3, MoveCommandMode.Direct);
        Require(repairBlocked.Kind == CommandRibbonContextKind.BlockedCommand, "Invalid armed repair must resolve as blocked.");
        var ability = Resolve(CommandPreviewPhase.ArmedCommand, "SCAN", true, null, 0, MoveCommandMode.Direct);
        Require(ability.Kind == CommandRibbonContextKind.ActiveCommand, "Targeted ability must resolve as active.");
        var buildValid = Resolve(CommandPreviewPhase.BuildPlacement, "PLACE HQ", true, null, 0, MoveCommandMode.Direct);
        var buildInvalid = Resolve(CommandPreviewPhase.BuildPlacement, "OUTSIDE", false, null, 0, MoveCommandMode.Direct);
        Require(buildValid.Kind == CommandRibbonContextKind.ActiveCommand && buildInvalid.Kind == CommandRibbonContextKind.BlockedCommand,
            "Build placement validity must resolve structurally.");

        var reset = Resolve(CommandPreviewPhase.None, "", false, UnitStance.Aggressive, 2, MoveCommandMode.Ignore);
        Require(reset.Kind == CommandRibbonContextKind.UniformStance, "Clearing an active preview must reveal selected stance context.");

        var previousLanguage = GameText.CurrentLanguage;
        try
        {
            GameText.CurrentLanguage = GameLanguage.ChineseSimplified;
            var chinese = Resolve(CommandPreviewPhase.ArmedCommand, "扫描", true, null, 0, MoveCommandMode.Direct);
            GameText.CurrentLanguage = GameLanguage.English;
            var english = Resolve(CommandPreviewPhase.ArmedCommand, "SCAN", true, null, 0, MoveCommandMode.Direct);
            Require(chinese.Kind == english.Kind && chinese.Kind == CommandRibbonContextKind.ActiveCommand,
                "Language changes must not alter typed command context classification.");
        }
        finally
        {
            GameText.CurrentLanguage = previousLanguage;
        }
    }

    private static CommandRibbonContextState Resolve(
        CommandPreviewPhase phase,
        string label,
        bool valid,
        UnitStance? stance,
        int count,
        MoveCommandMode mode) => CommandRibbonContextResolver.Resolve(phase, label, valid, stance, count, mode);

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
