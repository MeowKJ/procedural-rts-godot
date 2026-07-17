using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring.Editor;

public sealed record MapAuthoringBakeResult(
    string ResourcePath,
    string AbsolutePath,
    int Length,
    string Sha256);

public static class MapAuthoringArtifactWriter
{
    public static MapAuthoringBakeResult Write(
        MapSpec cleanMap,
        MapAuthoringArtifactTarget target,
        Action? beforeReplaceForQa = null,
        IMapAuthoringAtomicFileSystem? atomicFileSystemForQa = null)
    {
        ArgumentNullException.ThrowIfNull(cleanMap);
        ArgumentNullException.ThrowIfNull(target);
        var artifact = MapSpecArtifactCodec.Encode(cleanMap);
        var expected = artifact.ToArray();
        var directory = Path.GetDirectoryName(target.AbsolutePath)
            ?? throw new InvalidOperationException("Artifact target has no directory.");
        Directory.CreateDirectory(directory);
        _ = MapArtifactPathPolicy.RequireAbsolute(target.ProjectRoot, target.AbsolutePath);
        var atomic = atomicFileSystemForQa ?? MapAuthoringAtomicFileSystem.Instance;
        var temporary = Path.Combine(
            directory, $".{Path.GetFileName(target.AbsolutePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(
                temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 4096, FileOptions.WriteThrough))
            {
                stream.Write(expected);
                stream.Flush(flushToDisk: true);
            }

            var persisted = File.ReadAllBytes(temporary);
            if (!persisted.SequenceEqual(expected))
                throw new IOException("Temporary MapSpec bytes differ from the canonical artifact.");
            var decoded = MapSpecArtifactCodec.Decode(persisted);
            var verified = MapSpecArtifactCodec.Encode(decoded);
            if (verified.Sha256 != artifact.Sha256 || !verified.ToArray().SequenceEqual(expected))
                throw new IOException("Temporary MapSpec failed strict canonical read-back verification.");

            beforeReplaceForQa?.Invoke();
            if (atomic.Exists(target.AbsolutePath))
                atomic.ReplaceExisting(temporary, target.AbsolutePath);
            else
                atomic.MoveFirst(temporary, target.AbsolutePath);
            return new MapAuthoringBakeResult(
                target.ResourcePath, target.AbsolutePath, artifact.Length, artifact.Sha256);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
