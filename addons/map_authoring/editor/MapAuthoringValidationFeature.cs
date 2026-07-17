using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Nodes;

namespace ProceduralRts.MapAuthoring.Editor;

public sealed class MapAuthoringValidationFeature : IDisposable
{
    private readonly MapAuthoringPlugin _plugin;
    private readonly EditorSelection _selection;
    private readonly MapAuthoringStaleMonitor _staleMonitor;
    private MapAuthoringValidationDock? _dock;
    private MapAuthoringValidationReport? _report;
    private MapAuthoringOverlayPlan _plan = MapAuthoringOverlayPlan.Empty;
    private bool _disposed;
    private long _generation;

    public static MapAuthoringValidationFeature? Current { get; private set; }

    public MapAuthoringValidationFeature(MapAuthoringPlugin plugin)
    {
        _plugin = plugin;
        _selection = EditorInterface.Singleton.GetSelection();
        _dock = new MapAuthoringValidationDock();
        _dock.Bind(ValidateActiveScene, Navigate);
        _plugin.AddDock(_dock);
        MapAuthoringRegistrationState.DockAdded();
        _plugin.SceneChanged += OnSceneChanged;
        _selection.SelectionChanged += OnSelectionChanged;
        _staleMonitor = new MapAuthoringStaleMonitor(ActiveRoot, () => _report, MarkStale);
        MapAuthoringRegistrationState.FeatureAdded();
        Current = this;
        RebuildPlan();
    }

    public MapAuthoringValidationReport? Report => _report;
    public MapAuthoringOverlayPlan Plan => _plan;
    public MapAuthoringValidationDock? Dock => _dock;

    public void Draw(Control overlay)
    {
        var root = ActiveRoot();
        if (root is null) return;
        MapAuthoringOverlayDrawer.Draw(overlay, root, _plan);
    }

    public void ValidateActiveScene()
    {
        var root = ActiveRoot();
        if (root is null)
        {
            _report = null;
            _plan = MapAuthoringOverlayPlan.Empty;
            _dock?.SetStale("active scene is not a MapRoot");
            _plugin.UpdateOverlays();
            return;
        }
        _report = MapAuthoringValidationRunner.Validate(root, _generation);
        _dock?.ShowReport(_report);
        RebuildPlan();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (ReferenceEquals(Current, this)) Current = null;
        _staleMonitor.Dispose();
        _plugin.SceneChanged -= OnSceneChanged;
        _selection.SelectionChanged -= OnSelectionChanged;
        _report = null;
        _plan = MapAuthoringOverlayPlan.Empty;
        MapAuthoringRegistrationState.FeatureRemoved();
        if (_dock is not null)
        {
            _dock.ClearReport();
            _plugin.RemoveDock(_dock);
            _dock.QueueFree();
            _dock = null;
            MapAuthoringRegistrationState.DockRemoved();
        }
        _plugin.UpdateOverlays();
    }

    private void Navigate(MapValidationDiagnostic diagnostic, bool conflict)
        => NavigateForQa(diagnostic, conflict);

    public bool NavigateForQa(MapValidationDiagnostic diagnostic, bool conflict)
    {
        var root = ActiveRoot();
        var source = conflict ? diagnostic.Conflict : diagnostic.Source;
        if (root is null || source is null || _report is null
            || _report.Generation != _generation
            || root.GetInstanceId() != _report.RootInstanceId
            || (!string.IsNullOrEmpty(_report.ScenePath) && root.SceneFilePath != _report.ScenePath))
        {
            _dock?.SetStale("scene changed; validate again");
            return false;
        }
        var node = root.GetNodeOrNull(new NodePath(source.Path));
        if (node is null)
        {
            _dock?.SetStale("diagnostic node was deleted; validate again");
            return false;
        }
        _selection.Clear();
        _selection.AddNode(node);
        EditorInterface.Singleton.EditNode(node);
        EditorInterface.Singleton.SetMainScreenEditor("2D");
        RebuildPlan();
        return true;
    }

    public void NotifyPropertyEditedForQa(string property)
        => _staleMonitor.NotifyPropertyEditedForQa(property);

    public void RequestOverlayRedrawForQa() => _plugin.UpdateOverlays();

    private void OnSceneChanged(Node sceneRoot)
    {
        if (_report is not null && (sceneRoot is null || sceneRoot.GetInstanceId() != _report.RootInstanceId))
            MarkStale("scene changed; validate again");
        RebuildPlan();
    }

    private void OnSelectionChanged() => RebuildPlan();

    private void RebuildPlan()
    {
        var root = ActiveRoot();
        if (root is null)
        {
            _plan = MapAuthoringOverlayPlan.Empty;
        }
        else
        {
            var applicableReport = _report?.RootInstanceId == root.GetInstanceId()
                && _report.Generation == _generation ? _report : null;
            var sources = applicableReport?.Sources ?? MapAuthoringSourceIndex.Build(root);
            var selected = _selection.GetSelectedNodes().OfType<Node>().FirstOrDefault();
            _plan = MapAuthoringOverlayPlanner.Build(root, sources, applicableReport, selected);
        }
        _plugin.UpdateOverlays();
    }

    private static MapRoot? ActiveRoot() => EditorInterface.Singleton.GetEditedSceneRoot() as MapRoot;

    private void MarkStale(string reason)
    {
        if (_report is null || _report.Generation != _generation) return;
        _generation++;
        _dock?.SetStale(reason);
        RebuildPlan();
    }
}
