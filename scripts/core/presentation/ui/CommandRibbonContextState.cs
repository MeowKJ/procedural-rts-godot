namespace ProceduralRts.Core;

public enum CommandRibbonContextKind
{
    IdleMoveMode,
    UniformStance,
    MixedStance,
    ActiveCommand,
    BlockedCommand,
}

public readonly record struct CommandRibbonContextState(
    CommandRibbonContextKind Kind,
    string Text,
    UnitStanceAccentRole? StanceAccentRole = null);

public static class CommandRibbonContextResolver
{
    public static CommandRibbonContextState Resolve(
        CommandPreviewPhase previewPhase,
        string previewLabel,
        bool previewIsValid,
        UnitStance? uniformStance,
        int selectedUnitCount,
        MoveCommandMode moveMode)
    {
        if (previewPhase is CommandPreviewPhase.ArmedCommand or CommandPreviewPhase.BuildPlacement)
        {
            return new CommandRibbonContextState(
                previewIsValid ? CommandRibbonContextKind.ActiveCommand : CommandRibbonContextKind.BlockedCommand,
                GameText.Format(previewIsValid ? "command.ribbon.active" : "command.ribbon.blocked", previewLabel));
        }

        if (selectedUnitCount > 0)
        {
            if (uniformStance is { } stance)
            {
                var presentation = UnitStancePresentationCatalog.DefinitionFor(stance);
                return new CommandRibbonContextState(
                    CommandRibbonContextKind.UniformStance,
                    GameText.Format("stance.context.uniform", selectedUnitCount, presentation.Label),
                    presentation.AccentRole);
            }

            return new CommandRibbonContextState(
                CommandRibbonContextKind.MixedStance,
                GameText.Format("stance.context.mixed", selectedUnitCount));
        }

        return new CommandRibbonContextState(
            CommandRibbonContextKind.IdleMoveMode,
            GameText.Format("command.ribbon.mode", MoveModeLabel(moveMode)));
    }

    public static string MoveModeLabel(MoveCommandMode mode)
    {
        return mode switch
        {
            MoveCommandMode.Attack => GameText.T("move.attack"),
            MoveCommandMode.Ignore => GameText.T("move.ignore"),
            _ => GameText.T("move.direct"),
        };
    }
}
