using Godot;

namespace ProceduralRts.MapAuthoring.Editor;

[Tool]
public partial class MapAuthoringInspectorPlugin : EditorInspectorPlugin
{
    public override bool _CanHandle(GodotObject @object)
    {
        return MapAuthoringInspectorCatalog.Handles(@object);
    }

    public override bool _ParseProperty(
        GodotObject @object,
        Variant.Type type,
        string name,
        PropertyHint hintType,
        string hintString,
        PropertyUsageFlags usageFlags,
        bool wide)
    {
        if (MapAuthoringInspectorCatalog.TryOptions(@object, name, out _))
        {
            AddPropertyEditor(name, new MapCatalogOptionProperty());
            return true;
        }

        if (MapAuthoringInspectorCatalog.IsBuildingRotation(@object, name))
        {
            AddPropertyEditor(name, new MapQuarterTurnProperty());
            return true;
        }

        return false;
    }
}
