namespace ProceduralRts.Core;

public readonly record struct LocalAvoidanceBody(
    int Id,
    float X,
    float Y,
    float Radius,
    int AnchorPriority,
    bool CanBeDisplaced)
{
    public bool IsAnchor => AnchorPriority > 0;
}

public readonly record struct LocalAvoidanceVector(float X, float Y);
