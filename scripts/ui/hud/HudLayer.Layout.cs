using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private void LayoutDynamicHud(Vector2 viewport)
    {
        LayoutSlidingRightPanel(_rightProductionPanel, _productionDrawerProgress, avoidRail: true);
        LayoutSlidingRightPanel(_rightDetailPanel, _detailDrawerProgress, avoidRail: false);

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
            _commandRibbon.Visible = true;
        }
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
