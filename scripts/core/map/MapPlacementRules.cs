namespace ProceduralRts.Core;

public static class MapPlacementRules
{
    public const int ResourceClearanceCells = 1;

    public static float ResourceClearance(BuildSpec building)
    {
        return Math.Max(building.PlacementClearanceCells, ResourceClearanceCells)
            * PlacementMath.GridSize;
    }
}
