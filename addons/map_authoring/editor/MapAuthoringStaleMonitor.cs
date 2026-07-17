using Godot;
using ProceduralRts.MapAuthoring.Nodes;

namespace ProceduralRts.MapAuthoring.Editor;

public sealed class MapAuthoringStaleMonitor : IDisposable
{
    private readonly EditorInspector _inspector;
    private readonly SceneTree _tree;
    private readonly Func<MapRoot?> _activeRoot;
    private readonly Func<MapAuthoringValidationReport?> _report;
    private readonly Action<string> _markStale;

    public MapAuthoringStaleMonitor(
        Func<MapRoot?> activeRoot,
        Func<MapAuthoringValidationReport?> report,
        Action<string> markStale)
    {
        _activeRoot = activeRoot;
        _report = report;
        _markStale = markStale;
        _inspector = EditorInterface.Singleton.GetInspector();
        _tree = EditorInterface.Singleton.GetBaseControl().GetTree();
        _inspector.PropertyEdited += OnPropertyEdited;
        _tree.NodeAdded += OnNodeAdded;
        _tree.NodeRemoved += OnNodeRemoved;
        _tree.NodeRenamed += OnNodeRenamed;
    }

    public void Dispose()
    {
        _inspector.PropertyEdited -= OnPropertyEdited;
        _tree.NodeAdded -= OnNodeAdded;
        _tree.NodeRemoved -= OnNodeRemoved;
        _tree.NodeRenamed -= OnNodeRenamed;
    }

    public void NotifyPropertyEditedForQa(string property) => OnPropertyEdited(property);

    private void OnPropertyEdited(string property)
    {
        if (_inspector.GetEditedObject() is Node node && IsWithinValidatedRoot(node))
            _markStale($"property {property} changed; validate again");
    }

    private void OnNodeAdded(Node node)
    {
        if (IsWithinActiveRoot(node)) _markStale("scene node added or reparented; validate again");
    }

    private void OnNodeRemoved(Node node)
    {
        if (TouchesValidatedSources(node)) _markStale("scene node removed or reparented; validate again");
    }

    private void OnNodeRenamed(Node node)
    {
        if (IsWithinValidatedRoot(node)) _markStale("scene node renamed; validate again");
    }

    private bool IsWithinActiveRoot(Node node)
    {
        var root = _activeRoot();
        return root is not null && (node == root || root.IsAncestorOf(node));
    }

    private bool IsWithinValidatedRoot(Node node)
    {
        var report = _report();
        return report is not null && IsWithinActiveRoot(node);
    }

    private bool TouchesValidatedSources(Node node)
    {
        var report = _report();
        if (report is null) return false;
        return report.Sources.Entries.Any(entry =>
            GodotObject.IsInstanceValid(entry.Node)
            && (entry.Node == node || node.IsAncestorOf(entry.Node)));
    }
}
