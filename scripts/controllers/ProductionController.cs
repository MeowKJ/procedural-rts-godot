using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class ProductionController : Node
{
    public required GameState State { get; init; }
    public Action<ProductionKind>? ProductionRequested { get; init; }
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
            if (CancelProductionRequested is not null)
            {
                CancelProductionRequested.Invoke();
                GetViewport().SetInputAsHandled();
                return;
            }

            State.CancelFirstProduction(ProceduralRts.Core.Owner.Player, out var cancelStatus);
            StatusChanged?.Invoke(cancelStatus);
            ProductionStatusChanged?.Invoke(cancelStatus);
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

        if (ProductionRequested is not null)
        {
            ProductionRequested.Invoke(productionKind.Value);
            GetViewport().SetInputAsHandled();
            return;
        }

        State.EnqueueProduction(productionKind.Value, ProceduralRts.Core.Owner.Player, out var status);
        StatusChanged?.Invoke(status);
        ProductionStatusChanged?.Invoke(status);
        GetViewport().SetInputAsHandled();
    }
}
