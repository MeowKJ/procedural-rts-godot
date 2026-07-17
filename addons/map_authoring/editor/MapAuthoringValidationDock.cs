using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring.Editor;

[Tool]
public partial class MapAuthoringValidationDock : EditorDock
{
    private readonly Label _status = new();
    private readonly VBoxContainer _rows = new();
    private readonly Button _validate = new();
    private Action? _validateRequested;
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
        _validate.Text = "Validate";
        _validate.TooltipText = "Run deterministic read-only validation for the active MapRoot.";
        _validate.Pressed += OnValidatePressed;
        content.AddChild(_validate);
        _status.Text = "Fresh: not validated";
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_status);
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        scroll.AddChild(_rows);
        content.AddChild(scroll);
    }

    public string StatusText => _status.Text;
    public int DiagnosticRowCount => _rows.GetChildCount();

    public void Bind(
        Action validateRequested,
        Action<MapValidationDiagnostic, bool> navigateRequested)
    {
        _validateRequested = validateRequested;
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

    public void ClearReport()
    {
        ClearRows();
        _status.Text = "Fresh: not validated";
    }

    public override void _ExitTree()
    {
        _validate.Pressed -= OnValidatePressed;
        _validateRequested = null;
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
}
