using Godot;
using ProceduralRts.MapAuthoring.Editor;
using ProceduralRts.MapAuthoring.Nodes;

namespace ProceduralRts.MapAuthoring.Qa;

[Tool]
public partial class MapAuthoringPluginSmokeDriver : Node
{
    private const string PluginName = "map_authoring";
    private const string AcceptanceScenePath = "res://addons/map_authoring/qa/MapAuthoringEditorAcceptance.tscn";
    private const string UnknownScenePath = "res://addons/map_authoring/qa/MapAuthoringUnknownCatalogAcceptance.tscn";
    private const string UnknownBuildingId = "unknown.visual-sentinel.building";

    public static bool IsRunning { get; private set; }

    public static void Launch()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        var driver = new MapAuthoringPluginSmokeDriver { Name = "MapAuthoringPluginSmokeDriver" };
        EditorInterface.Singleton.GetBaseControl().AddChild(driver);
        driver.CallDeferred(nameof(Run));
    }

    public async void Run()
    {
        try
        {
            Require(MapAuthoringRegistrationState.Active, "Plugin should be active before lifecycle smoke.");
            Require(MapAuthoringRegistrationState.ActiveInspectorCount == 1, "Plugin should register exactly one Inspector.");
            ValidateTypeNameRejectionIsSideEffectFree();
            RequireTypes();
            var visualEditorChecked = DisplayServer.GetName() != "headless";
            await ValidateAcceptanceScene(visualEditorChecked);
            if (visualEditorChecked) await MapAuthoringEditorPersistenceSmoke.Run(NextFrame);
            await ValidateUnknownCatalogScene();
            if (visualEditorChecked)
            {
                await MapAuthoringCreateDialogSmoke.Run(NextFrame);
            }
            var enterBefore = MapAuthoringRegistrationState.EnterCount;
            var exitBefore = MapAuthoringRegistrationState.ExitCount;

            EditorInterface.Singleton.SetPluginEnabled(PluginName, false);
            await NextFrame();
            Require(!MapAuthoringRegistrationState.Active, "Plugin should fully unregister when disabled.");
            Require(MapAuthoringRegistrationState.ActiveTypeCount == 0, "Custom types should unregister when disabled.");
            Require(MapAuthoringRegistrationState.ActiveInspectorCount == 0, "Inspector should unregister when disabled.");
            Require(MapAuthoringRegistrationState.ExitCount == exitBefore + 1, "Disable should execute one teardown.");

            EditorInterface.Singleton.SetPluginEnabled(PluginName, true);
            await NextFrame();
            Require(MapAuthoringRegistrationState.Active, "Plugin should be active after re-enable.");
            Require(MapAuthoringRegistrationState.ActiveInspectorCount == 1, "Re-enable should register exactly one Inspector.");
            RequireTypes();
            if (visualEditorChecked) await MapAuthoringCreateDialogSmoke.Run(NextFrame);
            Require(MapAuthoringRegistrationState.EnterCount == enterBefore + 1, "Re-enable should execute one registration.");

            var createNodeEvidence = visualEditorChecked ? ", Inspector edits persisted and Create Node resolved all custom types before and after re-enable" : "";
            GD.Print($"Map Authoring plugin lifecycle smoke PASSED: enabled, disabled, and re-enabled with ten unique custom types and one Inspector{createNodeEvidence}.");
            IsRunning = false;
            QueueFree();
        }
        catch (Exception exception)
        {
            IsRunning = false;
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task NextFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task ValidateAcceptanceScene(bool validateInspectorControl)
    {
        EditorInterface.Singleton.OpenSceneFromPath(AcceptanceScenePath);
        await NextFrame();
        RequireAcceptanceRoot();
        if (validateInspectorControl)
        {
            MapCatalogPropertyScenarios.Run(
                EditorInterface.Singleton.GetEditedSceneRoot().GetNode<Building>("Headquarters"));
        }
        EditorInterface.Singleton.ReloadSceneFromPath(AcceptanceScenePath);
        await NextFrame();
        RequireAcceptanceRoot();
    }

    private static void RequireAcceptanceRoot()
    {
        var root = EditorInterface.Singleton.GetEditedSceneRoot();
        Require(root is MapRoot, "Editor acceptance fixture must open as typed MapRoot.");
        Require(root.GetChildren().Count >= 10, "Editor acceptance fixture must expose representative typed children.");
    }

    private async Task ValidateUnknownCatalogScene()
    {
        EditorInterface.Singleton.OpenSceneFromPath(UnknownScenePath);
        await NextFrame();
        RequireUnknownCatalogSentinel();
        EditorInterface.Singleton.ReloadSceneFromPath(UnknownScenePath);
        await NextFrame();
        RequireUnknownCatalogSentinel();
    }

    private static void RequireUnknownCatalogSentinel()
    {
        var root = EditorInterface.Singleton.GetEditedSceneRoot() as MapRoot
            ?? throw new InvalidOperationException("Unknown-catalog fixture must open as typed MapRoot.");
        var building = root.GetNode<Building>("UnknownBuilding");
        Require(building.BuildingId == UnknownBuildingId, "Unknown persisted building id must survive editor open/reload exactly.");
        Require(MapCatalogOptionProperty.DisplayText(building.BuildingId, known: false) == $"Unknown: {UnknownBuildingId}",
            "Unknown persisted building id must remain visibly marked without mutation.");
        EditorInterface.Singleton.GetSelection().Clear();
        EditorInterface.Singleton.GetSelection().AddNode(building);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void RequireTypes()
    {
        var expected = MapAuthoringTypeRegistry.Types.Select(type => type.Name).ToArray();
        Require(MapAuthoringRegistrationState.ActiveTypeCount == 10, "Plugin should register exactly ten custom types.");
        Require(MapAuthoringRegistrationState.ActiveTypeNames.Order(StringComparer.Ordinal)
            .SequenceEqual(expected.Order(StringComparer.Ordinal)), "Plugin custom type names should be unique and complete.");
        foreach (var descriptor in MapAuthoringTypeRegistry.Types)
        {
            Require(!ClassDB.ClassExists(descriptor.Name),
                $"Custom type name {descriptor.Name} must not collide with a native Godot class.");
            var script = GD.Load<Script>(descriptor.ScriptPath)
                ?? throw new InvalidOperationException($"Custom type script must load: {descriptor.ScriptPath}.");
            var scriptName = Path.GetFileNameWithoutExtension(descriptor.ScriptPath);
            if (descriptor.Name == "ResourceField")
            {
                Require(scriptName == "Resource", "ResourceField custom type must map to the typed Resource.cs script.");
            }
            else
            {
                Require(scriptName == descriptor.Name,
                    $"Custom type name {descriptor.Name} must match its C# script class/file name.");
            }
            Require(script.GetInstanceBaseType() == MapAuthoringTypeRegistry.BaseType,
                $"Custom type {descriptor.Name} must inherit {MapAuthoringTypeRegistry.BaseType}.");
        }
    }

    private static void ValidateTypeNameRejectionIsSideEffectFree()
    {
        var activeBefore = MapAuthoringRegistrationState.Active;
        var typesBefore = MapAuthoringRegistrationState.ActiveTypeNames.ToArray();
        var inspectorBefore = MapAuthoringRegistrationState.ActiveInspectorCount;
        var enterBefore = MapAuthoringRegistrationState.EnterCount;
        var exitBefore = MapAuthoringRegistrationState.ExitCount;
        InvalidOperationException? rejection = null;
        try
        {
            MapAuthoringTypeRegistry.ValidateTypeNames(
                [new MapAuthoringTypeDescriptor("Resource", "res://invalid/Resource.cs")],
                typeName => typeName == "Resource");
        }
        catch (InvalidOperationException exception)
        {
            rejection = exception;
        }

        Require(rejection?.Message == "Custom type name 'Resource' collides with native Godot class 'Resource'.",
            "Native type-name collision must fail with a deterministic diagnostic.");
        Require(MapAuthoringRegistrationState.Active == activeBefore
            && MapAuthoringRegistrationState.ActiveTypeNames.SequenceEqual(typesBefore)
            && MapAuthoringRegistrationState.ActiveInspectorCount == inspectorBefore
            && MapAuthoringRegistrationState.EnterCount == enterBefore
            && MapAuthoringRegistrationState.ExitCount == exitBefore,
            "Rejected type-name validation must not mutate registration state.");
    }
}
