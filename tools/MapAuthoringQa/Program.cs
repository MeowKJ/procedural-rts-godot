using ProceduralRts.Core;

var failures = new List<string>();
ValidateMapSpecIsGodotFree(failures);

var generated = SkirmishMapGenerator.GenerateSpec(MatchConfig.Default);
ValidateMap("generated skirmish", generated, failures);
ValidateDeterministicLoad("generated skirmish", generated, failures);

var scenePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "fixtures", "hand-designed-map.tscn");
var baked = GodotSceneMapBaker.Bake(File.ReadAllText(Path.GetFullPath(scenePath)), "qa.hand-designed", 20260701);
ValidateMap("baked hand-designed", baked, failures);
ValidateDeterministicLoad("baked hand-designed", baked, failures);
ValidatePlayableGameState("baked hand-designed", baked, failures);
ValidatePendingOptionsRoundTrip(failures);
Require(baked.Triggers.Count == 1, "baked map should keep trigger areas.", failures);
Require(baked.Objectives.Count == 1, "baked map should keep objective nodes.", failures);
Require(baked.NarrativeNodes.Count == 1, "baked map should keep narrative nodes.", failures);
Require(baked.TerrainCells.Count == 1, "baked map should keep terrain cells.", failures);

if (failures.Count > 0)
{
    Console.Error.WriteLine("MapAuthoringQa FAILED");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    Environment.Exit(1);
}

Console.WriteLine($"MapAuthoringQa PASSED: generated entities {generated.Buildings.Count + generated.Units.Count}, baked entities {baked.Buildings.Count + baked.Units.Count}.");

static void ValidateMapSpecIsGodotFree(List<string> failures)
{
    var types = new[]
    {
        typeof(MapSpec),
        typeof(MapPoint),
        typeof(MapSize),
        typeof(MapRect),
        typeof(MapResourceNodeSpec),
        typeof(MapOwnerStartSpec),
    };
    foreach (var type in types)
    {
        foreach (var property in type.GetProperties())
        {
            var propertyType = property.PropertyType;
            Require(!UsesGodotType(propertyType), $"{type.Name}.{property.Name} must not expose a Godot type.", failures);
        }
    }
}

static bool UsesGodotType(Type type)
{
    if ((type.Namespace ?? "").StartsWith("Godot", StringComparison.Ordinal))
    {
        return true;
    }

    return type.IsGenericType && type.GetGenericArguments().Any(UsesGodotType);
}

static void ValidateMap(string label, MapSpec spec, List<string> failures)
{
    Require(spec.OwnerStarts.Count >= 2, $"{label} should define at least two owner starts.", failures);
    Require(spec.Resources.Count > 0, $"{label} should define resources.", failures);
    Require(spec.Obstacles.Count > 0, $"{label} should define obstacles.", failures);
    Require(spec.Buildings.Count > 0, $"{label} should define starting buildings.", failures);
    Require(spec.Units.Count > 0, $"{label} should define starting units.", failures);
}

static void ValidateDeterministicLoad(string label, MapSpec spec, List<string> failures)
{
    var first = RunLoadedWorld(spec);
    var second = RunLoadedWorld(spec);
    Require(first == second, $"{label} should load and run deterministically through MapLoader.", failures);
}

