using Godot;

namespace ProceduralRts.Core;

public sealed class CatEngineer : UnitDesign
{
    public override string Id => "cat.engineer";
    public override UnitArchetype Archetype => UnitArchetype.Engineer;
    public override UnitFactionId Faction => UnitFactionId.Cat;
    public override string Label => "Cat Engineer";
    public override string NameKey => "unit.catEngineer.name";
    public override string RoleKey => "unit.catEngineer.role";
    public override string ShortCode => "CENG";
    public override IconGlyph Icon => IconGlyph.Infantry;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Infantry, UnitRoleTag.Repair, UnitRoleTag.Support };
    public override StatsSpec Stats => new(UnitWeightClass.Light, ArmorTag.Infantry, 40, 330, 170, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 122, 11.2f);
    public override CollisionSpec Collision => new(12, 0.68f, 1);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.BodyFixed("main", WeaponKind.NeedleRifle, Vector2.Zero, new Vector2(15, 0), 0.58f, true),
    ];

    public override IReadOnlyList<AbilitySpec> Abilities => [new(AbilityKind.RepairField, Radius: 122, Value: 14)];
    public override ProductionSpec Production => new(BuildingDesignIds.Barracks, ProductionCategory.Infantry, 6.0f, 1, "production.lane.infantry", IconGlyph.Settings);
    public override UnitArtRecipe Art => CatUnitArt.Infantry("art.cat.engineer", IconGlyph.Settings);
}
