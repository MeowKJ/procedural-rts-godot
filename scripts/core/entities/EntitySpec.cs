namespace ProceduralRts.Core;

public sealed record EntitySpec
{
    public required string Id { get; init; }
    public required EntityKind Kind { get; init; }
    public required EntityDisplaySpec Display { get; init; }
    public IReadOnlySet<string> Tags { get; init; } = new HashSet<string>();
    public StatsSpec? Stats { get; init; }
    public MovementSpec? Movement { get; init; }
    public CollisionSpec? Collision { get; init; }
    public IReadOnlyList<WeaponMountSpec> Weapons { get; init; } = [];
    public IReadOnlyList<AbilitySpec> Abilities { get; init; } = [];
    public ProductionSpec? Production { get; init; }
    public UnitArtRecipe? UnitArt { get; init; }
    public EntityAuthoringMetadata Authoring { get; init; } = new();
}
