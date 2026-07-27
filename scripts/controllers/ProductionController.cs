using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class ProductionController : Node
{
    public Action<ProductionKind, int>? ProductionRequested { get; init; }
    public Action? CancelProductionRequested { get; init; }
    public Action<string>? StatusChanged { get; init; }
    public Action<string>? ProductionStatusChanged { get; init; }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        if (key.Keycode == Key.Delete)
        {
            CancelProductionRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        ProductionKind? productionKind = key.Keycode switch
        {
            Key.Q => ProductionKind.InfantrySquad,
            Key.E => ProductionKind.LightTank,
            Key.T => ProductionKind.Harvester,
            _ => null,
        };

        if (productionKind is null)
        {
            return;
        }

        ProductionRequested?.Invoke(productionKind.Value, key.ShiftPressed ? BattleRoot.ShiftProductionBatchCount : 1);
        GetViewport().SetInputAsHandled();
    }
}
