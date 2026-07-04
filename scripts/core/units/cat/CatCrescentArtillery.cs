using Godot;

namespace ProceduralRts.Core;

public sealed class CatCrescentArtillery : UnitDesign
{
    public override string Id => "cat.crescent_artillery";
    public override UnitArchetype Archetype => UnitArchetype.SiegeArtillery;
    public override UnitFactionId Faction => UnitFactionId.Cat;
    public override string Label => "Crescent Artillery";
    public override string NameKey => "unit.catCrescentArtillery.name";
    public override string RoleKey => "unit.catCrescentArtillery.role";
    public override string ShortCode => "CART";
    public override IconGlyph Icon => IconGlyph.Turret;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Siege, UnitRoleTag.Support };
    public override StatsSpec Stats => new(UnitWeightClass.Heavy, ArmorTag.Vehicle, 126, 545, 440, 3);
    public override MovementSpec Movement => new(MovementDomain.Land, 90, 5.0f, TurnMode: TurnMode.ArcTurn);
    public override CollisionSpec Collision => new(25, 1.7f, 3);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Independent("main", WeaponKind.RocketPod, Vector2.Zero, new Vector2(31, 0), 0.36f, 5.8f, false),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.VehicleFactory, ProductionCategory.Vehicle, 13.0f, 3, "production.lane.vehicle", IconGlyph.AttackMove);
    public override UnitArtRecipe Art => CatUnitArt.Vehicle("art.cat.crescent_artillery", IconGlyph.AttackMove);
}
