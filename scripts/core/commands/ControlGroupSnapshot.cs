namespace ProceduralRts.Core;

public readonly record struct ControlGroupSnapshot(
    int Number,
    int InfantryCount,
    int TankCount,
    int HarvesterCount,
    bool Active,
    float FeedbackPulse)
{
    public int TotalCount => InfantryCount + TankCount + HarvesterCount;
}
