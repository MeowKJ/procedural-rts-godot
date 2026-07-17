using Godot;
using ProceduralRts.MapAuthoring.Qa;

namespace ProceduralRts.MapAuthoring.Editor;

[Tool]
public partial class MapAuthoringPlugin : EditorPlugin
{
    private MapAuthoringInspectorPlugin? _inspector;
    private readonly List<string> _registeredTypes = [];
    private bool _inspectorRegistered;
    private bool _registered;

    public override void _EnterTree()
    {
        Register();
        if (OS.GetEnvironment("MAP_AUTHORING_PLUGIN_SMOKE") == "1")
        {
            MapAuthoringPluginSmokeDriver.Launch();
        }
    }

    public override void _ExitTree()
    {
        Unregister();
    }

    private void Register()
    {
        if (_registered)
        {
            return;
        }

        MapAuthoringTypeRegistry.ValidateAliases(
            MapAuthoringTypeRegistry.Types,
            alias => ClassDB.ClassExists(alias));
        MapAuthoringRegistrationState.Begin();
        try
        {
            _inspector = new MapAuthoringInspectorPlugin();
            AddInspectorPlugin(_inspector);
            MapAuthoringRegistrationState.InspectorAdded();
            _inspectorRegistered = true;
            foreach (var descriptor in MapAuthoringTypeRegistry.Types)
            {
                var script = GD.Load<Script>(descriptor.ScriptPath)
                    ?? throw new InvalidOperationException($"Could not load custom type script {descriptor.ScriptPath}.");
                AddCustomType(descriptor.Name, MapAuthoringTypeRegistry.BaseType, script, null);
                _registeredTypes.Add(descriptor.Name);
                MapAuthoringRegistrationState.TypeAdded(descriptor.Name);
            }

            _registered = true;
        }
        catch
        {
            RollbackRegistration();
            throw;
        }
    }

    private void Unregister()
    {
        if (!_registered)
        {
            return;
        }

        RemoveRegisteredTypes();
        if (_inspectorRegistered && _inspector is not null)
        {
            RemoveInspectorPlugin(_inspector);
            MapAuthoringRegistrationState.InspectorRemoved();
            _inspectorRegistered = false;
        }

        _inspector?.Dispose();
        _inspector = null;
        _registered = false;
        MapAuthoringRegistrationState.End();
    }

    private void RollbackRegistration()
    {
        RemoveRegisteredTypes();
        if (_inspectorRegistered && _inspector is not null)
        {
            RemoveInspectorPlugin(_inspector);
            MapAuthoringRegistrationState.InspectorRemoved();
            _inspectorRegistered = false;
        }

        _inspector?.Dispose();
        _inspector = null;
        _registered = false;
        MapAuthoringRegistrationState.End();
    }

    private void RemoveRegisteredTypes()
    {
        for (var index = _registeredTypes.Count - 1; index >= 0; index--)
        {
            var name = _registeredTypes[index];
            RemoveCustomType(name);
            MapAuthoringRegistrationState.TypeRemoved(name);
        }

        _registeredTypes.Clear();
    }
}
