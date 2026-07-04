using Godot;

namespace ProceduralRts.Core;

public sealed class DogShieldTank : UnitDesign
{
    public override string Id => "dog.shield_tank";
    public override UnitArchetype Archetype => UnitArchetype.ShieldVehicle;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Dog Shield Tank";
    public override string NameKey => "unit.dogShieldTank.name";
    public override string RoleKey => "unit.dogShieldTank.role";
    public override string ShortCode => "DSHD";
    public override IconGlyph Icon => IconGlyph.Tank;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Shield, UnitRoleTag.Support };
    public override StatsSpec Stats => new(UnitWeightClass.Heavy, ArmorTag.Vehicle, 210, 410, 250, 2);
    public override MovementSpec Movement => new(MovementDomain.Land, 102, 5.9f, TurnMode: TurnMode.ArcTurn);
    public override CollisionSpec Collision => new(27, 1.55f, 3);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Omni("main", WeaponKind.ElectromagneticEmitter, Vector2.Zero, true),
    ];

    public override IReadOnlyList<AbilitySpec> Abilities => [new(AbilityKind.ShieldField, Radius: 150, Value: 0.55f)];
    public override ProductionSpec Production => new(BuildingDesignIds.VehicleFactory, ProductionCategory.Defense, 10.0f, 2, "production.lane.vehicle", IconGlyph.StanceHold);
    public override UnitArtRecipe Art => DogUnitArt.Vehicle("art.dog.shield_tank", IconGlyph.StanceHold);
}
