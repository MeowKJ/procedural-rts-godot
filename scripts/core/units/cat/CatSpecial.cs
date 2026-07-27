using Godot;

namespace ProceduralRts.Core;

public sealed class CatSpecial : UnitDesign
{
    public override string Id => "cat.special";
    public override UnitArchetype Archetype => UnitArchetype.Special;
    public override UnitFactionId Faction => UnitFactionId.Cat;
    public override string Label => "Special Cat";
    public override string NameKey => "unit.catSpecial.name";
    public override string RoleKey => "unit.catSpecial.role";
    public override string ShortCode => "CSPC";
    public override IconGlyph Icon => IconGlyph.Infantry;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Infantry, UnitRoleTag.Scout, UnitRoleTag.Support };
    public override StatsSpec Stats => new(UnitWeightClass.Light, ArmorTag.Infantry, 62, 430, 260, 3);
    public override MovementSpec Movement => new(MovementDomain.Land, 146, 12.1f);
    public override CollisionSpec Collision => new(14, 0.9f, 1);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.BodyFixed("main", WeaponIds.IonEmitter, Vector2.Zero, new Vector2(18, 0), 0.72f, true),
    ];

    public override IReadOnlyList<AbilitySpec> Abilities => [new(AbilityKind.Scan, Radius: 420, Value: 5)];
    public override ProductionSpec Production => new(BuildingDesignIds.Barracks, ProductionCategory.Infantry, 10.8f, 3, "production.lane.infantry", IconGlyph.StanceAggressive);
    public override UnitArtRecipe Art => CatUnitArt.Infantry("art.cat.special", IconGlyph.StanceAggressive);
}
