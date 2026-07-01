namespace ProceduralRts.Core;

public readonly record struct UnitProjectionDriftReport(
    int UnitCount,
    int MissingMirrors,
    float MaxPositionDrift,
    float MaxFacingDrift);
