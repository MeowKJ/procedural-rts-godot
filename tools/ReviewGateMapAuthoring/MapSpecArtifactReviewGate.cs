static class MapSpecArtifactReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireFiles(root, result);
        CheckCodec(root, result);
        CheckGodotBoundary(root, result);
        CheckQaMigration(root, result);
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
            "addons/map_authoring/baker/FixtureOnlyMetadataMapBaker.cs",
            "addons/map_authoring/baker/FixtureOnlyMetadataMapSceneAdapter.cs",
            "scripts/qa/MapApiBakeQaRoot.cs",
            "scenes/MapApiBakeQa.tscn",
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
        ForbidText(baker, "string id, int seed", "Product baker must not expose the retired fixture metadata signature.", result);
        RequireText(baker, "MapLoader.Prepare(snapshot)", "Godot baker must validate the deep snapshot before output.", result);
        var fixtureBaker = ReviewGateSource.Read(root, "addons", "map_authoring", "baker", "FixtureOnlyMetadataMapBaker.cs");
        RequireText(fixtureBaker, "internal static class FixtureOnlyMetadataMapBaker", "Metadata fixture baking must be a restricted retired API.", result);
        RequireText(fixtureBaker, "BakeFixture", "Retired metadata entry point must be explicitly fixture-only.", result);
        var adapter = ReviewGateSource.Read(root, "addons", "map_authoring", "baker", "FixtureOnlyMetadataMapSceneAdapter.cs");
        RequireText(adapter, "node.HasMeta", "Godot adapter must inspect loaded Node metadata.", result);
        RequireText(adapter, "node as Node2D", "Godot adapter must read positions from loaded Node2D APIs.", result);
        RequireText(adapter, "MapSceneProjection.RootLocalPoint", "Godot adapter must use shared root-local scene projection.", result);
        ForbidText(adapter, "Regex", "Godot adapter must not parse scene text with regex.", result);
        ForbidText(adapter, "File.ReadAllText", "Godot adapter must not treat scene text as authority.", result);
        var smoke = ReviewGateSource.Read(root, "scripts", "qa", "MapApiBakeQaRoot.cs");
        RequireText(smoke, "FixtureOnlyMetadataMapBaker.BakeFixture", "Retired #566 QA must use the explicit fixture-only baker.", result);
        RequireText(smoke, "ResourceLoader.Load<PackedScene>", "Godot smoke must load the fixture through PackedScene APIs.", result);
        RequireText(smoke, "first.Sha256 == second.Sha256", "Godot smoke must prove unchanged bake hash parity.", result);
        RequireText(smoke, "MapBuildingPlacementValidationException", "Godot smoke must prove typed invalid-scene rejection.", result);
        RequireText(smoke, "RunNestedTransformScene", "Godot smoke must cover nested transformed contributors.", result);
        RequireText(smoke, "new MapRect(70, 140, 32, 48)", "Godot smoke must lock root-local rectangle coordinates.", result);
    }

    private static void CheckQaMigration(string root, GateResult result)
    {
        var mapProject = ReviewGateSource.Read(root, "tools", "MapAuthoringQa", "MapAuthoringQa.csproj");
        var playableProject = ReviewGateSource.Read(root, "tools", "PlayableMapHandoffQa", "PlayableMapHandoffQa.csproj");
        RequireText(mapProject, "hand-designed-map.mapspec.json", "MapAuthoringQa must consume the canonical artifact.", result);
        RequireText(playableProject, "hand-designed-map.mapspec.json", "Playable handoff QA must consume the canonical artifact.", result);
        ReviewGateSource.ForbidTextInSources(root, result, "GodotSceneMapBaker", "tools/MapAuthoringQa", "tools/PlayableMapHandoffQa");
        var verifyAll = ReviewGateSource.Read(root, "tools", "VerifyAll", "Program.cs");
        RequireText(verifyAll, "mapspec-artifact-qa", "VerifyAll must run artifact codec QA.", result);
        RequireText(verifyAll, "godot-map-api-bake-qa", "VerifyAll must run Godot API bake smoke.", result);
        var artifactQa = ReviewGateSource.Read(root, "tools", "MapSpecArtifactQa", "MapSpecArtifactScenarios.cs");
        RequireText(artifactQa, "wrong-case faction wire value", "Artifact QA must reject wrong-case faction wire values.", result);
    }
}
