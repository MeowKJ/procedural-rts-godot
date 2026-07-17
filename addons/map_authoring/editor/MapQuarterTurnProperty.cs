using Godot;

namespace ProceduralRts.MapAuthoring.Editor;

[Tool]
public partial class MapQuarterTurnProperty : EditorProperty
{
    private const string ControlName = "QuarterTurns";

    public MapQuarterTurnProperty()
    {
        EnsureControl();
    }

    public override void _UpdateProperty()
    {
        var control = EnsureControl();
        SyncOptions(control);
        var radians = (float)GetEditedObject().Get(GetEditedProperty()).AsDouble();
        var index = MapBuildingQuarterTurns.IndexOf(radians);
        control.Selected = index;
        control.Text = index >= 0 ? MapBuildingQuarterTurns.All[index].Label : $"Invalid: {Mathf.RadToDeg(radians):0.###}°";
        control.TooltipText = index >= 0 ? "Cardinal building rotation" : "Persisted rotation is not a cardinal quarter-turn";
    }

    internal IReadOnlyList<string> CurrentOptions => ReadOptions(EnsureControl());

    public override void _SetReadOnly(bool readOnly)
    {
        EnsureControl().Disabled = readOnly;
    }

    private void OnItemSelected(long index)
    {
        EmitChanged(GetEditedProperty(), MapBuildingQuarterTurns.All[(int)index].Radians);
    }

    private OptionButton EnsureControl()
    {
        var control = GetNodeOrNull<OptionButton>(ControlName) ?? CreateControl();
        RebindSelection(control);
        return control;
    }

    private OptionButton CreateControl()
    {
        var control = new OptionButton { Name = ControlName };
        SyncOptions(control);
        AddChild(control);
        AddFocusable(control);
        return control;
    }

    private void RebindSelection(OptionButton control)
    {
        var signal = OptionButton.SignalName.ItemSelected;
        foreach (var connection in control.GetSignalConnectionList(signal))
        {
            control.Disconnect(signal, connection["callable"].AsCallable());
        }
        control.ItemSelected += OnItemSelected;
    }

    private static IReadOnlyList<string> ReadOptions(OptionButton control)
    {
        return Enumerable.Range(0, control.ItemCount).Select(control.GetItemText).ToArray();
    }

    private static void SyncOptions(OptionButton control)
    {
        var labels = MapBuildingQuarterTurns.All.Select(turn => turn.Label).ToArray();
        if (ReadOptions(control).SequenceEqual(labels)) return;
        control.Clear();
        foreach (var label in labels) control.AddItem(label);
    }
}
