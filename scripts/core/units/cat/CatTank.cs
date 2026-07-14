using Godot;

namespace ProceduralRts.Core;

public sealed class CatTank : UnitDesign
{
    public override string Id => "cat.tank";
    public override UnitArchetype Archetype => UnitArchetype.GuardTank;
    public override UnitFactionId Faction => UnitFactionId.Cat;
    public override string Label => "Crescent Tank";
    public override string NameKey => "unit.cat.tank.name";
    public override string RoleKey => "unit.role.guardTank";
    public override string ShortCode => "CTK";
    public override IconGlyph Icon => IconGlyph.Tank;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Assault };
    public override StatsSpec Stats => new(UnitWeightClass.Medium, ArmorTag.Vehicle, 145, 455, 320, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 136, 7.6f, TurnMode: TurnMode.ArcTurn);
    public override CollisionSpec Collision => new(22, 1.15f, 2);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Independent("main", WeaponKind.VectorCannon, Vector2.Zero, new Vector2(24, 0), 0.48f, 8.8f, true),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.VehicleFactory, ProductionCategory.Vehicle, 8.2f, 0, "production.lane.vehicle", IconGlyph.Tank);
    public override UnitArtRecipe Art => CatUnitArt.Vehicle("art.cat.tank", IconGlyph.Tank);
}
