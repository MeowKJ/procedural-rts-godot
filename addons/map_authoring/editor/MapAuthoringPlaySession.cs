using Godot;

namespace ProceduralRts.MapAuthoring.Editor;

public sealed class MapAuthoringPlaySession : IDisposable
{
    public const string BootstrapScene = "res://scenes/AuthoredMapPreviewBootstrap.tscn";
    private readonly IMapAuthoringPlayProcess _process;
    private readonly Func<bool> _unrelatedEditorRun;
    private readonly Func<string> _projectRoot;
    private readonly Func<bool> _headless;
    private int _ownedPid = -1;

    public MapAuthoringPlaySession()
        : this(
            new GodotMapAuthoringPlayProcess(),
            () => EditorInterface.Singleton.IsPlayingScene(),
            () => ProjectSettings.GlobalizePath("res://"),
            () => DisplayServer.GetName() == "headless")
    {
    }

    public MapAuthoringPlaySession(
        IMapAuthoringPlayProcess process,
        Func<bool> unrelatedEditorRun,
        Func<string> projectRoot,
        Func<bool> headless)
    {
        _process = process;
        _unrelatedEditorRun = unrelatedEditorRun;
        _projectRoot = projectRoot;
        _headless = headless;
    }

    public int? OwnedPid => _ownedPid > 0 ? _ownedPid : null;
    public bool IsRunning => _ownedPid > 0 && _process.IsRunning(_ownedPid);
    public Exception? LastDisposeError { get; private set; }

    public void Start(MapAuthoringBakeResult artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        Poll();
        if (_ownedPid > 0) throw new InvalidOperationException("An authored preview process is already owned.");
        if (_unrelatedEditorRun())
            throw new InvalidOperationException("Stop the unrelated editor-run game before authored preview Play.");

        var arguments = MapAuthoringPlayArguments.Build(_projectRoot(), artifact, _headless());
        var pid = _process.Create(arguments);
        if (pid <= 0) throw new InvalidOperationException("Godot failed to spawn the authored preview process.");
        _ownedPid = pid;
    }

    public bool Poll()
    {
        if (_ownedPid <= 0 || _process.IsRunning(_ownedPid)) return false;
        _ownedPid = -1;
        return true;
    }

    public void Stop()
    {
        if (_ownedPid <= 0) return;
        if (!_process.IsRunning(_ownedPid)) { _ownedPid = -1; return; }
        var error = _process.Kill(_ownedPid);
        if (error == Error.Ok || !_process.IsRunning(_ownedPid)) { _ownedPid = -1; return; }
        throw new InvalidOperationException($"Failed to stop owned preview PID {_ownedPid}: {error}.");
    }

    public void Dispose()
    {
        try { Stop(); }
        catch (Exception exception) { LastDisposeError = exception; }
    }
}
