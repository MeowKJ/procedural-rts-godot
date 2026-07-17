using ProceduralRts.Core;

var failures = new List<string>();
ValidateMapSpecIsGodotFree(failures);
PlacementValidationScenarios.Run(failures);

var generated = SkirmishMapGenerator.GenerateSpec(MatchConfig.Default);
ValidateMap("generated skirmish", generated, failures);
ValidateDeterministicLoad("generated skirmish", generated, failures);

var artifactPath = Path.Combine(AppContext.BaseDirectory, "fixtures", "hand-designed-map.mapspec.json");
var authored = MapSpecArtifactCodec.Decode(File.ReadAllBytes(artifactPath));
ValidateMap("authored artifact", authored, failures);
ValidateDeterministicLoad("authored artifact", authored, failures);
Require(authored.Triggers.Count == 1, "authored artifact should keep trigger areas.", failures);
Require(authored.Objectives.Count == 1, "authored artifact should keep objective nodes.", failures);
Require(authored.NarrativeNodes.Count == 1, "authored artifact should keep narrative nodes.", failures);
Require(authored.TerrainCells.Count == 2, "authored artifact should keep layered terrain cells.", failures);

if (failures.Count > 0)
{
    Console.Error.WriteLine("MapAuthoringQa FAILED");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    Environment.Exit(1);
}

Console.WriteLine($"MapAuthoringQa PASSED: generated entities {generated.Buildings.Count + generated.Units.Count}, authored entities {authored.Buildings.Count + authored.Units.Count}.");

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

    foreach (var conflict in MapBuildingPlacementValidator.Validate(spec))
    {
        failures.Add($"{label}: {conflict}");
    }
}

static void ValidateDeterministicLoad(string label, MapSpec spec, List<string> failures)
{
    var first = RunLoadedWorld(spec);
    var second = RunLoadedWorld(spec);
    Require(first == second, $"{label} should load and run deterministically through MapLoader.", failures);
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
