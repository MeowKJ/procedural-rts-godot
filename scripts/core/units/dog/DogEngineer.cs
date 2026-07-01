using Godot;

namespace ProceduralRts.Core;

public sealed class DogEngineer : UnitDesign
{
    public override string Id => "dog.engineer";
    public override UnitArchetype Archetype => UnitArchetype.Engineer;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Field Engineer";
    public override string NameKey => "unit.dog.engineer.name";
    public override string RoleKey => "unit.role.engineer";
    public override string ShortCode => "DEN";
    public override IconGlyph Icon => IconGlyph.Settings;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Infantry, UnitRoleTag.Repair, UnitRoleTag.Support };
    public override StatsSpec Stats => new(UnitWeightClass.Light, ArmorTag.Infantry, 42, 330, 150, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 116, 10.8f);
    public override CollisionSpec Collision => new(12, 0.7f, 1);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.BodyFixed("main", WeaponKind.NeedleRifle, Vector2.Zero, new Vector2(15, 0), 0.62f, true),
    ];

    public override IReadOnlyList<AbilitySpec> Abilities =>
    [
        new(AbilityKind.RepairField, Radius: 130, Value: 16),
        new(AbilityKind.Build, Radius: 220),
    ];
    public override ProductionSpec Production => new(BuildingDesignIds.Barracks, ProductionCategory.Infantry, 6.0f, 0, "production.lane.infantry", IconGlyph.Settings);
    public override UnitArtRecipe Art => DogUnitArt.Infantry("art.dog.engineer", IconGlyph.Settings);
}
