using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer
{
    private void RefreshCommandCards()
    {
        _visibleBuildCardStates.Clear();
        _visibleCommandCardStates.Clear();
        if (_selectedCatalogMode == CatalogModeKind.Build)
        {
            for (var index = 0; index < _buildCardStates.Count; index++)
            {
                var state = _buildCardStates[index];
                if (state.Category != _selectedBuildCategory)
                {
                    continue;
                }

                if (_visibleBuildCardStates.Count >= 12)
                {
                    break;
                }

                _visibleBuildCardStates.Add(state);
            }
        }
        else if (_selectedCatalogMode == CatalogModeKind.Train)
        {
            for (var index = 0; index < _commandCardStates.Count; index++)
            {
                var state = _commandCardStates[index];
                if (state.Category != _selectedProductionCategory)
                {
                    continue;
                }

                if (_visibleCommandCardStates.Count >= 12)
                {
                    break;
                }

                _visibleCommandCardStates.Add(state);
            }
        }

        _commandCardActiveIds.Clear();
        _commandCardStaleIds.Clear();
        if (_selectedCatalogMode == CatalogModeKind.Build)
        {
            for (var index = 0; index < _visibleBuildCardStates.Count; index++)
            {
                _commandCardActiveIds.Add(BuildOptionId(_visibleBuildCardStates[index]));
            }
        }
        else if (_selectedCatalogMode == CatalogModeKind.Train)
        {
            for (var index = 0; index < _visibleCommandCardStates.Count; index++)
            {
                _commandCardActiveIds.Add(ProductionOptionId(_visibleCommandCardStates[index]));
            }
        }

        foreach (var key in _commandButtons.Keys)
        {
            if (!_commandCardActiveIds.Contains(key))
            {
                _commandCardStaleIds.Add(key);
            }
        }

        foreach (var stale in _commandCardStaleIds)
        {
            InvalidateCatalogInspectorItem(CommandCardInspectorItemId(stale));
            _commandButtons[stale].QueueFree();
            _commandButtons.Remove(stale);
        }

        ClearRepeatFocusIfHidden();

        if (_selectedCatalogMode == CatalogModeKind.Build)
        {
            ClearUpgradeProjectCards();
            ClearAbilityCards();
            RefreshProductionProviderLaneSummary();
            RefreshProductionProviderLaneButtons();
            RefreshBuildCards();
            return;
        }

        if (_selectedCatalogMode == CatalogModeKind.Train)
        {
            ClearUpgradeProjectCards();
            ClearAbilityCards();
            RefreshProductionProviderLaneSummary();
            RefreshProductionProviderLaneButtons();
            RefreshProductionCards();
            return;
        }

        RefreshProductionProviderLaneSummary();
        RefreshProductionProviderLaneButtons();
        if (_selectedCatalogMode == CatalogModeKind.Upgrades)
        {
            ClearAbilityCards();
            RefreshRepeatProductionControl();
            RefreshUpgradeProjectCards();
            return;
        }

        ClearUpgradeProjectCards();
        RefreshAbilityCards();
    }

    private string LastProductionCatalogStatusText()
    {
        return string.IsNullOrWhiteSpace(_lastProductionStatus)
            ? GameText.T("ui.status.ready")
            : CommandFailurePresentation.PanelText(_lastProductionStatus);
    }

    private void RefreshBuildCards()
    {
        for (var index = 0; index < _visibleBuildCardStates.Count; index++)
        {
            var state = _visibleBuildCardStates[index];
            var optionId = BuildOptionId(state);
            if (!_commandButtons.TryGetValue(optionId, out var button))
            {
                button = AddCommandButton(_rightProductionPanel, optionId);
            }

            button.Hotkey = ProductionHotkey(index);
            button.Position = ProductionButtonPosition(index);

            var disabledReason = LocalizedDisabledReason(state.DisabledReasonKey, state.Cost);
            button.SetBuildState(state, disabledReason);
            RefreshCatalogInspectorItem(CommandCardInspectorItemId(optionId), button.InspectorText);
        }
    }

    private void RefreshProductionCards()
    {
        for (var index = 0; index < _visibleCommandCardStates.Count; index++)
        {
            var state = _visibleCommandCardStates[index];
            var optionId = ProductionOptionId(state);
            if (!_commandButtons.TryGetValue(optionId, out var button))
            {
                button = AddCommandButton(_rightProductionPanel, optionId);
            }

            button.Hotkey = ProductionHotkey(index);
            button.Position = ProductionButtonPosition(index);
            button.UnitDesignId = state.UnitDesignId;

            var disabledReason = LocalizedDisabledReason(state.DisabledReasonKey, state.Cost);
            button.SetState(state, disabledReason);
            RefreshCatalogInspectorItem(CommandCardInspectorItemId(optionId), button.InspectorText);
        }
    }

    private static string LocalizedDisabledReason(string disabledReasonKey, int cost)
    {
        return disabledReasonKey == "ui.needCredits"
            ? GameText.Format("ui.needCredits", cost)
            : string.IsNullOrWhiteSpace(disabledReasonKey) ? "" : GameText.T(disabledReasonKey);
    }
}
