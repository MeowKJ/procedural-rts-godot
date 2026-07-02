using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyProductionAi
{
    private static readonly Vector2[] PowerPlantBuildOffsets =
    [
        new(210, -185),
        new(250, 0),
        new(170, 185),
    ];

    private static readonly Vector2[] RefineryBuildOffsets =
    [
        new(325, 210),
        new(385, -170),
        new(480, 40),
    ];

    private static readonly Vector2[] BarracksBuildOffsets =
    [
        new(230, -320),
        new(365, -295),
        new(150, -380),
    ];

    private static readonly Vector2[] VehicleFactoryBuildOffsets =
    [
        new(250, 330),
        new(420, 285),
        new(125, 390),
    ];

    private static readonly Vector2[] GroundTurretBuildOffsets =
    [
        new(-155, 0),
        new(-110, -145),
        new(-110, 145),
    ];

    private static readonly Vector2[] DefaultBuildOffsets =
    [
        new(260, 0),
        new(0, 260),
        new(0, -260),
    ];

    private static IReadOnlyList<Vector2> CandidateBuildOffsets(string kind)
    {
        return kind switch
        {
            BuildingDesignIds.PowerPlant => PowerPlantBuildOffsets,
            BuildingDesignIds.Refinery => RefineryBuildOffsets,
            BuildingDesignIds.Barracks => BarracksBuildOffsets,
            BuildingDesignIds.VehicleFactory => VehicleFactoryBuildOffsets,
            BuildingDesignIds.GroundTurret => GroundTurretBuildOffsets,
            _ => DefaultBuildOffsets,
        };
    }
}
