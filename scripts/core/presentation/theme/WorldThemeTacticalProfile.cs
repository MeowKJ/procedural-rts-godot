namespace ProceduralRts.Core;

public sealed record WorldThemeTacticalProfile(
    float PlanningClarity,
    float RebuildingFocus,
    float RepairFocus,
    float ResourceReadability,
    float TerrainReadability,
    float Pressure,
    float FogProminence,
    float LightNetworkSafety,
    float SignalNoise,
    float DefensiveCaution);
