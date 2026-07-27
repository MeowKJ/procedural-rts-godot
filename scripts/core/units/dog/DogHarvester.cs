using Godot;

namespace ProceduralRts.Core;

public sealed class DogHarvester : UnitDesign
{
    public override string Id => "dog.harvester";
    public override UnitArchetype Archetype => UnitArchetype.Harvester;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Retriever";
    public override string NameKey => "unit.dog.harvester.name";
    public override string RoleKey => "unit.role.harvester";
    public override string ShortCode => "DHR";
    public override IconGlyph Icon => IconGlyph.Harvester;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Worker, UnitRoleTag.Economy };
    public override StatsSpec Stats => new(UnitWeightClass.Heavy, ArmorTag.Vehicle, 190, 295, 620, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 92, 5.6f, TurnMode: TurnMode.ArcTurn);
    public override CollisionSpec Collision => new(25, 1.8f, 3);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Omni("main", WeaponIds.ElectromagneticEmitter, Vector2.Zero, true),
    ];

    public override IReadOnlyList<AbilitySpec> Abilities => [new(AbilityKind.Harvest, Radius: 96, Value: 700)];
    public override ProductionSpec Production => new(BuildingDesignIds.VehicleFactory, ProductionCategory.Economy, 10.5f, 1, "production.lane.economy", IconGlyph.Harvester);
    public override UnitArtRecipe Art => DogUnitArt.Harvester("art.dog.harvester");
}
