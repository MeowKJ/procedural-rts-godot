using Godot;

namespace ProceduralRts.Core;

public sealed class CatScoutCar : UnitDesign
{
    public override string Id => "cat.scout_car";
    public override UnitArchetype Archetype => UnitArchetype.PatrolVehicle;
    public override UnitFactionId Faction => UnitFactionId.Cat;
    public override string Label => "Slipstream Car";
    public override string NameKey => "unit.cat.scoutCar.name";
    public override string RoleKey => "unit.role.patrolVehicle";
    public override string ShortCode => "CSC";
    public override IconGlyph Icon => IconGlyph.Move;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Scout, UnitRoleTag.AntiAir };
    public override StatsSpec Stats => new(UnitWeightClass.Medium, ArmorTag.Vehicle, 82, 500, 220, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 170, 9.4f, TurnMode: TurnMode.ArcTurn);
    public override CollisionSpec Collision => new(17, 1.0f, 2);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Independent("main", WeaponIds.LightRepeater, Vector2.Zero, new Vector2(22, 0), 0.88f, 10.5f, true),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.VehicleFactory, ProductionCategory.Vehicle, 6.2f, 0, "production.lane.vehicle", IconGlyph.Move);
    public override UnitArtRecipe Art => CatUnitArt.Vehicle("art.cat.scout_car", IconGlyph.Move);
}
