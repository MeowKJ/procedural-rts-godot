using ProceduralRts.MapAuthoring.Nodes;

namespace ProceduralRts.MapAuthoring.Editor;

public sealed partial class MapAuthoringValidationFeature
{
    private MapAuthoringBakeResult? _lastBake;

    public MapAuthoringBakeResult? LastBake => _lastBake;
    public int? OwnedPlayPid => _playSession.OwnedPid;

    public MapAuthoringBakeResult? BakeActiveScene()
    {
        var evaluation = EvaluateFresh();
        if (evaluation is null || !evaluation.IsClean)
        {
            var count = evaluation?.Report.Diagnostics.Count ?? 0;
            _dock?.SetOperationError($"current map has {count} diagnostic(s); Bake blocked");
            return null;
        }

        try
        {
            var root = ActiveRoot()!;
            var target = MapAuthoringArtifactPath.Resolve(root.ArtifactPath);
            var baked = MapAuthoringArtifactWriter.Write(evaluation.CleanMap!, target);
            _lastBake = baked;
            _dock?.ShowArtifact(baked, _playSession.IsRunning);
            return baked;
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or InvalidOperationException)
        {
            _dock?.SetOperationError(exception.Message);
            return null;
        }
    }

    public void TogglePlayActiveScene()
    {
        if (_playSession.IsRunning)
        {
            _playSession.Stop();
            if (_lastBake is not null) _dock?.ShowArtifact(_lastBake, playing: false);
            else _dock?.SetPlaying(playing: false);
            return;
        }

        var baked = BakeActiveScene();
        if (baked is null) return;
        try
        {
            _playSession.Start(baked);
            _dock?.ShowArtifact(baked, playing: true);
        }
        catch (InvalidOperationException exception)
        {
            _dock?.SetOperationError(exception.Message);
            _dock?.SetPlaying(playing: false);
        }
    }

    public void PollPlaySession()
    {
        if (!_playSession.Poll()) return;
        if (_lastBake is not null) _dock?.ShowArtifact(_lastBake, playing: false);
        else _dock?.SetPlaying(playing: false);
    }

    private MapAuthoringEvaluation? EvaluateFresh()
    {
        var root = ActiveRoot();
        if (root is null)
        {
            _report = null;
            _plan = MapAuthoringOverlayPlan.Empty;
            _dock?.SetStale("active scene is not a MapRoot");
            _plugin.UpdateOverlays();
            return null;
        }

        var evaluation = MapAuthoringValidationRunner.Evaluate(root, _generation);
        _report = evaluation.Report;
        _dock?.ShowReport(_report);
        RebuildPlan();
        return evaluation;
    }

}
