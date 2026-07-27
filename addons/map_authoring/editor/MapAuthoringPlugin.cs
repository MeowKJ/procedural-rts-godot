using Godot;
using ProceduralRts.MapAuthoring.Qa;

namespace ProceduralRts.MapAuthoring.Editor;

[Tool]
public partial class MapAuthoringPlugin : EditorPlugin
{
    private MapAuthoringInspectorPlugin? _inspector;
    private MapAuthoringValidationFeature? _validation;
    private readonly List<string> _registeredTypes = [];
    private bool _forceDrawForwardingRegistered;
    private bool _inspectorRegistered;
    private bool _registered;

    public override void _EnterTree()
    {
        Register();
        if (OS.GetEnvironment("MAP_AUTHORING_PLUGIN_SMOKE") == "1")
        {
            MapAuthoringPluginSmokeDriver.Launch();
        }
        if (OS.GetEnvironment("MAP_AUTHORING_VALIDATION_SMOKE") == "1")
        {
            MapAuthoringValidationSmokeDriver.Launch();
        }
        if (OS.GetEnvironment("MAP_AUTHORING_BAKE_PLAY_SMOKE") == "1")
        {
            MapAuthoringBakePlaySmokeDriver.Launch();
        }
    }

    public override void _ExitTree()
    {
        Unregister();
    }

    public override void _Process(double delta)
    {
        _validation?.PollPlaySession();
    }

    private void Register()
    {
        if (_registered)
        {
            return;
        }

        MapAuthoringTypeRegistry.ValidateTypeNames(
            MapAuthoringTypeRegistry.Types,
            typeName => ClassDB.ClassExists(typeName));
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

            SetForceDrawOverForwardingEnabled();
            MapAuthoringRegistrationState.ForceDrawForwarderAdded();
            _forceDrawForwardingRegistered = true;
            _validation = new MapAuthoringValidationFeature(this);

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

        _validation?.Dispose();
        _validation = null;
        RemoveForceDrawForwarder();
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
        _validation?.Dispose();
        _validation = null;
        RemoveForceDrawForwarder();
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

    private void RemoveForceDrawForwarder()
    {
        if (!_forceDrawForwardingRegistered) return;
        MapAuthoringRegistrationState.ForceDrawForwarderRemoved();
        _forceDrawForwardingRegistered = false;
    }

    public override bool _Handles(GodotObject @object)
    {
        return @object is Node node && node.GetTree()?.EditedSceneRoot is Nodes.MapRoot;
    }

    public override void _ForwardCanvasForceDrawOverViewport(Control viewportControl)
    {
        if (!_forceDrawForwardingRegistered) return;
        MapAuthoringRegistrationState.ForceDrawForwarded();
        _validation?.Draw(viewportControl);
    }
}
