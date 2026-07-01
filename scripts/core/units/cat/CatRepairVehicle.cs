using Godot;

namespace ProceduralRts.Core;

public sealed class CatRepairVehicle : UnitDesign
{
    public override string Id => "cat.repair_vehicle";
    public override UnitArchetype Archetype => UnitArchetype.RepairSupport;
    public override UnitFactionId Faction => UnitFactionId.Cat;
    public override string Label => "Cat Repair Vehicle";
    public override string NameKey => "unit.catRepairVehicle.name";
    public override string RoleKey => "unit.catRepairVehicle.role";
    public override string ShortCode => "CREP";
    public override IconGlyph Icon => IconGlyph.Tank;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Repair, UnitRoleTag.Support };
    public override StatsSpec Stats => new(UnitWeightClass.Medium, ArmorTag.Vehicle, 108, 390, 160, 2);
    public override MovementSpec Movement => new(MovementDomain.Land, 118, 7.2f);
    public override CollisionSpec Collision => new(20, 1.2f, 2);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Omni("main", WeaponKind.ElectromagneticEmitter, Vector2.Zero, true),
    ];

    public override IReadOnlyList<AbilitySpec> Abilities => [new(AbilityKind.RepairField, Radius: 150, Value: 18)];
    public override ProductionSpec Production => new(BuildingDesignIds.VehicleFactory, ProductionCategory.Vehicle, 8.8f, 2, "production.lane.vehicle", IconGlyph.Settings);
    public override UnitArtRecipe Art => CatUnitArt.Vehicle("art.cat.repair_vehicle", IconGlyph.Settings);
}
