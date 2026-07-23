using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public sealed record AuthoredMapPreviewLaunch(string MapId, string Sha256);

public static class AuthoredMapPreviewCommandLine
{
    public static string ResolveProjectRoot(string resourceRoot, string executablePath, string artifactPath)
    {
        var executableDirectory = Path.GetDirectoryName(executablePath);
        foreach (var candidate in new[] { resourceRoot, executableDirectory })
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            try
            {
                var normalizedCandidate = Path.GetFullPath(candidate);
                _ = MapArtifactPathPolicy.RequireAbsolute(normalizedCandidate, artifactPath);
                return normalizedCandidate;
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or InvalidOperationException)
            {
                // Keep the candidate boundary strict and try the exported directory next.
            }
        }

        throw new InvalidOperationException("Authored preview artifact must be directly under the source project or exported executable assets/maps directory.");
    }

    public static AuthoredMapPreviewLaunch StageRequired(IReadOnlyList<string> arguments)
        => StageRequired(arguments, ProjectSettings.GlobalizePath("res://"), OS.GetExecutablePath());

    public static AuthoredMapPreviewLaunch StageRequired(IReadOnlyList<string> arguments, string resourceRoot, string executablePath)
    {
        var request = AuthoredMapPreviewRequest.Parse(arguments);
        return Stage(request, ResolveProjectRoot(resourceRoot, executablePath, request.AbsoluteArtifactPath));
    }

    public static AuthoredMapPreviewLaunch StageRequired(IReadOnlyList<string> arguments, string projectRoot)
    {
        var request = AuthoredMapPreviewRequest.Parse(arguments);
        return Stage(request, projectRoot);
    }

    private static AuthoredMapPreviewLaunch Stage(AuthoredMapPreviewRequest request, string projectRoot)
    {
        var map = AuthoredMapPreviewRuntime.StageVerified(request, projectRoot);
        return new AuthoredMapPreviewLaunch(map.Id, request.Sha256);
    }
}
