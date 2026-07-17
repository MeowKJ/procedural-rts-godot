using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Editor;

internal static class MapAuthoringAtomicPathScenarios
{
    public static void Run(MapSpec map, byte[] committed, List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), $"procedural-rts-atomic-{Guid.NewGuid():N}");
        var outside = Path.Combine(Path.GetTempPath(), $"procedural-rts-link-{Guid.NewGuid():N}");
        var maps = Path.Combine(root, "assets", "maps");
        Directory.CreateDirectory(maps);
        Directory.CreateDirectory(outside);
        var path = Path.Combine(maps, "atomic.mapspec.json");
        var target = new MapAuthoringArtifactTarget("res://assets/maps/atomic.mapspec.json", root, path);
        try
        {
            var tracking = new TrackingAtomicFileSystem();
            _ = MapAuthoringArtifactWriter.Write(map, target, atomicFileSystemForQa: tracking);
            Require(tracking.MoveCalls == 1 && tracking.ReplaceCalls == 0,
                "First artifact creation must use same-directory File.Move.", failures);
            _ = MapAuthoringArtifactWriter.Write(map, target, atomicFileSystemForQa: tracking);
            Require(tracking.MoveCalls == 1 && tracking.ReplaceCalls == 1,
                "Existing artifact must use File.Replace.", failures);
            var before = File.ReadAllBytes(path);
            tracking.FailReplace = true;
            Require(Reject(() => MapAuthoringArtifactWriter.Write(
                    map, target, atomicFileSystemForQa: tracking))
                && File.ReadAllBytes(path).SequenceEqual(before)
                && !Directory.EnumerateFiles(maps, "*.tmp").Any(),
                "Replace failure must preserve old bytes and clean the temp file.", failures);
            Require(before.SequenceEqual(committed), "Atomic-path fixture must retain canonical sample bytes.", failures);

            Directory.Delete(Path.Combine(root, "assets"), recursive: true);
            Directory.CreateSymbolicLink(Path.Combine(root, "assets"), outside);
            Require(Reject(() => MapArtifactPathPolicy.ResolveResourcePath(
                    root, "res://assets/maps/escape.mapspec.json")),
                "Path policy must reject an ancestor assets symlink/reparse escape.", failures);
            Directory.Delete(Path.Combine(root, "assets"));
            Directory.CreateDirectory(Path.Combine(root, "assets"));
            Directory.CreateSymbolicLink(Path.Combine(root, "assets", "maps"), outside);
            Require(Reject(() => MapArtifactPathPolicy.ResolveResourcePath(
                    root, "res://assets/maps/escape.mapspec.json")),
                "Path policy must reject the maps-directory symlink/reparse segment.", failures);
            Directory.Delete(Path.Combine(root, "assets", "maps"));
            Directory.CreateDirectory(Path.Combine(root, "assets", "maps"));
            var outsideFile = Path.Combine(outside, "outside.mapspec.json");
            File.WriteAllBytes(outsideFile, committed);
            File.CreateSymbolicLink(path, outsideFile);
            Require(Reject(() => MapArtifactPathPolicy.ResolveResourcePath(
                    root, "res://assets/maps/atomic.mapspec.json")),
                "Path policy must reject a target-file symlink/reparse segment.", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            if (Directory.Exists(outside)) Directory.Delete(outside, recursive: true);
        }
    }

    private static bool Reject(Action action)
    {
        try { action(); return false; }
        catch { return true; }
    }

    private static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add(message);
    }

    private sealed class TrackingAtomicFileSystem : IMapAuthoringAtomicFileSystem
    {
        public int MoveCalls { get; private set; }
        public int ReplaceCalls { get; private set; }
        public bool FailReplace { get; set; }
        public bool Exists(string path) => File.Exists(path);
        public void MoveFirst(string source, string target) { MoveCalls++; File.Move(source, target); }
        public void ReplaceExisting(string source, string target)
        {
            ReplaceCalls++;
            if (FailReplace) throw new IOException("injected replace failure");
            File.Replace(source, target, null);
        }
    }
}
