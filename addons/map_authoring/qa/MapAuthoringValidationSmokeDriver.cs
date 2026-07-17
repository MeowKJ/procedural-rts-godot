using Godot;
using ProceduralRts.MapAuthoring.Editor;

namespace ProceduralRts.MapAuthoring.Qa;

[Tool]
public partial class MapAuthoringValidationSmokeDriver : Node
{
    private const string PluginName = "map_authoring";
    public static bool IsRunning { get; private set; }

    public static void Launch()
    {
        if (IsRunning) return;
        IsRunning = true;
        var driver = new MapAuthoringValidationSmokeDriver { Name = "MapAuthoringValidationSmokeDriver" };
        EditorInterface.Singleton.GetBaseControl().AddChild(driver);
        driver.CallDeferred(nameof(Run));
    }

    public async void Run()
    {
        try
        {
            await WaitForEditorReady();
            RequireLifecycle(active: true);
            var feature = RequireFeature();
            await ValidateOverlayScenarios(feature);
            await ValidateConflictAndStaleScenarios(feature);
            await ValidateLifecycleAndCapture();
            GD.Print("Map Authoring validation smoke PASSED: deterministic dock, source navigation, stale safety, complete overlays, and 1/0/1 lifecycle.");
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

    private async Task WaitForEditorReady()
    {
        for (var frame = 0; frame < 60; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static MapAuthoringValidationFeature RequireFeature()
        => MapAuthoringValidationFeature.Current
            ?? throw new InvalidOperationException("Validation feature must be active.");

    private static void RequireLifecycle(bool active)
    {
        Require(MapAuthoringRegistrationState.Active == active, "Plugin active state mismatch.");
        Require(MapAuthoringRegistrationState.ActiveFeatureCount == (active ? 1 : 0), "Feature count must be 1/0/1.");
        Require(MapAuthoringRegistrationState.ActiveDockCount == (active ? 1 : 0), "Dock count must be 1/0/1.");
        Require(MapAuthoringRegistrationState.ActiveForceDrawForwarderCount == (active ? 1 : 0),
            "Force draw forwarder count must be 1/0/1.");
        Require((MapAuthoringValidationFeature.Current is not null) == active, "Feature singleton state mismatch.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
