using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Editor;
using ProceduralRts.MapAuthoring.Nodes;
using GodotFileAccess = Godot.FileAccess;

namespace ProceduralRts.MapAuthoring.Qa;

static class MapAuthoringEditorPersistenceSmoke
{
    private const string SourceScenePath = "res://addons/map_authoring/qa/MapAuthoringEditorAcceptance.tscn";
    private const string TempScenePath = "res://artifacts/map-authoring-editor-persistence-qa.tscn";

    public static async Task Run(Func<Task> nextFrame)
    {
        CopyFixture();
        try
        {
            EditorInterface.Singleton.OpenSceneFromPath(TempScenePath);
            await nextFrame();
            var building = RequireBuilding();
            Select(building);
            await nextFrame();

            var catalogProperty = FindProperty<MapCatalogOptionProperty>("BuildingId");
            var catalogControl = catalogProperty.GetNode<OptionButton>("CatalogOptions");
            SelectItem(catalogControl, BuildingDesignIds.Barracks);
            await nextFrame();
            Require(building.BuildingId == BuildingDesignIds.Barracks,
                "Actual Inspector catalog selection must update the edited Building.");

            var rotationProperty = FindProperty<MapQuarterTurnProperty>("rotation");
            rotationProperty.GetNode<OptionButton>("QuarterTurns")
                .EmitSignal(OptionButton.SignalName.ItemSelected, 1);
            await nextFrame();
            Require(Mathf.IsEqualApprox(building.Rotation, MapBuildingQuarterTurns.All[1].Radians),
                "Actual Inspector quarter-turn selection must update the edited Building.");

            Require(EditorInterface.Singleton.SaveScene() == Error.Ok,
                "Inspector-edited persistence fixture must save successfully.");
            EditorInterface.Singleton.ReloadSceneFromPath(TempScenePath);
            await nextFrame();
            building = RequireBuilding();
            Require(building.BuildingId == BuildingDesignIds.Barracks,
                "Catalog id must persist after editor save and reload.");
            Require(Mathf.IsEqualApprox(building.Rotation, MapBuildingQuarterTurns.All[1].Radians),
                "Quarter-turn must persist after editor save and reload.");
        }
        finally
        {
            EditorInterface.Singleton.GetSelection().Clear();
            var editedRoot = EditorInterface.Singleton.GetEditedSceneRoot();
            if (editedRoot?.SceneFilePath == TempScenePath)
            {
                var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
                undoRedo.ClearHistory(undoRedo.GetObjectHistoryId(editedRoot), increaseVersion: false);
                EditorInterface.Singleton.CloseScene();
                await nextFrame();
            }
            EditorInterface.Singleton.OpenSceneFromPath(SourceScenePath);
            await nextFrame();
            RemoveTempFixture();
        }
    }

    private static void CopyFixture()
    {
        var bytes = GodotFileAccess.GetFileAsBytes(SourceScenePath);
        Require(bytes.Length > 0, "Editor persistence source fixture must be readable.");
        var absoluteDirectory = ProjectSettings.GlobalizePath("res://artifacts");
        _ = DirAccess.MakeDirRecursiveAbsolute(absoluteDirectory);
        using var file = GodotFileAccess.Open(TempScenePath, GodotFileAccess.ModeFlags.Write)
            ?? throw new InvalidOperationException("Editor persistence fixture could not be created.");
        file.StoreBuffer(bytes);
    }

    private static Building RequireBuilding()
    {
        return EditorInterface.Singleton.GetEditedSceneRoot()?.GetNodeOrNull<Building>("Headquarters")
            ?? throw new InvalidOperationException("Editor persistence fixture must contain Headquarters Building.");
    }

    private static void Select(Building building)
    {
        var selection = EditorInterface.Singleton.GetSelection();
        selection.Clear();
        selection.AddNode(building);
    }

    private static T FindProperty<T>(string propertyName) where T : EditorProperty
    {
        return Descendants<T>(EditorInterface.Singleton.GetInspector())
            .Single(property => Matches(property.GetEditedProperty().ToString(), propertyName));
    }

    private static void SelectItem(OptionButton control, string value)
    {
        var index = Enumerable.Range(0, control.ItemCount)
            .Single(item => control.GetItemText(item) == value);
        control.EmitSignal(OptionButton.SignalName.ItemSelected, index);
    }

    private static bool Matches(string actual, string pascalCase)
    {
        return actual == pascalCase || actual == pascalCase.ToSnakeCase();
    }

    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    private static void RemoveTempFixture()
    {
        var absolutePath = ProjectSettings.GlobalizePath(TempScenePath);
        if (GodotFileAccess.FileExists(TempScenePath)) _ = DirAccess.RemoveAbsolute(absolutePath);
        if (GodotFileAccess.FileExists(TempScenePath + ".uid")) _ = DirAccess.RemoveAbsolute(absolutePath + ".uid");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
