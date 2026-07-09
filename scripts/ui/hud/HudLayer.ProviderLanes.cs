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

        if (string.Equals(_focusedRepeatProductionDesignId, unitDesignId, StringComparison.Ordinal))
        {
            return;
        }

        _focusedRepeatProductionDesignId = unitDesignId;
        _focusedRepeatProductionLabel = "";
        _focusedRepeatProductionProducerKind = "";
        _focusedRepeatProductionProducerLabel = "";
        try
        {
            var spec = UnitDesignCatalog.Spec(unitDesignId);
            _focusedRepeatProductionLabel = spec.Label;
            if (spec.Production is not null)
            {
                _focusedRepeatProductionProducerKind = spec.Production.ProducerKind;
                _focusedRepeatProductionProducerLabel = BuildSpecCatalog.For(spec.Production.ProducerKind).Label;
            }
        }
        catch (InvalidOperationException)
        {
            _focusedRepeatProductionLabel = GameText.T("ui.catalog.train");
            _focusedRepeatProductionProducerLabel = GameText.T("ui.providerLane.specificFallback");
        }

        _lastRepeatProductionRefreshKey = "";
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
        if (!visible)
        {
            if (_repeatProductionStateCached
                && string.Equals(_lastRepeatProductionRefreshKey, "hidden", StringComparison.Ordinal))
            {
                return;
            }

            ApplyRepeatProductionControlState(
                visible: false,
                disabled: true,
                active: false,
                accent: Cyan,
                statusText: "",
                statusColor: InkMuted,
                tooltip: GameText.T("ui.repeat.needCard"),
                refreshKey: "hidden");
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
        var refreshKey = string.Join(
            "|",
            _focusedRepeatProductionDesignId,
            _focusedRepeatProductionProducerKind,
            laneState?.Scope.ToString() ?? "",
            laneState?.ProducerId.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
            laneState?.ProducerKind ?? "",
            laneState?.RepeatOutputSpecId ?? "",
            laneState?.Available.ToString() ?? "",
            laneState?.DisabledReasonKey ?? "",
            laneState?.Label ?? "");
        if (_repeatProductionStateCached
            && string.Equals(_lastRepeatProductionRefreshKey, refreshKey, StringComparison.Ordinal))
        {
            return;
        }

        ApplyRepeatProductionControlState(
            visible: true,
            disabled: !enabled,
            active: active,
            accent: active ? Mint : Cyan,
            statusText: RepeatProductionStateText(laneState, hasDesign, hasSpecificProvider, providerSupportsDesign, active),
            statusColor: active ? Mint : enabled ? Cyan : InkMuted,
            tooltip: RepeatProductionTooltip(laneState, hasDesign, hasSpecificProvider, providerSupportsDesign, active),
            refreshKey: refreshKey);
    }

    private void ApplyRepeatProductionControlState(bool visible, bool disabled, bool active, Color accent, string statusText, Color statusColor, string tooltip, string refreshKey)
    {
        _repeatProduction.Visible = visible;
        _repeatProduction.Disabled = disabled;
        _repeatProduction.ButtonPressed = active;
        _repeatProduction.Accent = accent;
        _repeatProductionStateValue.Visible = visible;
        _repeatProductionStateValue.Text = statusText;
        SetLabelColor(_repeatProductionStateValue, statusColor);
        _repeatProduction.TooltipText = tooltip;
        _lastRepeatProductionRefreshKey = refreshKey;
        _repeatProductionStateCached = true;
        _repeatProduction.QueueRedraw();
    }

    private static string RepeatProductionStateText(
        ProductionProviderLaneState? laneState,
        bool hasDesign,
        bool hasSpecificProvider,
        bool providerSupportsDesign, bool active) =>
        !hasDesign ? GameText.T("ui.repeat.state.needCard") :
        !hasSpecificProvider ? GameText.T("ui.repeat.state.needLane") :
        !providerSupportsDesign ? GameText.T("ui.repeat.state.noProvider") :
        active ? GameText.T("ui.repeat.state.active") :
        laneState is { Available: true } ? GameText.T("ui.repeat.state.available") : GameText.T("ui.repeat.state.blocked");

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
        if (string.IsNullOrWhiteSpace(_focusedRepeatProductionDesignId)
            || string.IsNullOrWhiteSpace(_focusedRepeatProductionProducerKind))
        {
            return false;
        }

        return string.Equals(_focusedRepeatProductionProducerKind, state.ProducerKind, StringComparison.Ordinal);
    }

    private string FocusedRepeatProductionLabel()
    {
        if (string.IsNullOrWhiteSpace(_focusedRepeatProductionLabel))
        {
            return GameText.T("ui.catalog.train");
        }

        return _focusedRepeatProductionLabel;
    }

    private string FocusedRepeatProductionProducerLabel()
    {
        if (string.IsNullOrWhiteSpace(_focusedRepeatProductionProducerLabel))
        {
            return GameText.T("ui.providerLane.specificFallback");
        }

        return _focusedRepeatProductionProducerLabel;
    }

    private void ClearRepeatFocusIfHidden()
    {
        if (string.IsNullOrWhiteSpace(_focusedRepeatProductionDesignId))
        {
            return;
        }

        if (_selectedCatalogMode != CatalogModeKind.Train)
        {
            ClearFocusedRepeatProductionDesign();
            return;
        }

        for (var index = 0; index < _visibleCommandCardStates.Count; index++)
        {
            if (string.Equals(_visibleCommandCardStates[index].UnitDesignId, _focusedRepeatProductionDesignId, StringComparison.Ordinal))
            {
                return;
            }
        }

        ClearFocusedRepeatProductionDesign();
    }

    private void ClearFocusedRepeatProductionDesign()
    {
        _focusedRepeatProductionDesignId = "";
        _focusedRepeatProductionLabel = "";
        _focusedRepeatProductionProducerKind = "";
        _focusedRepeatProductionProducerLabel = "";
        _lastRepeatProductionRefreshKey = "";
    }

    private int? SelectedConstructionProviderId(string? buildKind)
    {
        if (_selectedConstructionProviderLaneScope == ProductionProviderLaneScope.Specific
            && _selectedConstructionProviderId > 0)
        {
            return _selectedConstructionProviderId;
        }

        if (_selectedConstructionProviderLaneScope != ProductionProviderLaneScope.All
            || string.IsNullOrWhiteSpace(buildKind))
        {
            return null;
        }

        var requiredProducer = BuildSpecCatalog.For(buildKind).RequiredProducer;
        return string.IsNullOrWhiteSpace(requiredProducer)
            ? null
            : NextAllConstructionProviderId(requiredProducer);
    }

    private int? NextAllConstructionProviderId(string providerKind)
    {
        var matchingCount = 0;
        for (var index = 0; index < _constructionProviderLaneStates.Count; index++)
        {
            if (IsAvailableSpecificProviderLane(_constructionProviderLaneStates[index], providerKind))
            {
                matchingCount++;
            }
        }

        if (matchingCount == 0)
        {
            return null;
        }

        _allConstructionProviderCursorByKind.TryGetValue(providerKind, out var cursor);
        var targetOrdinal = cursor % matchingCount;
        var ordinal = 0;
        for (var index = 0; index < _constructionProviderLaneStates.Count; index++)
        {
            var state = _constructionProviderLaneStates[index];
            if (!IsAvailableSpecificProviderLane(state, providerKind))
            {
                continue;
            }

            if (ordinal == targetOrdinal)
            {
                _allConstructionProviderCursorByKind[providerKind] = targetOrdinal + 1;
                return state.ProducerId;
            }

            ordinal++;
        }

        return null;
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
        RefreshCatalogOverview();
        RefreshRepeatProductionControl();
        RefreshCommandFeedbackRail();
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
        RefreshCommandFeedbackRail();
        RefreshProductionProviderLaneSummary();
        RefreshProductionProviderLaneButtons();
        RefreshCatalogOverview();
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
