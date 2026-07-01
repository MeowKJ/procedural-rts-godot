namespace ProceduralRts.Core;

public static class AdvancedPathingPolicy
{
    public const bool PreferDirectLineBeforeAStar = true;
    public const bool UseAStarOnlyWhenDirectLineBlocked = true;
    public const bool PreserveRawAStarCellsForDebug = true;
    public const bool SmoothCollinearWaypoints = true;
    public const bool PruneWaypointsByLineOfSight = true;
    public const bool TreatCombatAnchorsAsGlobalBlockers = true;
    public const bool RouteAroundDenseIdleUnitBlobs = true;
    public const bool UseSpatialGridLocalAvoidance = true;

    public const float LineOfSightProbeCellFraction = 0.25f;
    public const float StuckRepathAfterSeconds = 0.7f;
    public const float RepathCooldownSeconds = 1.35f;
    public const float RepathProgressEpsilon = 2.4f;

    public static readonly string[] OrderedStages =
    [
        "direct-line-first",
        "astar-fallback",
        "raw-cell-debug",
        "collinear-smoothing",
        "line-of-sight-pruning",
        "corridor-following",
        "local-detour-window",
        "repath-throttling",
    ];
}
