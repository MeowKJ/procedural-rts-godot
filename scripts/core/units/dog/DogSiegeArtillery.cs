using Godot;

namespace ProceduralRts.Core;

public sealed class DogSiegeArtillery : UnitDesign
{
    public override string Id => "dog.siege_artillery";
    public override UnitArchetype Archetype => UnitArchetype.SiegeArtillery;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Dog Siege Artillery";
    public override string NameKey => "unit.dogSiegeArtillery.name";
    public override string RoleKey => "unit.dogSiegeArtillery.role";
    public override string ShortCode => "DSGE";
    public override IconGlyph Icon => IconGlyph.Turret;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Siege, UnitRoleTag.Support };
    public override StatsSpec Stats => new(UnitWeightClass.Heavy, ArmorTag.Vehicle, 135, 540, 455, 3);
    public override MovementSpec Movement => new(MovementDomain.Land, 86, 4.9f);
    public override CollisionSpec Collision => new(25, 1.8f, 3);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Independent("main", WeaponKind.VectorCannon, Vector2.Zero, new Vector2(32, 0), 0.34f, 5.5f, false),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.VehicleFactory, ProductionCategory.Vehicle, 13.0f, 3, "production.lane.vehicle", IconGlyph.AttackMove);
    public override UnitArtRecipe Art => DogUnitArt.Vehicle("art.dog.siege_artillery", IconGlyph.AttackMove);
}
