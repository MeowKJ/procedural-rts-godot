using Godot;
using System.Diagnostics;
using ProceduralRts.MapAuthoring.Editor;
using ProceduralRts.MapAuthoring.Nodes;

namespace ProceduralRts.MapAuthoring.Qa;

[Tool]
public partial class MapAuthoringBakePlaySmokeDriver : Node
{
    private const string PluginName = "map_authoring";
    private const string SampleScene = "res://addons/map_authoring/samples/AuthoredMapPreview.tscn";
    private const string EvidenceDirectory = "res://artifacts/issue-569";
    public static bool IsRunning { get; private set; }

    public static void Launch()
    {
        if (IsRunning) return;
        IsRunning = true;
        var driver = new MapAuthoringBakePlaySmokeDriver { Name = "MapAuthoringBakePlaySmokeDriver" };
        EditorInterface.Singleton.GetBaseControl().AddChild(driver);
        driver.CallDeferred(nameof(Run));
    }

    public async void Run()
    {
        try
        {
            for (var frame = 0; frame < 60; frame++) await ProcessFrame();
            var feature = RequireFeature();
            EditorInterface.Singleton.OpenSceneFromPath(SampleScene);
            await NextFrame();
            var root = ActiveRoot();
            feature.ValidateActiveScene();
            Require(feature.Report?.Diagnostics.Count == 0, "Typed preview sample must validate cleanly.");
            await Capture("typed-sample.png");

            var first = feature.BakeActiveScene() ?? throw new InvalidOperationException("First Bake failed.");
            var firstBytes = File.ReadAllBytes(first.AbsolutePath);
            var second = feature.BakeActiveScene() ?? throw new InvalidOperationException("Second Bake failed.");
            Require(first.Sha256 == second.Sha256 && firstBytes.SequenceEqual(File.ReadAllBytes(second.AbsolutePath)),
                "Validate -> Bake A -> Bake B must preserve exact bytes/hash.");
            Require(feature.Dock?.StatusText.Contains(first.ResourcePath, StringComparison.Ordinal) == true
                && feature.Dock.StatusText.Contains(first.Sha256, StringComparison.Ordinal),
                "Dock must display canonical res path and SHA-256.");
            await Capture("path-hash.png");

            var building = root.GetNode<Building>("PlayerHeadquarters");
            building.Rotation = 0.1f;
            feature.NotifyPropertyEditedForQa("rotation");
            var invalidBake = feature.BakeActiveScene();
            feature.TogglePlayActiveScene();
            Require(invalidBake is null && firstBytes.SequenceEqual(File.ReadAllBytes(first.AbsolutePath))
                && feature.OwnedPlayPid is null, "Invalid edit must block Bake/Play and preserve last-known-good.");
            await Capture("invalid-last-good.png");
            building.Rotation = 0;
            feature.NotifyPropertyEditedForQa("rotation");

            feature.TogglePlayActiveScene();
            Require(feature.OwnedPlayPid is not null && feature.Dock?.PlayButtonText == "Stop",
                "Play must own one child PID and switch dock action to Stop.");
            await WaitForOwnedChildExit(feature);
            Require(feature.OwnedPlayPid is null && feature.Dock?.PlayButtonText == "Play",
                "Natural child exit must clear owned Play state.");
            await WaitForEditorRunToSettle();

            feature.TogglePlayActiveScene();
            var disposePid = feature.OwnedPlayPid ?? throw new InvalidOperationException(
                $"Dispose test child did not spawn: {feature.Dock?.StatusText ?? "no dock status"}");
            EditorInterface.Singleton.SetPluginEnabled(PluginName, false);
            await NextFrame();
            Require(!MapAuthoringRegistrationState.Active && !ProcessExists(disposePid),
                "Plugin disable must stop only its owned preview child and clear registration.");
            EditorInterface.Singleton.SetPluginEnabled(PluginName, true);
            await NextFrame();
            Require(MapAuthoringRegistrationState.Active && MapAuthoringRegistrationState.ActiveFeatureCount == 1,
                "Plugin re-enable must restore exactly one feature.");
            EditorInterface.Singleton.OpenSceneFromPath(SampleScene);
            await NextFrame();
            await Capture("post-reenable.png");

            GD.Print("Map Authoring Bake Play smoke PASSED: fresh validation, atomic parity, invalid preservation, owned Play lifecycle, runtime command, return, and clean normal skirmish.");
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

    private async Task WaitForOwnedChildExit(MapAuthoringValidationFeature feature)
    {
        for (var frame = 0; frame < 1200 && feature.OwnedPlayPid is not null; frame++) await ProcessFrame();
        feature.PollPlaySession();
        Require(feature.OwnedPlayPid is null, "Owned preview child did not exit after runtime smoke.");
    }

    private async Task WaitForEditorRunToSettle()
    {
        for (var frame = 0; frame < 120 && EditorInterface.Singleton.IsPlayingScene(); frame++) await ProcessFrame();
        Require(!EditorInterface.Singleton.IsPlayingScene(), "Editor run state did not settle after owned preview exit.");
    }

    private async Task Capture(string file)
    {
        if (DisplayServer.GetName() == "headless") return;
        await NextFrame();
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(EvidenceDirectory));
        var image = EditorInterface.Singleton.GetBaseControl().GetViewport().GetTexture().GetImage();
        Require(image.SavePng($"{EvidenceDirectory}/{file}") == Error.Ok, $"Screenshot failed: {file}.");
    }

    private async Task NextFrame() { await ProcessFrame(); await ProcessFrame(); }
    private async Task ProcessFrame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private static MapAuthoringValidationFeature RequireFeature() => MapAuthoringValidationFeature.Current
        ?? throw new InvalidOperationException("Validation feature must be active.");
    private static MapRoot ActiveRoot() => EditorInterface.Singleton.GetEditedSceneRoot() as MapRoot
        ?? throw new InvalidOperationException("Active scene must be MapRoot.");
    private static bool ProcessExists(int pid)
    {
        try { return !Process.GetProcessById(pid).HasExited; }
        catch (ArgumentException) { return false; }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
