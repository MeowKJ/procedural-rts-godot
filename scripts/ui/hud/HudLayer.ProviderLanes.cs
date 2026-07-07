using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private int? SelectedProductionProviderId(string? unitDesignId)
    {
        if (_selectedProductionProviderLaneScope == ProductionProviderLaneScope.Specific
            && _selectedProductionProviderId > 0)
        {
            return _selectedProductionProviderId;
        }

        if (_selectedProductionProviderLaneScope != ProductionProviderLaneScope.All
            || string.IsNullOrWhiteSpace(unitDesignId))
        {
            return null;
        }

        var spec = UnitDesignCatalog.Spec(unitDesignId);
        return spec.Production is null ? null : NextAllProductionProviderId(spec.Production.ProducerKind);
    }

    private int? NextAllProductionProviderId(string producerKind)
    {
        var matchingCount = 0;
        for (var index = 0; index < _productionProviderLaneStates.Count; index++)
        {
            if (IsAvailableSpecificProviderLane(_productionProviderLaneStates[index], producerKind))
            {
                matchingCount++;
            }
        }

        if (matchingCount == 0)
        {
            return null;
        }

        _allProductionProviderCursorByKind.TryGetValue(producerKind, out var cursor);
        var targetOrdinal = cursor % matchingCount;
        var ordinal = 0;
        for (var index = 0; index < _productionProviderLaneStates.Count; index++)
        {
            var state = _productionProviderLaneStates[index];
            if (!IsAvailableSpecificProviderLane(state, producerKind))
            {
                continue;
            }

            if (ordinal == targetOrdinal)
            {
                _allProductionProviderCursorByKind[producerKind] = targetOrdinal + 1;
                return state.ProducerId;
            }

            ordinal++;
        }

        return null;
    }

    private static bool IsAvailableSpecificProviderLane(ProductionProviderLaneState state, string producerKind)
    {
        return state.Scope == ProductionProviderLaneScope.Specific
            && state.Available
            && state.ProducerKind == producerKind;
    }

    private void SelectProductionProviderLane(ProductionProviderLaneState state)
    {
        _selectedProductionProviderLaneScope = state.Scope;
        _selectedProductionProviderId = state.Scope == ProductionProviderLaneScope.Specific ? state.ProducerId : 0;
        SetCatalogStatusText(GameText.Format(
            "ui.providerLane.selected",
            state.Label,
            state.ProviderCount,
            state.QueueCount));
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
    }

    private void RefreshProductionProviderLaneButtons()
    {
        var visibleIndex = 0;
        if (_selectedCatalogMode == CatalogModeKind.Train)
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
                button.Position = new Vector2(4, 52 + visibleIndex * 28);
                button.SetState(state, IsProductionProviderLaneSelected(state), IsProductionProviderLaneEnabled(state));
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
