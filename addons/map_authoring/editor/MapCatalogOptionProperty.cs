using Godot;

namespace ProceduralRts.MapAuthoring.Editor;

[Tool]
public partial class MapCatalogOptionProperty : EditorProperty
{
    private const string ControlName = "CatalogOptions";

    public MapCatalogOptionProperty()
    {
        EnsureControl();
    }

    public override void _UpdateProperty()
    {
        var control = EnsureControl();
        var editedObject = GetEditedObject();
        var editedProperty = GetEditedProperty();
        SyncOptions(control, ResolveOptions(editedObject, editedProperty));
        var value = editedObject.Get(editedProperty).AsString();
        var index = Enumerable.Range(0, control.ItemCount)
            .FirstOrDefault(index => control.GetItemText(index) == value, -1);
        control.Selected = index;
        control.Text = DisplayText(value, index >= 0);
        control.TooltipText = index >= 0 ? value : $"Unsupported persisted id '{value}'";
    }

    public static string DisplayText(string value, bool known)
    {
        return known ? value : $"Unknown: {value}";
    }

    internal IReadOnlyList<string> CurrentOptions => ReadOptions(EnsureControl());

    internal string CurrentText => EnsureControl().Text;

    public override void _SetReadOnly(bool readOnly)
    {
        EnsureControl().Disabled = readOnly;
    }

    private void OnItemSelected(long index)
    {
        var control = GetNode<OptionButton>(ControlName);
        if (index < 0 || index >= control.ItemCount) return;
        EmitChanged(GetEditedProperty(), control.GetItemText((int)index));
    }

    private static IReadOnlyList<string> ResolveOptions(GodotObject value, string propertyName)
    {
        return MapAuthoringInspectorCatalog.TryOptions(value, propertyName, out var options)
            ? options
            : Array.Empty<string>();
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

    private static void SyncOptions(OptionButton control, IReadOnlyList<string> values)
    {
        if (ReadOptions(control).SequenceEqual(values)) return;
        control.Clear();
        foreach (var value in values) control.AddItem(value);
    }
}
