namespace ProceduralRts.Core;

public abstract class UnitDesign
{
    public abstract string Id { get; }
    public abstract UnitArchetype Archetype { get; }
    public abstract UnitFactionId Faction { get; }
    public abstract string Label { get; }
    public abstract string NameKey { get; }
    public abstract string RoleKey { get; }
    public abstract string ShortCode { get; }
    public abstract IconGlyph Icon { get; }
    public abstract IReadOnlySet<UnitRoleTag> RoleTags { get; }
    public abstract StatsSpec Stats { get; }
    public abstract MovementSpec Movement { get; }
    public abstract CollisionSpec Collision { get; }
    public abstract IReadOnlyList<WeaponMountSpec> Weapons { get; }
    public virtual IReadOnlyList<AbilitySpec> Abilities => [];
    public virtual ProductionSpec? Production => null;
    public abstract UnitArtRecipe Art { get; }

    public UnitSpec ToSpec()
    {
        return new UnitSpec(
            Id,
            Archetype,
            Faction,
            Label,
            NameKey,
            RoleKey,
            ShortCode,
            Icon,
            RoleTags,
            Stats,
            Movement,
            Collision,
            Weapons,
            Abilities,
            Production,
            Art);
    }
}
