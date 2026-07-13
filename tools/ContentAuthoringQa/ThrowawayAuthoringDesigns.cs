using Godot;
using ProceduralRts.Core;

public sealed class ThrowawayProbeUnitDesign : UnitDesign
{
    public override string Id => "qa.throwaway.probe_unit";
    public override UnitArchetype Archetype => UnitArchetype.Infantry;
    public override UnitFactionId Faction => UnitFactionId.Dog;
    public override string Label => "Throwaway Probe";
    public override string NameKey => "qa.throwaway.probe_unit.name";
    public override string RoleKey => "qa.throwaway.probe_unit.role";
    public override string ShortCode => "QPU";
    public override IconGlyph Icon => IconGlyph.Infantry;
    public override IReadOnlySet<UnitRoleTag> RoleTags => new HashSet<UnitRoleTag> { UnitRoleTag.Infantry, UnitRoleTag.Assault };
    public override StatsSpec Stats => new(UnitWeightClass.Light, ArmorTag.Infantry, 64, 520, 1, 1);
    public override MovementSpec Movement => new(MovementDomain.Land, 140, 11);
    public override CollisionSpec Collision => new(12, 0.7f, 1);
    public override IReadOnlyList<WeaponMountSpec> Weapons =>
    [
        WeaponMountSpec.BodyFixed("main", ThrowawayProbeWeaponDesign.WeaponId, Vector2.Zero, new Vector2(16, 0), 0.75f, true),
    ];

    public override UnitArtRecipe Art => DogUnitArt.Infantry("art.qa.throwaway.probe_unit", IconGlyph.Infantry);
}

public sealed class ThrowawayProbeWeaponDesign : WeaponDesign
{
    public const string WeaponId = "weapon.qa.throwaway.probe";
    public const string AmmoId = "ammo.qa.throwaway.spark";

    public override string Id => WeaponId;

    public override WeaponDefinition ToDefinition()
    {
        return new WeaponDefinition(
            Id,
            "Throwaway Probe Spark",
            AmmoId,
            WeaponMountKind.FixedForward,
            170,
            0.32f,
            0.75f,
            true,
            CombatProfileDesign.TargetProfile(
                domains: [MovementDomain.Land],
                armor: [ArmorTag.Infantry, ArmorTag.Vehicle],
                weights: new() { [UnitWeightClass.Light] = 1.2f, [UnitWeightClass.Medium] = 0.8f },
                armorPriority: new() { [ArmorTag.Infantry] = 1.2f, [ArmorTag.Vehicle] = 0.5f }),
            SpecialAttackHook.FireAuthorization | SpecialAttackHook.ProjectileUpdate,
            MinRange: 0);
    }
}

public sealed class ThrowawayProbeAmmoDesign : AmmoDesign
{
    public override string Id => ThrowawayProbeWeaponDesign.AmmoId;

    public override AmmoDefinition ToDefinition()
    {
        return new AmmoDefinition(
            Id,
            "Throwaway Probe Spark",
            ProjectileBehavior.Direct,
            HitRule.Guaranteed,
            820,
            2,
            0,
            0,
            1,
            0,
            new Color("#a7ffd1"),
            CombatProfileDesign.DamageProfile(
                weights: new() { [UnitWeightClass.Light] = 1.05f, [UnitWeightClass.Medium] = 0.9f },
                armor: new() { [ArmorTag.Infantry] = 1.15f, [ArmorTag.Vehicle] = 0.65f }),
            SpecialAttackHook.ProjectileUpdate | SpecialAttackHook.Impact);
    }
}

public sealed class ThrowawayProbeBuildingDesign : BuildingDesign
{
    public override string Kind => "qa.throwaway.probe_building";
    public override int SortOrder => 9999;

    public override BuildSpec ToSpec()
    {
        return new BuildSpec(
            Kind,
            "qa.throwaway.probe_building",
            "Throwaway Probe Building",
            180,
            new Vector2(64, 56),
            new PlacementGridFootprint(2, 2),
            280,
            ArmorTag.Structure,
            null,
            new Color("#8fd9c4"),
            BuildCategory.Defense,
            IconGlyph.Building,
            1,
            0.15f,
            null,
            new HashSet<string>(),
            0,
            0,
            0,
            MovementDomain.Land);
    }
}
