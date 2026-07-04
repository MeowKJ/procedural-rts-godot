namespace ProceduralRts.Core;

public sealed record StatsSpec(
    UnitWeightClass WeightClass,
    ArmorTag ArmorTag,
    float MaxHp,
    float SightRange,
    int Cost,
    int TechTier,
    ElementDefenseProfile? ElementDefense = null,
    TargetTraitProfile? TargetTraits = null);

public sealed record MovementSpec(
    MovementDomain Domain,
    float Speed,
    float TurnRate,
    float Acceleration = 0,
    float StopDistance = 0,
    TurnMode TurnMode = TurnMode.PivotInPlace);

public sealed record CollisionSpec(
    float Radius,
    float Mass,
    int PushPriority,
    bool BlocksMovement = true);

public enum AbilityKind
{
    Harvest,
    RepairField,
    ShieldField,
    Scan,
    Deploy,
    Build
}

public enum AbilityTargetRule
{
    Auto,
    Self,
    Point,
    Entity,
    FriendlyEntity,
    HostileEntity,
    PointOrEntity,
    FriendlyPointOrEntity,
    HostilePointOrEntity
}

public sealed record AbilitySpec(
    AbilityKind Kind,
    float Radius = 0,
    float Value = 0,
    int Cost = 0,
    AbilityTargetRule TargetRule = AbilityTargetRule.Auto);

public sealed record ProductionSpec(
    string ProducerKind,
    ProductionCategory Category,
    float Duration,
    int LaneIndex,
    string LaneKey,
    IconGlyph CategoryIcon);

public sealed record UnitSpec(
    string Id,
    UnitArchetype Archetype,
    UnitFactionId Faction,
    string Label,
    string NameKey,
    string RoleKey,
    string ShortCode,
    IconGlyph Icon,
    IReadOnlySet<UnitRoleTag> RoleTags,
    StatsSpec Stats,
    MovementSpec Movement,
    CollisionSpec Collision,
    IReadOnlyList<WeaponMountSpec> Weapons,
    IReadOnlyList<AbilitySpec> Abilities,
    ProductionSpec? Production,
    UnitArtRecipe Art)
{
    public WeaponMountSpec PrimaryWeapon => Weapons[0];

    public bool HasAbility(AbilityKind kind)
    {
        for (var index = 0; index < Abilities.Count; index++)
        {
            if (Abilities[index].Kind == kind)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetAbility(AbilityKind kind, out AbilitySpec ability)
    {
        for (var index = 0; index < Abilities.Count; index++)
        {
            if (Abilities[index].Kind == kind)
            {
                ability = Abilities[index];
                return true;
            }
        }

        ability = null!;
        return false;
    }
}
