using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private void LayoutDynamicHud(Vector2 viewport)
    {
        LayoutProductionDrawerDensity();
        LayoutSlidingRightPanel(_rightProductionPanel, _productionDrawerProgress, avoidRail: true);
        LayoutSlidingRightPanel(_rightDetailPanel, _detailDrawerProgress, avoidRail: true);

        if (_rightRail is not null)
        {
            _rightRail.OffsetLeft = -RailWidth;
            _rightRail.OffsetTop = HudLayoutMath.ProductionPanelTop;
            _rightRail.OffsetRight = 0;
            _rightRail.OffsetBottom = -12;
            _rightRail.Modulate = Colors.White;
            _rightRail.Visible = true;
        }

        if (_commandRibbon is not null)
        {
            var commandWidth = HudLayoutMath.CommandRibbonWidth(Mathf.RoundToInt(viewport.X));
            var commandAreaWidth = viewport.X - HudLayoutMath.RightColumnWidth - RailWidth;
            var commandLeft = Mathf.Max(12, (commandAreaWidth - commandWidth) * 0.5f);
            _commandRibbon.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
            _commandRibbon.OffsetLeft = commandLeft;
            _commandRibbon.OffsetTop = -68;
            _commandRibbon.OffsetRight = commandLeft + commandWidth;
            _commandRibbon.OffsetBottom = -12;
            _commandRibbon.Visible = true;
        }
    }

    private void LayoutProductionDrawerDensity()
    {
        if (_rightProductionPanel is null)
        {
            return;
        }

        var visibleCardCount = _selectedCatalogMode switch
        {
            CatalogModeKind.Build => _visibleBuildCardStates.Count,
            CatalogModeKind.Train => _visibleCommandCardStates.Count,
            CatalogModeKind.Upgrades => _upgradeProjectCards.Count,
            CatalogModeKind.Abilities => _abilityCards.Count,
            _ => 0,
        };
        var footerTop = HudLayoutMath.ProductionDrawerFooterTop(visibleCardCount);
        _rightProductionPanel.OffsetBottom = HudLayoutMath.ProductionPanelTop + HudLayoutMath.ProductionDrawerHeight(visibleCardCount);
        _productionFooterDivider.Position = new Vector2(8, footerTop - 4);
        _repeatProductionStateValue.Position = new Vector2(204, footerTop - 20);
        _repeatProduction.Position = new Vector2(248, footerTop);
        _queueValue.Position = new Vector2(14, footerTop + 8);
    }

    private void LayoutSlidingRightPanel(Panel? panel, float progress, bool avoidRail)
    {
        if (panel is null)
        {
            return;
        }

        var openLeft = avoidRail ? -RailWidth - DrawerWidth - 12f : -312f;
        var closedLeft = RailWidth;
        var left = Mathf.Lerp(closedLeft, openLeft, progress);
        panel.OffsetLeft = left;
        panel.OffsetRight = left + DrawerWidth;
        panel.Modulate = new Color(1, 1, 1, Mathf.Clamp(progress, 0, 1));
        panel.Visible = progress > 0.02f;
    }
}
