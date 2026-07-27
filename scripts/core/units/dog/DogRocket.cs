using Godot;

namespace ProceduralRts.Core;

public sealed class DogRocket : UnitDesign
{
    public override string Id => "dog.rocket";
    public override UnitArchetype Archetype => UnitArchetype.RocketUnit;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Rocket Dog";
    public override string NameKey => "unit.dog.rocket.name";
    public override string RoleKey => "unit.role.rocket";
    public override string ShortCode => "DRK";
    public override IconGlyph Icon => IconGlyph.AttackMove;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Infantry, UnitRoleTag.AntiAir, UnitRoleTag.Support };
    public override StatsSpec Stats => new(UnitWeightClass.Light, ArmorTag.Infantry, 46, 390, 160, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 112, 9.6f);
    public override CollisionSpec Collision => new(13, 0.75f, 1);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.BodyFixed("main", WeaponIds.RocketPod, Vector2.Zero, new Vector2(18, 0), 0.84f, true),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.Barracks, ProductionCategory.Infantry, 6.5f, 0, "production.lane.infantry", IconGlyph.AttackMove);
    public override UnitArtRecipe Art => DogUnitArt.Infantry("art.dog.rocket", IconGlyph.AttackMove);
}
