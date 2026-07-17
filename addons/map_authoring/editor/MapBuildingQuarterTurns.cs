using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring.Editor;

public readonly record struct MapBuildingQuarterTurn(string Label, float Radians);

public sealed class MapAuthoringRotationException : InvalidOperationException
{
    public MapAuthoringRotationException(float radians)
        : base($"Building rotation '{radians:R}' is not one of the four canonical quarter-turn states.")
    {
    }
}

public static class MapBuildingQuarterTurns
{
    public static IReadOnlyList<MapBuildingQuarterTurn> All { get; } = Array.AsReadOnly(new[]
    {
        new MapBuildingQuarterTurn("0°", 0),
        new MapBuildingQuarterTurn("90°", MathF.PI * 0.5f),
        new MapBuildingQuarterTurn("180°", MathF.PI),
        new MapBuildingQuarterTurn("270°", -MathF.PI * 0.5f),
    });

    public static int IndexOf(float radians)
    {
        for (var index = 0; index < All.Count; index++)
        {
            var matches = MathF.Abs(radians - All[index].Radians) <= PlacementMath.CardinalFacingTolerance;
            if (index == 2)
            {
                matches |= MathF.Abs(MathF.Abs(radians) - MathF.PI) <= PlacementMath.CardinalFacingTolerance;
            }

            if (matches)
            {
                return index;
            }
        }

        return -1;
    }

    public static float RequirePersisted(float radians)
    {
        return Require(radians);
    }

    public static float RequireRootLocal(float radians)
    {
        return Require(radians);
    }

    private static float Require(float radians)
    {
        var index = IndexOf(radians);
        return index >= 0
            ? All[index].Radians
            : throw new MapAuthoringRotationException(radians);
    }
}
