using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Nodes;
using AuthoringResource = ProceduralRts.MapAuthoring.Nodes.Resource;

namespace ProceduralRts.MapAuthoring.Qa;

sealed class MapTypedProjectionFixture
{
    public required MapRoot Root { get; init; }
    public required OwnerStart PlayerStart { get; init; }
    public required Building Building { get; init; }
    public required Unit Unit { get; init; }
    public required AuthoringResource Resource { get; init; }
    public required Obstacle Obstacle { get; init; }
    public required TerrainRegion FirstTerrain { get; init; }
    public required Trigger Trigger { get; init; }
    public required Objective Objective { get; init; }
    public required Narrative Narrative { get; init; }

    public static MapTypedProjectionFixture Create()
    {
        var root = new MapRoot
        {
            Name = "SceneRootNameMustNotBecomeId",
            Id = "qa.typed",
            Seed = 20260717,
            WorldSize = new Vector2(1600, 1000),
            Position = new Vector2(500, 300),
            Rotation = 0.2f,
        };
        var playerStart = Owner("PlayerStart", 1, "dog", new Vector2(260, 320));
        root.AddChild(playerStart);
        root.AddChild(Owner("EnemyStart", 2, "cat", new Vector2(1260, 680)));
        var building = new Building
        {
            Name = "DogHeadquarters", BuildingId = BuildingDesignIds.Headquarters,
            OwnerId = 1, FactionId = "dog", Position = new Vector2(256, 304),
        };
        root.AddChild(building);
        var unit = new Unit
        {
            Name = "DogGuard", DesignId = "dog.guard_tank",
            OwnerId = 1, Position = new Vector2(380, 320),
        };
        root.AddChild(unit);
        var resource = new AuthoringResource
        {
            Name = "IgnoredResourceName", Id = "NorthField", Position = new Vector2(780, 230), Radius = 100, Amount = 2800,
        };
        root.AddChild(resource);
        var obstacle = new Obstacle
        {
            Name = "IgnoredObstacleName", Id = "CourtyardBlock", Position = new Vector2(690, 450), Size = new Vector2(128, 96),
        };
        root.AddChild(obstacle);
        var firstTerrain = Terrain("SoftRoad", "soft-road", new Vector2(540, 500), new Vector2(500, 140), 0.85f);
        root.AddChild(firstTerrain);
        root.AddChild(Terrain("CatBasePad", "base-ground", new Vector2(1152, 608), new Vector2(192, 160), 1));
        var trigger = new Trigger
        {
            Name = "IgnoredTriggerName", Id = "GateTrigger", Position = new Vector2(720, 420), Size = new Vector2(180, 180),
        };
        root.AddChild(trigger);
        var objective = new Objective
        {
            Name = "IgnoredObjectiveName", Id = "SignalObjective", Position = new Vector2(840, 420),
        };
        root.AddChild(objective);
        var nested = new Node2D { Name = "NestedGroup", Position = new Vector2(700, 300), Rotation = MathF.PI * 0.5f };
        root.AddChild(nested);
        var narrative = new Narrative
        {
            Name = "IgnoredNarrativeName", Id = "FirstMark", Position = new Vector2(80, 50), TriggerId = trigger.Id,
        };
        nested.AddChild(narrative);
        return new MapTypedProjectionFixture
        {
            Root = root, PlayerStart = playerStart, Building = building, Unit = unit, Resource = resource,
            Obstacle = obstacle, FirstTerrain = firstTerrain, Trigger = trigger, Objective = objective, Narrative = narrative,
        };
    }

    private static OwnerStart Owner(string name, int ownerId, string faction, Vector2 position)
    {
        return new OwnerStart { Name = name, OwnerId = ownerId, FactionId = faction, Position = position };
    }

    private static TerrainRegion Terrain(string id, string terrainId, Vector2 position, Vector2 size, float cost)
    {
        return new TerrainRegion { Name = $"Ignored{id}", Id = id, TerrainId = terrainId, Position = position, Size = size, MovementCost = cost };
    }
}
