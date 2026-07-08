using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private IconActionButton _sellOrCancelAction = null!;
    private string _lastSellOrCancelRefreshKey = "";

    private void RefreshSellOrCancelAction()
    {
        if (_sellOrCancelAction is null)
        {
            return;
        }

        var mode = _hasBuildingSelection
            ? "sell"
            : _lastCanCancelProduction ? "cancel" : "none";
        if (string.Equals(_lastSellOrCancelRefreshKey, mode, StringComparison.Ordinal))
        {
            return;
        }

        var (disabled, accent, tooltip) = mode switch
        {
            "sell" => (false, Danger, GameText.T("ui.sellOrCancel.sellTooltip")),
            "cancel" => (false, Amber, GameText.T("ui.sellOrCancel.cancelTooltip")),
            _ => (true, InkMuted, GameText.T("ui.sellOrCancel.noneTooltip")),
        };

        _sellOrCancelAction.Disabled = disabled;
        _sellOrCancelAction.Accent = accent;
        _sellOrCancelAction.TooltipText = tooltip;
        UiFactory.ApplyHudActionButtonTheme(_sellOrCancelAction, CurrentPalette, accent, FontTiny);
        _sellOrCancelAction.QueueRedraw();
        _lastSellOrCancelRefreshKey = mode;
    }
}
