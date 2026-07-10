using ProceduralRts.Core;

namespace ProceduralRts;

public partial class BattleRoot
{
    private CatalogInspectorState _catalogInspectorState = CatalogInspectorState.Empty;

    private void OnCatalogInspectorIntentRequested(CatalogInspectorIntent intent)
    {
        _catalogInspectorState = CatalogInspectorReducer.Apply(_catalogInspectorState, intent);
        _hud.SetCatalogInspectorState(_catalogInspectorState);
    }
}
