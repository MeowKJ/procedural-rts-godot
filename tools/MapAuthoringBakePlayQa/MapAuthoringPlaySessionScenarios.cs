using Godot;
using ProceduralRts.MapAuthoring.Editor;

internal static class MapAuthoringPlaySessionScenarios
{
    public static void Run(MapAuthoringBakeResult artifact, string root, List<string> failures)
    {
        var graphical = MapAuthoringPlayArguments.Build(root, artifact, headless: false).ToArray();
        var headless = MapAuthoringPlayArguments.Build(root, artifact, headless: true).ToArray();
        Require(graphical.SequenceEqual(new[]
        {
            "--path", Path.GetFullPath(root), "--scene", MapAuthoringPlaySession.BootstrapScene, "--",
            "--authored-map-preview", artifact.AbsolutePath, "--authored-map-sha256", artifact.Sha256,
        }), "Graphical editor Play arguments changed.", failures);
        Require(headless.SequenceEqual(new[] { "--headless" }.Concat(graphical)),
            "Headless editor Play must add exactly one --headless argument.", failures);

        var race = new FakeProcess([true, false], Error.Failed);
        var raceSession = Session(race, root);
        raceSession.Start(artifact);
        raceSession.Stop();
        Require(raceSession.OwnedPid is null,
            "Non-OK Kill must succeed when the owned child disappeared during Stop.", failures);

        var alive = new FakeProcess([true, true, true, true], Error.Failed);
        var aliveSession = Session(alive, root);
        aliveSession.Start(artifact);
        Require(Reject(aliveSession.Stop) && aliveSession.OwnedPid == FakeProcess.Pid,
            "Failed Stop must retain ownership while the child remains alive.", failures);
        var cleanupContinued = false;
        aliveSession.Dispose();
        cleanupContinued = true;
        Require(cleanupContinued && aliveSession.LastDisposeError is not null
            && aliveSession.OwnedPid == FakeProcess.Pid,
            "Teardown must continue after Stop failure without dropping live PID ownership.", failures);

        var natural = new FakeProcess([false], Error.Ok);
        var naturalSession = Session(natural, root);
        naturalSession.Start(artifact);
        Require(naturalSession.Poll() && naturalSession.OwnedPid is null,
            "Natural child exit must clear ownership deterministically.", failures);
    }

    private static MapAuthoringPlaySession Session(FakeProcess process, string root)
        => new(process, () => false, () => root, () => false);
    private static bool Reject(Action action)
    {
        try { action(); return false; }
        catch { return true; }
    }
    private static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add(message);
    }

    private sealed class FakeProcess(IEnumerable<bool> running, Error killResult) : IMapAuthoringPlayProcess
    {
        public const int Pid = 569;
        private readonly Queue<bool> _running = new(running);
        public int Create(IReadOnlyList<string> arguments) => Pid;
        public bool IsRunning(int pid) => _running.Count > 0 && _running.Dequeue();
        public Error Kill(int pid) => killResult;
    }
}
