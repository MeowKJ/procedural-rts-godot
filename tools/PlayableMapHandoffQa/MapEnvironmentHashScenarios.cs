using ProceduralRts.Core;

internal static class MapEnvironmentHashScenarios
{
    public static void Run(MapSpec map, List<string> failures)
    {
        var baseline = MapLoader.Load(map).DeterministicStateHash();
        Require(baseline == MapLoader.Load(map).DeterministicStateHash(),
            "identical authored environment input should produce the same hash.", failures);

        AssertDelta(map, baseline, "owner-start", map with
        {
            OwnerStarts =
            [
                map.OwnerStarts[0] with { Position = map.OwnerStarts[0].Position + new MapOffset(1, 0) },
                .. map.OwnerStarts.Skip(1),
            ],
        }, failures);
        AssertDelta(map, baseline, "trigger", map with
        {
            Triggers = [map.Triggers[0] with { EventKey = map.Triggers[0].EventKey + ".delta" }, .. map.Triggers.Skip(1)],
        }, failures);
        AssertDelta(map, baseline, "objective", map with
        {
            Objectives = [map.Objectives[0] with { ObjectiveKey = map.Objectives[0].ObjectiveKey + ".delta" }, .. map.Objectives.Skip(1)],
        }, failures);
        AssertDelta(map, baseline, "narrative", map with
        {
            NarrativeNodes = [map.NarrativeNodes[0] with { TextKey = map.NarrativeNodes[0].TextKey + ".delta" }, .. map.NarrativeNodes.Skip(1)],
        }, failures);
    }

    private static void AssertDelta(
        MapSpec original,
        ulong baseline,
        string label,
        MapSpec changed,
        List<string> failures)
    {
        changed = changed with { Id = original.Id + ".hash-" + label };
        Require(MapLoader.Load(changed).DeterministicStateHash() != baseline,
            $"one-field {label} metadata delta should change the deterministic hash.", failures);
    }

    private static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }
}
