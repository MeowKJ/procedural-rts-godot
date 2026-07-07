using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer
{
    private BattleCursorState? _activeCursorState;

    private void ApplyCommandCursor(CommandPreviewState preview)
    {
        var state = BattleCursorCatalog.StateForPreview(preview);
        if (state == _activeCursorState)
        {
            return;
        }

        _activeCursorState = state;
        var definition = BattleCursorCatalog.DefinitionFor(state);
        Input.SetDefaultCursorShape(BattleCursorGodotShapes.ToInputShape(definition.Shape));
    }
}
