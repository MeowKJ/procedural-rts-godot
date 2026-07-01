using Godot;

namespace ProceduralRts.Core;

public sealed class DogSkyPatrolAircraft : UnitDesign
{
    public override string Id => "dog.sky_patrol_aircraft";
    public override UnitArchetype Archetype => UnitArchetype.ScoutAircraft;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Dog Sky Patrol";
    public override string NameKey => "unit.dogSkyPatrolAircraft.name";
    public override string RoleKey => "unit.dogSkyPatrolAircraft.role";
    public override string ShortCode => "DAIR";
    public override IconGlyph Icon => IconGlyph.Air;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Aircraft, UnitRoleTag.Scout, UnitRoleTag.AntiAir };
    public override StatsSpec Stats => new(UnitWeightClass.Light, ArmorTag.Aircraft, 72, 600, 220, 2);
    public override MovementSpec Movement => new(MovementDomain.Air, 205, 11.4f);
    public override CollisionSpec Collision => new(18, 0.55f, 1, BlocksMovement: false);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.BodyFixed("main", WeaponKind.SkySpear, Vector2.Zero, new Vector2(25, 0), 0.74f, true),
    ];

    public override ProductionSpec Production => new(BuildingDesignIds.Airfield, ProductionCategory.Air, 9.2f, 0, "production.lane.air", IconGlyph.Air);
    public override UnitArtRecipe Art => DogUnitArt.Aircraft("art.dog.sky_patrol_aircraft", IconGlyph.Air);
}
