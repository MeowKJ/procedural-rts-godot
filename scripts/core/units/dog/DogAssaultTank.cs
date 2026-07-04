using Godot;

namespace ProceduralRts.Core;

public sealed class DogAssaultTank : UnitDesign
{
    public override string Id => "dog.assault_tank";
    public override UnitArchetype Archetype => UnitArchetype.AssaultTank;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Dog Assault Tank";
    public override string NameKey => "unit.dogAssaultTank.name";
    public override string RoleKey => "unit.dogAssaultTank.role";
    public override string ShortCode => "DAST";
    public override IconGlyph Icon => IconGlyph.Tank;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Assault, UnitRoleTag.Siege };
    public override StatsSpec Stats => new(UnitWeightClass.Heavy, ArmorTag.Vehicle, 185, 460, 360, 2);
    public override MovementSpec Movement => new(MovementDomain.Land, 116, 6.4f, TurnMode: TurnMode.ArcTurn);
    public override CollisionSpec Collision => new(26, 1.45f, 3);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Independent("main", WeaponKind.RocketPod, Vector2.Zero, new Vector2(29, 0), 0.56f, 7.2f, true),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.VehicleFactory, ProductionCategory.Vehicle, 10.5f, 2, "production.lane.vehicle", IconGlyph.Tank);
    public override UnitArtRecipe Art => DogUnitArt.Vehicle("art.dog.assault_tank", IconGlyph.AttackMove);
}
