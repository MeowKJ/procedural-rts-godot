using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Editor;
using ProceduralRts.MapAuthoring.Nodes;
using AuthoringResource = ProceduralRts.MapAuthoring.Nodes.Resource;

namespace ProceduralRts.MapAuthoring.Qa;

public partial class MapAuthoringValidationSmokeDriver
{
    private const string AcceptanceScene = "res://addons/map_authoring/qa/MapAuthoringEditorAcceptance.tscn";
    private const string ConflictScene = "res://addons/map_authoring/qa/MapAuthoringValidationConflictAcceptance.tscn";
    private const string MixedScene = "res://addons/map_authoring/qa/MapAuthoringMixedOverlayAcceptance.tscn";
    private async Task ValidateOverlayScenarios(MapAuthoringValidationFeature feature)
    {
        EditorInterface.Singleton.OpenSceneFromPath(AcceptanceScene);
        await NextFrame();
        var root = ActiveRoot();
        await RequireForceDrawAfterSelectionClear(feature, "initial registration");
        Require(feature.Report is null || feature.Report.RootInstanceId != root.GetInstanceId(),
            "Fresh scene must not reuse a prior validation report.");
        Require(feature.Plan.Primitives.Any(value => value.Kind == MapOverlayPrimitiveKind.HardFootprint),
            "Building overlays must exist before first Validate with no selection.");
        RequireGridParity(feature.Plan);
        var before = Fingerprint(root);
        feature.ValidateActiveScene();
        Require(before == Fingerprint(root), "Validation must not mutate the edited scene.");
        Require(feature.Dock?.StatusText.StartsWith("Fresh:", StringComparison.Ordinal) == true,
            "Dock must show fresh state after validation.");
        var expected = Enum.GetValues<MapOverlayPrimitiveKind>()
            .Where(value => value != MapOverlayPrimitiveKind.InvalidBuildingFallback);
        var kinds = feature.Plan.Primitives.Select(value => value.Kind).ToHashSet();
        Require(expected.All(kinds.Contains), $"Overlay plan incomplete: {string.Join(',', expected.Except(kinds))}.");
        await CaptureRotatedBuilding(
            feature, root.GetNode<Building>("RotatedRefinery"),
            "rotated-footprint-clearance-reservations.png");
        await SelectAndCapture(root.GetNode<AuthoringResource>("NorthField"), "environment-markers.png");
        if (DisplayServer.GetName() != "headless")
        {
            await ValidateRealInspectorPropertyStale(feature, root.GetNode<Building>("Barracks"));
            EditorInterface.Singleton.ReloadSceneFromPath(AcceptanceScene);
            await NextFrame();
        }

        EditorInterface.Singleton.OpenSceneFromPath(MixedScene);
        await NextFrame();
        Require(feature.Plan.Primitives.Any(value => value.Kind == MapOverlayPrimitiveKind.HardFootprint)
            && feature.Plan.Primitives.Any(value => value.Kind == MapOverlayPrimitiveKind.InvalidBuildingFallback),
            "One invalid building must retain valid sibling geometry plus its own fallback primitive.");
        feature.ValidateActiveScene();
        Require(feature.Report?.Diagnostics.Any(value => value.Code == MapValidationCodes.CatalogUnknown) == true,
            "Mixed overlay fixture must retain its deliberate authoring diagnostic.");
    }

    private async Task ValidateConflictAndStaleScenarios(MapAuthoringValidationFeature feature)
    {
        EditorInterface.Singleton.OpenSceneFromPath(ConflictScene);
        await NextFrame();
        feature.ValidateActiveScene();
        var pair = PairDiagnostic(feature);
        await Capture("diagnostic-dock.png");
        Require(feature.NavigateForQa(pair, conflict: false),
            $"Source navigation must succeed; dock={feature.Dock?.StatusText}.");
        await Capture("source-selection.png");
        Require(SelectedName() == pair.Source.Path,
            $"Source navigation expected {pair.Source.Path}, got {SelectedName()} in {ActiveRoot().Name}.");
        Require(feature.NavigateForQa(pair, conflict: true), "Conflict navigation must succeed.");
        await Capture("conflict-selection.png");
        Require(SelectedName() == pair.Conflict?.Path,
            $"Conflict navigation expected {pair.Conflict?.Path}, got {SelectedName()} in {ActiveRoot().Name}.");

        if (DisplayServer.GetName() == "headless")
        {
            var edited = EditorInterface.Singleton.GetSelection().GetSelectedNodes().OfType<Building>().Single();
            edited.Hp += 1;
            feature.NotifyPropertyEditedForQa("Hp");
            await NextFrame();
            RequireStale(feature, pair, "property edit");
            feature.ValidateActiveScene(); pair = PairDiagnostic(feature);
        }
        var root = ActiveRoot();
        var first = root.GetNode<Building>(pair.Source.Path);
        var originalName = first.Name;
        first.Name = "RenamedBuilding";
        await NextFrame();
        RequireStale(feature, pair, "rename");
        first.Name = originalName;
        await NextFrame();
        feature.ValidateActiveScene(); pair = PairDiagnostic(feature);
        var group = root.GetNode<Node2D>("ReparentGroup");
        feature.ValidateActiveScene(); pair = PairDiagnostic(feature);
        first.Reparent(group);
        await NextFrame();
        RequireStale(feature, pair, "reparent");
        first.Reparent(root);
        await NextFrame();
        feature.ValidateActiveScene(); pair = PairDiagnostic(feature);
        EditorInterface.Singleton.ReloadSceneFromPath(ConflictScene);
        await NextFrame();
        RequireStale(feature, pair, "reload");
    }

