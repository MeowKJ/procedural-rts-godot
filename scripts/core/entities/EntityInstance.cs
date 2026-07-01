namespace ProceduralRts.Core;

public sealed class EntityInstance
{
    public required EntityId Id { get; init; }
    public required string SpecId { get; init; }
    public required OwnerId OwnerId { get; set; }
    public required EntityTransform Transform { get; set; }
    public EntityComponentSet Components { get; } = new();
}
