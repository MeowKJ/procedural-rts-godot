using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private Panel _sandboxDeveloperPanel = null!;
    private Label _sandboxDeveloperStatus = null!;
    private Label _sandboxStateHashValue = null!;
    private Label _sandboxCommandLogValue = null!;
    private Button _sandboxOwnerButton = null!;
    private Button _sandboxFactionButton = null!;
    private Button _sandboxTeamButton = null!;
    private Button _sandboxRelationButton = null!;
    private Button _sandboxTimeButton = null!;
    private Button _sandboxAtmosphereButton = null!;
    private Button _sandboxOverlayButton = null!;
    private Button _sandboxStressButton = null!;
    private readonly List<Button> _sandboxDeveloperButtons = [];
    private SandboxDeveloperContext _sandboxDeveloperContext = SandboxDeveloperContext.Default;

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

        if (!context.DebugOverlay.IsEnabled(SandboxDebugOverlayFlag.CommandLog))
        {
            SetSandboxCommandLog([], visible: false);
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

    public void SetSandboxCommandLog(IReadOnlyList<string> lines, bool visible)
    {
        if (_sandboxCommandLogValue is null)
        {
            return;
        }

        var shouldShow = visible && lines.Count > 0;
        _sandboxCommandLogValue.Visible = shouldShow;
        if (!shouldShow)
        {
            _sandboxCommandLogValue.Text = "";
            return;
        }

        var shown = Math.Min(lines.Count, 4);
        _sandboxCommandLogValue.Text = shown switch
        {
            1 => CompactMultiline($"CMD LOG\n{lines[0]}", 34),
            2 => CompactMultiline($"CMD LOG\n{lines[0]}\n{lines[1]}", 34),
            3 => CompactMultiline($"CMD LOG\n{lines[0]}\n{lines[1]}\n{lines[2]}", 34),
            _ => CompactMultiline($"CMD LOG\n{lines[0]}\n{lines[1]}\n{lines[2]}\n{lines[3]}", 34),
        };
    }
}