    private async Task ValidateLifecycleAndCapture()
    {
        EditorInterface.Singleton.SetPluginEnabled(PluginName, false);
        await NextFrame(); RequireLifecycle(active: false);
        EditorInterface.Singleton.SetPluginEnabled(PluginName, true);
        await NextFrame(); RequireLifecycle(active: true);
        var feature = RequireFeature();
        EditorInterface.Singleton.OpenSceneFromPath(AcceptanceScene);
        await NextFrame();
        await RequireForceDrawAfterSelectionClear(feature, "re-enabled registration");
        feature.ValidateActiveScene();
        await Capture("post-reenable-clean.png");
    }

    private async Task ValidateRealInspectorPropertyStale(
        MapAuthoringValidationFeature feature, Building building)
    {
        EditorInterface.Singleton.GetSelection().Clear();
        EditorInterface.Singleton.GetSelection().AddNode(building);
        await NextFrame();
        var property = Descendants<MapCatalogOptionProperty>(EditorInterface.Singleton.GetInspector())
            .Single(value => value.GetEditedProperty().ToString() is "BuildingId" or "building_id");
        var control = property.GetNode<OptionButton>("CatalogOptions");
        var index = Enumerable.Range(0, control.ItemCount)
            .Single(value => control.GetItemText(value) == BuildingDesignIds.Airfield);
        control.EmitSignal(OptionButton.SignalName.ItemSelected, index);
        await NextFrame();
        Require(building.BuildingId == BuildingDesignIds.Airfield,
            "Real Inspector catalog edit must update the selected Building.");
        var source = feature.Report?.Sources.Entries.Single(value => value.Node == building).Source
            ?? throw new InvalidOperationException("Validated building source missing.");
        var probe = MapValidationService.AuthoringDiagnostic(
            MapValidationCodes.CatalogUnknown, source, "stale_probe");
        RequireStale(feature, probe, "real Inspector property edit");
    }

    private static MapValidationDiagnostic PairDiagnostic(MapAuthoringValidationFeature feature)
    {
        var pair = feature.Report?.Diagnostics.FirstOrDefault(value => value.Code == MapValidationCodes.BuildingOverlap);
        if (pair is null)
        {
            throw new InvalidOperationException(
                $"Pair conflict diagnostic missing; root={EditorInterface.Singleton.GetEditedSceneRoot()?.Name} "
                + $"codes={string.Join(',', feature.Report?.Diagnostics.Select(value => value.Code) ?? [])}.");
        }
        Require(pair.Source.Index == 0 && pair.Conflict?.Index == 1
            && pair.Source.Path != pair.Conflict.Path
            && ActiveRoot().GetNodeOrNull(pair.Source.Path) is Building
            && ActiveRoot().GetNodeOrNull(pair.Conflict.Path) is Building,
            $"Pair diagnostic must retain exact Building[0]/Building[1] paths, got "
            + $"{pair.Source.Index}:{pair.Source.Path}/{pair.Conflict?.Index}:{pair.Conflict?.Path}.");
        return pair;
    }

    private static void RequireStale(
        MapAuthoringValidationFeature feature, MapValidationDiagnostic diagnostic, string action)
    {
        Require(!feature.NavigateForQa(diagnostic, conflict: false), $"{action} must block stale navigation.");
        Require(feature.Dock?.StatusText.StartsWith("Stale:", StringComparison.Ordinal) == true,
            $"{action} must expose explicit stale state.");
    }

    private static void RequireGridParity(MapAuthoringOverlayPlan plan)
    {
        var vertical = plan.Primitives.Where(value => value.Kind == MapOverlayPrimitiveKind.Grid
            && Mathf.IsEqualApprox(value.Start.X, value.End.X)).Select(value => value.Start.X).Order().Take(2).ToArray();
        Require(vertical.Length == 2 && Mathf.IsEqualApprox(vertical[1] - vertical[0], PlacementMath.GridSize),
            "Overlay grid must use PlacementMath.GridSize.");
    }

    private static string SelectedName() => EditorInterface.Singleton.GetSelection()
        .GetSelectedNodes().OfType<Node>().Single().Name;
    private static MapRoot ActiveRoot() => EditorInterface.Singleton.GetEditedSceneRoot() as MapRoot
        ?? throw new InvalidOperationException("Active scene must be MapRoot.");
    private static string Fingerprint(MapRoot root) => string.Join('|',
        MapSceneProjection.SceneOrder(root).Select(node => node is Node2D value
            ? $"{node.GetPath()}:{value.Position:R}:{value.Rotation:R}:{value.Scale:R}" : node.GetPath().ToString()));
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
