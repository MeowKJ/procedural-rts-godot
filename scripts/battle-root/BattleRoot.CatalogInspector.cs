using ProceduralRts.Core;

namespace ProceduralRts;

public partial class BattleRoot
{
    private CatalogInspectorState _catalogInspectorState = CatalogInspectorState.Empty;

    private void OnCatalogInspectorIntentRequested(CatalogInspectorIntent intent)
    {
        var next = CatalogInspectorReducer.Apply(_catalogInspectorState, intent);
        if (next == _catalogInspectorState)
        {
            return;
        }

        _catalogInspectorState = next;
        _hud.SetCatalogInspectorState(_catalogInspectorState);
    }
}
