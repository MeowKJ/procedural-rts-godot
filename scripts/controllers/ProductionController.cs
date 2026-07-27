using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class ProductionController : Node
{
    public required UnitFactionId LocalFaction { get; init; }
    public Action<string, int>? ProductionDesignRequested { get; init; }
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

        ProductionCategory? category = key.Keycode switch
        {
            Key.Q => ProductionCategory.Infantry,
            Key.E => ProductionCategory.Vehicle,
            Key.T => ProductionCategory.Economy,
            _ => null,
        };

        if (category is null
            || UnitDesignFactionRosterCatalog.PreferredProductionDesignId(LocalFaction, category.Value) is not { } designId)
        {
            return;
        }

        ProductionDesignRequested?.Invoke(designId, key.ShiftPressed ? BattleRoot.ShiftProductionBatchCount : 1);
        GetViewport().SetInputAsHandled();
    }
}
