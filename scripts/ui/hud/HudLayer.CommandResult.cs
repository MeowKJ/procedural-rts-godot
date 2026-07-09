using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private Label _commandResultValue = null!;
    private string _lastCommandPanelResult = "";
    private float _commandResultPulse;

    public void SetCommandPanelResult(string status)
    {
        if (_commandResultValue is null)
        {
            return;
        }

        var result = string.IsNullOrWhiteSpace(status) ? GameText.T("ui.status.ready") : status;
        if (!string.Equals(_lastCommandPanelResult, result, StringComparison.Ordinal))
        {
            _commandResultPulse = 1f;
            _lastCommandPanelResult = result;
        }

        _commandResultValue.Text = CompactMultiline(result, 24);
        SetLabelColor(_commandResultValue, CommandPanelResultColor(result));
    }

    private void BuildCommandResult(Control parent)
    {
        _commandResultValue = MakeSizedLabel(GameText.T("ui.status.ready"), new Vector2(14, 306), new Vector2(136, 18), FontTiny, InkMuted);
        _commandResultValue.Name = "CommandResult";
        _commandResultValue.VerticalAlignment = VerticalAlignment.Center;
        parent.AddChild(_commandResultValue);
    }

    private Color CommandPanelResultColor(string result)
    {
        if (IsCommandPanelProblemResult(result))
        {
            return Danger;
        }

        return string.Equals(result, GameText.T("ui.status.ready"), StringComparison.Ordinal)
            ? InkMuted
            : Mint;
    }

    private static bool IsCommandPanelProblemResult(string result)
    {
        return ContainsLocalizedPrefix(result, "ui.needCredits")
            || ContainsLocalizedPrefix(result, "production.needCredits")
            || ContainsAnyOrdinal(result, "No ", "Need ", "Select ", "Cannot ");
    }

    private static bool ContainsLocalizedPrefix(string status, string key)
    {
        var prefix = GameText.T(key).Split('{')[0].Trim();
        return !string.IsNullOrWhiteSpace(prefix)
            && status.Contains(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsAnyOrdinal(string value, params string[] needles)
    {
        for (var index = 0; index < needles.Length; index++)
        {
            if (value.Contains(needles[index], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
