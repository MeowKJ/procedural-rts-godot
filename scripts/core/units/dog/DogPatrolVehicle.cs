using Godot;

namespace ProceduralRts.Core;

public sealed class DogPatrolVehicle : UnitDesign
{
    public override string Id => "dog.patrol_vehicle";
    public override UnitArchetype Archetype => UnitArchetype.PatrolVehicle;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Patrol Vehicle";
    public override string NameKey => "unit.dog.patrolVehicle.name";
    public override string RoleKey => "unit.role.patrolVehicle";
    public override string ShortCode => "DPV";
    public override IconGlyph Icon => IconGlyph.Move;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Scout, UnitRoleTag.AntiAir };
    public override StatsSpec Stats => new(UnitWeightClass.Medium, ArmorTag.Vehicle, 92, 460, 235, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 156, 8.8f, TurnMode: TurnMode.ArcTurn);
    public override CollisionSpec Collision => new(18, 1.1f, 2);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Independent("main", WeaponKind.LightRepeater, Vector2.Zero, new Vector2(22, 0), 0.9f, 10, true),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.VehicleFactory, ProductionCategory.Vehicle, 6.5f, 0, "production.lane.vehicle", IconGlyph.Move);
    public override UnitArtRecipe Art => DogUnitArt.Vehicle("art.dog.patrol_vehicle", IconGlyph.Move);
}
