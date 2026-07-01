namespace ProceduralRts.Core;

public sealed record SimInvariantViolation(EntityId EntityId, string Component, string Message)
{
    public override string ToString()
    {
        var entity = EntityId.IsValid ? $"entity {EntityId.Value}" : "world";
        return $"{entity} [{Component}]: {Message}";
    }
}
