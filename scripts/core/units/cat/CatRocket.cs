using Godot;

namespace ProceduralRts.Core;

public sealed class CatRocket : UnitDesign
{
    public override string Id => "cat.rocket";
    public override UnitArchetype Archetype => UnitArchetype.RocketUnit;
    public override UnitFactionId Faction => UnitFactionId.Cat;
    public override string Label => "Rocket Cat";
    public override string NameKey => "unit.catRocket.name";
    public override string RoleKey => "unit.catRocket.role";
    public override string ShortCode => "CRKT";
    public override IconGlyph Icon => IconGlyph.AttackMove;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Infantry, UnitRoleTag.AntiAir, UnitRoleTag.Support };
    public override StatsSpec Stats => new(UnitWeightClass.Light, ArmorTag.Infantry, 48, 395, 305, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 112, 9.6f);
    public override CollisionSpec Collision => new(13, 0.72f, 1);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.BodyFixed("main", WeaponIds.RocketPod, Vector2.Zero, new Vector2(18, 0), 0.82f, true),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.Barracks, ProductionCategory.Infantry, 6.4f, 0, "production.lane.infantry", IconGlyph.AttackMove);
    public override UnitArtRecipe Art => CatUnitArt.Infantry("art.cat.rocket", IconGlyph.AttackMove);
}
