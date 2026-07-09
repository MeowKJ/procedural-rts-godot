using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private static readonly UpgradeProjectCardState[] DefaultUpgradeProjectShellStates =
    [
        new(
            "focusedMunitions",
            IconGlyph.Ability,
            UpgradeProjectAccentKind.Combat,
            "ui.upgrade.project.focusedMunitions",
            "ui.upgrade.short.focusedMunitions",
            "ui.upgrade.target.combat",
            "ui.upgrade.source.researchBuilding",
            "ui.upgrade.effect.focusedMunitions",
            "ui.upgrade.status.sourceNeeded",
            "ui.upgrade.badge.sourceNeeded",
            450,
            40),
        new(
            "opticArray",
            IconGlyph.Scan,
            UpgradeProjectAccentKind.Vision,
            "ui.upgrade.project.opticArray",
            "ui.upgrade.short.opticArray",
            "ui.upgrade.target.vision",
            "ui.upgrade.source.researchBuilding",
            "ui.upgrade.effect.opticArray",
            "ui.upgrade.status.campaignGate",
            "ui.upgrade.badge.campaignGate",
            320,
            35),
        new(
            "fieldRepairs",
            IconGlyph.Repair,
            UpgradeProjectAccentKind.Support,
            "ui.upgrade.project.fieldRepairs",
            "ui.upgrade.short.fieldRepairs",
            "ui.upgrade.target.support",
            "ui.upgrade.source.researchBuilding",
            "ui.upgrade.effect.fieldRepairs",
            "ui.upgrade.status.sourceNeeded",
            "ui.upgrade.badge.sourceNeeded",
            380,
            45),
    ];

    private readonly Dictionary<string, UpgradeProjectCard> _upgradeProjectCards = [];
    private readonly HashSet<string> _upgradeProjectCardActiveIds = [];
    private readonly List<string> _upgradeProjectCardStaleIds = [];

    private void ClearUpgradeProjectCards()
    {
        if (_upgradeProjectCards.Count == 0)
        {
            return;
        }

        _upgradeProjectCardStaleIds.Clear();
        foreach (var key in _upgradeProjectCards.Keys)
        {
            _upgradeProjectCardStaleIds.Add(key);
        }

        foreach (var stale in _upgradeProjectCardStaleIds)
        {
            _upgradeProjectCards[stale].QueueFree();
            _upgradeProjectCards.Remove(stale);
        }
    }

    private void RefreshUpgradeProjectCards()
    {
        _upgradeProjectCardActiveIds.Clear();
        _upgradeProjectCardStaleIds.Clear();
        var visibleIndex = 0;
        for (var index = 0; index < DefaultUpgradeProjectShellStates.Length && visibleIndex < 12; index++)
        {
            var state = DefaultUpgradeProjectShellStates[index];
            if (state.Accent != _selectedUpgradeCategory)
            {
                continue;
            }

            _upgradeProjectCardActiveIds.Add(state.Id);
            if (!_upgradeProjectCards.TryGetValue(state.Id, out var card))
            {
                card = AddUpgradeProjectCard(_rightProductionPanel, state.Id);
            }

            card.Position = ProductionButtonPosition(visibleIndex);
            card.SetState(state);
            visibleIndex++;
        }

        foreach (var key in _upgradeProjectCards.Keys)
        {
            if (!_upgradeProjectCardActiveIds.Contains(key))
            {
                _upgradeProjectCardStaleIds.Add(key);
            }
        }

        foreach (var stale in _upgradeProjectCardStaleIds)
        {
            _upgradeProjectCards[stale].QueueFree();
            _upgradeProjectCards.Remove(stale);
        }

        SetCatalogStatusText(UpgradeProjectCatalogStatusText());
    }

    private UpgradeProjectCard AddUpgradeProjectCard(Control parent, string id)
    {
        var card = new UpgradeProjectCard
        {
            Name = $"UpgradeProjectCard{id}",
            CustomMinimumSize = new Vector2(82, 58),
            FocusMode = Control.FocusModeEnum.Click,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        card.Size = card.CustomMinimumSize;
        UiFactory.ApplyHudCommandButtonTheme(card, CurrentPalette, FontBody);
        _upgradeProjectCards[id] = card;
        parent.AddChild(card);
        card.MouseEntered += () => SetCatalogStatusText(card.InspectorText);
        card.MouseExited += RestoreCatalogStatusText;
        card.FocusEntered += () => SetCatalogStatusText(card.InspectorText);
        card.FocusExited += RestoreCatalogStatusText;
        card.Pressed += () => SetCatalogStatusText(card.InspectorText);
        return card;
    }

    private readonly record struct UpgradeProjectCardState(
        string Id,
        IconGlyph Icon,
        UpgradeProjectAccentKind Accent,
        string LabelKey,
        string ShortKey,
        string TargetKey,
        string SourceKey,
        string EffectKey,
        string StatusKey,
        string StatusBadgeKey,
        int Cost,
        int DurationSeconds);

    private enum UpgradeProjectAccentKind
    {
        Combat,
        Vision,
        Support,
    }

    private string UpgradeProjectCatalogStatusText()
    {
        return GameText.Format(
            "ui.catalog.upgradesCount",
            Math.Min(VisibleUpgradeProjectShellCount(), 12),
            GameText.T("ui.upgrade.source.researchBuilding"));
    }

    private int VisibleUpgradeProjectShellCount()
    {
        var count = 0;
        for (var index = 0; index < DefaultUpgradeProjectShellStates.Length; index++)
        {
            if (DefaultUpgradeProjectShellStates[index].Accent == _selectedUpgradeCategory)
            {
                count++;
            }
        }

        return count;
    }

    private static string UpgradeProjectCardMetricText(UpgradeProjectCardState state)
    {
        return GameText.Format(
            "ui.catalog.upgradeCardMetric",
            state.Cost,
            state.DurationSeconds);
    }

    private static Color UpgradeProjectAccent(UpgradeProjectAccentKind accent)
    {
        return accent switch
        {
            UpgradeProjectAccentKind.Combat => Amber,
            UpgradeProjectAccentKind.Vision => Cyan,
            UpgradeProjectAccentKind.Support => Mint,
            _ => InkMuted,
        };
    }
}
