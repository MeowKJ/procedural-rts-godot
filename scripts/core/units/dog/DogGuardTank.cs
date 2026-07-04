using Godot;

namespace ProceduralRts.Core;

public sealed class DogGuardTank : UnitDesign
{
    public override string Id => "dog.guard_tank";
    public override UnitArchetype Archetype => UnitArchetype.GuardTank;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Guard Tank";
    public override string NameKey => "unit.dog.guardTank.name";
    public override string RoleKey => "unit.role.guardTank";
    public override string ShortCode => "GDT";
    public override IconGlyph Icon => IconGlyph.Tank;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Assault };
    public override StatsSpec Stats => new(UnitWeightClass.Medium, ArmorTag.Vehicle, 145, 455, 330, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 126, 7.1f, TurnMode: TurnMode.ArcTurn);
    public override CollisionSpec Collision => new(23, 1.25f, 2);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Independent("main", WeaponKind.VectorCannon, Vector2.Zero, new Vector2(24, 0), 0.42f, 8, true),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.VehicleFactory, ProductionCategory.Vehicle, 8.5f, 0, "production.lane.vehicle", IconGlyph.Tank);
    public override UnitArtRecipe Art => DogUnitArt.Vehicle("art.dog.guard_tank", IconGlyph.Tank);
}
