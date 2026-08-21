using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private void SelectProviderLane(ProductionProviderLaneState state)
    {
        if (_selectedCatalogMode == CatalogModeKind.Build)
        {
            SelectConstructionProviderLane(state);
            return;
        }

        SelectProductionProviderLane(state);
    }

    private void SelectProductionProviderLane(ProductionProviderLaneState state)
    {
        _selectedProductionProviderLaneScope = state.Scope;
        _selectedProductionProviderId = state.Scope == ProductionProviderLaneScope.Specific ? state.ProducerId : 0;
        ResetCatalogInspectorContext(GameText.Format(
            "ui.providerLane.selected",
            state.Label,
            state.ProviderCount,
            state.QueueCount));
        RefreshProductionProviderLaneSummary();
        RefreshProductionProviderLaneButtons();
        RefreshRepeatProductionControl();
    }

    private void SelectConstructionProviderLane(ProductionProviderLaneState state)
    {
        _selectedConstructionProviderLaneScope = state.Scope;
        _selectedConstructionProviderId = state.Scope == ProductionProviderLaneScope.Specific ? state.ProducerId : 0;
        ResetCatalogInspectorContext(GameText.Format(
            "ui.constructionProviderLane.selected",
            state.Label,
            state.ProviderCount,
            state.QueueCount));
        RefreshProductionProviderLaneSummary();
        RefreshProductionProviderLaneButtons();
    }

    private void ValidateProductionProviderLaneSelection()
    {
        if (_selectedProductionProviderLaneScope != ProductionProviderLaneScope.Specific)
        {
            return;
        }

        for (var index = 0; index < _productionProviderLaneStates.Count; index++)
        {
            var state = _productionProviderLaneStates[index];
            if (state.Scope == ProductionProviderLaneScope.Specific
                && state.ProducerId == _selectedProductionProviderId
                && ProviderLaneMatchesSelectedTrainCategory(state))
            {
                return;
            }
        }

        _selectedProductionProviderLaneScope = ProductionProviderLaneScope.Auto;
        _selectedProductionProviderId = 0;
        ResetCatalogInspectorContext(DefaultCatalogInspectorText());
    }

    private void ValidateConstructionProviderLaneSelection()
    {
        if (_selectedConstructionProviderLaneScope != ProductionProviderLaneScope.Specific)
        {
            return;
        }

        for (var index = 0; index < _constructionProviderLaneStates.Count; index++)
        {
            var state = _constructionProviderLaneStates[index];
            if (state.Scope == ProductionProviderLaneScope.Specific
                && state.ProducerId == _selectedConstructionProviderId)
            {
                return;
            }
        }

        _selectedConstructionProviderLaneScope = ProductionProviderLaneScope.Auto;
        _selectedConstructionProviderId = 0;
        ResetCatalogInspectorContext(DefaultCatalogInspectorText());
    }

    private void RefreshProductionProviderLaneButtons()
    {
        var visibleIndex = 0;
        if (_selectedCatalogMode == CatalogModeKind.Build)
        {
            for (var index = 0; index < _constructionProviderLaneStates.Count; index++)
            {
                var state = _constructionProviderLaneStates[index];
                if (visibleIndex >= _productionProviderLaneButtons.Count)
                {
                    continue;
                }

                var button = _productionProviderLaneButtons[visibleIndex];
                button.Position = new Vector2(8, 52 + visibleIndex * 28);
                button.SetState(state, IsConstructionProviderLaneSelected(state), state.Available, constructionMode: true);
                button.Visible = true;
                visibleIndex++;
            }
        }
        else if (_selectedCatalogMode == CatalogModeKind.Train)
        {
            for (var index = 0; index < _productionProviderLaneStates.Count; index++)
            {
                var state = _productionProviderLaneStates[index];
                if (!ProviderLaneMatchesSelectedTrainCategory(state)
                    || visibleIndex >= _productionProviderLaneButtons.Count)
                {
                    continue;
                }

                var button = _productionProviderLaneButtons[visibleIndex];
                button.Position = new Vector2(8, 52 + visibleIndex * 28);
                button.SetState(state, IsProductionProviderLaneSelected(state), IsProductionProviderLaneEnabled(state), constructionMode: false);
                button.Visible = true;
                visibleIndex++;
            }
        }

        for (var index = visibleIndex; index < _productionProviderLaneButtons.Count; index++)
        {
            _productionProviderLaneButtons[index].Visible = false;
        }
    }

    private bool IsProductionProviderLaneSelected(ProductionProviderLaneState state)
    {
        return state.Scope == _selectedProductionProviderLaneScope
            && (state.Scope != ProductionProviderLaneScope.Specific || state.ProducerId == _selectedProductionProviderId);
    }

    private bool IsConstructionProviderLaneSelected(ProductionProviderLaneState state)
    {
        return state.Scope == _selectedConstructionProviderLaneScope
            && (state.Scope != ProductionProviderLaneScope.Specific || state.ProducerId == _selectedConstructionProviderId);
    }

    private bool IsProductionProviderLaneEnabled(ProductionProviderLaneState state)
    {
        return state.Available && HasVisibleTrainProviderForSelectedCategory(state);
    }

    private bool ProviderLaneMatchesSelectedTrainCategory(ProductionProviderLaneState state)
    {
        return state.Scope != ProductionProviderLaneScope.Specific
            || HasVisibleTrainProviderForSelectedCategory(state);
    }

    private bool HasVisibleTrainProviderForSelectedCategory(ProductionProviderLaneState state)
    {
        for (var index = 0; index < _commandCardStates.Count; index++)
        {
            var command = _commandCardStates[index];
            if (command.Category != _selectedProductionCategory)
            {
                continue;
            }

            if (state.Scope != ProductionProviderLaneScope.Specific
                || command.ProducerKind == state.ProducerKind)
            {
                if (command.HasProducer)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
