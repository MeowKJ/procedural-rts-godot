using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private readonly Dictionary<AbilityKind, AbilityCard> _abilityCards = [];
    private readonly List<AbilityCardState> _abilityCardStates = [];
    private readonly HashSet<AbilityKind> _abilityCardActiveKinds = [];
    private readonly List<AbilityKind> _abilityCardStaleKinds = [];
    private int _abilitySourceUnitCount;

    public void SetAbilityCardState(IReadOnlyList<AbilityCardState> states)
    {
        SetAbilityCardState(states, 0);
    }

    public void SetAbilityCardState(IReadOnlyList<AbilityCardState> states, int sourceUnitCount)
    {
        _abilityCardStates.Clear();
        for (var index = 0; index < states.Count; index++)
        {
            _abilityCardStates.Add(states[index]);
        }

        _abilitySourceUnitCount = Math.Max(0, sourceUnitCount);
        RefreshCommandCards();
    }

    private void ClearAbilityCards()
    {
        if (_abilityCards.Count == 0)
        {
            return;
        }

        _abilityCardStaleKinds.Clear();
        foreach (var key in _abilityCards.Keys)
        {
            _abilityCardStaleKinds.Add(key);
        }

        foreach (var stale in _abilityCardStaleKinds)
        {
            _abilityCards[stale].QueueFree();
            _abilityCards.Remove(stale);
        }
    }

    private void RefreshAbilityCards()
    {
        _abilityCardActiveKinds.Clear();
        _abilityCardStaleKinds.Clear();
        var visibleCount = Math.Min(_abilityCardStates.Count, 12);
        for (var index = 0; index < visibleCount; index++)
        {
            var state = _abilityCardStates[index];
            _abilityCardActiveKinds.Add(state.Ability.Kind);
            if (!_abilityCards.TryGetValue(state.Ability.Kind, out var card))
            {
                card = AddAbilityCard(_rightProductionPanel, state.Ability.Kind);
            }

            card.Position = ProductionButtonPosition(index);
            card.SetState(state);
        }

        foreach (var key in _abilityCards.Keys)
        {
            if (!_abilityCardActiveKinds.Contains(key))
            {
                _abilityCardStaleKinds.Add(key);
            }
        }

        foreach (var stale in _abilityCardStaleKinds)
        {
            _abilityCards[stale].QueueFree();
            _abilityCards.Remove(stale);
        }

        SetCatalogStatusText(AbilityCatalogSourceContextText(visibleCount));
    }

    private string AbilityCatalogSourceContextText(int visibleCount)
    {
        if (visibleCount == 0 || _abilitySourceUnitCount == 0)
        {
            return GameText.T("ui.catalog.abilitiesSourceNone");
        }

        return _abilitySourceUnitCount == 1
            ? GameText.Format("ui.catalog.abilitiesSourceSelected", visibleCount)
            : GameText.Format("ui.catalog.abilitiesSourceMixed", _abilitySourceUnitCount, visibleCount);
    }

    private string AbilityRailSourceContextText()
    {
        var visibleCount = Math.Min(_abilityCardStates.Count, 12);
        if (visibleCount == 0 || _abilitySourceUnitCount == 0)
        {
            return GameText.T("ui.providerLane.abilitiesSourceNone");
        }

        return _abilitySourceUnitCount == 1
            ? GameText.Format("ui.providerLane.abilitiesSourceSelected", visibleCount)
            : GameText.Format("ui.providerLane.abilitiesSourceMixed", _abilitySourceUnitCount);
    }

    private AbilityCard AddAbilityCard(Control parent, AbilityKind kind)
    {
        var card = new AbilityCard
        {
            Name = $"AbilityCard{kind}",
            Kind = kind,
            CustomMinimumSize = new Vector2(82, 58),
            FocusMode = Control.FocusModeEnum.Click,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        card.Size = card.CustomMinimumSize;
        UiFactory.ApplyHudCommandButtonTheme(card, CurrentPalette, FontBody);
        _abilityCards[kind] = card;
        parent.AddChild(card);
        card.MouseEntered += () => SetCatalogStatusText(card.InspectorText);
        card.MouseExited += RestoreCatalogStatusText;
        card.FocusEntered += () => SetCatalogStatusText(card.InspectorText);
        card.FocusExited += RestoreCatalogStatusText;
        card.Pressed += () =>
        {
            SetCatalogStatusText(card.InspectorText);
            AbilityRequested?.Invoke(kind);
        };
        return card;
    }
}
