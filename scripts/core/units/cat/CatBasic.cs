using Godot;

namespace ProceduralRts.Core;

public sealed class CatBasic : UnitDesign
{
    public override string Id => "cat.basic";
    public override UnitArchetype Archetype => UnitArchetype.Infantry;
    public override UnitFactionId Faction => UnitFactionId.Cat;
    public override string Label => "Alley Runner";
    public override string NameKey => "unit.cat.basic.name";
    public override string RoleKey => "unit.role.infantry";
    public override string ShortCode => "CB";
    public override IconGlyph Icon => IconGlyph.Infantry;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Infantry, UnitRoleTag.Assault };
    public override StatsSpec Stats => new(UnitWeightClass.Light, ArmorTag.Infantry, 52, 355, 115, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 132, 11.8f);
    public override CollisionSpec Collision => new(12, 0.68f, 1);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.BodyFixed("main", WeaponKind.NeedleRifle, Vector2.Zero, new Vector2(17, 0), 0.58f, true),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.Barracks, ProductionCategory.Infantry, 5.2f, 0, "production.lane.infantry", IconGlyph.Infantry);
    public override UnitArtRecipe Art => CatUnitArt.Infantry("art.cat.basic", IconGlyph.Infantry);
}
