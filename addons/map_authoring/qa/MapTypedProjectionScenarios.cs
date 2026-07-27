using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Editor;
using ProceduralRts.MapAuthoring.Nodes;
using ProceduralRts.MapAuthoring.Projection;
using AuthoringResource = ProceduralRts.MapAuthoring.Nodes.Resource;

namespace ProceduralRts.MapAuthoring.Qa;

static class MapTypedProjectionScenarios
{
    private const string EditorAcceptanceScenePath = "res://addons/map_authoring/qa/MapAuthoringEditorAcceptance.tscn";

    public static void Run()
    {
        ValidateEditorAcceptanceScene();
        var fixture = MapTypedProjectionFixture.Create();
        try
        {
            ValidateProjection(fixture);
            ValidateCatalogFailures(fixture);
            ValidateSemanticFailures(fixture);
            ValidateRuntimeIdPresence(fixture);
            ValidateRotations(fixture);
            MapTypedTransformScenarios.Run(fixture);
            ValidateInspectorCatalog(fixture);
        }
        finally
        {
            fixture.Root.Free();
        }
    }

    private static void ValidateEditorAcceptanceScene()
    {
        var packed = ResourceLoader.Load<PackedScene>(EditorAcceptanceScenePath)
            ?? throw new InvalidOperationException($"Could not load {EditorAcceptanceScenePath}.");
        var root = packed.Instantiate<MapRoot>();
        try
        {
            var types = MapSceneProjection.SceneOrder(root).Select(node => node.GetType()).ToArray();
            foreach (var type in new[]
            {
                typeof(OwnerStart), typeof(Building), typeof(Unit),
                typeof(AuthoringResource), typeof(Obstacle), typeof(TerrainRegion),
                typeof(Trigger), typeof(Objective), typeof(Narrative),
            })
            {
                Require(types.Contains(type), $"Editor acceptance scene must instantiate {type.Name}.");
            }
            _ = GodotMapSpecBaker.Bake(root, TypedMapSceneProjector.Instance);
        }
        finally
        {
            root.Free();
        }
    }

    private static void ValidateProjection(MapTypedProjectionFixture fixture)
    {
        var first = GodotMapSpecBaker.Bake(fixture.Root, TypedMapSceneProjector.Instance);
        var second = GodotMapSpecBaker.Bake(fixture.Root, TypedMapSceneProjector.Instance);
        Require(first.Sha256 == second.Sha256 && first.ToArray().SequenceEqual(second.ToArray()), "Typed projection must be canonical and deterministic.");
        var map = MapSpecArtifactCodec.Decode(first.ToArray());
        Require(map.Id == fixture.Root.Id && map.Seed == fixture.Root.Seed, "MapRoot explicit id/seed must project without Node.Name fallback.");
        Require(map.OwnerStarts.Count == 2 && map.Buildings.Count == 1 && map.Units.Count == 1, "Typed entity collections must project.");
        Require(map.Buildings[0].RuntimeId is null, "Unset runtime-id presence must project as null.");
        Require(map.Resources.Count == 1 && map.Obstacles.Count == 1 && map.TerrainCells.Count == 2, "Typed environment collections must project.");
        Require(map.Triggers.Count == 1 && map.Objectives.Count == 1 && map.NarrativeNodes.Count == 1, "Typed narrative collections must project.");
        Require(map.Resources[0].Id == fixture.Resource.Id && map.Resources[0].Id != fixture.Resource.Name.ToString(), "Semantic ids must use exported values, never Node.Name.");
        Require(map.TerrainCells.Select(item => item.Id).SequenceEqual(["SoftRoad", "CatBasePad"]), "Typed projection must preserve child-index terrain order.");
        Require(Approx(map.NarrativeNodes[0].Position, new MapPoint(650, 380)), "Nested typed positions must be root-local.");
    }

