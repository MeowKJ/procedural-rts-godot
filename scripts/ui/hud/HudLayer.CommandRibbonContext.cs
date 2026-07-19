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
        var projection = UnitStanceStripProjection.FromSelection(stance, selectedUnitCount);
        _selectedUnitStance = projection.SelectedStance;
        _selectedUnitCount = projection.SelectedUnitCount;
        _unitStanceStrip?.ApplyProjection(projection);
        RefreshCommandRibbonContext();
    }

    private void RefreshCommandRibbonContext()
    {
        if (_commandRibbonContextValue is null || !string.IsNullOrEmpty(_fixedHoverOwner))
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
        _commandRibbonContextValue.Text = HudLayoutMath.CommandRibbonSurfaceText(context.Text);
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
