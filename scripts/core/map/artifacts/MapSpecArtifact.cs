using System.Security.Cryptography;
using System.Text;

namespace ProceduralRts.Core;

public sealed class MapSpecArtifactException : Exception
{
    public MapSpecArtifactException(string message)
        : base(message)
    {
    }

    public MapSpecArtifactException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class MapSpecArtifact
{
    private readonly byte[] _bytes;

    internal MapSpecArtifact(byte[] bytes)
    {
        _bytes = bytes.ToArray();
        Sha256 = Convert.ToHexString(SHA256.HashData(_bytes)).ToLowerInvariant();
    }

    public string Sha256 { get; }

    public int Length => _bytes.Length;

    public byte[] ToArray()
    {
        return _bytes.ToArray();
    }
}

public static class MapSpecArtifactCodec
{
    public const string Format = "procedural-rts.mapspec";
    public const int SchemaVersion = 1;

    public static MapSpecArtifact Encode(MapSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var snapshot = MapSpecSnapshot.Create(spec);
        MapLoader.Prepare(snapshot);
        return new MapSpecArtifact(MapSpecArtifactWriter.Write(snapshot));
    }

    public static MapSpec Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            throw new MapSpecArtifactException("MapSpec artifact is empty.");
        }

        try
        {
            var parsed = MapSpecArtifactReader.Read(bytes);
            MapLoader.Prepare(parsed);
            var canonical = Encode(parsed).ToArray();
            if (!bytes.SequenceEqual(canonical))
            {
                throw new MapSpecArtifactException("MapSpec artifact is not canonical schema-v1 UTF-8.");
            }

            return MapSpecSnapshot.Create(parsed);
        }
        catch (MapSpecArtifactException)
        {
            throw;
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException
            or OverflowException
            or FormatException
            or DecoderFallbackException)
        {
            throw new MapSpecArtifactException("MapSpec artifact is malformed.", exception);
        }
    }

}
