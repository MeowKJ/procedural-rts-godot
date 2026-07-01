using System.Reflection;

namespace ProceduralRts.Core;

public static class WeaponCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, WeaponDefinition>> DiscoveredWeaponDefinitions = new(DiscoverWeapons);
    private static readonly Lazy<IReadOnlyDictionary<string, AmmoDefinition>> DiscoveredAmmoDefinitions = new(DiscoverAmmo);
    private static readonly Lazy<IReadOnlyDictionary<WeaponKind, WeaponDefinition>> LegacyWeapons = new(BuildLegacyWeapons);
    private static readonly Lazy<IReadOnlyDictionary<AmmoKind, AmmoDefinition>> LegacyAmmo = new(BuildLegacyAmmo);

    public static IReadOnlyDictionary<string, WeaponDefinition> WeaponDefinitions => DiscoveredWeaponDefinitions.Value;

    public static IReadOnlyDictionary<string, AmmoDefinition> AmmoDefinitions => DiscoveredAmmoDefinitions.Value;

    public static IReadOnlyDictionary<WeaponKind, WeaponDefinition> Weapons => LegacyWeapons.Value;

    public static IReadOnlyDictionary<AmmoKind, AmmoDefinition> Ammo => LegacyAmmo.Value;

    public static string IdFor(WeaponKind kind)
    {
        return $"weapon.{kind.ToString().ToLowerInvariant()}";
    }

    public static string IdFor(AmmoKind kind)
    {
        return $"ammo.{kind.ToString().ToLowerInvariant()}";
    }

    public static WeaponKind? LegacyKindForWeapon(string id)
    {
        return Weapons
            .Where(pair => pair.Value.Id.Equals(id, StringComparison.Ordinal))
            .Select(pair => (WeaponKind?)pair.Key)
            .FirstOrDefault();
    }

    public static AmmoKind? LegacyKindForAmmo(string id)
    {
        return Ammo
            .Where(pair => pair.Value.Id.Equals(id, StringComparison.Ordinal))
            .Select(pair => (AmmoKind?)pair.Key)
            .FirstOrDefault();
    }

    private static IReadOnlyDictionary<string, WeaponDefinition> DiscoverWeapons()
    {
        return DiscoverWeaponsFrom(typeof(WeaponDesign).Assembly);
    }

    public static IReadOnlyDictionary<string, WeaponDefinition> DiscoverWeaponsFrom(params Assembly[] assemblies)
    {
        var ammoDefinitions = DiscoverAmmoFrom(assemblies);
        var definitions = DiscoverDesigns<WeaponDesign>(assemblies)
            .OrderBy(design => design.Id, StringComparer.Ordinal)
            .Select(design => design.ToDefinition())
            .ToDictionary(definition => definition.Id, StringComparer.Ordinal);

        foreach (var weapon in definitions.Values)
        {
            if (!ammoDefinitions.ContainsKey(weapon.AmmoId)
                && !AmmoDefinitions.ContainsKey(weapon.AmmoId))
            {
                throw new InvalidOperationException($"{weapon.Id} references missing ammo {weapon.AmmoId}.");
            }
        }

        return definitions;
    }

    private static IReadOnlyDictionary<string, AmmoDefinition> DiscoverAmmo()
    {
        return DiscoverAmmoFrom(typeof(AmmoDesign).Assembly);
    }

    public static IReadOnlyDictionary<string, AmmoDefinition> DiscoverAmmoFrom(params Assembly[] assemblies)
    {
        return DiscoverDesigns<AmmoDesign>(assemblies)
            .OrderBy(design => design.Id, StringComparer.Ordinal)
            .Select(design => design.ToDefinition())
            .ToDictionary(definition => definition.Id, StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<WeaponKind, WeaponDefinition> BuildLegacyWeapons()
    {
        return WeaponDefinitions
            .Values
            .Where(definition => definition.LegacyKind is not null)
            .ToDictionary(definition => definition.LegacyKind!.Value);
    }

    private static IReadOnlyDictionary<AmmoKind, AmmoDefinition> BuildLegacyAmmo()
    {
        return AmmoDefinitions
            .Values
            .Where(definition => definition.LegacyKind is not null)
            .ToDictionary(definition => definition.LegacyKind!.Value);
    }

    private static IEnumerable<TDesign> DiscoverDesigns<TDesign>(IReadOnlyList<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(TDesign).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
            .Select(type => (TDesign)Activator.CreateInstance(type)!));
    }
}
