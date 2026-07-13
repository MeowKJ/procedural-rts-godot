using Godot;

namespace ProceduralRts.Ui;

public partial class HudLayer
{
    private string _fixedHoverOwner = "";

    private void BindFixedHoverText(Control control, string owner, Func<string> text, Func<Color> accent)
    {
        control.MouseEntered += () => ShowFixedHoverText(owner, text(), accent());
        control.MouseExited += () => ClearFixedHoverText(owner);
        control.FocusEntered += () => ShowFixedHoverText(owner, text(), accent());
        control.FocusExited += () => ClearFixedHoverText(owner);
    }

    private void ShowFixedHoverText(string owner, string text, Color accent)
    {
        if (_commandRibbonContextValue is null || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _fixedHoverOwner = owner;
        _commandRibbonContextValue.Text = CompactText(text.Replace('\n', ' '), 34);
        SetLabelColor(_commandRibbonContextValue, accent);
    }

    private void ClearFixedHoverText(string owner)
    {
        if (!string.Equals(_fixedHoverOwner, owner, StringComparison.Ordinal))
        {
            return;
        }

        _fixedHoverOwner = "";
        RefreshCommandRibbonContext();
    }
}
