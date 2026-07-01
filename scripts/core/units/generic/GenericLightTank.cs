using Godot;

namespace ProceduralRts.Core;

public sealed class GenericLightTank : UnitDesign
{
    public override string Id => "generic.light_tank";
    public override UnitArchetype Archetype => UnitArchetype.GuardTank;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Vector Tank";
    public override string NameKey => "unit.lightTank.name";
    public override string RoleKey => "unit.lightTank.role";
    public override string ShortCode => "TNK";
    public override IconGlyph Icon => IconGlyph.Tank;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Assault };
    public override StatsSpec Stats => new(UnitWeightClass.Medium, ArmorTag.Vehicle, 120, 430, 420, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 132, 7.5f);
    public override CollisionSpec Collision => new(21, 1.05f, 2);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Independent("main", WeaponKind.VectorCannon, Vector2.Zero, new Vector2(24, 0), 0.42f, 8, true),
    ];

    public override UnitArtRecipe Art => DogUnitArt.Vehicle("art.generic.light_tank", IconGlyph.Tank);
}
