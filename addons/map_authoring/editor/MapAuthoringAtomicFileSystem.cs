namespace ProceduralRts.MapAuthoring.Editor;

public interface IMapAuthoringAtomicFileSystem
{
    bool Exists(string path);
    void MoveFirst(string source, string target);
    void ReplaceExisting(string source, string target);
}

public sealed class MapAuthoringAtomicFileSystem : IMapAuthoringAtomicFileSystem
{
    public static MapAuthoringAtomicFileSystem Instance { get; } = new();

    public bool Exists(string path) => File.Exists(path);
    public void MoveFirst(string source, string target) => File.Move(source, target);
    public void ReplaceExisting(string source, string target) => File.Replace(source, target, null);
}
