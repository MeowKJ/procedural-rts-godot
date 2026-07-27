using Godot;

namespace ProceduralRts.Core;

public sealed class CatSniper : UnitDesign
{
    public override string Id => "cat.sniper";
    public override UnitArchetype Archetype => UnitArchetype.Sniper;
    public override UnitFactionId Faction => UnitFactionId.Cat;
    public override string Label => "Sniper Cat";
    public override string NameKey => "unit.catSniper.name";
    public override string RoleKey => "unit.catSniper.role";
    public override string ShortCode => "CSNP";
    public override IconGlyph Icon => IconGlyph.Infantry;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Infantry, UnitRoleTag.Scout, UnitRoleTag.Assault };
    public override StatsSpec Stats => new(UnitWeightClass.Light, ArmorTag.Infantry, 38, 470, 385, 2);
    public override MovementSpec Movement => new(MovementDomain.Land, 102, 9.2f);
    public override CollisionSpec Collision => new(12, 0.62f, 1);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.BodyFixed("main", WeaponIds.IonEmitter, Vector2.Zero, new Vector2(18, 0), 0.38f, false),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.Barracks, ProductionCategory.Infantry, 7.2f, 1, "production.lane.infantry", IconGlyph.AttackMove);
    public override UnitArtRecipe Art => CatUnitArt.Infantry("art.cat.sniper", IconGlyph.AttackMove);
}
