using Godot;

namespace ProceduralRts.Core;

public sealed class GenericInfantry : UnitDesign
{
    public override string Id => "generic.infantry";
    public override UnitArchetype Archetype => UnitArchetype.Infantry;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Pulse Infantry";
    public override string NameKey => "unit.infantry.name";
    public override string RoleKey => "unit.infantry.role";
    public override string ShortCode => "INF";
    public override IconGlyph Icon => IconGlyph.Infantry;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Infantry, UnitRoleTag.Assault };
    public override StatsSpec Stats => new(UnitWeightClass.Light, ArmorTag.Infantry, 46, 330, 120, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 118, 10);
    public override CollisionSpec Collision => new(13, 0.55f, 1);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.BodyFixed("main", WeaponIds.NeedleRifle, Vector2.Zero, new Vector2(17, 0), 0.62f, true),
    ];

    public override UnitArtRecipe Art => DogUnitArt.Infantry("art.generic.infantry", IconGlyph.Infantry);
}
