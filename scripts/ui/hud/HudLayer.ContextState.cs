using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer
{
    public void SetSandboxDeveloperContext(SandboxDeveloperContext context)
    {
        _sandboxDeveloperContext = context;
        if (_sandboxDeveloperPanel is null)
        {
            return;
        }

        var owner = SandboxDeveloperContextOptions.OwnerOption(context.OwnerId);
        var faction = SandboxDeveloperContextOptions.FactionOption(context.Faction);
        var team = SandboxDeveloperContextOptions.TeamOption(context.TeamId);
        var relation = SandboxDeveloperContextOptions.RelationOption(context.Relation);
        var environment = SandboxDeveloperContextOptions.EnvironmentOption(context.Environment);
        var overlay = context.DebugOverlay.FormatStatus();

        _sandboxDeveloperStatus.Text = CompactText($"{faction.Label} / {SandboxTimeScaleMath.Format(context.TimeScale)}", 36);
        _sandboxOwnerButton.Text = $"Own {owner.OwnerId.Value}";
        _sandboxFactionButton.Text = faction.CanSpawn ? faction.Label : "Locked";
        _sandboxTeamButton.Text = $"Team {team.TeamId}";
        _sandboxRelationButton.Text = relation.Label;
        _sandboxTimeButton.Text = SandboxTimeScaleMath.Format(context.TimeScale).Replace("Sandbox time ", "", StringComparison.Ordinal);
        _sandboxAtmosphereButton.Text = CompactText(environment.Label, 12);
        _sandboxOverlayButton.Text = overlay == "Sandbox overlays: off" ? "Overlay off" : "Overlay on";
        _sandboxStressButton.Text = context.CanSpawnCurrentFaction ? "Stress spawn" : "Locked";
        _sandboxStressButton.Disabled = !context.CanSpawnCurrentFaction;
        if (!context.DebugOverlay.IsEnabled(SandboxDebugOverlayFlag.StateHash))
        {
            SetSandboxStateHash(null);
        }

        foreach (var button in _sandboxDeveloperButtons)
        {
            button.QueueRedraw();
        }
    }

    public void SetSandboxStateHash(ulong? hash)
    {
        if (_sandboxStateHashValue is null)
        {
            return;
        }

        _sandboxStateHashValue.Visible = hash is not null;
        _sandboxStateHashValue.Text = hash is null ? "" : $"HASH {hash.Value:X16}";
    }

    public void SetSelectedCount(int count)
    {
        SetHudContext(count > 0, hasBuildingSelection: false, _buildModeActive);
        if (count == 0)
        {
            SetSelectionInfo(
                GameText.T("ui.noSelection.title"),
                GameText.T("ui.noSelection.meta"),
                GameText.T("ui.noSelection.stats"),
                GameText.T("ui.noSelection.detail"),
                "none",
                IconGlyph.None);
        }
        else
        {
            SetSelectionInfo(GameText.Format("ui.multi.title", count), GameText.T("ui.status.ready"), GameText.T("ui.multi.mixedSelection"), GameText.T("ui.multi.detail"), "multi", IconGlyph.Group);
        }
    }

    public void SetHudContext(bool hasSelection, bool hasBuildingSelection, bool buildModeActive)
    {
        var wasShowingNoSelectionCommandHint = ShouldShowNoSelectionCommandHint();
        _hasSelection = hasSelection;
        _hasBuildingSelection = hasBuildingSelection;
        _buildModeActive = buildModeActive;
        if (hasSelection)
        {
            _detailDrawerProgress = 1f;
        }

        if (hasBuildingSelection || buildModeActive)
        {
            _productionDrawerProgress = 1f;
        }

        if (_commandRibbon is not null)
        {
            _commandRibbon.Visible = true;
        }

        if (wasShowingNoSelectionCommandHint != ShouldShowNoSelectionCommandHint())
        {
            SetCatalogInspectorDefault(DefaultCatalogInspectorText());
        }

        if (IsInsideTree()) LayoutDynamicHud(GetViewport().GetVisibleRect().Size);
    }

    public void SetSelectionInfo(string title, string meta, string stats, string detail, string portraitMode, IconGlyph icon = IconGlyph.None)
    {
        SetSelectionInfo(title, meta, stats, detail, portraitMode, icon, [], icon == IconGlyph.None ? InkMuted : Mint, null);
    }

    public void SetSelectionInfo(
        string title,
        string meta,
        string stats,
        string detail,
        string portraitMode,
        IconGlyph icon,
        IReadOnlyList<SelectionIconItem> iconSummary,
        Color iconAccent,
        string? unitDesignId = null)
    {
        SetLabelTextAndResetSizeWhenChanged(_drawerSelectedTitle, CompactText(title, 24));
        SetLabelTextAndResetSizeWhenChanged(_drawerSelectedMeta, CompactText(meta, 30));
        SetLabelTextAndResetSizeWhenChanged(_drawerSelectedStats, CompactText(stats, 31));
        SetLabelTextAndResetSizeWhenChanged(_drawerSelectedDetail, CompactText(detail, 34));
        _drawerPortrait.Mode = portraitMode;
        _drawerPortrait.Icon = icon;
        _drawerPortrait.UnitDesignId = unitDesignId;
        _drawerPortrait.Accent = iconAccent;
        _drawerPortrait.QueueRedraw();
        _drawerIconSummary.Items = iconSummary;
        _drawerIconSummary.Visible = iconSummary.Count > 0;
        _drawerPortrait.Visible = iconSummary.Count == 0;
        _drawerIconSummary.QueueRedraw();
    }
}
