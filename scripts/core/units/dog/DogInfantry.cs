using Godot;

namespace ProceduralRts.Core;

public sealed class DogInfantry : UnitDesign
{
    public override string Id => "dog.infantry";
    public override UnitArchetype Archetype => UnitArchetype.Infantry;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Patrol Dog";
    public override string NameKey => "unit.dog.infantry.name";
    public override string RoleKey => "unit.role.infantry";
    public override string ShortCode => "DIN";
    public override IconGlyph Icon => IconGlyph.Infantry;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Infantry, UnitRoleTag.Assault };
    public override StatsSpec Stats => new(UnitWeightClass.Light, ArmorTag.Infantry, 52, 350, 120, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 122, 10.5f);
    public override CollisionSpec Collision => new(13, 0.75f, 1);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.BodyFixed("main", WeaponKind.NeedleRifle, Vector2.Zero, new Vector2(17, 0), 0.62f, true),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.Barracks, ProductionCategory.Infantry, 5.5f, 0, "production.lane.infantry", IconGlyph.Infantry);
    public override UnitArtRecipe Art => DogUnitArt.Infantry("art.dog.infantry", IconGlyph.Infantry);
}
