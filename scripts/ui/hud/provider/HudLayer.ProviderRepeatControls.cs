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
            statusText: hasDesign ? RepeatProductionStateText(laneState, hasDesign, hasSpecificProvider, providerSupportsDesign, active) : "",
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
        _repeatProductionStateValue.Visible = visible && !string.IsNullOrWhiteSpace(statusText);
        _repeatProductionStateValue.Text = statusText;
        SetLabelColor(_repeatProductionStateValue, statusColor);
        _repeatProduction.FixedHoverText = tooltip;
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
}
