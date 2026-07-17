namespace ProceduralRts.Core;

static class MapSpecSnapshot
{
    public static MapSpec Create(MapSpec source)
    {
        return new MapSpec
        {
            Id = source.Id,
            Seed = source.Seed,
            WorldSize = source.WorldSize,
            OwnerStarts = source.OwnerStarts.Select(item => item with { }).ToArray(),
            TerrainCells = source.TerrainCells.Select(item => item with { }).ToArray(),
            Resources = source.Resources.Select(item => item with { }).ToArray(),
            Obstacles = source.Obstacles.Select(item => item with { }).ToArray(),
            Buildings = source.Buildings.Select(item => item with { }).ToArray(),
            Units = source.Units.Select(item => item with { }).ToArray(),
            Triggers = source.Triggers.Select(item => item with { }).ToArray(),
            Objectives = source.Objectives.Select(item => item with { }).ToArray(),
            NarrativeNodes = source.NarrativeNodes.Select(item => item with { }).ToArray(),
        };
    }
}
