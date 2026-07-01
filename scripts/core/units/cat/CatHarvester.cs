using Godot;

namespace ProceduralRts.Core;

public sealed class CatHarvester : UnitDesign
{
    public override string Id => "cat.harvester";
    public override UnitArchetype Archetype => UnitArchetype.Harvester;
    public override UnitFactionId Faction => UnitFactionId.Cat;
    public override string Label => "Cache Gatherer";
    public override string NameKey => "unit.cat.harvester.name";
    public override string RoleKey => "unit.role.harvester";
    public override string ShortCode => "CHV";
    public override IconGlyph Icon => IconGlyph.Harvester;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Economy, UnitRoleTag.Worker };
    public override StatsSpec Stats => new(UnitWeightClass.Heavy, ArmorTag.Vehicle, 176, 285, 125, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 96, 5.8f);
    public override CollisionSpec Collision => new(25, 1.5f, 3);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Omni("main", WeaponKind.ElectromagneticEmitter, Vector2.Zero, true),
    ];

    public override IReadOnlyList<AbilitySpec> Abilities => [new AbilitySpec(AbilityKind.Harvest, 128, 700)];
    public override ProductionSpec Production => new(BuildingDesignIds.VehicleFactory, ProductionCategory.Economy, 10.2f, 1, "production.lane.economy", IconGlyph.Harvester);
    public override UnitArtRecipe Art => CatUnitArt.Harvester("art.cat.harvester");
}
