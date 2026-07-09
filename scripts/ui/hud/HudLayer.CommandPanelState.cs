using Godot;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    public BattleCommandPanelState CommandPanelState { get; private set; } = BattleCommandPanelState.Empty;
}
