using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer
{
    public void SetMoveCommandMode(MoveCommandMode mode)
    {
        _selectedMoveMode = mode;
        foreach (var button in _moveModeButtons)
        {
            button.SetSelected(button.Mode == mode);
        }
        RefreshCommandRibbonContext();
    }

    public void SetSelectedUnitStance(UnitStance? stance, int selectedUnitCount)
    {
        _selectedUnitStance = stance;
        _selectedUnitCount = Math.Max(0, selectedUnitCount);
        foreach (var button in _stanceModeButtons)
        {
            button.SetSelected(stance is not null && button.Stance == stance.Value);
        }
        RefreshCommandRibbonContext();
    }

    private void RefreshCommandRibbonContext()
    {
        if (_commandRibbonContextValue is null)
        {
            return;
        }

        var preview = _commandPreview?.Preview ?? CommandPreviewState.None;
        var context = CommandRibbonContextResolver.Resolve(
            preview.Phase,
            preview.Label,
            preview.IsValid,
            _selectedUnitStance,
            _selectedUnitCount,
            _selectedMoveMode);
        _commandRibbonContextValue.Text = CompactText(context.Text, 28);
        var color = context.Kind switch
        {
            CommandRibbonContextKind.ActiveCommand => Cyan,
            CommandRibbonContextKind.BlockedCommand => Danger,
            CommandRibbonContextKind.MixedStance => Amber,
            CommandRibbonContextKind.UniformStance when context.StanceAccentRole is { } role => UiFactory.HudStanceAccent(role, CurrentPalette),
            _ => InkMuted,
        };
        SetLabelColor(_commandRibbonContextValue, color);
    }
}
