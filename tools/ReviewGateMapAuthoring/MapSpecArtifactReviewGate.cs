static class MapSpecArtifactReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireFiles(root, result);
        CheckCodec(root, result);
        CheckGodotBoundary(root, result);
        CheckQaConsumers(root, result);
    }

    private static void RequireFiles(string root, GateResult result)
    {
        foreach (var path in new[]
        {
            "scripts/core/map/artifacts/MapSpecArtifact.cs",
            "scripts/core/map/artifacts/MapSpecArtifactWriter.cs",
            "scripts/core/map/artifacts/MapSpecArtifactReader.cs",
            "scripts/core/map/artifacts/MapSpecArtifactFactionWire.cs",
            "scripts/core/map/artifacts/MapSpecSnapshot.cs",
            "addons/map_authoring/baker/GodotMapSpecBaker.cs",
            "tools/MapSpecArtifactQa/MapSpecArtifactQa.csproj",
            "tools/MapAuthoringQa/fixtures/hand-designed-map.mapspec.json",
        })
        {
            ReviewGateSource.RequireFile(root, result, path.Split('/'));
        }

        ReviewGateSource.ForbidFile(root, result, "tools", "MapAuthoringQa", "GodotSceneMapBaker.cs");
    }

    private static void CheckCodec(string root, GateResult result)
    {
        var artifact = ReviewGateSource.Read(root, "scripts", "core", "map", "artifacts", "MapSpecArtifact.cs");
        RequireText(artifact, "procedural-rts.mapspec", "MapSpec artifact must keep its stable format identifier.", result);
        RequireText(artifact, "SchemaVersion = 2", "MapSpec artifact must keep explicit schemaVersion 2.", result);
        RequireText(artifact, "SHA256.HashData", "MapSpec artifact hash must use SHA-256 over exact bytes.", result);
        RequireText(artifact, "MapSpecSnapshot.Create", "Artifact encoding must deep-snapshot caller-owned collections.", result);
        RequireText(artifact, "MapLoader.Prepare(snapshot)", "Artifact encoding must use MapLoader.Prepare as domain-validation authority.", result);
        RequireText(artifact, "MapLoader.Prepare(parsed)", "Artifact decoding must use MapLoader.Prepare as domain-validation authority.", result);
        ForbidText(artifact, "MapOwnerTopologyValidator.EnsureValid", "Artifact codec must not duplicate MapLoader validation composition.", result);
        var writer = ReviewGateSource.Read(root, "scripts", "core", "map", "artifacts", "MapSpecArtifactWriter.cs");
        RequireText(writer, "bytes[^1] = (byte)'\\n'", "Canonical writer must append exactly one terminal LF.", result);
        RequireText(writer, "float.IsFinite", "Canonical writer must reject non-finite numbers.", result);
        RequireText(writer, "value == 0f ? 0f : value", "Canonical writer must normalize negative zero.", result);
        var reader = ReviewGateSource.Read(root, "scripts", "core", "map", "artifacts", "MapSpecArtifactReader.cs");
        RequireText(reader, "StrictUtf8", "Artifact reader must reject malformed UTF-8.", result);
        RequireText(artifact, "bytes.SequenceEqual(canonical)", "Artifact reader must reject noncanonical bytes.", result);
        var factionWire = ReviewGateSource.Read(root, "scripts", "core", "map", "artifacts", "MapSpecArtifactFactionWire.cs");
        RequireText(factionWire, "FactionId.Dog => \"dog\"", "Artifact faction wire values must be explicit and stable.", result);
        RequireText(factionWire, "\"corruption\" => FactionId.Corruption", "Artifact reader must explicitly support the corruption wire value.", result);
        ForbidText(factionWire, "TryParse", "Faction wire decoding must not depend on enum names.", result);
        ForbidText(factionWire, "ToString", "Faction wire encoding must not depend on enum names.", result);
        var collectionWriter = ReviewGateSource.Read(root, "scripts", "core", "map", "artifacts", "MapSpecArtifactCollectionWriter.cs");
        RequireText(collectionWriter, "MapSpecArtifactFactionWire.Write", "Canonical writer must use the explicit faction wire mapping.", result);
        RequireText(reader, "MapSpecArtifactFactionWire.Read", "Strict reader must use the explicit faction wire mapping.", result);
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "scripts", "core", "map", "artifacts"), "*.cs"))
        {
            var source = File.ReadAllText(path);
            ForbidText(source, "using Godot", "MapSpec artifact codec must remain Godot-free.", result);
            ForbidText(source, "Vector2", "MapSpec artifact codec must remain Godot-free.", result);
        }
    }

    private static void CheckGodotBoundary(string root, GateResult result)
    {
        var baker = ReviewGateSource.Read(root, "addons", "map_authoring", "baker", "GodotMapSpecBaker.cs");
        RequireText(baker, "MapLoader.Prepare(snapshot)", "Godot baker must validate the deep snapshot before output.", result);
    }

    private static void CheckQaConsumers(string root, GateResult result)
    {
        var mapProject = ReviewGateSource.Read(root, "tools", "MapAuthoringQa", "MapAuthoringQa.csproj");
        var playableProject = ReviewGateSource.Read(root, "tools", "PlayableMapHandoffQa", "PlayableMapHandoffQa.csproj");
        RequireText(mapProject, "hand-designed-map.mapspec.json", "MapAuthoringQa must consume the canonical artifact.", result);
        RequireText(playableProject, "hand-designed-map.mapspec.json", "Playable handoff QA must consume the canonical artifact.", result);
        ReviewGateSource.ForbidTextInSources(root, result, "GodotSceneMapBaker", "tools/MapAuthoringQa", "tools/PlayableMapHandoffQa");
        var verifyAll = ReviewGateSource.Read(root, "tools", "VerifyAll", "Program.cs");
        RequireText(verifyAll, "mapspec-artifact-qa", "VerifyAll must run artifact codec QA.", result);
        var artifactQa = ReviewGateSource.Read(root, "tools", "MapSpecArtifactQa", "MapSpecArtifactScenarios.cs");
        RequireText(artifactQa, "wrong-case faction wire value", "Artifact QA must reject wrong-case faction wire values.", result);
    }
}
