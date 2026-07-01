using Godot;

namespace ProceduralRts.Core;

public sealed class CatShieldVehicle : UnitDesign
{
    public override string Id => "cat.shield_vehicle";
    public override UnitArchetype Archetype => UnitArchetype.ShieldVehicle;
    public override UnitFactionId Faction => UnitFactionId.Cat;
    public override string Label => "Cat Shield Vehicle";
    public override string NameKey => "unit.catShieldVehicle.name";
    public override string RoleKey => "unit.catShieldVehicle.role";
    public override string ShortCode => "CSHD";
    public override IconGlyph Icon => IconGlyph.Tank;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Shield, UnitRoleTag.Support };
    public override StatsSpec Stats => new(UnitWeightClass.Heavy, ArmorTag.Vehicle, 190, 420, 250, 2);
    public override MovementSpec Movement => new(MovementDomain.Land, 108, 6.1f);
    public override CollisionSpec Collision => new(26, 1.45f, 3);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Omni("main", WeaponKind.ElectromagneticEmitter, Vector2.Zero, true),
    ];

    public override IReadOnlyList<AbilitySpec> Abilities => [new(AbilityKind.ShieldField, Radius: 145, Value: 0.55f)];
    public override ProductionSpec Production => new(BuildingDesignIds.VehicleFactory, ProductionCategory.Defense, 9.8f, 2, "production.lane.vehicle", IconGlyph.StanceHold);
    public override UnitArtRecipe Art => CatUnitArt.Vehicle("art.cat.shield_vehicle", IconGlyph.StanceHold);
}
