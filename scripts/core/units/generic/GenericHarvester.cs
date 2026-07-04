using Godot;

namespace ProceduralRts.Core;

public sealed class GenericHarvester : UnitDesign
{
    public override string Id => "generic.harvester";
    public override UnitArchetype Archetype => UnitArchetype.Harvester;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Ion Harvester";
    public override string NameKey => "unit.harvester.name";
    public override string RoleKey => "unit.harvester.role";
    public override string ShortCode => "HAR";
    public override IconGlyph Icon => IconGlyph.Harvester;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle, UnitRoleTag.Economy, UnitRoleTag.Worker };
    public override StatsSpec Stats => new(UnitWeightClass.Heavy, ArmorTag.Vehicle, 180, 280, 620, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 92, 5.5f, TurnMode: TurnMode.ArcTurn);
    public override CollisionSpec Collision => new(25, 1.6f, 3);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.Omni("main", WeaponKind.ElectromagneticEmitter, Vector2.Zero, true),
    ];

    public override IReadOnlyList<AbilitySpec> Abilities => [new(AbilityKind.Harvest, Radius: 128, Value: 700)];
    public override UnitArtRecipe Art => DogUnitArt.Harvester("art.generic.harvester");
}
