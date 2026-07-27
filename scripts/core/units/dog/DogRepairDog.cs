using Godot;

namespace ProceduralRts.Core;

public sealed class DogRepairDog : UnitDesign
{
    public override string Id => "dog.repair_dog";
    public override UnitArchetype Archetype => UnitArchetype.RepairSupport;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Repair Dog";
    public override string NameKey => "unit.dogRepairDog.name";
    public override string RoleKey => "unit.dogRepairDog.role";
    public override string ShortCode => "RDOG";
    public override IconGlyph Icon => IconGlyph.Infantry;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Infantry, UnitRoleTag.Repair, UnitRoleTag.Support };
    public override StatsSpec Stats => new(UnitWeightClass.Light, ArmorTag.Infantry, 58, 360, 155, 2);
    public override MovementSpec Movement => new(MovementDomain.Land, 150, 11.2f);
    public override CollisionSpec Collision => new(14, 0.72f, 1);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Omni("main", WeaponIds.ElectromagneticEmitter, Vector2.Zero, true),
    ];

    public override IReadOnlyList<AbilitySpec> Abilities => [new(AbilityKind.RepairField, Radius: 145, Value: 18)];
    public override ProductionSpec Production => new(BuildingDesignIds.Barracks, ProductionCategory.Infantry, 7.0f, 1, "production.lane.infantry", IconGlyph.Settings);
    public override UnitArtRecipe Art => DogUnitArt.Infantry("art.dog.repair_dog", IconGlyph.Settings);
}
