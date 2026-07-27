using Godot;

namespace ProceduralRts.Core;

public sealed class CatScoutAircraft : UnitDesign
{
    public override string Id => "cat.scout_aircraft";
    public override UnitArchetype Archetype => UnitArchetype.ScoutAircraft;
    public override UnitFactionId Faction => UnitFactionId.Cat;
    public override string Label => "Cat Scout Aircraft";
    public override string NameKey => "unit.catScoutAircraft.name";
    public override string RoleKey => "unit.catScoutAircraft.role";
    public override string ShortCode => "CAIR";
    public override IconGlyph Icon => IconGlyph.Air;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Aircraft, UnitRoleTag.Scout, UnitRoleTag.Assault };
    public override StatsSpec Stats => new(UnitWeightClass.Light, ArmorTag.Aircraft, 64, 620, 185, 1);
    public override MovementSpec Movement => new(MovementDomain.Air, 220, 12.5f, TurnMode: TurnMode.ArcTurn);
    public override CollisionSpec Collision => new(17, 0.5f, 1, BlocksMovement: false);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.BodyFixed("main", WeaponIds.NeedleRifle, Vector2.Zero, new Vector2(24, 0), 0.62f, true),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.Airfield, ProductionCategory.Air, 8.0f, 0, "production.lane.air", IconGlyph.Air);
    public override UnitArtRecipe Art => CatUnitArt.Aircraft("art.cat.scout_aircraft", IconGlyph.Air);
}
