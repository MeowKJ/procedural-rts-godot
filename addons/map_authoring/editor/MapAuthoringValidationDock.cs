using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring.Editor;

[Tool]
public partial class MapAuthoringValidationDock : EditorDock
{
    private readonly Label _status = new();
    private readonly VBoxContainer _rows = new();
    private readonly Button _validate = new();
    private readonly Button _bake = new();
    private readonly Button _play = new();
    private Action? _validateRequested;
    private Action? _bakeRequested;
    private Action? _playRequested;
    private Action<MapValidationDiagnostic, bool>? _navigateRequested;

    public MapAuthoringValidationDock()
    {
        Name = "MapValidation";
        Title = "Map Validation";
        LayoutKey = "procedural_rts_map_validation";
        DefaultSlot = DockSlot.RightBl;
        AvailableLayouts = DockLayout.Vertical | DockLayout.Floating;
        CustomMinimumSize = new Vector2(300, 240);

        var content = new VBoxContainer();
        content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(content);
        var actions = new HBoxContainer();
        content.AddChild(actions);
        _validate.Text = "Validate";
        _validate.TooltipText = "Run deterministic read-only validation for the active MapRoot.";
        _validate.Pressed += OnValidatePressed;
        _validate.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        actions.AddChild(_validate);
        _bake.Text = "Bake";
        _bake.TooltipText = "Freshly validate and atomically write the canonical MapSpec artifact.";
        _bake.Pressed += OnBakePressed;
        _bake.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        actions.AddChild(_bake);
        _play.Text = "Play";
        _play.TooltipText = "Freshly validate, bake, and launch an isolated authored preview process.";
        _play.Pressed += OnPlayPressed;
        _play.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        actions.AddChild(_play);
        _status.Text = "Fresh: not validated";
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_status);
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        scroll.AddChild(_rows);
        content.AddChild(scroll);
    }

    public string StatusText => _status.Text;
    public int DiagnosticRowCount => _rows.GetChildCount();
    public string PlayButtonText => _play.Text;

    public void Bind(
        Action validateRequested,
        Action bakeRequested,
        Action playRequested,
        Action<MapValidationDiagnostic, bool> navigateRequested)
    {
        _validateRequested = validateRequested;
        _bakeRequested = bakeRequested;
        _playRequested = playRequested;
        _navigateRequested = navigateRequested;
    }

    public void ShowReport(MapAuthoringValidationReport report)
    {
        ClearRows();
        _status.Text = report.Diagnostics.Count == 0
            ? "Fresh: no diagnostics"
            : $"Fresh: {report.Diagnostics.Count} error(s)";
        foreach (var diagnostic in report.Diagnostics)
        {
            _rows.AddChild(CreateRow(diagnostic));
        }
    }

    public void SetStale(string reason)
    {
        _status.Text = $"Stale: {reason}";
    }

    public void ShowArtifact(MapAuthoringBakeResult artifact, bool playing)
    {
        _status.Text = $"Fresh: {artifact.ResourcePath} · {artifact.Length} bytes · sha256 {artifact.Sha256}";
        SetPlaying(playing);
    }

    public void SetOperationError(string message)
    {
        _status.Text = $"Blocked: {message}";
    }

    public void SetPlaying(bool playing)
    {
        _play.Text = playing ? "Stop" : "Play";
        _play.TooltipText = playing
            ? "Stop only the authored preview process owned by this dock."
            : "Freshly validate, bake, and launch an isolated authored preview process.";
    }

    public void ClearReport()
    {
        ClearRows();
        _status.Text = "Fresh: not validated";
    }

    public override void _ExitTree()
    {
        _validate.Pressed -= OnValidatePressed;
        _bake.Pressed -= OnBakePressed;
        _play.Pressed -= OnPlayPressed;
        _validateRequested = null;
        _bakeRequested = null;
        _playRequested = null;
        _navigateRequested = null;
    }

    private Control CreateRow(MapValidationDiagnostic diagnostic)
    {
        var row = new VBoxContainer { TooltipText = diagnostic.Message };
        row.AddChild(new Label
        {
            Text = $"{diagnostic.Code} · {diagnostic.Source.Id}",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        var actions = new HBoxContainer();
        actions.AddChild(NavigationButton("Source", diagnostic, conflict: false));
        if (diagnostic.Conflict is not null)
        {
            actions.AddChild(NavigationButton("Conflict", diagnostic, conflict: true));
        }
        row.AddChild(actions);
        return row;
    }

    private Button NavigationButton(string text, MapValidationDiagnostic diagnostic, bool conflict)
    {
        var button = new Button { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        button.Pressed += () => _navigateRequested?.Invoke(diagnostic, conflict);
        return button;
    }

    private void ClearRows()
    {
        foreach (var child in _rows.GetChildren())
        {
            _rows.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void OnValidatePressed() => _validateRequested?.Invoke();
    private void OnBakePressed() => _bakeRequested?.Invoke();
    private void OnPlayPressed() => _playRequested?.Invoke();
}
