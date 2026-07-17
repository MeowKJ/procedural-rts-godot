using ProceduralRts;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Editor;

internal sealed record BakePlayEvidence(
    string MapId, int Length, string Sha256,
    bool DoubleBakeEqual, bool InvalidPreserved, bool InjectedFailurePreserved,
    bool StrictRequestRejected, bool AuthoredHandoffCleared);

internal static class MapAuthoringBakePlayScenarios
{
    public static BakePlayEvidence Run(MapSpec map, byte[] committed, List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), $"procedural-rts-569-{Guid.NewGuid():N}");
        var maps = Path.Combine(root, "assets", "maps");
        Directory.CreateDirectory(maps);
        var path = Path.Combine(maps, "qa.mapspec.json");
        var target = new MapAuthoringArtifactTarget("res://assets/maps/qa.mapspec.json", root, path);
        try
        {
            var first = MapAuthoringArtifactWriter.Write(map, target);
            var firstBytes = File.ReadAllBytes(path);
            var second = MapAuthoringArtifactWriter.Write(map, target);
            var doubleBake = first.Sha256 == second.Sha256
                && firstBytes.SequenceEqual(File.ReadAllBytes(path)) && firstBytes.SequenceEqual(committed);
            Require(doubleBake, "Two atomic bakes and committed bytes must match exactly.", failures);

            var invalidPreserved = Reject(() => MapAuthoringArtifactWriter.Write(
                map with { OwnerStarts = [map.OwnerStarts[0]] }, target))
                && File.ReadAllBytes(path).SequenceEqual(firstBytes);
            Require(invalidPreserved, "Invalid Bake must preserve last-known-good bytes.", failures);
            var injectedPreserved = Reject(() => MapAuthoringArtifactWriter.Write(
                map, target, () => throw new IOException("injected pre-replace failure")))
                && File.ReadAllBytes(path).SequenceEqual(firstBytes)
                && !Directory.EnumerateFiles(maps, "*.tmp").Any();
            Require(injectedPreserved, "Pre-replace failure must preserve bytes and remove temp files.", failures);

            var strictRejected = ValidateRequestRejections(path, first.Sha256)
                && Reject(() => MapArtifactPathPolicy.RequireAbsolute(
                    root, Path.Combine(root, "outside.mapspec.json")));
            Require(strictRejected, "CLI preview request must reject missing, duplicate, unknown, malformed, and outside-root input.", failures);

            SkirmishSetupState.PendingOptions = SkirmishOptions.Default;
            var request = new AuthoredMapPreviewRequest(path, first.Sha256);
            var staged = AuthoredMapPreviewRuntime.StageVerified(request, root);
            var stagedCorrectly = staged.Id == map.Id && SkirmishSetupState.PendingMatchConfig.AuthoredMap?.Id == map.Id;
            SkirmishSetupState.ClearAuthoredMapHandoff();
            var handoffCleared = stagedCorrectly && SkirmishSetupState.PendingMatchConfig == MatchConfig.Default;
            Require(handoffCleared, "Verified runtime handoff must stage only #453 and clear to default.", failures);
            MapAuthoringAtomicPathScenarios.Run(map, committed, failures);
            MapAuthoringPlaySessionScenarios.Run(first, root, failures);
            return new BakePlayEvidence(
                map.Id, first.Length, first.Sha256, doubleBake,
                invalidPreserved, injectedPreserved, strictRejected, handoffCleared);
        }
        finally
        {
            SkirmishSetupState.ClearAuthoredMapHandoff();
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static bool ValidateRequestRejections(string path, string hash)
    {
        return Reject(() => AuthoredMapPreviewRequest.Parse([]))
            && Reject(() => AuthoredMapPreviewRequest.Parse(["--unknown", "x", "--authored-map-sha256", hash]))
            && Reject(() => AuthoredMapPreviewRequest.Parse(["--authored-map-preview", path, "--authored-map-preview", path, "--authored-map-sha256", hash]))
            && Reject(() => AuthoredMapPreviewRequest.Parse(["--authored-map-preview", path, "--authored-map-sha256", "ABC"]));
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
}