    private static void ValidateCatalogFailures(MapTypedProjectionFixture fixture)
    {
        RejectCatalog(() => fixture.Building.BuildingId, value => fixture.Building.BuildingId = value, "unknown.building", fixture);
        RejectCatalog(() => fixture.Unit.DesignId, value => fixture.Unit.DesignId = value, "unknown.unit", fixture);
        RejectCatalog(() => fixture.PlayerStart.FactionId, value => fixture.PlayerStart.FactionId = value, "Dog", fixture);
        RejectCatalog(() => fixture.FirstTerrain.TerrainId, value => fixture.FirstTerrain.TerrainId = value, "unknown.terrain", fixture);
        RejectCatalog(() => fixture.Trigger.EventKey, value => fixture.Trigger.EventKey = value, "unknown.event", fixture);
        RejectCatalog(() => fixture.Objective.ObjectiveKey, value => fixture.Objective.ObjectiveKey = value, "unknown.objective", fixture);
        RejectCatalog(() => fixture.Narrative.TextKey, value => fixture.Narrative.TextKey = value, "unknown.narrative", fixture);
    }

    private static void ValidateSemanticFailures(MapTypedProjectionFixture fixture)
    {
        var oldObstacleId = fixture.Obstacle.Id;
        fixture.Obstacle.Id = fixture.Resource.Id;
        Expect<MapSemanticValidationException>(() => Bake(fixture), "Duplicate explicit semantic ids must reach shared validation.");
        fixture.Obstacle.Id = oldObstacleId;
        var oldTerrainId = fixture.FirstTerrain.Id;
        fixture.FirstTerrain.Id = "";
        Expect<MapSemanticValidationException>(() => Bake(fixture), "Missing explicit semantic ids must reach shared validation.");
        fixture.FirstTerrain.Id = oldTerrainId;
        fixture.Root.SetMeta("map_kind", "resource");
        Expect<InvalidOperationException>(() => Bake(fixture), "Typed root must reject metadata fallback.");
        fixture.Root.RemoveMeta("map_kind");
        var metadata = new Node2D(); metadata.SetMeta("map_kind", "resource"); fixture.Root.AddChild(metadata);
        Expect<InvalidOperationException>(() => Bake(fixture), "Typed descendants must reject metadata fallback.");
        fixture.Root.RemoveChild(metadata); metadata.Free();
    }

    private static void ValidateRuntimeIdPresence(MapTypedProjectionFixture fixture)
    {
        fixture.Building.HasRuntimeId = true;
        fixture.Building.RuntimeId = -7;
        var exception = Capture<MapSemanticValidationException>(() => Bake(fixture), "Negative explicit runtime id must reach shared validation.");
        Require(exception.Diagnostics.Any(value => value.Contains("runtime_id=-7 expected_positive", StringComparison.Ordinal)),
            "Shared validation must report the original negative runtime id.");
        fixture.Building.HasRuntimeId = false;
        fixture.Building.RuntimeId = 0;
    }

    private static void ValidateRotations(MapTypedProjectionFixture fixture)
    {
        Require(MapBuildingQuarterTurns.All.Count == 4, "Building Inspector must expose exactly four quarter turns.");
        var originalPosition = fixture.Building.Position;
        var buildSpec = BuildSpecCatalog.For(fixture.Building.BuildingId);
        foreach (var turn in MapBuildingQuarterTurns.All)
        {
            fixture.Building.Rotation = turn.Radians;
            var footprint = buildSpec.FootprintCells.Rotated(turn.Radians);
            fixture.Building.Position = new Vector2(
                PlacementMath.SnapAnchor(320, footprint.WidthCells),
                PlacementMath.SnapAnchor(320, footprint.HeightCells));
            var map = MapSpecArtifactCodec.Decode(GodotMapSpecBaker.Bake(fixture.Root, TypedMapSceneProjector.Instance).ToArray());
            Require(MapBuildingQuarterTurns.IndexOf(map.Buildings[0].Facing) >= 0, $"Quarter turn {turn.Label} must project as cardinal.");
        }
        fixture.Building.Rotation = 0.1f;
        Expect<MapAuthoringRotationException>(() => Bake(fixture), "Non-cardinal persisted building rotation must fail without coercion.");
        fixture.Building.Rotation = MathF.Tau;
        Expect<MapAuthoringRotationException>(() => Bake(fixture), "Persisted MathF.Tau must fail before Transform2D normalization.");
        Require(MapBuildingQuarterTurns.IndexOf(MathF.Tau) == -1, "Equivalent modulo rotations must not be coerced into one of four persisted states.");
        fixture.Building.Rotation = 0;
        fixture.Building.Position = originalPosition;
        ValidateRootLocalRotation(fixture);
    }

