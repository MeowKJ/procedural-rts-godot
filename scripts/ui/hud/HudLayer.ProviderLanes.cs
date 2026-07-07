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

    private void FocusRepeatProductionDesign(string? unitDesignId)
    {
        if (_selectedCatalogMode != CatalogModeKind.Train || string.IsNullOrWhiteSpace(unitDesignId))
        {
            return;
        }

        _focusedRepeatProductionDesignId = unitDesignId;
        RefreshRepeatProductionControl();
    }

    private void RequestFocusedProductionRepeat()
    {
        if (string.IsNullOrWhiteSpace(_focusedRepeatProductionDesignId)
            || CurrentProductionProviderLaneState() is not { Scope: ProductionProviderLaneScope.Specific, ProducerId: > 0 } state
            || !SpecificProviderSupportsFocusedRepeatDesign(state))
        {
            RefreshRepeatProductionControl();
            return;
        }

        ProductionRepeatRequested?.Invoke(_focusedRepeatProductionDesignId, state.ProducerId);
    }

    private void RefreshRepeatProductionControl()
    {
        if (_repeatProduction is null)
        {
            return;
        }

        var visible = _selectedCatalogMode == CatalogModeKind.Train;
        _repeatProduction.Visible = visible;
        if (!visible)
        {
            _repeatProduction.Disabled = true;
            _repeatProduction.ButtonPressed = false;
            return;
        }

        var hasDesign = !string.IsNullOrWhiteSpace(_focusedRepeatProductionDesignId);
        var laneState = CurrentProductionProviderLaneState();
        var hasSpecificProvider = laneState is { Scope: ProductionProviderLaneScope.Specific, ProducerId: > 0 };
        var providerSupportsDesign = hasSpecificProvider
            && SpecificProviderSupportsFocusedRepeatDesign(laneState!);
        var active = hasDesign
            && hasSpecificProvider
            && providerSupportsDesign
            && string.Equals(laneState!.RepeatOutputSpecId, _focusedRepeatProductionDesignId, StringComparison.Ordinal);
        var enabled = hasDesign
            && hasSpecificProvider
            && providerSupportsDesign
            && (active || laneState!.Available);

        _repeatProduction.Disabled = !enabled;
        _repeatProduction.ButtonPressed = active;
        _repeatProduction.Accent = active ? Mint : Cyan;
        _repeatProduction.TooltipText = RepeatProductionTooltip(laneState, hasDesign, hasSpecificProvider, providerSupportsDesign, active);
        _repeatProduction.QueueRedraw();
    }

    private string RepeatProductionTooltip(
        ProductionProviderLaneState? laneState,
        bool hasDesign,
        bool hasSpecificProvider,
        bool providerSupportsDesign,
        bool active)
    {
        if (!hasDesign)
        {
            return GameText.T("ui.repeat.needCard");
        }

        if (!hasSpecificProvider)
        {
            return GameText.T("ui.repeat.needSpecific");
        }

        if (!providerSupportsDesign)
        {
            return GameText.Format("production.needProducer", FocusedRepeatProductionProducerLabel(), FocusedRepeatProductionLabel());
        }

        if (!active && laneState is not null && !laneState.Available)
        {
            return LocalizedDisabledReason(laneState.DisabledReasonKey, 0);
        }

        return GameText.Format(
            active ? "ui.repeat.active" : "ui.repeat.available",
            FocusedRepeatProductionLabel(),
            laneState?.Label ?? GameText.T("ui.providerLane.specificFallback"));
    }

    private bool SpecificProviderSupportsFocusedRepeatDesign(ProductionProviderLaneState state)
    {
        if (string.IsNullOrWhiteSpace(_focusedRepeatProductionDesignId))
        {
            return false;
        }

        try
        {
            var spec = UnitDesignCatalog.Spec(_focusedRepeatProductionDesignId);
            return spec.Production is not null && spec.Production.ProducerKind == state.ProducerKind;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private string FocusedRepeatProductionLabel()
    {
        if (string.IsNullOrWhiteSpace(_focusedRepeatProductionDesignId))
        {
            return GameText.T("ui.catalog.train");
        }

        try
        {
            return UnitDesignCatalog.Spec(_focusedRepeatProductionDesignId).Label;
        }
        catch (InvalidOperationException)
        {
            return GameText.T("ui.catalog.train");
        }
    }

    private string FocusedRepeatProductionProducerLabel()
    {
        if (string.IsNullOrWhiteSpace(_focusedRepeatProductionDesignId))
        {
            return GameText.T("ui.providerLane.specificFallback");
        }

        try
        {
            var spec = UnitDesignCatalog.Spec(_focusedRepeatProductionDesignId);
            return spec.Production is null
                ? GameText.T("ui.providerLane.specificFallback")
                : BuildSpecCatalog.For(spec.Production.ProducerKind).Label;
        }
        catch (InvalidOperationException)
        {
            return GameText.T("ui.providerLane.specificFallback");
        }
    }

    private void ClearRepeatFocusIfHidden()
    {
        if (string.IsNullOrWhiteSpace(_focusedRepeatProductionDesignId))
        {
            return;
        }

        if (_selectedCatalogMode != CatalogModeKind.Train)
        {
            _focusedRepeatProductionDesignId = "";
            return;
        }

        for (var index = 0; index < _visibleCommandCardStates.Count; index++)
        {
            if (string.Equals(_visibleCommandCardStates[index].UnitDesignId, _focusedRepeatProductionDesignId, StringComparison.Ordinal))
            {
                return;
            }
        }

        _focusedRepeatProductionDesignId = "";
    }

    private int? SelectedConstructionProviderId()
    {
        return _selectedConstructionProviderLaneScope == ProductionProviderLaneScope.Specific
            && _selectedConstructionProviderId > 0
            ? _selectedConstructionProviderId
            : null;
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
        SetCatalogStatusText(GameText.Format(
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
        SetCatalogStatusText(GameText.Format(
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
                button.Position = new Vector2(4, 52 + visibleIndex * 28);
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
                button.Position = new Vector2(4, 52 + visibleIndex * 28);
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

    private void RefreshProductionProviderLaneSummary()
    {
        if (_providerLaneSummaryValue is null)
        {
            return;
        }

        if (_selectedCatalogMode is not (CatalogModeKind.Build or CatalogModeKind.Train))
        {
            _providerLaneSummaryValue.Visible = false;
            return;
        }

        var state = CurrentProviderLaneState();
        _providerLaneSummaryValue.Visible = true;
        _providerLaneSummaryValue.Text = state is null
            ? CurrentProviderLaneEmptyText()
            : ProviderLaneSummaryText(state);
        SetLabelColor(_providerLaneSummaryValue, state is null || !state.Available ? InkMuted : Ink);
    }

    private ProductionProviderLaneState? CurrentProviderLaneState()
    {
        return _selectedCatalogMode == CatalogModeKind.Build
            ? CurrentConstructionProviderLaneState()
            : CurrentProductionProviderLaneState();
    }

    private ProductionProviderLaneState? CurrentProductionProviderLaneState()
    {
        for (var index = 0; index < _productionProviderLaneStates.Count; index++)
        {
            var state = _productionProviderLaneStates[index];
            if (!ProviderLaneMatchesSelectedTrainCategory(state)
                || state.Scope != _selectedProductionProviderLaneScope)
            {
                continue;
            }

            if (state.Scope != ProductionProviderLaneScope.Specific
                || state.ProducerId == _selectedProductionProviderId)
            {
                return state;
            }
        }

        return null;
    }

    private ProductionProviderLaneState? CurrentConstructionProviderLaneState()
    {
        for (var index = 0; index < _constructionProviderLaneStates.Count; index++)
        {
            var state = _constructionProviderLaneStates[index];
            if (state.Scope != _selectedConstructionProviderLaneScope)
            {
                continue;
            }

            if (state.Scope != ProductionProviderLaneScope.Specific
                || state.ProducerId == _selectedConstructionProviderId)
            {
                return state;
            }
        }

        return null;
    }

    private string CurrentProviderLaneEmptyText()
    {
        return _selectedCatalogMode == CatalogModeKind.Build
            ? GameText.T("ui.constructionProviderLane.empty")
            : GameText.T("ui.providerLane.empty");
    }

    private static string ProviderLaneSummaryText(ProductionProviderLaneState state)
    {
        var availability = state.Available
            ? GameText.T("ui.providerLane.summaryOk")
            : ProviderLaneSummaryDisabledReason(state.DisabledReasonKey);
        var progress = state.ActiveProgress > 0 ? Mathf.RoundToInt(state.ActiveProgress * 100) : 0;
        return GameText.Format(
            "ui.providerLane.summary",
            state.ShortLabel,
            state.ProviderCount,
            state.QueueCount,
            progress,
            availability);
    }

    private static string ProviderLaneSummaryDisabledReason(string disabledReasonKey)
    {
        return disabledReasonKey switch
        {
            "ui.providerLane.offline" => GameText.T("ui.providerLane.summaryOffline"),
            "ui.providerLane.incomplete" => GameText.T("ui.providerLane.summaryIncomplete"),
            "ui.constructionProviderLane.offline" => GameText.T("ui.providerLane.summaryOffline"),
            "ui.constructionProviderLane.incomplete" => GameText.T("ui.providerLane.summaryIncomplete"),
            "ui.constructionProviderLane.none" => GameText.T("ui.providerLane.summaryNone"),
            "ui.producerUnavailable" => GameText.T("ui.providerLane.summaryNone"),
            _ => GameText.T("ui.providerLane.summaryLocked"),
        };
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
