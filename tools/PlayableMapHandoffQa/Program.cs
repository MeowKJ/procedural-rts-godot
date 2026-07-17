using ProceduralRts.Core;

var failures = new List<string>();
var fixturePath = Path.Combine(AppContext.BaseDirectory, "fixtures", "hand-designed-map.tscn");
var baked = GodotSceneMapBaker.Bake(
    File.ReadAllText(fixturePath),
    "qa.playable-map-handoff",
    20260701);

PlayableMapHandoffScenarios.Run(baked, failures);
if (failures.Count > 0)
{
    Console.Error.WriteLine("PlayableMapHandoffQa FAILED");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    Environment.Exit(1);
}

Console.WriteLine(
    $"PlayableMapHandoffQa PASSED: buildings {baked.Buildings.Count}, units {baked.Units.Count}, environment metadata {baked.Triggers.Count + baked.Objectives.Count + baked.NarrativeNodes.Count}.");