    private static void ValidateRootLocalRotation(MapTypedProjectionFixture fixture)
    {
        var group = new Node2D { Name = "InvalidRotationGroup", Rotation = 0.1f };
        var building = new Building
        {
            Name = "NestedBarracks", BuildingId = BuildingDesignIds.Barracks,
            OwnerId = 1, FactionId = "dog", Position = new Vector2(900, 700), Rotation = 0,
        };
        fixture.Root.AddChild(group); group.AddChild(building);
        Expect<MapAuthoringRotationException>(() => Bake(fixture), "Non-cardinal final root-local building rotation must fail.");
        fixture.Root.RemoveChild(group); group.Free();
    }

    private static void ValidateInspectorCatalog(MapTypedProjectionFixture fixture)
    {
        Require(MapAuthoringInspectorCatalog.Handles(fixture.Root), "Inspector must handle typed MapRoot.");
        Require(MapAuthoringInspectorCatalog.TryOptions(fixture.Building, "BuildingId", out var buildings)
            && buildings.SequenceEqual(MapAuthoringCatalog.BuildingIds), "Building Inspector options must match authoritative stable ids.");
        Require(MapAuthoringInspectorCatalog.TryOptions(fixture.Unit, "DesignId", out var units)
            && units.SequenceEqual(MapAuthoringCatalog.UnitIds), "Unit Inspector options must match authoritative stable ids.");
        RequireActualOptions(fixture.Building, "BuildingId", MapAuthoringCatalog.BuildingIds);
        RequireActualOptions(fixture.Unit, "DesignId", MapAuthoringCatalog.UnitIds);
        RequireActualOptions(fixture.PlayerStart, "FactionId", MapAuthoringCatalog.FactionIds);
    }

    private static void RejectCatalog(Func<string> read, Action<string> write, string invalid, MapTypedProjectionFixture fixture)
    {
        var original = read(); write(invalid);
        Expect<MapAuthoringCatalogException>(() => Bake(fixture), $"Unknown catalog id '{invalid}' must fail closed.");
        write(original);
    }

    private static void RequireActualOptions(GodotObject value, string exportedName, IReadOnlyList<string> expected)
    {
        var propertyName = value.GetPropertyList()
            .Select(property => property["name"].AsStringName().ToString())
            .Single(name => name == exportedName || name == exportedName.ToSnakeCase());
        Require(MapAuthoringInspectorCatalog.TryOptions(value, propertyName, out var actual)
            && actual.SequenceEqual(expected), $"Inspector must recognize actual exported property '{propertyName}'.");
    }

    private static void Bake(MapTypedProjectionFixture fixture) => GodotMapSpecBaker.Bake(fixture.Root, TypedMapSceneProjector.Instance);
    private static bool Approx(MapPoint actual, MapPoint expected) => Mathf.IsEqualApprox(actual.X, expected.X) && Mathf.IsEqualApprox(actual.Y, expected.Y);
    private static void Expect<T>(Action action, string message) where T : Exception => _ = Capture<T>(action, message);
    private static T Capture<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T exception) { return exception; }
        catch (Exception exception) { throw new InvalidOperationException($"{message} Got {exception.GetType().Name}.", exception); }
        throw new InvalidOperationException(message);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
