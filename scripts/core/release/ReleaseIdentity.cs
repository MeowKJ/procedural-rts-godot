using System.Text.Json;
using Godot;

namespace ProceduralRts.Core;

public sealed record ReleaseIdentity(
    string Version,
    string Tag,
    string WindowsFileVersion,
    string Godot,
    string Target,
    string SampleMapId,
    string SampleMapHash)
{
    public const string ResourcePath = "res://assets/release/release-identity.json";

    private static readonly Lazy<ReleaseIdentity> CurrentIdentity = new(Load);

    public static ReleaseIdentity Current => CurrentIdentity.Value;

    private static ReleaseIdentity Load()
    {
        var text = global::Godot.FileAccess.GetFileAsString(ResourcePath);
        var identity = JsonSerializer.Deserialize<ReleaseIdentity>(text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
        if (identity is null
            || string.IsNullOrWhiteSpace(identity.Version)
            || string.IsNullOrWhiteSpace(identity.Tag)
            || string.IsNullOrWhiteSpace(identity.WindowsFileVersion)
            || string.IsNullOrWhiteSpace(identity.Target)
            || string.IsNullOrWhiteSpace(identity.SampleMapId)
            || identity.SampleMapHash.Length != 64)
        {
            throw new InvalidOperationException("Release identity resource is missing required fields.");
        }

        return identity;
    }
}
