using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer
{
    public void SetCatalogInspectorState(CatalogInspectorState state)
    {
        if (_productionValue is not null)
        {
            _productionValue.Text = CompactMultiline(state.Current.Text, 34);
        }
    }

    private void InitializeCatalogInspector()
    {
        ResetCatalogInspectorContext(DefaultCatalogInspectorText());
    }

    private void ResetCatalogInspectorContext(string text)
    {
        RequestCatalogInspector(CatalogInspectorIntent.Reset(CatalogInspectorContextId(), text));
    }

    private void SetCatalogInspectorDefault(string text)
    {
        RequestCatalogInspector(CatalogInspectorIntent.Default(CatalogInspectorContextId(), text));
    }

    private void ShowCatalogInspectorHover(string itemId, string text)
    {
        RequestCatalogInspector(CatalogInspectorIntent.Hover(itemId, text));
    }

    private void ClearCatalogInspectorHover(string itemId)
    {
        RequestCatalogInspector(CatalogInspectorIntent.ClearHover(itemId));
    }

    private void ShowCatalogInspectorCommandFeedback(string text)
    {
        RequestCatalogInspector(CatalogInspectorIntent.CommandFeedback(text));
    }

    private void ClearCatalogInspectorCommandFeedback()
    {
        RequestCatalogInspector(CatalogInspectorIntent.ClearCommandFeedback());
    }

    private void InvalidateCatalogInspectorItem(string itemId)
    {
        RequestCatalogInspector(CatalogInspectorIntent.Invalidate(itemId));
    }

    private void RequestCatalogInspector(CatalogInspectorIntent intent)
    {
        CatalogInspectorIntentRequested?.Invoke(intent);
    }

    private string DefaultCatalogInspectorText()
    {
        return _selectedCatalogMode switch
        {
            CatalogModeKind.Upgrades => UpgradeProjectCatalogStatusText(),
            CatalogModeKind.Abilities => _abilityCardStates.Count == 0
                ? GameText.T("ui.catalog.abilitiesEmpty")
                : GameText.Format("ui.catalog.abilitiesCount", Math.Min(_abilityCardStates.Count, 12)),
            _ => LastProductionCatalogStatusText(),
        };
    }

    private string CatalogInspectorContextId()
    {
        return _selectedCatalogMode switch
        {
            CatalogModeKind.Build => $"catalog:build:{_selectedBuildCategory}:{_selectedConstructionProviderLaneScope}:{_selectedConstructionProviderId}",
            CatalogModeKind.Train => $"catalog:train:{_selectedProductionCategory}:{_selectedProductionProviderLaneScope}:{_selectedProductionProviderId}",
            CatalogModeKind.Upgrades => "catalog:upgrades",
            CatalogModeKind.Abilities => "catalog:abilities",
            _ => "catalog:none",
        };
    }

    private static string CatalogModeInspectorItemId(CatalogModeKind mode) => $"catalog-mode:{mode}";

    private static string CommandCardInspectorItemId(string optionId) => $"catalog-card:{optionId}";

    private static string AbilityInspectorItemId(AbilityKind kind) => $"ability-card:{kind}";

    private static string UpgradeInspectorItemId(string id) => $"upgrade-card:{id}";
}