static void ValidatePlayableGameState(string label, MapSpec spec, List<string> failures)
{
    var previousPendingMatch = SkirmishSetupState.PendingMatchConfig;
    GameState state;
    try
    {
        SkirmishSetupState.PendingMatchConfig = AuthoredMatchConfig(spec);
        state = new GameState(SkirmishSetupState.PendingMatchConfig);
    }
    finally
    {
        SkirmishSetupState.PendingMatchConfig = previousPendingMatch;
    }

    Require(state.MatchConfig.AuthoredMap == spec, $"{label} should preserve the authored map on MatchConfig.", failures);
    Require(state.WorldSize == spec.WorldSize.ToVector2(), $"{label} should size GameState from authored map.", failures);
    Require(state.Credits(Owner.Player) == spec.StartFor(new OwnerId(1)).StartingCredits, $"{label} should seed player credits from owner starts.", failures);
    Require(state.Credits(Owner.Enemy) == spec.StartFor(new OwnerId(2)).StartingCredits, $"{label} should seed enemy credits from owner starts.", failures);
    Require(state.ResourceFields.Count == spec.Resources.Count, $"{label} should seed resource fields into playable GameState.", failures);
    Require(state.MapObstacles.Count == spec.Obstacles.Count, $"{label} should seed authored obstacles into playable GameState.", failures);
    Require(state.Buildings.Count == spec.Buildings.Count, $"{label} should seed authored buildings into playable GameState.", failures);
    Require(state.Units.Count == spec.Units.Count, $"{label} should seed authored units into playable GameState.", failures);
    var playerHq = state.Buildings.FirstOrDefault(building => building.Owner == Owner.Player && building.Kind == BuildingDesignIds.Headquarters);
    Require(playerHq is not null
        && playerHq.Id == 77
        && playerHq.FactionId == FactionId.Dog
        && MathF.Abs(playerHq.Hp - 725) < 0.01f
        && MathF.Abs(playerHq.BuildProgress - 0.95f) < 0.01f
        && playerHq.Position.DistanceTo(new Godot.Vector2(260, 320)) < 0.01f, $"{label} should keep authored player HQ id, faction, hp, build progress, and position.", failures);
    Require(state.Units.Any(unit => unit.Owner == Owner.Enemy && unit.DesignId == "cat.tank" && unit.Position.DistanceTo(new Godot.Vector2(1140, 680)) < 0.01f), $"{label} should keep the authored enemy unit position.", failures);
}

static void ValidatePendingOptionsRoundTrip(List<string> failures)
{
    var previousPendingMatch = SkirmishSetupState.PendingMatchConfig;
    try
    {
        var customOptions = new SkirmishOptions(
            StartingCredits: 3100,
            MapSeed: 445566,
            EnemyDifficulty: EnemyDifficulty.Hard,
            LaunchMode: LaunchMode.Skirmish,
            PlayerFaction: FactionId.Cat,
            AiFaction: FactionId.Dog);
        SkirmishSetupState.PendingOptions = customOptions;
        Require(SkirmishSetupState.PendingOptions == customOptions, "PendingOptions should still round-trip plain skirmish setup.", failures);
        Require(SkirmishSetupState.PendingMatchConfig.AuthoredMap is null, "PendingOptions should clear any authored map handoff.", failures);

        SkirmishSetupState.PendingOptions = SkirmishOptions.Sandbox;
        Require(SkirmishSetupState.PendingOptions == SkirmishOptions.Sandbox, "PendingOptions should still round-trip sandbox setup.", failures);
    }
    finally
    {
        SkirmishSetupState.PendingMatchConfig = previousPendingMatch;
    }
}

static MatchConfig AuthoredMatchConfig(MapSpec spec)
{
    return new MatchConfig(
        StartingCredits: 0,
        MapSeed: spec.Seed,
        EnemyDifficulty: EnemyDifficulty.Normal,
        WorldSize: spec.WorldSize.ToVector2(),
        PlayerFaction: spec.StartFor(new OwnerId(1)).Faction,
        AiFaction: spec.StartFor(new OwnerId(2)).Faction,
        AuthoredMap: spec);
}

static ulong RunLoadedWorld(MapSpec spec)
{
    var world = MapLoader.Load(spec, options: new MapLoadOptions(ConfigureLiveSystems: true, OutcomeViewer: new OwnerId(1)));
    for (var tick = 0; tick < 12; tick++)
    {
        world.Step(tick, 1f / 10f, []);
    }

    return world.DeterministicStateHash();
}

static void Require(bool condition, string message, List<string> failures)
{
    if (!condition)
    {
        failures.Add(message);
    }
}
