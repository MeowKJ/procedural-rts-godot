using System.Security.Cryptography;
using ProceduralRts.Core;

namespace ProceduralRts;

public static class AuthoredMapPreviewRuntime
{
    public const string CommittedResourcePath = "res://assets/maps/authored-map-preview.mapspec.json";

    public static MapSpec StageVerified(AuthoredMapPreviewRequest request, string projectRoot)
    {
        ArgumentNullException.ThrowIfNull(request);
        var absolute = MapArtifactPathPolicy.RequireAbsolute(projectRoot, request.AbsoluteArtifactPath);
        var bytes = File.ReadAllBytes(absolute);
        return VerifyAndStage(bytes, request.Sha256);
    }

    public static MapSpec StageCommittedSample()
    {
        var artifact = LoadCommittedSample();
        SkirmishSetupState.StageAuthoredMap(artifact.Map);
        return artifact.Map;
    }

    public static CommittedAuthoredMapArtifact LoadCommittedSample()
    {
        using var file = Godot.FileAccess.Open(CommittedResourcePath, Godot.FileAccess.ModeFlags.Read);
        if (file is null) throw new InvalidOperationException("Committed authored preview artifact is unavailable.");
        var bytes = file.GetBuffer((long)file.GetLength());
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var map = MapSpecArtifactCodec.Decode(bytes);
        return new CommittedAuthoredMapArtifact(map, bytes.Length, hash);
    }

    private static MapSpec VerifyAndStage(byte[] bytes, string expectedHash)
    {
        var actualHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
            throw new InvalidOperationException("Authored preview artifact SHA-256 mismatch.");
        var map = MapSpecArtifactCodec.Decode(bytes);
        SkirmishSetupState.StageAuthoredMap(map);
        return map;
    }
}

public sealed record CommittedAuthoredMapArtifact(MapSpec Map, int Length, string Sha256);
