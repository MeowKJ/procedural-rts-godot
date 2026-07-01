namespace ProceduralRts.Core;

public enum ResourceDepletionBehavior
{
    DepleteToZero,
    DepleteThenRegrow
}

public enum ResourceVisibilityRule
{
    VisibleWhenExplored,
    RequiresCurrentVision
}

public enum ResourceCorruptionState
{
    Clean,
    Tainted,
    Hostile
}
