using Godot;
using ProceduralRts.MapAuthoring.Editor;
using ProceduralRts.MapAuthoring.Nodes;

namespace ProceduralRts.MapAuthoring.Qa;

public partial class MapAuthoringValidationSmokeDriver
{
    private const string EvidenceDirectory = "res://artifacts/issue-568";

    private async Task SelectAndCapture(Node node, string file)
    {
        EditorInterface.Singleton.GetSelection().Clear();
        EditorInterface.Singleton.GetSelection().AddNode(node);
        EditorInterface.Singleton.EditNode(node);
        await Capture(file);
    }

    private async Task RequireForceDrawAfterSelectionClear(
        MapAuthoringValidationFeature feature, string phase)
    {
        EditorInterface.Singleton.GetSelection().Clear();
        await NextFrame();
        var before = MapAuthoringRegistrationState.ForceDrawCallCount;
        feature.RequestOverlayRedrawForQa();
        await NextFrame();
        Require(MapAuthoringRegistrationState.ActiveForceDrawForwarderCount == 1
            && MapAuthoringRegistrationState.ForceDrawCallCount == before + 1,
            $"{phase} must deliver exactly one force draw callback after Selection.Clear().");
    }

    private async Task CaptureRotatedBuilding(
        MapAuthoringValidationFeature feature, Building building, string file)
    {
        Require(building.Name == "RotatedRefinery"
            && Mathf.IsEqualApprox(building.Rotation, MapBuildingQuarterTurns.All[1].Radians),
            "Rotated evidence fixture must persist the shared 90-degree cardinal state.");
        var path = ActiveRoot().GetPathTo(building).ToString();
        var primitives = feature.Plan.Primitives
            .Where(value => value.Source.ToString() == path).ToArray();
        var hard = primitives.Single(value => value.Kind == MapOverlayPrimitiveKind.HardFootprint);
        Require(hard.Rect.Size.X < hard.Rect.Size.Y
            && primitives.Any(value => value.Kind == MapOverlayPrimitiveKind.Clearance)
            && primitives.Any(value => value.Kind == MapOverlayPrimitiveKind.RefineryDock),
            "90-degree non-square footprint evidence must include clearance and refinery dock geometry.");
        await SelectAndCapture(building, file);
    }

    private async Task Capture(string file)
    {
        if (DisplayServer.GetName() == "headless") return;
        EditorInterface.Singleton.SetMainScreenEditor("2D");
        await NextFrame();
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(EvidenceDirectory));
        var image = EditorInterface.Singleton.GetBaseControl().GetViewport().GetTexture().GetImage();
        Require(image.SavePng($"{EvidenceDirectory}/{file}") == Error.Ok, $"Screenshot {file} failed.");
    }
}
