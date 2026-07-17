using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring;

namespace ProceduralRts;

public partial class MapApiBakeQaRoot : Node
{
    private const string ScenePath = "res://tools/MapAuthoringQa/fixtures/hand-designed-map.tscn";
    private const string GoldenPath = "res://tools/MapAuthoringQa/fixtures/hand-designed-map.mapspec.json";

    public override void _Ready()
    {
        try
        {
            RunFixtureBake();
            RunNestedTransformScene();
            RunInvalidScene();
            GD.Print("Map API bake QA passed: PackedScene APIs, canonical bytes/hash, round-trip order, and typed preflight rejection.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void RunFixtureBake()
    {
        var packed = ResourceLoader.Load<PackedScene>(ScenePath)
            ?? throw new InvalidOperationException($"Could not load {ScenePath} as PackedScene.");
        Node? firstRoot = null;
        Node? secondRoot = null;
        try
        {
            firstRoot = packed.Instantiate();
            secondRoot = packed.Instantiate();
            var first = GodotMapSpecBaker.Bake(firstRoot, "qa.hand-designed", 20260701);
            var second = GodotMapSpecBaker.Bake(secondRoot, "qa.hand-designed", 20260701);
            Require(first.Sha256 == second.Sha256 && first.ToArray().SequenceEqual(second.ToArray()), "Two unchanged scene bakes must be byte/hash identical.");

            var golden = File.ReadAllBytes(ProjectSettings.GlobalizePath(GoldenPath));
            Require(first.ToArray().SequenceEqual(golden), "Godot API bake must equal the checked-in canonical artifact.");
            var map = MapSpecArtifactCodec.Decode(first.ToArray());
            Require(MapSpecArtifactCodec.Encode(map).ToArray().SequenceEqual(golden), "Codec round-trip must preserve exact golden bytes.");
            Require(map.TerrainCells.Select(item => item.Id).SequenceEqual(["SoftRoad", "CatBasePad"]), "Terrain source order must remain semantic and stable.");
            Require(AllCollectionsPreserved(map), "Godot API bake must preserve every MapSpec collection.");
        }
        finally
        {
            firstRoot?.Free();
            secondRoot?.Free();
        }
    }

    private static void RunInvalidScene()
    {
        var root = new Node2D { Name = "InvalidMap" };
        try
        {
            root.SetMeta("world_width", 512); root.SetMeta("world_height", 512);
            root.AddChild(OwnerStart("PlayerStart", 1, "dog", new Vector2(64, 64)));
            root.AddChild(OwnerStart("EnemyStart", 2, "cat", new Vector2(448, 448)));
            var building = new Node2D { Name = "Barracks", Position = new Vector2(480, 320) };
            building.SetMeta("map_kind", "building"); building.SetMeta("building_kind", BuildingDesignIds.Barracks);
            building.SetMeta("owner_id", 1); building.SetMeta("faction", "dog");
            root.AddChild(building);

            try
            {
                GodotMapSpecBaker.Bake(root, "qa.invalid-baked-map", 550);
                throw new InvalidOperationException("Invalid Godot scene should fail before artifact output.");
            }
            catch (MapBuildingPlacementValidationException exception)
            {
                Require(exception.MapId == "qa.invalid-baked-map"
                    && exception.Conflicts.Any(conflict => conflict.Conflict == MapBuildingPlacementConflictKind.Outside),
                    "Godot baker must surface the shared typed placement conflict.");
            }
        }
        finally
        {
            root.Free();
        }
    }

    private static void RunNestedTransformScene()
    {
        var root = new Node2D
        {
            Name = "TransformedMap",
            Position = new Vector2(500, 300),
            Rotation = 0.5f,
        };
        try
        {
            root.SetMeta("world_width", 800); root.SetMeta("world_height", 600);
            var group = new Node2D
            {
                Name = "NestedGroup",
                Position = new Vector2(100, 80),
                Rotation = MathF.PI / 2,
            };
            root.AddChild(group);
            group.AddChild(OwnerStart("NestedStart", 1, "dog", new Vector2(40, 20)));
            var obstacle = new Node2D { Name = "NestedBlock", Position = new Vector2(60, 30) };
            obstacle.SetMeta("map_kind", "obstacle"); obstacle.SetMeta("width", 32); obstacle.SetMeta("height", 48);
            group.AddChild(obstacle);
            root.AddChild(OwnerStart("DirectStart", 2, "cat", new Vector2(700, 500)));

            var map = MapSpecArtifactCodec.Decode(GodotMapSpecBaker.Bake(root, "qa.root-local", 551).ToArray());
            Require(map.OwnerStarts.Select(item => item.OwnerId.Value).SequenceEqual([1, 2]), "Nested contributors must retain scene preorder.");
            Require(Approx(map.OwnerStarts[0].Position, new MapPoint(80, 120)), "Nested owner position must be relative to the supplied map root.");
            Require(Approx(map.Obstacles[0].Bounds, new MapRect(70, 140, 32, 48)), "Nested rectangle origin must be relative to the supplied map root.");
        }
        finally
        {
            root.Free();
        }
    }

    private static Node2D OwnerStart(string name, int ownerId, string faction, Vector2 position)
    {
        var node = new Node2D { Name = name, Position = position };
        node.SetMeta("map_kind", "owner_start"); node.SetMeta("owner_id", ownerId);
        node.SetMeta("faction", faction); node.SetMeta("credits", 0);
        return node;
    }

    private static bool AllCollectionsPreserved(MapSpec map)
    {
        return map.OwnerStarts.Count == 2 && map.TerrainCells.Count == 2 && map.Resources.Count == 1
            && map.Obstacles.Count == 1 && map.Buildings.Count == 2 && map.Units.Count == 2
            && map.Triggers.Count == 1 && map.Objectives.Count == 1 && map.NarrativeNodes.Count == 1;
    }

    private static bool Approx(MapPoint actual, MapPoint expected)
    {
        return Mathf.IsEqualApprox(actual.X, expected.X) && Mathf.IsEqualApprox(actual.Y, expected.Y);
    }

    private static bool Approx(MapRect actual, MapRect expected)
    {
        return Mathf.IsEqualApprox(actual.X, expected.X) && Mathf.IsEqualApprox(actual.Y, expected.Y)
            && Mathf.IsEqualApprox(actual.Width, expected.Width) && Mathf.IsEqualApprox(actual.Height, expected.Height);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
