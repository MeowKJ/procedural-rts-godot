namespace ProceduralRts;

public sealed record AuthoredMapPreviewLaunch(string MapId, string Sha256);

public static class AuthoredMapPreviewCommandLine
{
    public static AuthoredMapPreviewLaunch StageRequired(IReadOnlyList<string> arguments, string projectRoot)
    {
        var request = AuthoredMapPreviewRequest.Parse(arguments);
        var map = AuthoredMapPreviewRuntime.StageVerified(request, projectRoot);
        return new AuthoredMapPreviewLaunch(map.Id, request.Sha256);
    }
}
