using Godot;

namespace ProceduralRts.MapAuthoring.Editor;

public interface IMapAuthoringPlayProcess
{
    int Create(IReadOnlyList<string> arguments);
    bool IsRunning(int pid);
    Error Kill(int pid);
}

public sealed class GodotMapAuthoringPlayProcess : IMapAuthoringPlayProcess
{
    public int Create(IReadOnlyList<string> arguments)
        => OS.CreateProcess(OS.GetExecutablePath(), arguments.ToArray());
    public bool IsRunning(int pid) => OS.IsProcessRunning(pid);
    public Error Kill(int pid) => OS.Kill(pid);
}

public static class MapAuthoringPlayArguments
{
    public static IReadOnlyList<string> Build(
        string projectRoot, MapAuthoringBakeResult artifact, bool headless)
    {
        var result = new List<string>();
        if (headless) result.Add("--headless");
        result.AddRange([
            "--path", Path.GetFullPath(projectRoot),
            "--scene", MapAuthoringPlaySession.BootstrapScene,
            "--",
            "--authored-map-preview", artifact.AbsolutePath,
            "--authored-map-sha256", artifact.Sha256,
        ]);
        return result.AsReadOnly();
    }
}
