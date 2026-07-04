namespace ProceduralRts.Core;

public static class UnitDesignDefinitionCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, UnitSpecRuntimeDescriptor>> DiscoveredDescriptors = new(BuildDescriptors);

    public static IReadOnlyDictionary<string, UnitSpecRuntimeDescriptor> RuntimeDescriptors => DiscoveredDescriptors.Value;

    public static UnitSpecRuntimeDescriptor ForDesign(string designId)
    {
        return ForSpec(UnitDesignCatalog.Spec(designId));
    }

    public static UnitSpecRuntimeDescriptor ForSpec(UnitSpec spec)
    {
        var primaryWeapon = spec.PrimaryWeapon;
        var weapon = WeaponCatalog.WeaponDefinitions[primaryWeapon.WeaponId];
        var ammo = WeaponCatalog.AmmoDefinitions[weapon.AmmoId];
        return new UnitSpecRuntimeDescriptor(
            spec.Id,
            spec.Archetype,
            spec.Faction,
            spec.Label,
            spec.Stats.WeightClass,
            spec.Movement.Domain,
            spec.Stats.ArmorTag,
            primaryWeapon.WeaponKind,
            spec.Stats.MaxHp,
            spec.Collision.Radius,
            spec.Movement.Speed,
            spec.Movement.TurnRate,
            spec.Movement.TurnMode,
            spec.Stats.SightRange,
            weapon.Range,
            ammo.BaseDamage,
            weapon.Cooldown,
            ammo.Speed,
            SoftOldCityPalette.FactionColor(spec.Faction),
            spec.Stats.TechTier,
            spec.RoleTags,
            spec.Stats.ElementDefense,
            TargetTraitProfile.FromRoleTags(spec.RoleTags, spec.Stats.TargetTraits));
    }

    public static IEnumerable<UnitSpecRuntimeDescriptor> WithRole(UnitRoleTag roleTag)
    {
        return RuntimeDescriptors.Values.Where(descriptor => descriptor.RoleTags.Contains(roleTag));
    }

    private static IReadOnlyDictionary<string, UnitSpecRuntimeDescriptor> BuildDescriptors()
    {
        return UnitDesignCatalog.Designs.Values
            .Select(design => design.ToSpec())
            .ToDictionary(spec => spec.Id, ForSpec);
    }
}
