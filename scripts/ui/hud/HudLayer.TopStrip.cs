using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private const float ResourceStripWidth = HudLayoutMath.ResourceStripWidth;

    private Label _creditsValue = null!;
    private Label _armyReadinessValue = null!;
    private Label _statusValue = null!;

    public void SetArmyReadiness(int selectedArmyUnits, int totalArmyUnits, string readiness)
    {
        _armyReadinessValue.Text = CompactText(
            GameText.Format("ui.top.armyReadiness", selectedArmyUnits, totalArmyUnits, readiness),
            18);
        SetLabelColor(_armyReadinessValue, selectedArmyUnits > 0 ? Mint : InkMuted);
    }
}
